using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CombatManager : MonoBehaviourPunCallbacks
{
    public static CombatManager Instance;
    
    [Header("Combat Settings")]
    [SerializeField] private float criticalHitMultiplier = 1.5f;
    [SerializeField] private float baseCriticalChance = 0.05f;
    
    private Dictionary<int, DamageEvent> damageEvents = new Dictionary<int, DamageEvent>();
    private int nextEventId = 0;
    
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
    
    // Método para aplicar daño directamente
    public float ApplyDamage(Hero attacker, Hero target, float amount, DamageType damageType)
    {
        if (target == null) return 0f;
        
        // Verificar si el atacante soy yo (o si soy el MasterClient)
        if ((attacker != null && attacker.photonView.IsMine) || PhotonNetwork.IsMasterClient)
        {
            // Calcular daño real
            float actualDamage = CalculateDamage(attacker, target, amount, damageType);
            
            // Aplicar daño al objetivo
            if (target.photonView.IsMine)
            {
                // Si el objetivo es local, aplicar daño directamente
                target.TakeDamage(actualDamage, attacker);
            }
            else
            {
                // Si el objetivo es remoto, enviar RPC
                int attackerId = (attacker != null && attacker.photonView != null) ? attacker.photonView.ViewID : -1;
                
                // Usar PlayerNetwork para el RPC
                PlayerNetwork targetNetwork = target.GetComponent<PlayerNetwork>();
                if (targetNetwork != null)
                {
                    targetNetwork.ApplyDamage(actualDamage, attacker);
                }
            }
            
            // Registrar evento de daño
            RegisterDamageEvent(attacker, target, actualDamage, damageType);
            
            return actualDamage;
        }
        
        return 0f;
    }
    
    // Calcular el daño teniendo en cuenta estadísticas, resistencias, críticos, etc.
    private float CalculateDamage(Hero attacker, Hero target, float baseDamage, DamageType damageType)
    {
        if (target == null) return 0f;
        
        float finalDamage = baseDamage;
        
        // Si hay atacante, aplicar modificadores ofensivos
        if (attacker != null)
        {
            HeroStats attackerStats = attacker.GetComponent<HeroStats>();
            if (attackerStats != null)
            {
                // Escalar daño según tipo
                if (damageType == DamageType.Physical)
                {
                    finalDamage *= (attackerStats.AttackDamage / 100f) + 1f;
                }
                
                // Comprobar crítico
                if (IsCriticalHit(attackerStats.CritChance))
                {
                    finalDamage *= criticalHitMultiplier;
                }
            }
        }
        
        // Aplicar resistencias del objetivo
        HeroStats targetStats = target.GetComponent<HeroStats>();
        if (targetStats != null)
        {
            // Calcular reducción de daño
            finalDamage = targetStats.CalculateDamageReduction(finalDamage, damageType);
        }
        
        return Mathf.Max(1f, finalDamage); // Mínimo 1 de daño
    }
    
    // Determinar si un ataque es crítico
    private bool IsCriticalHit(float critChance)
    {
        float totalCritChance = baseCriticalChance + critChance;
        return Random.value <= totalCritChance;
    }
    
    // Registrar un evento de daño para estadísticas y efectos
    private void RegisterDamageEvent(Hero attacker, Hero target, float damage, DamageType damageType)
    {
        DamageEvent newEvent = new DamageEvent
        {
            id = nextEventId++,
            timestamp = Time.time,
            attackerId = attacker != null ? attacker.photonView.ViewID : -1,
            targetId = target.photonView.ViewID,
            damage = damage,
            damageType = damageType,
            isCritical = damage > 0 // Simplificado, idealmente tendríamos una bandera específica
        };
        
        damageEvents[newEvent.id] = newEvent;
        
        // Limitar el tamaño del histórico (opcional)
        if (damageEvents.Count > 100)
        {
            // Eliminar eventos antiguos
            int oldestId = nextEventId - 100;
            damageEvents.Remove(oldestId);
        }
        
        // Notificar para visuales y feedback
        if (target.photonView.IsMine)
        {
            // Mostrar números de daño flotantes, feedback visual, etc.
            // DamageVisualsManager.Instance.ShowDamageNumber(target.transform.position, damage, damageType, newEvent.isCritical);
        }
    }
    
    // Obtener eventos de daño recientes para un jugador
    public List<DamageEvent> GetRecentDamageEvents(Hero hero, float timeWindow = 5f)
    {
        if (hero == null || hero.photonView == null) return new List<DamageEvent>();
        
        List<DamageEvent> result = new List<DamageEvent>();
        float currentTime = Time.time;
        
        foreach (var evt in damageEvents.Values)
        {
            // Filtrar por ventana de tiempo
            if (currentTime - evt.timestamp > timeWindow) continue;
            
            // Filtrar por jugador (como atacante o víctima)
            if (evt.attackerId == hero.photonView.ViewID || evt.targetId == hero.photonView.ViewID)
            {
                result.Add(evt);
            }
        }
        
        return result;
    }
}

// Clase para representar un evento de daño
public class DamageEvent
{
    public int id;
    public float timestamp;
    public int attackerId;
    public int targetId;
    public float damage;
    public DamageType damageType;
    public bool isCritical;
}