using UnityEngine;

public class MOBACamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float height = 15f;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float edgePanSpeed = 20f;
    [SerializeField] private float edgePanThreshold = 20f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private Vector2 zoomRange = new Vector2(5f, 20f);
    
    private float currentZoom;
    private Vector3 targetPosition;
    
    private void Start()
    {
        currentZoom = distance;
        
        // Si no hay target asignado, buscar el jugador local
        if (target == null)
        {
            PlayerManager playerManager = FindObjectOfType<PlayerManager>();
            if (playerManager != null && playerManager.LocalPlayer != null)
            {
                target = playerManager.LocalPlayer.transform;
            }
        }
        
        if (target != null)
        {
            targetPosition = target.position;
        }
    }
    
    private void LateUpdate()
    {
        HandleInput();
        UpdateCameraPosition();
    }
    
    private void HandleInput()
    {
        // Movimiento con bordes de pantalla
        Vector3 moveDir = Vector3.zero;
        
        if (Input.mousePosition.x < edgePanThreshold)
            moveDir.x -= 1;
        else if (Input.mousePosition.x > Screen.width - edgePanThreshold)
            moveDir.x += 1;
        
        if (Input.mousePosition.y < edgePanThreshold)
            moveDir.z -= 1;
        else if (Input.mousePosition.y > Screen.height - edgePanThreshold)
            moveDir.z += 1;
        
        // Normalizar para evitar movimientos más rápidos en diagonal
        if (moveDir.magnitude > 0)
            moveDir.Normalize();
        
        // Aplicar movimiento
        targetPosition += moveDir * edgePanSpeed * Time.deltaTime;
        
        // Zoom con rueda del ratón
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        currentZoom = Mathf.Clamp(currentZoom - scrollDelta * zoomSpeed, zoomRange.x, zoomRange.y);
        
        // Volver a centrar con el target (héroe)
        if (Input.GetKeyDown(KeyCode.Space) && target != null)
        {
            targetPosition = target.position;
        }
    }
    
    private void UpdateCameraPosition()
    {
        // Calcular posición ideal
        Vector3 idealPosition = targetPosition - Vector3.forward * currentZoom;
        idealPosition.y = height;
        
        // Aplicar suavizado
        transform.position = Vector3.Lerp(transform.position, idealPosition, Time.deltaTime * 5f);
        
        // Mirar hacia abajo
        transform.LookAt(targetPosition);
    }
}