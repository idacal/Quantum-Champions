using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq; // Añadida para usar el método Where

public class NotificationSystem : MonoBehaviour
{
    public static NotificationSystem Instance;
    
    [SerializeField] private Transform notificationsContainer;
    [SerializeField] private GameObject notificationPrefab;
    
    [Header("Settings")]
    [SerializeField] private float defaultDisplayTime = 5f;
    [SerializeField] private float fadeOutTime = 0.5f;
    [SerializeField] private int maxNotifications = 5;
    [SerializeField] private AudioClip notificationSound;
    
    private Queue<GameObject> activeNotifications = new Queue<GameObject>();
    
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
    
    // Método para mostrar una notificación sencilla
    public void ShowNotification(string message, NotificationType type = NotificationType.Info)
    {
        ShowNotification(message, type, defaultDisplayTime);
    }
    
    // Método para mostrar una notificación con tiempo personalizado
    public void ShowNotification(string message, NotificationType type, float displayTime)
    {
        // Limitar número de notificaciones activas
        if (activeNotifications.Count >= maxNotifications)
        {
            // Eliminar la más antigua
            GameObject oldNotification = activeNotifications.Dequeue();
            Destroy(oldNotification);
        }
        
        // Crear nueva notificación
        GameObject notification = Instantiate(notificationPrefab, notificationsContainer);
        
        // Configurar UI
        Text textComponent = notification.GetComponentInChildren<Text>();
        if (textComponent != null)
        {
            textComponent.text = message;
        }
        
        // Configurar color según tipo
        Image background = notification.GetComponent<Image>();
        if (background != null)
        {
            background.color = GetColorForType(type);
        }
        
        // Reproducir sonido
        if (notificationSound != null)
        {
            AudioSource.PlayClipAtPoint(notificationSound, Camera.main.transform.position);
        }
        
        // Añadir a la cola de activas
        activeNotifications.Enqueue(notification);
        
        // Iniciar temporizador para eliminarla
        StartCoroutine(RemoveNotification(notification, displayTime));
    }
    
    // Método para mostrar notificación de objetivo
    public void ShowObjectiveNotification(string message, bool isCompleted = false)
    {
        NotificationType type = isCompleted ? NotificationType.Success : NotificationType.Objective;
        ShowNotification(message, type, defaultDisplayTime * 1.5f);
    }
    
    // Método para mostrar notificación de asesinato/muerte
    public void ShowKillNotification(string killerName, string victimName)
    {
        string message = $"{killerName} ha eliminado a {victimName}!";
        ShowNotification(message, NotificationType.Kill, defaultDisplayTime);
    }
    
    // Método para obtener color según tipo
    private Color GetColorForType(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Info:
                return new Color(0.2f, 0.2f, 0.8f, 0.8f);  // Azul
                
            case NotificationType.Warning:
                return new Color(0.8f, 0.8f, 0.2f, 0.8f);  // Amarillo
                
            case NotificationType.Error:
                return new Color(0.8f, 0.2f, 0.2f, 0.8f);  // Rojo
                
            case NotificationType.Success:
                return new Color(0.2f, 0.8f, 0.2f, 0.8f);  // Verde
                
            case NotificationType.Objective:
                return new Color(0.8f, 0.6f, 0.0f, 0.8f);  // Naranja
                
            case NotificationType.Kill:
                return new Color(0.7f, 0.0f, 0.7f, 0.8f);  // Púrpura
                
            default:
                return new Color(0.2f, 0.2f, 0.2f, 0.8f);  // Gris
        }
    }
    
    // Coroutine para eliminar notificación después de un tiempo
    private IEnumerator RemoveNotification(GameObject notification, float displayTime)
    {
        // Esperar tiempo de display
        yield return new WaitForSeconds(displayTime);
        
        // Fade out
        CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            float startTime = Time.time;
            float startAlpha = canvasGroup.alpha;
            
            while (Time.time < startTime + fadeOutTime)
            {
                float t = (Time.time - startTime) / fadeOutTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
        }
        
        // Eliminar de la cola y destruir
        if (activeNotifications.Contains(notification))
        {
            activeNotifications = new Queue<GameObject>(activeNotifications.ToArray().Where(n => n != notification));
        }
        
        Destroy(notification);
    }
}

// Tipos de notificación
public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success,
    Objective,
    Kill
}