using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MapManager : MonoBehaviourPunCallbacks
{
    public static MapManager Instance;
    
    [Header("Map Components")]
    [SerializeField] private LaneController[] lanes;
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private StructureManager structureManager;
    [SerializeField] private FogOfWarSystem fogOfWarSystem;
    
    [Header("Terrain")]
    [SerializeField] private GameObject mapTerrain;
    [SerializeField] private GameObject[] mapDecorations;
    
    [Header("Vision")]
    [SerializeField] private float defaultVisionRange = 10f;
    
    // Diccionario de áreas especiales del mapa
    private Dictionary<string, MapArea> mapAreas = new Dictionary<string, MapArea>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Inicializar sistemas del mapa
        InitializeMapSystems();
        
        // Registrar áreas especiales
        RegisterMapAreas();
    }
    
    private void InitializeMapSystems()
    {
        // Inicializar sistemas solo en el master client
        if (PhotonNetwork.IsMasterClient)
        {
            if (objectiveManager != null)
            {
                objectiveManager.Initialize();
            }
            
            if (structureManager != null)
            {
                structureManager.Initialize();
            }
            
            // Inicializar lanes
            if (lanes != null)
            {
                foreach (LaneController lane in lanes)
                {
                    if (lane != null)
                    {
                        lane.Initialize();
                    }
                }
            }
        }
        
        // Inicializar sistema de fog of war (en todos los clientes)
        if (fogOfWarSystem != null)
        {
            fogOfWarSystem.Initialize(defaultVisionRange);
        }
    }
    
    private void RegisterMapAreas()
    {
        // Buscar todas las áreas especiales del mapa
        MapArea[] areas = FindObjectsOfType<MapArea>();
        
        foreach (MapArea area in areas)
        {
            if (!string.IsNullOrEmpty(area.AreaId) && !mapAreas.ContainsKey(area.AreaId))
            {
                mapAreas.Add(area.AreaId, area);
            }
        }
    }
    
    // Obtener un área específica del mapa
    public MapArea GetMapArea(string areaId)
    {
        if (mapAreas.TryGetValue(areaId, out MapArea area))
        {
            return area;
        }
        return null;
    }
    
    // Obtener una posición aleatoria dentro de un área específica
    public Vector3 GetRandomPositionInArea(string areaId)
    {
        MapArea area = GetMapArea(areaId);
        
        if (area != null)
        {
            return area.GetRandomPosition();
        }
        
        // Posición por defecto en el centro del mapa
        return Vector3.zero;
    }
    
    // Comprobar si una posición está en un área específica
    public bool IsPositionInArea(Vector3 position, string areaId)
    {
        MapArea area = GetMapArea(areaId);
        
        if (area != null)
        {
            return area.IsPositionInArea(position);
        }
        
        return false;
    }
    
    // Obtener la línea más cercana a una posición
    public LaneController GetNearestLane(Vector3 position)
    {
        if (lanes == null || lanes.Length == 0) return null;
        
        LaneController nearest = lanes[0];
        float minDistance = float.MaxValue;
        
        foreach (LaneController lane in lanes)
        {
            if (lane != null)
            {
                float distance = lane.GetDistanceToLane(position);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = lane;
                }
            }
        }
        
        return nearest;
    }
}

// Clase para representar un área específica del mapa
public class MapArea : MonoBehaviour
{
    [SerializeField] private string areaId;
    [SerializeField] private bool isSpawnArea = false;
    [SerializeField] private int teamId = -1; // -1 = neutral, 0 = team 1, 1 = team 2
    
    [Header("Area Size")]
    [SerializeField] private Vector3 areaSize = new Vector3(10f, 5f, 10f);
    [SerializeField] private bool useCustomCollider = false;
    
    private Collider areaCollider;
    
    public string AreaId => areaId;
    public bool IsSpawnArea => isSpawnArea;
    public int TeamId => teamId;
    
    private void Awake()
    {
        if (!useCustomCollider)
        {
            // Usar un box collider por defecto
            areaCollider = GetComponent<Collider>();
            
            if (areaCollider == null)
            {
                // Crear un collider si no existe
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.size = areaSize;
                boxCollider.isTrigger = true;
                areaCollider = boxCollider;
            }
        }
        else
        {
            // Usar collider existente
            areaCollider = GetComponent<Collider>();
        }
    }
    
    // Obtener una posición aleatoria dentro del área
    public Vector3 GetRandomPosition()
    {
        if (areaCollider is BoxCollider boxCollider)
        {
            // Para Box Collider
            Vector3 extents = boxCollider.size / 2f;
            Vector3 point = new Vector3(
                Random.Range(-extents.x, extents.x),
                0,
                Random.Range(-extents.z, extents.z)
            );
            
            return transform.TransformPoint(point);
        }
        else if (areaCollider is SphereCollider sphereCollider)
        {
            // Para Sphere Collider
            Vector3 point = Random.insideUnitSphere * sphereCollider.radius;
            point.y = 0; // Mantener en el plano Y
            
            return transform.TransformPoint(point);
        }
        else
        {
            // Fallback
            return transform.position;
        }
    }
    
    // Comprobar si una posición está dentro del área
    public bool IsPositionInArea(Vector3 position)
    {
        if (areaCollider != null)
        {
            return areaCollider.bounds.Contains(position);
        }
        return false;
    }
    
    // Dibujar gizmos en el editor
    private void OnDrawGizmos()
    {
        if (!useCustomCollider)
        {
            Gizmos.color = isSpawnArea ? new Color(0, 1, 0, 0.3f) : new Color(1, 1, 0, 0.3f);
            
            if (teamId == 0)
            {
                Gizmos.color = new Color(0, 0, 1, 0.3f); // Azul para equipo 1
            }
            else if (teamId == 1)
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f); // Rojo para equipo 2
            }
            
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            Gizmos.DrawCube(Vector3.zero, areaSize);
            
            Gizmos.matrix = originalMatrix;
        }
    }
}