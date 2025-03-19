using UnityEngine;

// Habilidad dirigida a un objetivo específico
public class TargetAbility : BaseAbility
{
    public TargetAbility(AbilityDefinition definition, Hero caster) : base(definition, caster) { }
    
    public override bool Use(Vector3 targetPosition, GameObject targetObject = null)
    {
        if (targetObject == null) return false;
        
        // Verificar rango
        float distance = Vector3.Distance(caster.transform.position, targetObject.transform.position);
        if (distance > Definition.range) return false;
        
        // Obtener componente Hero del objetivo
        Hero targetHero = targetObject.GetComponent<Hero>();
        
        if (targetHero != null)
        {
            // Aplicar daño si es necesario
            if (Definition.baseDamage > 0)
            {
                targetHero.TakeDamage(Definition.baseDamage, caster);
            }
            
            // Aplicar curación si es necesario
            if (Definition.baseHealing > 0)
            {
                targetHero.Heal(Definition.baseHealing);
            }
        }
        
        // Instanciar efectos visuales
        if (Definition.effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(
                Definition.effectPrefab,
                targetObject.transform.position,
                Quaternion.identity
            );
            
            // Destruir después de la duración
            Object.Destroy(effect, Definition.duration > 0 ? Definition.duration : 2f);
        }
        
        return true;
    }
}