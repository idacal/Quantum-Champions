using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Collections;

public class ObjectiveManager : MonoBehaviourPunCallbacks
{
    [Header("Special Objectives")]
    [SerializeField] private GameObject[] majorObjectives;
    [SerializeField] private float majorObjectiveRespawnTime = 300f; // 5 minutos
    
    [Header("Announcement")]
    [SerializeField] private float objectiveWarningTime = 30f; // Anuncio 30 segundos antes
    
    // Estado de objetivos
    private Dictionary<GameObject, ObjectiveState> objectiveStates = new Dictionary<GameObject, ObjectiveState>();
    
    // Seguimiento de estructuras
    private Dictionary<StructureType, int> teamOneStructures = new Dictionary<StructureType, int>();
    private Dictionary<StructureType, int> teamTwoStructures = new Dictionary<StructureType, int>();
    
    public void Initialize()
    {
        // Inicializar estado de objetivos especiales
        if (majorObjectives != null)
        {
            foreach (GameObject objective in majorObjectives)
            {
                if (objective != null)
                {
                    ObjectiveState state = new ObjectiveState
                    {
                        isActive = true,
                        nextSpawnTime = 0f,
                        controllingTeam = -1  // Neutral
                    };
                    
                    objectiveStates[objective] = state;
                    
                    // Inicializar el objetivo
                    MajorObjective objectiveComponent = objective.GetComponent<MajorObjective>();
                    if (objectiveComponent != null)
                    {
                        objectiveComponent.Initialize(this);
                    }
                }
            }
        }
        
        // Inicializar contadores de estructuras
        teamOneStructures[StructureType.Tower] = 0;
        teamOneStructures[StructureType.Inhibitor] = 0;
        teamOneStructures[StructureType.Base] = 0;
        
        teamTwoStructures[StructureType.Tower] = 0;
        teamTwoStructures[StructureType.Inhibitor] = 0;
        teamTwoStructures[StructureType.Base] = 0;
        
        // Iniciar comprobación periódica de objetivos
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(CheckObjectivesRoutine());
        }
    }
    
    private IEnumerator CheckObjectivesRoutine()
    {
        while (true)
        {
            // Comprobar objetivos inactivos para posible respawn
            foreach (var pair in objectiveStates)
            {
                GameObject objective = pair.Key;
                ObjectiveState state = pair.Value;
                
                if (!state.isActive && Time.time >= state.nextSpawnTime)
                {
                    RespawnObjective(objective);
                }
                else if (!state.isActive && !state.warningGiven && 
                         Time.time >= state.nextSpawnTime - objectiveWarningTime)
                {
                    // Dar aviso previo
                    AnnounceObjectiveSpawningSoon(objective);
                    state.warningGiven = true;
                    objectiveStates[objective] = state;
                }
            }
            
            yield return new WaitForSeconds(1f); // Comprobar cada segundo
        }
    }
    
    private void RespawnObjective(GameObject objective)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (objectiveStates.TryGetValue(objective, out ObjectiveState state))
        {
            state.isActive = true;
            state.controllingTeam = -1;  // Vuelve a ser neutral
            state.warningGiven = false;
            objectiveStates[objective] = state;
            
            // Activar el objetivo
            photonView.RPC("SyncObjectiveSpawn", RpcTarget.All, objective.GetComponent<PhotonView>().ViewID);
            
            // Anunciar respawn
            string objectiveName = objective.name;
            NotificationSystem.Instance.ShowObjectiveNotification($"¡{objectiveName} ha reaparecido!");
        }
    }
    
    private void AnnounceObjectiveSpawningSoon(GameObject objective)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Notificar a todos los clientes
        string objectiveName = objective.name;
        photonView.RPC("SyncObjectiveWarning", RpcTarget.All, objectiveName);
    }
    
    [PunRPC]
    private void SyncObjectiveWarning(string objectiveName)
    {
        NotificationSystem.Instance.ShowObjectiveNotification($"{objectiveName} aparecerá pronto!");
    }
    
    [PunRPC]
    private void SyncObjectiveSpawn(int objectiveViewID)
    {
        PhotonView objectiveView = PhotonView.Find(objectiveViewID);
        if (objectiveView != null)
        {
            GameObject objective = objectiveView.gameObject;
            
            // Activar el objetivo visualmente
            objective.SetActive(true);
            
            // Reiniciar componente de objetivo
            MajorObjective objectiveComponent = objective.GetComponent<MajorObjective>();
            if (objectiveComponent != null)
            {
                objectiveComponent.Reset();
            }
        }
    }
    
    // Notificación cuando un objetivo es capturado/derrotado
    public void OnObjectiveTaken(GameObject objective, int teamId)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (objectiveStates.TryGetValue(objective, out ObjectiveState state))
        {
            state.isActive = false;
            state.controllingTeam = teamId;
            state.nextSpawnTime = Time.time + majorObjectiveRespawnTime;
            state.warningGiven = false;
            objectiveStates[objective] = state;
            
            // Aplicar efectos/recompensas por capturar el objetivo
            ApplyObjectiveRewards(objective, teamId);
            
            // Notificar a todos los clientes
            string objectiveName = objective.name;
            string teamName = teamId == 0 ? "Azul" : "Rojo";
            photonView.RPC("SyncObjectiveTaken", RpcTarget.All, objectiveName, teamName);
        }
    }
    
    [PunRPC]
    private void SyncObjectiveTaken(string objectiveName, string teamName)
    {
        NotificationSystem.Instance.ShowObjectiveNotification($"¡{teamName} ha capturado {objectiveName}!");
    }
    
    // Aplicar recompensas por capturar un objetivo
    private void ApplyObjectiveRewards(GameObject objective, int teamId)
    {
        // Implementar recompensas específicas según el objetivo
        // Por ejemplo, buffs para todo el equipo, oro, etc.
        
        MajorObjective objectiveComponent = objective.GetComponent<MajorObjective>();
        if (objectiveComponent != null)
        {
            objectiveComponent.ApplyRewards(teamId);
        }
    }
    
    // Notificación cuando se destruye una estructura
    public void OnStructureDestroyed(GameObject structure, int teamId, StructureType type, LaneController lane)
    {
        // Actualizar contadores
        Dictionary<StructureType, int> structureCount = teamId == 0 ? teamOneStructures : teamTwoStructures;
        
        if (structureCount.ContainsKey(type))
        {
            structureCount[type]++;
        }
        
        // Comprobar condiciones de victoria
        if (type == StructureType.Base)
        {
            // Victoria para el equipo opuesto
            int winningTeam = teamId == 0 ? 1 : 0;
            
            // Aquí se podría notificar al GameManager para finalizar la partida
        }
    }
    
    // Obtener estado de un objetivo
    public ObjectiveState GetObjectiveState(GameObject objective)
    {
        if (objectiveStates.TryGetValue(objective, out ObjectiveState state))
        {
            return state;
        }
        
        return new ObjectiveState { isActive = false, nextSpawnTime = 0f, controllingTeam = -1 };
    }
}

