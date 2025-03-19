using System.Collections.Generic;
using UnityEngine;

public class HeroLoadout : MonoBehaviour
{
    // Referencia al héroe correspondiente
    [SerializeField] private HeroDefinition heroDefinition;
    
    // Habilidades seleccionadas
    private List<AbilityDefinition> selectedAbilities = new List<AbilityDefinition>();
    
    // Elementos adicionales que podrían incluirse
    private Dictionary<string, object> talentos = new Dictionary<string, object>();
    private Dictionary<string, object> runas = new Dictionary<string, object>();
    
    // Guardar configuración para uso posterior
    private string configName = "Default";
    
    public void SetHeroDefinition(HeroDefinition definition)
    {
        heroDefinition = definition;
        ResetToDefaults();
    }
    
    // Resetear a la configuración por defecto del héroe
    public void ResetToDefaults()
    {
        if (heroDefinition == null) return;
        
        // Copiar las habilidades predeterminadas
        selectedAbilities.Clear();
        foreach (AbilityDefinition ability in heroDefinition.abilities)
        {
            selectedAbilities.Add(ability);
        }
        
        // Limpiar los talentos y runas
        talentos.Clear();
        runas.Clear();
    }
    
    // Cambiar una habilidad por otra alternativa
    public bool ReplaceAbility(int slot, AbilityDefinition newAbility)
    {
        if (heroDefinition == null) return false;
        if (slot < 0 || slot >= selectedAbilities.Count) return false;
        
        // Verificar si la habilidad es válida para este héroe
        // Aquí se podría implementar una lógica más compleja para verificar compatibilidad
        
        selectedAbilities[slot] = newAbility;
        return true;
    }
    
    // Obtener las habilidades actuales
    public List<AbilityDefinition> GetSelectedAbilities()
    {
        return new List<AbilityDefinition>(selectedAbilities);
    }
    
    // Guardar la configuración actual
    public void SaveConfiguration(string name)
    {
        configName = name;
        
        // Aquí se implementaría la lógica para guardar en PlayerPrefs o en un sistema de persistencia
        // Por ejemplo:
        string heroName = heroDefinition ? heroDefinition.heroName : "unknown";
        string saveKey = $"HeroLoadout_{heroName}_{name}";
        
        // Guardar las referencias a las habilidades (simplificado)
        List<string> abilityNames = new List<string>();
        foreach (var ability in selectedAbilities)
        {
            abilityNames.Add(ability.name);
        }
        
        // En un sistema real, también guardarías los talentos y runas
        
        // Guardar en PlayerPrefs (simplificado)
        PlayerPrefs.SetString(saveKey, string.Join(",", abilityNames));
        PlayerPrefs.Save();
    }
    
    // Cargar una configuración guardada
    public bool LoadConfiguration(string name)
    {
        if (heroDefinition == null) return false;
        
        string heroName = heroDefinition.heroName;
        string loadKey = $"HeroLoadout_{heroName}_{name}";
        
        if (!PlayerPrefs.HasKey(loadKey)) return false;
        
        // Cargar las referencias a las habilidades
        string savedData = PlayerPrefs.GetString(loadKey);
        string[] abilityNames = savedData.Split(',');
        
        // Resetear la configuración
        selectedAbilities.Clear();
        
        // Buscar las definiciones de habilidades correspondientes
        // Simplificado - en un sistema real necesitarías acceso a todas las definiciones
        AbilityRegistry abilityRegistry = FindObjectOfType<AbilityRegistry>();
        if (abilityRegistry != null)
        {
            foreach (string abilityName in abilityNames)
            {
                AbilityDefinition ability = abilityRegistry.GetAbilityByName(abilityName);
                if (ability != null)
                {
                    selectedAbilities.Add(ability);
                }
            }
        }
        
        // Actualizar el nombre de la configuración
        configName = name;
        
        return true;
    }
    
    // Verificar si la configuración actual es válida
    public bool IsConfigurationValid()
    {
        if (heroDefinition == null) return false;
        if (selectedAbilities.Count == 0) return false;
        
        // Verificar que todas las habilidades sean válidas
        foreach (var ability in selectedAbilities)
        {
            if (ability == null) return false;
        }
        
        return true;
    }
}