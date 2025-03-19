using UnityEngine;

// Habilidad de área
public class AreaAbility : BaseAbility
{
    public AreaAbility(AbilityDefinition definition, Hero caster) : base(definition, caster) { }
    
    public override bool Use(Vector3 targetPosition, GameObject targetObject = null)
    {
        // Verificar rango
        float distance = Vector3.Distance(caster.transform.position, targetPosition);
        if (distance > Definition.range) return false;
        
        // Instanciar efecto visual
        if (Definition.effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(
                Definition.effectPrefab,
                targetPosition,
                Quaternion.identity
            );
            
            // Destruir después de la duración
            Object.Destroy(effect, Definition.duration > 0 ? Definition.duration : 2f);
        }
        
        // Encontrar objetivos en el área
        Collider[] hitColliders = Physics.OverlapSphere(targetPosition, Definition.areaOfEffect);
        
        foreach (var hitCollider in hitColliders)
        {
            Hero targetHero = hitCollider.GetComponent<Hero>();
            
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
        }
        
        return true;
    }
}