// Clase para almacenar estado de objetivos
public class ObjectiveState
{
    public bool isActive;        // Si el objetivo está activo actualmente
    public float nextSpawnTime;  // Cuándo volverá a aparecer si no está activo
    public int controllingTeam;  // Qué equipo lo controla (-1 = neutral)
    public bool warningGiven;    // Si ya se ha dado el aviso de respawn
}

// Componente para objetivos especiales
public class MajorObjective : MonoBehaviourPun, IDamageable
{
    [Header("Objective Properties")]
    [SerializeField] private string objectiveName = "Dragon";
    [SerializeField] private float maxHealth = 3000f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float attackDamage = 100f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackRate = 2f;
    
    [Header("Rewards")]
    [SerializeField] private float goldReward = 300f;
    [SerializeField] private float expReward = 400f;
    [SerializeField] private bool giveTeamBuff = true;
    [SerializeField] private float buffDuration = 180f; // 3 minutos
    
    // Referencias
    private ObjectiveManager objectiveManager;
    private Animator animator;
    
    // Estado
    private bool isActive = true;
    private Transform currentTarget;
    private float nextAttackTime;
    
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    public void Initialize(ObjectiveManager manager)
    {
        objectiveManager = manager;
        currentHealth = maxHealth;
        isActive = true;
    }
    
