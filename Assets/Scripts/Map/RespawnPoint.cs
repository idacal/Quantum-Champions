using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [SerializeField] private int teamId = -1; // -1 = neutral, 0 = team 1, 1 = team 2
    [SerializeField] private float protectionRadius = 10f;
    [SerializeField] private float protectionDuration = 3f;
    
    [Header("Visuals")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.3f);
    
    public int TeamId => teamId;
    
    // Este método se llamaría cuando un héroe respawnea en este punto
    public void OnHeroRespawn(Hero hero)
    {
        if (hero == null) return;
        
        // Aplicar protección temporal
        ApplyRespawnProtection(hero);
    }
    
    // Aplicar efecto de protección al respawnear
    private void ApplyRespawnProtection(Hero hero)
    {
        // Aquí se implementaría la lógica para aplicar un buff de protección
        // por ejemplo, invulnerabilidad temporal
        
        // Ejemplo simple:
        StatusEffectManager statusManager = hero.GetComponent<StatusEffectManager>();
        if (statusManager != null)
        {
            // Crear un efecto de protección
            // statusManager.ApplyEffect("RespawnProtection", protectionDuration);
        }
        
        // Mostrar efecto visual
        GameObject protectionEffect = hero.transform.Find("ProtectionEffect")?.gameObject;
        if (protectionEffect != null)
        {
            protectionEffect.SetActive(true);
            
            // Programar desactivación después de la duración
            Invoke("DisableProtectionEffect", protectionDuration);
        }
    }
    
    // Desactivar efecto visual
    private void DisableProtectionEffect(GameObject protectionEffect)
    {
        if (protectionEffect != null)
        {
            protectionEffect.SetActive(false);
        }
    }
    
    // Dibuja gizmos en el editor para visualizar el punto de respawn
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, 1f);
        
        // Dibujar área de protección
        Gizmos.DrawWireSphere(transform.position, protectionRadius);
        
        // Marcar equipo con color
        if (teamId == 0)
        {
            Gizmos.color = Color.blue;
        }
        else if (teamId == 1)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.gray;
        }
        
        // Dibujar flecha indicando dirección
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}