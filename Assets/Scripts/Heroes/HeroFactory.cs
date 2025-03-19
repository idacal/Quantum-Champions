using UnityEngine;
using Photon.Pun;

public class HeroFactory : MonoBehaviour
{
    public static HeroFactory Instance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    // Crear un héroe en base a su definición
    public GameObject CreateHero(HeroDefinition heroDefinition, Vector3 position, Quaternion rotation, int teamId)
    {
        if (heroDefinition == null)
        {
            Debug.LogError("HeroFactory: Intent de crear héroe con definición nula");
            return null;
        }
        
        // Instanciar prefab del héroe
        GameObject heroInstance = Instantiate(heroDefinition.heroPrefab, position, rotation);
        
        // Configurar componentes
        ConfigureHero(heroInstance, heroDefinition, teamId);
        
        return heroInstance;
    }
    
    // Crear un héroe a través de Photon Network
    public GameObject CreateNetworkedHero(HeroDefinition heroDefinition, Vector3 position, Quaternion rotation, int teamId)
    {
        if (heroDefinition == null)
        {
            Debug.LogError("HeroFactory: Intent de crear héroe en red con definición nula");
            return null;
        }
        
        // Instanciar a través de Photon
        GameObject heroInstance = PhotonNetwork.Instantiate(
            heroDefinition.heroPrefab.name,
            position,
            rotation
        );
        
        // Configurar componentes
        ConfigureHero(heroInstance, heroDefinition, teamId);
        
        return heroInstance;
    }
    
    // Configurar todos los componentes del héroe
    private void ConfigureHero(GameObject heroInstance, HeroDefinition heroDefinition, int teamId)
    {
        // Obtener componente Hero
        Hero heroComponent = heroInstance.GetComponent<Hero>();
        if (heroComponent == null)
        {
            Debug.LogError("HeroFactory: El prefab del héroe no tiene componente Hero");
            return;
        }
        
        // Asignar equipo
        TeamAssignment teamAssignment = heroInstance.GetComponent<TeamAssignment>();
        if (teamAssignment != null)
        {
            teamAssignment.SetTeam(teamId);
        }
        
        // Configurar componentes adicionales según definición
        // Por ejemplo, habilidades, estadísticas, etc.
        
        // Notificar al GameManager que se ha creado un héroe
        // Si es necesario
    }
    
    // Crear un héroe basado en el nombre de la definición
    public GameObject CreateHeroByName(string heroName, Vector3 position, Quaternion rotation, int teamId)
    {
        // Buscar la definición por nombre
        HeroDefinition heroDefinition = FindHeroDefinitionByName(heroName);
        
        if (heroDefinition != null)
        {
            return CreateHero(heroDefinition, position, rotation, teamId);
        }
        
        Debug.LogError($"HeroFactory: No se encontró definición para el héroe '{heroName}'");
        return null;
    }
    
    // Buscar definición por nombre
    private HeroDefinition FindHeroDefinitionByName(string heroName)
    {
        // Buscar en el HeroRegistry si está disponible
        HeroRegistry registry = FindObjectOfType<HeroRegistry>();
        if (registry != null)
        {
            return registry.GetHeroByName(heroName);
        }
        
        // Alternativa: buscar directamente entre los ScriptableObjects
        HeroDefinition[] definitions = Resources.LoadAll<HeroDefinition>("Heroes");
        foreach (HeroDefinition def in definitions)
        {
            if (def.heroName == heroName)
            {
                return def;
            }
        }
        
        return null;
    }
}