    public void Reset()
    {
        currentHealth = maxHealth;
        isActive = true;
        
        // Resetear estado visual
        if (animator != null)
        {
            animator.Rebind();
        }
    }
    
    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient || !isActive) return;
        
        // Comportamiento básico de IA para el objetivo
        // Este es un ejemplo simplificado
        
        if (currentTarget != null)
        {
            // Comprobar si el objetivo está en rango
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            
            if (distanceToTarget <= attackRange)
            {
                // Atacar si es el momento
                if (Time.time >= nextAttackTime)
                {
                    Attack(currentTarget);
                    nextAttackTime = Time.time + attackRate;
                }
            }
        }
    }
    
    private void Attack(Transform target)
    {
        // Aplicar daño
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(attackDamage, null); // null porque no es un héroe
        }
        
        // Reproducir animación de ataque
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }
    
    // Método para aplicar recompensas cuando el objetivo es derrotado
    public void ApplyRewards(int teamId)
    {
        // Dar oro a todos los jugadores del equipo
        foreach (PlayerNetwork player in TeamManager.Instance.GetTeamPlayers(teamId))
        {
            // Si existiera un sistema de economía:
            // player.GetComponent<GoldManager>().AddGold(goldReward);
            
            // Dar experiencia
            Hero hero = player.GetComponent<Hero>();
            if (hero != null)
            {
                hero.GainExperience(expReward);
            }
        }
        
        // Aplicar buff de equipo si corresponde
        if (giveTeamBuff)
        {
            ApplyTeamBuff(teamId);
        }
    }
    
    private void ApplyTeamBuff(int teamId)
    {
        // Crear un buff y aplicarlo a todo el equipo
        // Ejemplo simplificado:
        foreach (PlayerNetwork player in TeamManager.Instance.GetTeamPlayers(teamId))
        {
            Hero hero = player.GetComponent<Hero>();
            if (hero != null)
            {
                // Si tuviéramos un sistema de buffs completo:
                // StatusEffectManager statusManager = hero.GetComponent<StatusEffectManager>();
                // statusManager.ApplyBuff("ObjectiveBuff", buffDuration);
                
                // Por ahora, simplemente notificar
                if (player.photonView.IsMine)
                {
                    NotificationSystem.Instance.ShowNotification(
                        $"¡Has recibido el buff de {objectiveName}!",
                        NotificationType.Objective
                    );
                }
            }
        }
    }
    
    // Implementación de IDamageable
    public void TakeDamage(float damage, Hero attacker)
    {
        if (!isActive) return;
        
        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.IsConnected)
        {
            // Si no somos host, enviar RPC
            photonView.RPC("RPCTakeDamage", RpcTarget.MasterClient, damage, attacker != null ? attacker.photonView.ViewID : -1);
            return;
        }
        
        // Aplicar daño
        currentHealth -= damage;
        
        // Sincronizar con otros clientes
        photonView.RPC("SyncHealth", RpcTarget.Others, currentHealth);
        
        // Si tenemos atacante, establecer como objetivo
        if (attacker != null && currentTarget == null)
        {
            currentTarget = attacker.transform;
        }
        
        // Comprobar si ha sido derrotado
        if (currentHealth <= 0 && isActive)
        {
            Defeat(attacker);
        }
    }
    
    [PunRPC]
    private void RPCTakeDamage(float damage, int attackerViewID)
    {
        // Buscar atacante si se proporciona ID
        Hero attacker = null;
        if (attackerViewID >= 0)
        {
            PhotonView attackerView = PhotonView.Find(attackerViewID);
            if (attackerView != null)
            {
                attacker = attackerView.GetComponent<Hero>();
            }
        }
        
        // Aplicar daño
        TakeDamage(damage, attacker);
    }
    
    [PunRPC]
    private void SyncHealth(float health)
    {
        currentHealth = health;
    }
    
    private void Defeat(Hero killer)
    {
        if (!isActive) return;
        
        isActive = false;
        
        // Determinar equipo vencedor
        int winningTeam = -1;
        if (killer != null)
        {
            TeamAssignment teamAssignment = killer.GetComponent<TeamAssignment>();
            if (teamAssignment != null)
            {
                winningTeam = teamAssignment.TeamId;
            }
        }
        
        // Notificar al ObjectiveManager
        if (objectiveManager != null)
        {
            objectiveManager.OnObjectiveTaken(gameObject, winningTeam);
        }
        
        // Mostrar efecto de derrota
        photonView.RPC("ShowDefeatEffect", RpcTarget.All);
    }
    
    [PunRPC]
    private void ShowDefeatEffect()
    {
        // Reproducir animación de muerte
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        // Desactivar componentes como colliders
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // Desactivar comportamiento de IA
        enabled = false;
        
        // Opcionalmente, desactivar el objeto después de la animación
        StartCoroutine(DisableAfterAnimation());
    }
    
    private IEnumerator DisableAfterAnimation()
    {
        // Esperar tiempo para que se reproduzca la animación
        yield return new WaitForSeconds(3f);
        
        // Desactivar objeto
        gameObject.SetActive(false);
    }
}