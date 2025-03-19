using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class StructureManager : MonoBehaviourPunCallbacks
{
    [Header("Win Conditions")]
    [SerializeField] private GameObject teamOneMainStructure;
    [SerializeField] private GameObject teamTwoMainStructure;
    
    // Lista de estructuras registradas
    private Dictionary<GameObject, StructureInfo> structures = new Dictionary<GameObject, StructureInfo>();
    
    public void Initialize()
    {
        // Registrar estructuras principales
        if (teamOneMainStructure != null)
        {
            RegisterStructure(teamOneMainStructure, 0, StructureType.Base, null);
        }
        
        if (teamTwoMainStructure != null)
        {
            RegisterStructure(teamTwoMainStructure, 1, StructureType.Base, null);
        }
    }
    
    // Registrar una estructura
    public void RegisterStructure(GameObject structure, int teamId, StructureType type, LaneController lane)
    {
        if (structure == null) return;
        
        StructureInfo info = new StructureInfo
        {
            gameObject = structure,
            teamId = teamId,
            type = type,
            lane = lane,
            isDestroyed = false
        };
        
        structures[structure] = info;
        
        // Añadir componente de estructura si no existe
        Structure structureComponent = structure.GetComponent<Structure>();
        if (structureComponent == null)
        {
            structureComponent = structure.AddComponent<Structure>();
        }
        
        // Configurar estructura
        structureComponent.Initialize(teamId, type, lane);
    }
    
    // Notificar destrucción de estructura
    public void OnStructureDestroyed(GameObject structure)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (structures.TryGetValue(structure, out StructureInfo info))
        {
            if (info.isDestroyed) return;  // Ya estaba destruida
            
            info.isDestroyed = true;
            structures[structure] = info;
            
            // Notificar a la línea correspondiente
            if (info.lane != null)
            {
                info.lane.OnStructureDestroyed(structure, info.teamId, info.type);
            }
            
            // Comprobar condiciones de victoria
            if (info.type == StructureType.Base)
            {
                // Victoria para el equipo opuesto
                int winningTeam = info.teamId == 0 ? 1 : 0;
                
                // Notificar al GameManager
                GameManager gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null)
                {
                    // gameManager.EndGame(winningTeam);
                    // No hay método EndGame, pero aquí se llamaría
                }
                
                // Notificar a todos los clientes
                photonView.RPC("AnnounceGameEnd", RpcTarget.All, winningTeam);
            }
        }
    }
    
    [PunRPC]
    private void AnnounceGameEnd(int winningTeam)
    {
        // Mostrar mensaje de fin de juego
        string teamName = winningTeam == 0 ? "Azul" : "Rojo";
        NotificationSystem.Instance.ShowNotification(
            $"¡Fin del juego! El equipo {teamName} ha ganado!", 
            NotificationType.Success
        );
        
        // Aquí también podríamos mostrar una pantalla de resultados
    }
    
    // Obtener todas las estructuras de un equipo
    public List<GameObject> GetTeamStructures(int teamId)
    {
        List<GameObject> result = new List<GameObject>();
        
        foreach (var pair in structures)
        {
            if (pair.Value.teamId == teamId && !pair.Value.isDestroyed)
            {
                result.Add(pair.Key);
            }
        }
        
        return result;
    }
    
    // Obtener todas las estructuras de un tipo específico
    public List<GameObject> GetStructuresByType(StructureType type)
    {
        List<GameObject> result = new List<GameObject>();
        
        foreach (var pair in structures)
        {
            if (pair.Value.type == type && !pair.Value.isDestroyed)
            {
                result.Add(pair.Key);
            }
        }
        
        return result;
    }
    
    // Verificar si todas las estructuras de un tipo/equipo están destruidas
    public bool AreAllStructuresDestroyed(int teamId, StructureType type)
    {
        foreach (var pair in structures)
        {
            if (pair.Value.teamId == teamId && pair.Value.type == type && !pair.Value.isDestroyed)
            {
                return false;  // Encontramos al menos una no destruida
            }
        }
        
        return true;  // Todas destruidas
    }
}

// Clase para almacenar información de estructura
public class StructureInfo
{
    public GameObject gameObject;
    public int teamId;
    public StructureType type;
    public LaneController lane;
    public bool isDestroyed;
}

// Componente para una estructura individual
public class Structure : MonoBehaviourPun, IDamageable
{
    [SerializeField] private float maxHealth = 2000f;
    [SerializeField] private float currentHealth;
    
    private int teamId;
    private StructureType type;
    private LaneController lane;
    private bool isDestroyed = false;
    
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    
    public void Initialize(int teamId, StructureType type, LaneController lane)
    {
        this.teamId = teamId;
        this.type = type;
        this.lane = lane;
        
        // Configurar salud según tipo
        switch (type)
        {
            case StructureType.Tower:
                maxHealth = 1500f;
                break;
            case StructureType.Inhibitor:
                maxHealth = 2500f;
                break;
            case StructureType.Base:
                maxHealth = 4000f;
                break;
        }
        
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(float damage, Hero attacker)
    {
        if (isDestroyed) return;
        
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
        {
            // Si no somos el dueño y estamos en red, enviar RPC
            photonView.RPC("RPCTakeDamage", RpcTarget.MasterClient, damage, attacker != null ? attacker.photonView.ViewID : -1);
            return;
        }
        
        // Aplicar daño
        currentHealth -= damage;
        
        // Sincronizar con todos los clientes
        photonView.RPC("SyncHealth", RpcTarget.Others, currentHealth);
        
        // Comprobar si se ha destruido
        if (currentHealth <= 0 && !isDestroyed)
        {
            DestroyStructure();
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
    
    private void DestroyStructure()
    {
        if (isDestroyed) return;
        
        isDestroyed = true;
        
        // Notificar al Structure Manager
        StructureManager structureManager = FindObjectOfType<StructureManager>();
        if (structureManager != null)
        {
            structureManager.OnStructureDestroyed(gameObject);
        }
        
        // Mostrar efectos de destrucción
        photonView.RPC("ShowDestructionEffects", RpcTarget.All);
    }
    
    [PunRPC]
    private void ShowDestructionEffects()
    {
        // Mostrar efectos visuales de destrucción
        // Por ejemplo, activar un sistema de partículas, cambiar modelo, etc.
        
        // Como ejemplo, cambiar el color a gris
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.gray;
        }
    }
}