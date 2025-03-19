using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class LaneController : MonoBehaviourPunCallbacks
{
    [Header("Lane Info")]
    [SerializeField] private string laneName = "Mid";
    [SerializeField] private Transform[] teamOnePath;
    [SerializeField] private Transform[] teamTwoPath;
    
    [Header("Creep Spawning")]
    [SerializeField] private GameObject creepPrefab;
    [SerializeField] private float spawnInterval = 30f;
    [SerializeField] private int creepsPerWave = 6;
    [SerializeField] private Transform teamOneSpawnPoint;
    [SerializeField] private Transform teamTwoSpawnPoint;
    
    [Header("Lane Structures")]
    [SerializeField] private GameObject[] teamOneTowers;
    [SerializeField] private GameObject[] teamTwoTowers;
    [SerializeField] private GameObject teamOneInhibitor;
    [SerializeField] private GameObject teamTwoInhibitor;
    
    private float nextSpawnTime = 0f;
    private bool isLaneInitialized = false;
    
    public Transform[] TeamOnePath => teamOnePath;
    public Transform[] TeamTwoPath => teamTwoPath;
    
    // Información de estado de la línea
    private bool teamOneInhibitorDestroyed = false;
    private bool teamTwoInhibitorDestroyed = false;
    private int teamOneTowersDestroyed = 0;
    private int teamTwoTowersDestroyed = 0;
    
    public void Initialize()
    {
        if (isLaneInitialized) return;
        
        // Sólo el master client maneja la lógica de spawn
        if (PhotonNetwork.IsMasterClient)
        {
            nextSpawnTime = Time.time + 5f; // Primer spawn después de 5 segundos
        }
        
        // Registrar en estructuras
        RegisterTowersWithStructureManager();
        
        isLaneInitialized = true;
    }
    
    private void Update()
    {
        // Sólo el master client maneja la lógica de spawn
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Comprobar si es hora de spawnar creeps
        if (Time.time >= nextSpawnTime)
        {
            SpawnCreepWave();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    private void SpawnCreepWave()
    {
        // Spawn para equipo 1
        for (int i = 0; i < creepsPerWave; i++)
        {
            Vector3 spawnPos = teamOneSpawnPoint.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            SpawnCreep(spawnPos, 0);
        }
        
        // Spawn para equipo 2
        for (int i = 0; i < creepsPerWave; i++)
        {
            Vector3 spawnPos = teamTwoSpawnPoint.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            SpawnCreep(spawnPos, 1);
        }
        
        // Si un inhibidor está destruido, spawnar creeps adicionales (super creeps)
        if (teamOneInhibitorDestroyed)
        {
            for (int i = 0; i < creepsPerWave / 2; i++)
            {
                Vector3 spawnPos = teamTwoSpawnPoint.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                SpawnSuperCreep(spawnPos, 1);
            }
        }
        
        if (teamTwoInhibitorDestroyed)
        {
            for (int i = 0; i < creepsPerWave / 2; i++)
            {
                Vector3 spawnPos = teamOneSpawnPoint.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                SpawnSuperCreep(spawnPos, 0);
            }
        }
    }
    
    private void SpawnCreep(Vector3 position, int teamId)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Instanciar creep a través de Photon
        GameObject creepObject = PhotonNetwork.Instantiate(creepPrefab.name, position, Quaternion.identity);
        
        // Configurar equipo y comportamiento
        CreepAI creepAI = creepObject.GetComponent<CreepAI>();
        if (creepAI != null)
        {
            creepAI.Initialize(teamId, this);
        }
        
        // Configurar asignación de equipo
        TeamAssignment teamAssignment = creepObject.GetComponent<TeamAssignment>();
        if (teamAssignment != null)
        {
            teamAssignment.SetTeam(teamId);
        }
    }
    
    private void SpawnSuperCreep(Vector3 position, int teamId)
    {
        // Similar a SpawnCreep pero con creeps más fuertes
        // En una implementación real, usaríamos un prefab diferente o configuración
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Instanciar super creep (por ahora usamos el mismo prefab)
        GameObject creepObject = PhotonNetwork.Instantiate(creepPrefab.name, position, Quaternion.identity);
        
        // Configurar equipo y comportamiento
        CreepAI creepAI = creepObject.GetComponent<CreepAI>();
        if (creepAI != null)
        {
            creepAI.Initialize(teamId, this, true); // true = super creep
        }
        
        // Configurar asignación de equipo
        TeamAssignment teamAssignment = creepObject.GetComponent<TeamAssignment>();
        if (teamAssignment != null)
        {
            teamAssignment.SetTeam(teamId);
        }
    }
    
    private void RegisterTowersWithStructureManager()
    {
        StructureManager structureManager = FindObjectOfType<StructureManager>();
        if (structureManager == null) return;
        
        // Registrar torres del equipo 1
        foreach (GameObject tower in teamOneTowers)
        {
            if (tower != null)
            {
                structureManager.RegisterStructure(tower, 0, StructureType.Tower, this);
            }
        }
        
        // Registrar torres del equipo 2
        foreach (GameObject tower in teamTwoTowers)
        {
            if (tower != null)
            {
                structureManager.RegisterStructure(tower, 1, StructureType.Tower, this);
            }
        }
        
        // Registrar inhibidores
        if (teamOneInhibitor != null)
        {
            structureManager.RegisterStructure(teamOneInhibitor, 0, StructureType.Inhibitor, this);
        }
        
        if (teamTwoInhibitor != null)
        {
            structureManager.RegisterStructure(teamTwoInhibitor, 1, StructureType.Inhibitor, this);
        }
    }
    
    // Notificación cuando se destruye una estructura en esta línea
    public void OnStructureDestroyed(GameObject structure, int teamId, StructureType type)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (type == StructureType.Tower)
        {
            if (teamId == 0)
            {
                teamOneTowersDestroyed++;
                photonView.RPC("SyncTowerDestroyed", RpcTarget.All, 0);
            }
            else
            {
                teamTwoTowersDestroyed++;
                photonView.RPC("SyncTowerDestroyed", RpcTarget.All, 1);
            }
        }
        else if (type == StructureType.Inhibitor)
        {
            if (teamId == 0)
            {
                teamOneInhibitorDestroyed = true;
                photonView.RPC("SyncInhibitorDestroyed", RpcTarget.All, 0);
            }
            else
            {
                teamTwoInhibitorDestroyed = true;
                photonView.RPC("SyncInhibitorDestroyed", RpcTarget.All, 1);
            }
        }
        
        // Notificar al sistema de objetivos
        ObjectiveManager objectiveManager = FindObjectOfType<ObjectiveManager>();
        if (objectiveManager != null)
        {
            objectiveManager.OnStructureDestroyed(structure, teamId, type, this);
        }
    }
    
    [PunRPC]
    private void SyncTowerDestroyed(int teamId)
    {
        if (teamId == 0)
        {
            teamOneTowersDestroyed++;
        }
        else
        {
            teamTwoTowersDestroyed++;
        }
        
        // Notificar al jugador
        NotificationSystem.Instance.ShowNotification(
            $"¡Torre destruida en la línea {laneName}!", 
            NotificationType.Objective
        );
    }
    
    [PunRPC]
    private void SyncInhibitorDestroyed(int teamId)
    {
        if (teamId == 0)
        {
            teamOneInhibitorDestroyed = true;
        }
        else
        {
            teamTwoInhibitorDestroyed = true;
        }
        
        // Notificar al jugador
        NotificationSystem.Instance.ShowNotification(
            $"¡Inhibidor destruido en la línea {laneName}!", 
            NotificationType.Objective
        );
    }
    
    // Calcular la distancia desde un punto a la línea
    public float GetDistanceToLane(Vector3 position)
    {
        float minDistance = float.MaxValue;
        
        // Comprobar con los waypoints de ambos caminos
        Transform[] allWaypoints = new Transform[teamOnePath.Length + teamTwoPath.Length];
        teamOnePath.CopyTo(allWaypoints, 0);
        teamTwoPath.CopyTo(allWaypoints, teamOnePath.Length);
        
        for (int i = 0; i < allWaypoints.Length; i++)
        {
            if (allWaypoints[i] != null)
            {
                float distance = Vector3.Distance(position, allWaypoints[i].position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }
        
        return minDistance;
    }
}

// Enumeración para los tipos de estructuras
public enum StructureType
{
    Tower,
    Inhibitor,
    Base
}