using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

[RequireComponent(typeof(NavMeshAgent))]
public class CreepAI : MonoBehaviourPun, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRate = 1.5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float aggroRange = 10f;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    
    // Referencias componentes
    private NavMeshAgent agent;
    private TeamAssignment teamAssignment;
    private Animator animator;
    
    // Estado
    private int teamId = -1;
    private bool isSuperCreep = false;
    private LaneController lane;
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private Transform currentTarget;
    private float nextAttackTime;
    
    // Estadísticas
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    
    // Estados AI
    private enum AIState
    {
        FollowingLane,
        AttackingEnemy,
        AttackingStructure
    }
    
    private AIState currentState = AIState.FollowingLane;
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        teamAssignment = GetComponent<TeamAssignment>();
        animator = GetComponent<Animator>();
    }
    
    public void Initialize(int teamId, LaneController lane, bool isSuperCreep = false)
    {
        this.teamId = teamId;
        this.lane = lane;
        this.isSuperCreep = isSuperCreep;
        
        // Asignar equipo
        if (teamAssignment != null)
        {
            teamAssignment.SetTeam(teamId);
        }
        
        // Configurar propiedades según tipo
        if (isSuperCreep)
        {
            maxHealth *= 1.5f;
            attackDamage *= 1.5f;
            transform.localScale *= 1.2f; // Hacerlo más grande visualmente
        }
        
        currentHealth = maxHealth;
        
        // Configurar movimiento
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange * 0.8f;
        }
        
        // Obtener waypoints según equipo
        if (lane != null)
        {
            waypoints = teamId == 0 ? lane.TeamOnePath : lane.TeamTwoPath;
            
            if (waypoints.Length > 0)
            {
                // Configurar primer destino
                agent.SetDestination(waypoints[0].position);
            }
        }
    }
    
    private void Update()
    {
        // Solo el host actualiza la lógica de IA
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Si no hemos sido inicializados correctamente
        if (teamId < 0 || waypoints == null || waypoints.Length == 0) return;
        
        switch (currentState)
        {
            case AIState.FollowingLane:
                FollowLane();
                LookForEnemies();
                break;
                
            case AIState.AttackingEnemy:
                AttackEnemy();
                break;
                
            case AIState.AttackingStructure:
                AttackStructure();
                break;
        }
    }
    
    private void FollowLane()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        
        // Comprobar si hemos llegado al waypoint actual
        float distanceToWaypoint = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
        
        if (distanceToWaypoint < 1.5f)
        {
            // Avanzar al siguiente waypoint
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }
    
    private void LookForEnemies()
    {
        // Buscar enemigos cercanos
        Collider[] colliders = Physics.OverlapSphere(transform.position, aggroRange);
        
        float closestDistance = float.MaxValue;
        Transform closestEnemy = null;
        
        foreach (var collider in colliders)
        {
            // Comprobar si es un enemigo (héroe o creep)
            TeamAssignment targetTeam = collider.GetComponent<TeamAssignment>();
            
            if (targetTeam != null && targetTeam.TeamId != teamId)
            {
                // Verificar si es un objetivo válido
                if (collider.GetComponent<IDamageable>() != null)
                {
                    float distance = Vector3.Distance(transform.position, collider.transform.position);
                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = collider.transform;
                    }
                }
            }
        }
        
        // Si encontramos un enemigo, cambiar a estado de ataque
        if (closestEnemy != null)
        {
            currentTarget = closestEnemy;
            currentState = AIState.AttackingEnemy;
            agent.SetDestination(currentTarget.position);
        }
    }
    
    private void AttackEnemy()
    {
        if (currentTarget == null)
        {
            // El objetivo ya no existe, volver a seguir el carril
            currentState = AIState.FollowingLane;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
            return;
        }
        
        // Comprobar si el objetivo está vivo
        IDamageable targetDamageable = currentTarget.GetComponent<IDamageable>();
        if (targetDamageable != null && targetDamageable.CurrentHealth <= 0)
        {
            currentTarget = null;
            currentState = AIState.FollowingLane;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
            return;
        }
        
        // Comprobar distancia al objetivo
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distanceToTarget > aggroRange * 1.5f)
        {
            // El objetivo está demasiado lejos, volver a seguir el carril
            currentTarget = null;
            currentState = AIState.FollowingLane;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
        else if (distanceToTarget <= attackRange)
        {
            // Detener movimiento y atacar
            agent.isStopped = true;
            
            // Rotar hacia el objetivo
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            
            // Atacar si es el momento
            if (Time.time >= nextAttackTime)
            {
                Attack(currentTarget);
                nextAttackTime = Time.time + attackRate;
            }
        }
        else
        {
            // Acercarse al objetivo
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
        }
    }
    
    private void AttackStructure()
    {
        // Similar a AttackEnemy pero para estructuras
        if (currentTarget == null)
        {
            // El objetivo ya no existe, volver a seguir el carril
            currentState = AIState.FollowingLane;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
            return;
        }
        
        // Comprobar distancia al objetivo
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        
        if (distanceToTarget <= attackRange)
        {
            // Detener movimiento y atacar
            agent.isStopped = true;
            
            // Rotar hacia el objetivo
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            
            // Atacar si es el momento
            if (Time.time >= nextAttackTime)
            {
                Attack(currentTarget);
                nextAttackTime = Time.time + attackRate;
            }
        }
        else
        {
            // Acercarse al objetivo
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
        }
    }
    
    private void Attack(Transform target)
    {
        // Aplicar daño
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            Hero heroAttacker = null; // Los creeps no tienen hero como atacante
            damageable.TakeDamage(attackDamage, heroAttacker);
        }
        
        // Reproducir animación de ataque
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Sincronizar ataque para efectos visuales
        photonView.RPC("SyncAttackAnimation", RpcTarget.Others);
    }
    
    [PunRPC]
    private void SyncAttackAnimation()
    {
        // Reproducir animación de ataque en clientes remotos
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }
    
    // Implementación de IDamageable
    public void TakeDamage(float damage, Hero attacker)
    {
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
        
        // Si tenemos atacante y no estábamos ya en combate, cambiar objetivo
        if (attacker != null && currentState == AIState.FollowingLane)
        {
            currentTarget = attacker.transform;
            currentState = AIState.AttackingEnemy;
            agent.SetDestination(currentTarget.position);
        }
        
        // Comprobar si ha muerto
        if (currentHealth <= 0)
        {
            Die(attacker);
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
    
    private void Die(Hero killer)
    {
        // Otorgar experiencia y oro al asesino si es un héroe
        if (killer != null)
        {
            // 25 exp base, 15 más si es super creep
            float expReward = 25f + (isSuperCreep ? 15f : 0f);
            killer.GainExperience(expReward);
            
            // Aquí también se podría dar oro
            // killer.GainGold(12f + (isSuperCreep ? 8f : 0f));
        }
        
        // Mostrar efecto de muerte
        photonView.RPC("ShowDeathEffect", RpcTarget.All);
        
        // Destruir objeto después de un breve retraso
        PhotonNetwork.Destroy(gameObject);
    }
    
    [PunRPC]
    private void ShowDeathEffect()
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
    }
}