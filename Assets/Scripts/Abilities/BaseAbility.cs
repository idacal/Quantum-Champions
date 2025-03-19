using UnityEngine;

public abstract class BaseAbility
{
    public AbilityDefinition Definition { get; private set; }
    protected Hero caster;
    
    public float CurrentCooldown { get; protected set; }
    
    public BaseAbility(AbilityDefinition definition, Hero caster)
    {
        Definition = definition;
        this.caster = caster;
        CurrentCooldown = 0f;
    }
    
    public virtual bool CanUse()
    {
        // Verificar cooldown
        if (CurrentCooldown > 0f) return false;
        
        // Verificar maná
        if (caster.CurrentMana < Definition.manaCost) return false;
        
        return true;
    }
    
    public abstract bool Use(Vector3 targetPosition, GameObject targetObject = null);
    
    public void StartCooldown()
    {
        CurrentCooldown = Definition.cooldownTime;
    }
    
    public void UpdateCooldown(float deltaTime)
    {
        if (CurrentCooldown > 0f)
        {
            CurrentCooldown = Mathf.Max(0f, CurrentCooldown - deltaTime);
        }
    }
}