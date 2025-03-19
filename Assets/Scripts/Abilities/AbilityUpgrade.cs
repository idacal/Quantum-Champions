using System.Collections.Generic;
using UnityEngine;

public class AbilityUpgrade : MonoBehaviour
{
    private Hero hero;
    private AbilityController abilityController;
    
    // Seguimiento de niveles de habilidad
    private Dictionary<int, int> abilityLevels = new Dictionary<int, int>();
    
    // Puntos de habilidad disponibles
    private int availablePoints = 0;
    
    private void Awake()
    {
        hero = GetComponent<Hero>();
        abilityController = GetComponent<AbilityController>();
    }
    
    private void Start()
    {
        // Inicializar niveles de habilidad a 1
        List<BaseAbility> abilities = abilityController.GetAbilities();
        for (int i = 0; i < abilities.Count; i++)
        {
            abilityLevels[i] = 1;
        }
        
        // Suscribirse al evento de subida de nivel del héroe
        if (hero != null)
        {
            hero.OnLevelUp += OnHeroLevelUp;
        }
    }
    
    private void OnDestroy()
    {
        // Desuscribirse del evento
        if (hero != null)
        {
            hero.OnLevelUp -= OnHeroLevelUp;
        }
    }
    
    // Cuando el héroe sube de nivel
    private void OnHeroLevelUp(Hero hero)
    {
        // Dar un punto de habilidad al subir de nivel
        availablePoints++;
    }
    
    // Mejorar una habilidad
    public bool UpgradeAbility(int abilityIndex)
    {
        // Verificar si tenemos puntos disponibles
        if (availablePoints <= 0) return false;
        
        // Verificar que la habilidad existe
        List<BaseAbility> abilities = abilityController.GetAbilities();
        if (abilityIndex < 0 || abilityIndex >= abilities.Count) return false;
        
        // Obtener nivel actual
        int currentLevel = GetAbilityLevel(abilityIndex);
        
        // Verificar que no excedemos el nivel máximo (por ejemplo, 5)
        if (currentLevel >= 5) return false;
        
        // Aumentar nivel
        abilityLevels[abilityIndex] = currentLevel + 1;
        
        // Consumir punto de habilidad
        availablePoints--;
        
        // Podríamos aplicar cambios a la habilidad aquí
        // Por ejemplo, aumentar daño, reducir cooldown, etc.
        
        return true;
    }
    
    // Obtener nivel de una habilidad
    public int GetAbilityLevel(int abilityIndex)
    {
        if (abilityLevels.TryGetValue(abilityIndex, out int level))
        {
            return level;
        }
        return 1; // Nivel base por defecto
    }
    
    // Obtener puntos disponibles
    public int GetAvailablePoints()
    {
        return availablePoints;
    }
}