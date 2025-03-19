using System.Collections.Generic;
using UnityEngine;

public class HeroRegistry : MonoBehaviour
{
    [SerializeField] private List<HeroDefinition> availableHeroes = new List<HeroDefinition>();
    
    private Dictionary<string, HeroDefinition> heroLookup = new Dictionary<string, HeroDefinition>();
    
    private void Awake()
    {
        // Construir lookup para búsqueda eficiente
        foreach (HeroDefinition hero in availableHeroes)
        {
            heroLookup[hero.heroName] = hero;
        }
    }
    
    public List<HeroDefinition> GetAllHeroes()
    {
        return availableHeroes;
    }
    
    public HeroDefinition GetHeroByName(string heroName)
    {
        if (heroLookup.TryGetValue(heroName, out HeroDefinition hero))
        {
            return hero;
        }
        return null;
    }
    
    public HeroDefinition GetRandomHero()
    {
        if (availableHeroes.Count > 0)
        {
            int randomIndex = Random.Range(0, availableHeroes.Count);
            return availableHeroes[randomIndex];
        }
        return null;
    }
    
    // Obtener héroes de un arquetipo específico
    public List<HeroDefinition> GetHeroesByArchetype(HeroArchetype archetype)
    {
        List<HeroDefinition> result = new List<HeroDefinition>();
        
        foreach (HeroDefinition hero in availableHeroes)
        {
            if (hero.archetype == archetype)
            {
                result.Add(hero);
            }
        }
        
        return result;
    }
}