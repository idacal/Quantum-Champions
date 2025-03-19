using UnityEngine;

// Habilidad pasiva
public class PassiveAbility : BaseAbility
{
    public PassiveAbility(AbilityDefinition definition, Hero caster) : base(definition, caster) { }
    
    public override bool Use(Vector3 targetPosition, GameObject targetObject = null)
    {
        // Las habilidades pasivas no se usan activamente
        return false;
    }
    
    // Implementar efectos pasivos (se llamaría desde otro lugar)
    public void ApplyPassiveEffect()
    {
        // Aplicar efectos pasivos
        // Por ejemplo, aumentar regeneración de vida/maná, modificar stats, etc.
        
        // Esto sería algo como:
        // StatModifier modifier = new StatModifier();
        // modifier.healthRegenFlat = Definition.baseHealing;
        // caster.GetComponent<HeroStats>().AddStatModifier("PassiveAbility_" + Definition.abilityName, modifier);
    }
}