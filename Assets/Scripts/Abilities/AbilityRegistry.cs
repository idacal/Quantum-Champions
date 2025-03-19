using System.Collections.Generic;
using UnityEngine;

public class AbilityRegistry : MonoBehaviour
{
    [SerializeField] private List<AbilityDefinition> availableAbilities = new List<AbilityDefinition>();
    
    private Dictionary<string, AbilityDefinition> abilityLookup = new Dictionary<string, AbilityDefinition>();
    
    private void Awake()
    {
        // Construir lookup para búsqueda eficiente
        foreach (AbilityDefinition ability in availableAbilities)
        {
            abilityLookup[ability.name] = ability;
            abilityLookup[ability.abilityName] = ability; // También buscar por nombre de habilidad
        }
    }
    
    public AbilityDefinition GetAbilityByName(string abilityName)
    {
        if (abilityLookup.TryGetValue(abilityName, out AbilityDefinition ability))
        {
            return ability;
        }
        return null;
    }
    
    public List<AbilityDefinition> GetAllAbilities()
    {
        return new List<AbilityDefinition>(availableAbilities);
    }
    
    public List<AbilityDefinition> GetAbilitiesByType(AbilityDefinition.TargetingType targetingType)
    {
        List<AbilityDefinition> result = new List<AbilityDefinition>();
        
        foreach (AbilityDefinition ability in availableAbilities)
        {
            if (ability.targetingType == targetingType)
            {
                result.Add(ability);
            }
        }
        
        return result;
    }
}