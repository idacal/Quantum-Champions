using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MinimapSystem : MonoBehaviour
{
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private RectTransform minimapRect; // Referencia al RectTransform para evitar GetComponent repetidos
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float panSpeed = 5f;
    [SerializeField] private Vector2 zoomRange = new Vector2(5f, 20f);
    
    [Header("Icons")]
    [SerializeField] private GameObject allyIconPrefab;
    [SerializeField] private GameObject enemyIconPrefab;
    [SerializeField] private GameObject objectiveIconPrefab;
    [SerializeField] private GameObject pingIconPrefab;
    
    [Header("Colors")]
    [SerializeField] private Color teamOneColor = Color.blue;
    [SerializeField] private Color teamTwoColor = Color.red;
    
    // Control de minimapa
    private float currentZoom;
    private bool isPanning = false;
    private Vector3 panStartPosition;
    
    // Seguimiento de iconos
    private Dictionary<Transform, GameObject> entityIcons = new Dictionary<Transform, GameObject>();
    
    private void Start()
    {
        currentZoom = minimapCamera.orthographicSize;
        
        // Asegurarse de que tenemos el RectTransform del minimapa
        if (minimapRect == null && minimapImage != null)
        {
            minimapRect = minimapImage.GetComponent<RectTransform>();
        }
        
        // Crear iconos para entidades existentes
        CreateAllEntityIcons();
    }
    
    private void Update()
    {
        // Controles de zoom
        HandleZoom();
        
        // Controles de pan
        HandlePan();
        
        // Actualizar posición de iconos
        UpdateIconPositions();
    }
    
    private void HandleZoom()
    {
        // Zoom con rueda del ratón
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        
        // Comprobar si el cursor está sobre el minimapa
        if (IsPointerOverMinimap())
        {
            currentZoom = Mathf.Clamp(currentZoom - scrollDelta * zoomSpeed, zoomRange.x, zoomRange.y);
            minimapCamera.orthographicSize = currentZoom;
        }
    }
    
    private void HandlePan()
    {
        // Pan con click derecho
        if (Input.GetMouseButtonDown(1) && IsPointerOverMinimap())
        {
            isPanning = true;
            panStartPosition = Input.mousePosition;
        }
        
        if (Input.GetMouseButtonUp(1))
        {
            isPanning = false;
        }
        
        if (isPanning)
        {
            Vector3 panDelta = Input.mousePosition - panStartPosition;
            Vector3 camPos = minimapCamera.transform.position;
            
            camPos.x -= panDelta.x * panSpeed * Time.deltaTime;
            camPos.z -= panDelta.y * panSpeed * Time.deltaTime;
            
            minimapCamera.transform.position = camPos;
            panStartPosition = Input.mousePosition;
        }
        
        // Centrar en el jugador con espacio
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CenterOnPlayer();
        }
    }
    
    // Verifica si el puntero está sobre el minimapa
    private bool IsPointerOverMinimap()
    {
        if (minimapRect == null) return false;
        
        // Comprobar si el cursor está sobre el rect del minimapa
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minimapRect, 
            Input.mousePosition, 
            null, // No necesitamos camera para UI en espacio de pantalla
            out localPoint
        );
        
        // Si el punto está dentro del rect (teniendo en cuenta su tamaño)
        return minimapRect.rect.Contains(localPoint);
    }
    
    private void CenterOnPlayer()
    {
        PlayerNetwork localPlayer = PlayerManager.Instance.LocalPlayer;
        if (localPlayer != null)
        {
            Vector3 playerPos = localPlayer.transform.position;
            Vector3 camPos = minimapCamera.transform.position;
            
            camPos.x = playerPos.x;
            camPos.z = playerPos.z;
            
            minimapCamera.transform.position = camPos;
        }
    }
    
    private void CreateAllEntityIcons()
    {
        // Crear iconos para jugadores
        foreach (PlayerNetwork player in PlayerManager.Instance.GetAllPlayers())
        {
            CreateEntityIcon(player.transform, player.GetComponent<TeamAssignment>().TeamId);
        }
        
        // Aquí podríamos crear iconos para otros elementos como objetivos, torres, etc.
    }
    
    public void CreateEntityIcon(Transform entity, int teamId)
    {
        if (entityIcons.ContainsKey(entity)) return;
        
        // Determinar qué prefab usar
        GameObject iconPrefab;
        
        if (entity.GetComponent<PlayerNetwork>() != null)
        {
            // Es un jugador
            iconPrefab = teamId == PlayerManager.Instance.LocalPlayer.GetComponent<TeamAssignment>().TeamId ? 
                        allyIconPrefab : enemyIconPrefab;
        }
        else if (entity.GetComponent<ObjectiveMarker>() != null)
        {
            // Es un objetivo
            iconPrefab = objectiveIconPrefab;
        }
        else
        {
            // Otro tipo de entidad, usar indicador genérico
            return;
        }
        
        // Crear icono
        GameObject iconObject = Instantiate(iconPrefab, minimapImage.transform);
        
        // Colorear según equipo
        Image iconImage = iconObject.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.color = teamId == 0 ? teamOneColor : teamTwoColor;
        }
        
        // Guardar referencia
        entityIcons[entity] = iconObject;
    }
    
    private void UpdateIconPositions()
    {
        // Crear lista temporal para entidades destruidas
        List<Transform> destroyedEntities = new List<Transform>();
        
        foreach (var pair in entityIcons)
        {
            Transform entity = pair.Key;
            GameObject icon = pair.Value;
            
            if (entity == null)
            {
                // Entidad destruida
                destroyedEntities.Add(entity);
                continue;
            }
            
            // Convertir posición del mundo a viewport
            Vector3 viewportPoint = minimapCamera.WorldToViewportPoint(entity.position);
            
            // Convertir viewport a coordenadas de UI
            if (minimapRect != null)
            {
                Vector2 uiPosition = new Vector2(
                    viewportPoint.x * minimapRect.rect.width,
                    viewportPoint.y * minimapRect.rect.height
                );
                
                // Aplicar posición
                RectTransform iconRect = icon.GetComponent<RectTransform>();
                iconRect.anchoredPosition = uiPosition;
                
                // Si está fuera del mapa, ajustar escala o visibilidad
                bool isVisible = viewportPoint.x >= 0 && viewportPoint.x <= 1 && 
                               viewportPoint.y >= 0 && viewportPoint.y <= 1 && 
                               viewportPoint.z > 0;
                
                icon.SetActive(isVisible);
            }
        }
        
        // Limpiar entidades destruidas
        foreach (Transform entity in destroyedEntities)
        {
            Destroy(entityIcons[entity]);
            entityIcons.Remove(entity);
        }
    }
    
    // Método para crear un ping en el minimapa
    public void CreatePing(Vector3 worldPosition, PingType pingType)
    {
        GameObject pingObject = Instantiate(pingIconPrefab, minimapImage.transform);
        
        // Configurar apariencia según tipo
        Image pingImage = pingObject.GetComponent<Image>();
        switch (pingType)
        {
            case PingType.Alert:
                pingImage.color = Color.yellow;
                break;
            case PingType.Attack:
                pingImage.color = Color.red;
                break;
            case PingType.Defend:
                pingImage.color = Color.blue;
                break;
            case PingType.Help:
                pingImage.color = Color.green;
                break;
        }
        
        // Posicionar en el mapa
        Vector3 viewportPoint = minimapCamera.WorldToViewportPoint(worldPosition);
        
        if (minimapRect != null)
        {
            Vector2 uiPosition = new Vector2(
                viewportPoint.x * minimapRect.rect.width,
                viewportPoint.y * minimapRect.rect.height
            );
            
            RectTransform pingRect = pingObject.GetComponent<RectTransform>();
            pingRect.anchoredPosition = uiPosition;
        }
        
        // Destruir después de un tiempo
        Destroy(pingObject, 5f);
    }
}

// Tipos de ping
public enum PingType
{
    Alert,
    Attack,
    Defend,
    Help
}

// Marcador para objetivos
public class ObjectiveMarker : MonoBehaviour
{
    // Clase marcadora para identificar objetivos en el minimapa
}