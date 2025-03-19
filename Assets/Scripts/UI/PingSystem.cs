using Photon.Pun;
using UnityEngine;
using System.Collections;

public class PingSystem : MonoBehaviourPun
{
    [SerializeField] private GameObject alertPingPrefab;
    [SerializeField] private GameObject attackPingPrefab;
    [SerializeField] private GameObject defendPingPrefab;
    [SerializeField] private GameObject helpPingPrefab;
    
    [SerializeField] private float pingDuration = 5f;
    [SerializeField] private float pingCooldown = 2f;
    
    [Header("Control")]
    [SerializeField] private KeyCode pingKey = KeyCode.G;
    [SerializeField] private KeyCode pingWheelKey = KeyCode.LeftAlt;
    
    private float lastPingTime = -100f;
    private bool isPingWheelOpen = false;
    private PingType selectedPingType = PingType.Alert;
    
    private void Update()
    {
        if (!photonView.IsMine) return;
        
        // Control de ping wheel
        if (Input.GetKeyDown(pingWheelKey))
        {
            OpenPingWheel();
        }
        else if (Input.GetKeyUp(pingWheelKey))
        {
            ClosePingWheel();
        }
        
        if (isPingWheelOpen)
        {
            UpdatePingWheelSelection();
        }
        
        // Crear ping con tecla G
        if (Input.GetKeyDown(pingKey) && Time.time > lastPingTime + pingCooldown)
        {
            CreatePing();
        }
    }
    
    private void OpenPingWheel()
    {
        isPingWheelOpen = true;
        // Aquí podrías mostrar una UI de rueda de ping
        // UI.ShowPingWheel();
    }
    
    private void ClosePingWheel()
    {
        isPingWheelOpen = false;
        // Aquí podrías ocultar la UI de rueda de ping
        // UI.HidePingWheel();
    }
    
    private void UpdatePingWheelSelection()
    {
        // Esto es un ejemplo simplificado. En una implementación real,
        // calcularíamos qué segmento de la rueda está seleccionado basado
        // en la posición del ratón.
        
        Vector2 mouseDir = Input.mousePosition - new Vector3(Screen.width / 2, Screen.height / 2);
        float angle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
        
        // Convertir a 0-360
        angle = (angle + 360) % 360;
        
        // Mapear ángulo a tipo de ping
        if (angle >= 315 || angle < 45)
        {
            selectedPingType = PingType.Alert;
        }
        else if (angle >= 45 && angle < 135)
        {
            selectedPingType = PingType.Attack;
        }
        else if (angle >= 135 && angle < 225)
        {
            selectedPingType = PingType.Defend;
        }
        else // angle >= 225 && angle < 315
        {
            selectedPingType = PingType.Help;
        }
        
        // Actualizar visual del ping wheel
        // UI.UpdatePingWheelSelection(selectedPingType);
    }
    
    private void CreatePing()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            // Usar el tipo seleccionado o predeterminado
            PingType pingType = isPingWheelOpen ? selectedPingType : PingType.Alert;
            
            // Enviar RPC para crear ping en todos los clientes
            photonView.RPC("CreatePingRPC", RpcTarget.All, hit.point, (int)pingType);
            
            // Actualizar timestamp
            lastPingTime = Time.time;
        }
    }
    
    [PunRPC]
    private void CreatePingRPC(Vector3 position, int pingTypeInt)
    {
        // Convertir int a enum
        PingType pingType = (PingType)pingTypeInt;
        
        // Seleccionar prefab según tipo
        GameObject pingPrefab = GetPingPrefab(pingType);
        
        if (pingPrefab != null)
        {
            // Instanciar ping en el mundo
            GameObject ping = Instantiate(pingPrefab, position, Quaternion.identity);
            
            // Configurar ping
            PingMarker marker = ping.GetComponent<PingMarker>();
            if (marker != null)
            {
                marker.Initialize(pingType, photonView.Owner.NickName);
            }
            
            // Añadir al minimapa
            MinimapSystem minimap = FindObjectOfType<MinimapSystem>();
            if (minimap != null)
            {
                minimap.CreatePing(position, pingType);
            }
            
            // Mostrar notificación
            string message = photonView.Owner.NickName + " " + GetPingMessage(pingType);
            NotificationSystem.Instance.ShowNotification(message, NotificationType.Info);
            
            // Destruir después de un tiempo
            Destroy(ping, pingDuration);
        }
    }
    
    private GameObject GetPingPrefab(PingType pingType)
    {
        switch (pingType)
        {
            case PingType.Alert:
                return alertPingPrefab;
            case PingType.Attack:
                return attackPingPrefab;
            case PingType.Defend:
                return defendPingPrefab;
            case PingType.Help:
                return helpPingPrefab;
            default:
                return alertPingPrefab;
        }
    }
    
    private string GetPingMessage(PingType pingType)
    {
        switch (pingType)
        {
            case PingType.Alert:
                return "señala ¡Alerta!";
            case PingType.Attack:
                return "señala ¡Atacar!";
            case PingType.Defend:
                return "señala ¡Defender!";
            case PingType.Help:
                return "solicita ¡Ayuda!";
            default:
                return "ha marcado una ubicación";
        }
    }
}

// Componente para marcador de ping
public class PingMarker : MonoBehaviour
{
    [SerializeField] private TextMesh playerNameText;
    [SerializeField] private AudioSource pingSound;
    [SerializeField] private Transform visualContainer;
    
    public void Initialize(PingType pingType, string playerName)
    {
        // Mostrar nombre del jugador
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }
        
        // Reproducir sonido
        if (pingSound != null)
        {
            pingSound.Play();
        }
        
        // Aplicar animación de aparición
        if (visualContainer != null)
        {
            visualContainer.localScale = Vector3.zero;
            
            // Usar una coroutine en lugar de LeanTween
            StartCoroutine(ScaleIn(visualContainer, 0.3f));
        }
    }
    
    private IEnumerator ScaleIn(Transform target, float duration)
    {
        float time = 0;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            
            // Ease out back (similar a LeanTween.setEaseOutBack)
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            float tEased = 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
            
            target.localScale = Vector3.Lerp(startScale, targetScale, tEased);
            
            yield return null;
        }
        
        target.localScale = targetScale;
    }
}