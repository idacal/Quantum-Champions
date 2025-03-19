using UnityEngine;

// Habilidad dirigida a uno mismo
public class SelfAbility : BaseAbility
{
    public SelfAbility(AbilityDefinition definition, Hero caster) : base(definition, caster) { }
    
    public override bool Use(Vector3 targetPosition, GameObject targetObject = null)
    {
        // Implementar efectos de la habilidad en el caster
        // Por ejemplo: curación, buffs, etc.
        
        if (Definition.baseHealing > 0)
        {
            float healing = Definition.baseHealing;
            // Aplicar curación al caster
            caster.Heal(healing);
        }
        
        // Instanciar efectos visuales
        if (Definition.effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(
                Definition.effectPrefab,
                caster.transform.position,
                Quaternion.identity
            );
            
            // Destruir después de la duración
            Object.Destroy(effect, Definition.duration > 0 ? Definition.duration : 2f);
        }
        
        return true;
    }
}