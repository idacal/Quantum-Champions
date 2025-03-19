using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AbilityController : MonoBehaviourPun
{
    private List<BaseAbility> abilities = new List<BaseAbility>();
    private Hero heroComponent;
    
    private void Awake()
    {
        heroComponent = GetComponent<Hero>();
    }
    
    private void Update()
    {
        // Actualizar cooldowns de las habilidades
        foreach (BaseAbility ability in abilities)
        {
            ability.UpdateCooldown(Time.deltaTime);
        }
    }
    
    public void Initialize(List<AbilityDefinition> abilityDefinitions)
    {
        // Limpiar habilidades existentes
        abilities.Clear();
        
        // Crear instancias de habilidades basadas en las definiciones
        foreach (var definition in abilityDefinitions)
        {
            BaseAbility ability = CreateAbilityFromDefinition(definition);
            if (ability != null)
            {
                abilities.Add(ability);
            }
        }
    }
    
    private BaseAbility CreateAbilityFromDefinition(AbilityDefinition definition)
    {
        // Crear la instancia apropiada según tipo
        BaseAbility ability = null;
        
        switch (definition.targetingType)
        {
            case AbilityDefinition.TargetingType.Self:
                ability = new SelfAbility(definition, heroComponent);
                break;
            case AbilityDefinition.TargetingType.Target:
                ability = new TargetAbility(definition, heroComponent);
                break;
            case AbilityDefinition.TargetingType.Direction:
                ability = new DirectionalAbility(definition, heroComponent);
                break;
            case AbilityDefinition.TargetingType.Area:
                ability = new AreaAbility(definition, heroComponent);
                break;
            case AbilityDefinition.TargetingType.Passive:
                ability = new PassiveAbility(definition, heroComponent);
                break;
        }
        
        return ability;
    }
    
    public void ActivateAbility(int abilityIndex, Vector3 targetPosition, int targetNetworkId = -1)
    {
        if (abilityIndex < 0 || abilityIndex >= abilities.Count) return;
        
        BaseAbility ability = abilities[abilityIndex];
        
        // Para habilidades pasivas no hacemos nada
        if (ability is PassiveAbility) return;
        
        // Verificar si se puede usar la habilidad
        if (!ability.CanUse()) return;
        
        // Conseguir el objetivo si se proporcionó un ID
        GameObject target = null;
        if (targetNetworkId != -1)
        {
            PhotonView targetView = PhotonView.Find(targetNetworkId);
            if (targetView != null)
            {
                target = targetView.gameObject;
            }
        }
        
        // Usar la habilidad
        bool success = ability.Use(targetPosition, target);
        
        if (success && photonView.IsMine)
        {
            // Consumir maná
            heroComponent.UseMana(ability.Definition.manaCost);
            
            // Iniciar cooldown
            ability.StartCooldown();
        }
    }
    
    // Retorna el cooldown actual de una habilidad
    public float GetAbilityCooldown(int abilityIndex)
    {
        if (abilityIndex < 0 || abilityIndex >= abilities.Count) return 0f;
        return abilities[abilityIndex].CurrentCooldown;
    }
    
    // Obtener todas las habilidades
    public List<BaseAbility> GetAbilities()
    {
        return new List<BaseAbility>(abilities);
    }
}