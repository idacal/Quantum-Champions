using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FogOfWarSystem : MonoBehaviourPunCallbacks
{
    [Header("Fog of War Settings")]
    [SerializeField] private float defaultVisionRange = 10f;
    [SerializeField] private float visionUpdateRate = 0.2f;
    [SerializeField] private LayerMask fogLayer;
    [SerializeField] private LayerMask visionBlockingLayers;
    
    [Header("Fog Rendering")]
    [SerializeField] private Material fogMaterial;
    [SerializeField] private Color exploredColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private Color fogColor = new Color(0, 0, 0, 1f);
    
    // Referencias a componentes
    private Renderer fogRenderer;
    private Texture2D fogTexture;
    private Texture2D exploredTexture;
    
    // Entidades con visión
    private Dictionary<Transform, VisionSource> teamOneVisionSources = new Dictionary<Transform, VisionSource>();
    private Dictionary<Transform, VisionSource> teamTwoVisionSources = new Dictionary<Transform, VisionSource>();
    
    // Dimensiones del mapa y texturas
    private Vector2 mapSize;
    private Vector2 textureSize = new Vector2(256, 256); // Ajustar según necesidades
    private Vector2 pixelsPerUnit;
    
    // Estado local
    private int localPlayerTeam = -1;
    private float nextVisionUpdateTime = 0f;
    
    public void Initialize(float defaultVisionRadius)
    {
        // Configurar tamaño del mapa
        Terrain terrain = FindObjectOfType<Terrain>();
        if (terrain != null)
        {
            mapSize = new Vector2(terrain.terrainData.size.x, terrain.terrainData.size.z);
        }
        else
        {
            // Tamaño por defecto si no hay terreno
            mapSize = new Vector2(200f, 200f);
        }
        
        // Calcular píxeles por unidad
        pixelsPerUnit = new Vector2(textureSize.x / mapSize.x, textureSize.y / mapSize.y);
        
        // Determinar equipo del jugador local
        PlayerNetwork localPlayer = PlayerManager.Instance.LocalPlayer;
        if (localPlayer != null)
        {
            TeamAssignment teamAssignment = localPlayer.GetComponent<TeamAssignment>();
            if (teamAssignment != null)
            {
                localPlayerTeam = teamAssignment.TeamId;
            }
        }
        
        // Inicializar texturas
        InitializeTextures();
        
        // Configurar renderer de niebla
        SetupFogRenderer();
        
        // Buscar y registrar entidades con visión al inicio
        FindAndRegisterVisionSources();
        
        // Actualizar visión inicial
        UpdateVision();
    }
    
    private void InitializeTextures()
    {
        // Crear textura para niebla actual
        fogTexture = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.RGBA32, false);
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        
        // Crear textura para áreas exploradas
        exploredTexture = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.RGBA32, false);
        exploredTexture.filterMode = FilterMode.Bilinear;
        exploredTexture.wrapMode = TextureWrapMode.Clamp;
        
        // Inicializar texturas con color de niebla
        Color[] fogColors = new Color[(int)textureSize.x * (int)textureSize.y];
        for (int i = 0; i < fogColors.Length; i++)
        {
            fogColors[i] = fogColor;
        }
        
        fogTexture.SetPixels(fogColors);
        fogTexture.Apply();
        
        // Inicializar textura de explorados con negro completo
        exploredTexture.SetPixels(fogColors);
        exploredTexture.Apply();
    }
    
    private void SetupFogRenderer()
    {
        // Buscar o crear objeto para renderizar la niebla
        GameObject fogPlane = GameObject.Find("FogOfWarPlane");
        if (fogPlane == null)
        {
            fogPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fogPlane.name = "FogOfWarPlane";
            fogPlane.transform.position = new Vector3(mapSize.x / 2, 0.1f, mapSize.y / 2);
            fogPlane.transform.localScale = new Vector3(mapSize.x / 10, 1, mapSize.y / 10); // El plano por defecto es 10x10
            fogPlane.layer = LayerMask.NameToLayer("FogOfWar");
        }
        
        // Configurar renderer
        fogRenderer = fogPlane.GetComponent<Renderer>();
        if (fogRenderer != null)
        {
            // Asignar material
            if (fogMaterial != null)
            {
                fogRenderer.material = new Material(fogMaterial);
            }
            else
            {
                // Material básico si no se proporciona uno
                fogRenderer.material = new Material(Shader.Find("Unlit/Transparent"));
            }
            
            // Asignar textura
            fogRenderer.material.mainTexture = fogTexture;
            
            // Configurar propiedades adicionales si el shader las usa
            if (fogRenderer.material.HasProperty("_ExploredTex"))
            {
                fogRenderer.material.SetTexture("_ExploredTex", exploredTexture);
            }
        }
    }
    
    private void FindAndRegisterVisionSources()
    {
        // Registrar al jugador local
        PlayerNetwork localPlayer = PlayerManager.Instance.LocalPlayer;
        if (localPlayer != null)
        {
            RegisterVisionSource(localPlayer.transform, defaultVisionRange, localPlayerTeam);
        }
        
        // Registrar otras entidades con visión (torres, wards, etc.)
        VisionProvider[] visionProviders = FindObjectsOfType<VisionProvider>();
        foreach (VisionProvider provider in visionProviders)
        {
            if (provider.gameObject.activeInHierarchy)
            {
                RegisterVisionSource(provider.transform, provider.VisionRange, provider.TeamId);
            }
        }
    }
    
    private void Update()
    {
        // Actualizar visión periódicamente
        if (Time.time >= nextVisionUpdateTime)
        {
            UpdateVision();
            nextVisionUpdateTime = Time.time + visionUpdateRate;
        }
    }
    
    private void UpdateVision()
    {
        // Solo procesar para el equipo del jugador local
        if (localPlayerTeam < 0) return;
        
        // Crear copia de la textura de niebla
        Color[] currentFogColors = fogTexture.GetPixels();
        Color[] currentExploredColors = exploredTexture.GetPixels();
        
        // Resetear textura de niebla actual (todo en negro)
        for (int i = 0; i < currentFogColors.Length; i++)
        {
            currentFogColors[i] = fogColor;
        }
        
        // Calcular visión para cada fuente del equipo local
        Dictionary<Transform, VisionSource> teamVisionSources = 
            localPlayerTeam == 0 ? teamOneVisionSources : teamTwoVisionSources;
        
        foreach (var pair in teamVisionSources)
        {
            Transform source = pair.Key;
            VisionSource visionData = pair.Value;
            
            if (source != null && visionData.isActive)
            {
                Vector2 sourcePosition = new Vector2(source.position.x, source.position.z);
                RevealArea(sourcePosition, visionData.visionRange, currentFogColors, currentExploredColors);
            }
        }
        
        // Aplicar cambios a las texturas
        fogTexture.SetPixels(currentFogColors);
        fogTexture.Apply();
        
        exploredTexture.SetPixels(currentExploredColors);
        exploredTexture.Apply();
    }
    
    private void RevealArea(Vector2 worldPos, float radius, Color[] fogColors, Color[] exploredColors)
    {
        // Convertir posición del mundo a coordenadas de textura
        Vector2 texturePos = new Vector2(
            worldPos.x * pixelsPerUnit.x,
            worldPos.y * pixelsPerUnit.y
        );
        
        // Radio en píxeles
        float pixelRadius = radius * ((pixelsPerUnit.x + pixelsPerUnit.y) / 2);
        
        // Calcular área afectada
        int startX = Mathf.Max(0, Mathf.FloorToInt(texturePos.x - pixelRadius));
        int endX = Mathf.Min((int)textureSize.x, Mathf.CeilToInt(texturePos.x + pixelRadius));
        int startY = Mathf.Max(0, Mathf.FloorToInt(texturePos.y - pixelRadius));
        int endY = Mathf.Min((int)textureSize.y, Mathf.CeilToInt(texturePos.y + pixelRadius));
        
        float sqrRadius = pixelRadius * pixelRadius;
        
        // Procesar cada píxel en el área
        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                float sqrDist = (x - texturePos.x) * (x - texturePos.x) + 
                               (y - texturePos.y) * (y - texturePos.y);
                
                if (sqrDist <= sqrRadius)
                {
                    // Verificar si hay línea de visión (simplificado)
                    bool hasLineOfSight = true;
                    
                    if (hasLineOfSight)
                    {
                        // Calcular índice en el array
                        int index = y * (int)textureSize.x + x;
                        
                        // Revelar área
                        fogColors[index] = Color.clear;
                        
                        // Marcar como explorado
                        exploredColors[index] = exploredColor;
                    }
                }
            }
        }
    }
    
    // Registrar una fuente de visión
    public void RegisterVisionSource(Transform source, float visionRange, int teamId)
    {
        if (source == null) return;
        
        VisionSource visionData = new VisionSource
        {
            visionRange = visionRange,
            isActive = true
        };
        
        // Añadir a la lista del equipo correspondiente
        if (teamId == 0)
        {
            teamOneVisionSources[source] = visionData;
        }
        else
        {
            teamTwoVisionSources[source] = visionData;
        }
    }
    
    // Eliminar una fuente de visión
    public void UnregisterVisionSource(Transform source, int teamId)
    {
        if (source == null) return;
        
        if (teamId == 0)
        {
            teamOneVisionSources.Remove(source);
        }
        else
        {
            teamTwoVisionSources.Remove(source);
        }
    }
    
    // Activar/desactivar una fuente de visión
    public void SetVisionSourceActive(Transform source, int teamId, bool isActive)
    {
        Dictionary<Transform, VisionSource> teamSources = 
            teamId == 0 ? teamOneVisionSources : teamTwoVisionSources;
        
        if (teamSources.TryGetValue(source, out VisionSource visionData))
        {
            visionData.isActive = isActive;
            teamSources[source] = visionData;
        }
    }
    
    // Comprobar si una entidad es visible para el equipo local
    public bool IsVisibleToLocalTeam(Vector3 position)
    {
        if (localPlayerTeam < 0) return false;
        
        // Convertir posición del mundo a coordenadas de textura
        Vector2 texturePos = new Vector2(
            position.x * pixelsPerUnit.x,
            position.z * pixelsPerUnit.y
        );
        
        // Comprobar si las coordenadas están dentro de la textura
        int x = Mathf.FloorToInt(texturePos.x);
        int y = Mathf.FloorToInt(texturePos.y);
        
        if (x < 0 || x >= textureSize.x || y < 0 || y >= textureSize.y)
        {
            return false;
        }
        
        // Obtener valor de niebla en esa posición
        Color fogValue = fogTexture.GetPixel(x, y);
        
        // Si el alpha es cercano a 0, es visible
        return fogValue.a < 0.1f;
    }
    
    // Comprobar si una entidad ha sido explorada por el equipo local
    public bool IsExploredByLocalTeam(Vector3 position)
    {
        if (localPlayerTeam < 0) return false;
        
        // Convertir posición del mundo a coordenadas de textura
        Vector2 texturePos = new Vector2(
            position.x * pixelsPerUnit.x,
            position.z * pixelsPerUnit.y
        );
        
        // Comprobar si las coordenadas están dentro de la textura
        int x = Mathf.FloorToInt(texturePos.x);
        int y = Mathf.FloorToInt(texturePos.y);
        
        if (x < 0 || x >= textureSize.x || y < 0 || y >= textureSize.y)
        {
            return false;
        }
        
        // Obtener valor explorado en esa posición
        Color exploredValue = exploredTexture.GetPixel(x, y);
        
        // Si el alpha no es 1 (negro total), ha sido explorada
        return exploredValue.a < 1f;
    }
}

// Clase para almacenar datos de una fuente de visión
public class VisionSource
{
    public float visionRange;
    public bool isActive;
}

// Componente para añadir a objetos que proporcionan visión (torres, wards, etc.)
public class VisionProvider : MonoBehaviour
{
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private int teamId = 0;
    [SerializeField] private bool isActiveOnStart = true;
    
    public float VisionRange => visionRange;
    public int TeamId => teamId;
    
    private void Start()
    {
        // Registrar en el sistema de fog of war
        FogOfWarSystem fogSystem = FindObjectOfType<FogOfWarSystem>();
        if (fogSystem != null)
        {
            fogSystem.RegisterVisionSource(transform, visionRange, teamId);
            
            if (!isActiveOnStart)
            {
                fogSystem.SetVisionSourceActive(transform, teamId, false);
            }
        }
    }
    
    private void OnDestroy()
    {
        // Eliminar del sistema al destruirse
        FogOfWarSystem fogSystem = FindObjectOfType<FogOfWarSystem>();
        if (fogSystem != null)
        {
            fogSystem.UnregisterVisionSource(transform, teamId);
        }
    }
}