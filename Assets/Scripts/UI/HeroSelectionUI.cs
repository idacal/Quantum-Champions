using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class HeroSelectionUI : MonoBehaviour
{
    [Header("Hero Grid")]
    [SerializeField] private Transform heroGridContent;
    [SerializeField] private GameObject heroGridItemPrefab;
    
    [Header("Info Panel")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Image heroPortrait;
    [SerializeField] private Text heroNameText;
    [SerializeField] private Text heroDescriptionText;
    [SerializeField] private Text heroStatsText;
    [SerializeField] private Transform abilitiesContainer;
    [SerializeField] private GameObject abilityItemPrefab;
    
    [Header("Team Selection")]
    [SerializeField] private Transform teamOneContent;
    [SerializeField] private Transform teamTwoContent;
    [SerializeField] private GameObject teamHeroItemPrefab;
    
    [Header("Bottom Panel")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Text timerText;
    
    private HeroRegistry heroRegistry;
    private HeroDefinition selectedHero;
    private Dictionary<int, GameObject> teamOneItems = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> teamTwoItems = new Dictionary<int, GameObject>();
    
    private void Start()
    {
        // Obtener referencia al registro de héroes
        heroRegistry = FindObjectOfType<HeroRegistry>();
        
        // Configurar botón Ready
        readyButton.onClick.AddListener(OnReadyButtonClicked);
        readyButton.interactable = false; // Desactivado hasta seleccionar héroe
        
        // Suscribirse a eventos
        HeroSelectionManager.Instance.OnTimerUpdated += UpdateTimer;
        HeroSelectionManager.Instance.OnSelectionFinished += OnSelectionFinished;
        
        // Inicializar UI
        PopulateHeroGrid();
        ClearInfoPanel();
        UpdateTeamPanels();
    }
    
    private void OnDestroy()
    {
        // Desuscribirse de eventos
        if (HeroSelectionManager.Instance != null)
        {
            HeroSelectionManager.Instance.OnTimerUpdated -= UpdateTimer;
            HeroSelectionManager.Instance.OnSelectionFinished -= OnSelectionFinished;
        }
    }
    
    private void PopulateHeroGrid()
    {
        // Limpiar grid existente
        foreach (Transform child in heroGridContent)
        {
            Destroy(child.gameObject);
        }
        
        // Crear un item por cada héroe disponible
        foreach (HeroDefinition hero in heroRegistry.GetAllHeroes())
        {
            GameObject gridItem = Instantiate(heroGridItemPrefab, heroGridContent);
            
            // Configurar visual del item
            Image heroIcon = gridItem.GetComponent<Image>();
            heroIcon.sprite = hero.heroIcon;
            
            // Configurar interacción
            Button button = gridItem.GetComponent<Button>();
            button.onClick.AddListener(() => OnHeroSelected(hero));
            
            // Etiquetar para identificación
            gridItem.name = "HeroItem_" + hero.heroName;
        }
    }
    
    private void OnHeroSelected(HeroDefinition hero)
    {
        selectedHero = hero;
        
        // Actualizar panel de información
        UpdateInfoPanel(hero);
        
        // Actualizar selección en el manager
        HeroSelectionManager.Instance.SelectHero(hero);
        
        // Activar botón de Ready
        readyButton.interactable = true;
        
        // Actualizar paneles de equipo
        UpdateTeamPanels();
    }
    
    private void UpdateInfoPanel(HeroDefinition hero)
    {
        infoPanel.SetActive(true);
        
        // Actualizar información básica
        heroPortrait.sprite = hero.heroPortrait;
        heroNameText.text = hero.heroName;
        heroDescriptionText.text = hero.description;
        
        // Actualizar estadísticas
        heroStatsText.text = $"Health: {hero.baseHealth}\n" +
                             $"Mana: {hero.baseMana}\n" +
                             $"Attack: {hero.baseAttackDamage}\n" +
                             $"Speed: {hero.baseMoveSpeed}";
        
        // Limpiar contenedor de habilidades
        foreach (Transform child in abilitiesContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Crear items para cada habilidad
        foreach (AbilityDefinition ability in hero.abilities)
        {
            GameObject abilityItem = Instantiate(abilityItemPrefab, abilitiesContainer);
            
            // Configurar visual del item
            Image abilityIcon = abilityItem.transform.Find("AbilityIcon").GetComponent<Image>();
            Text abilityName = abilityItem.transform.Find("AbilityName").GetComponent<Text>();
            Text abilityDesc = abilityItem.transform.Find("AbilityDescription").GetComponent<Text>();
            
            abilityIcon.sprite = ability.icon;
            abilityName.text = ability.abilityName;
            abilityDesc.text = ability.description;
        }
    }
    
    private void ClearInfoPanel()
    {
        infoPanel.SetActive(false);
    }
    
    private void UpdateTeamPanels()
    {
        // Para cada jugador, mostrar su selección actual
        foreach (var player in PhotonNetwork.PlayerList)
        {
            int actorNumber = player.ActorNumber;
            HeroDefinition selectedHero = HeroSelectionManager.Instance.GetPlayerSelection(actorNumber);
            bool isReady = HeroSelectionManager.Instance.IsPlayerReady(actorNumber);
            
            // Determinar equipo (simplificado)
            bool isTeamOne = actorNumber % 2 == 0;
            Dictionary<int, GameObject> teamItems = isTeamOne ? teamOneItems : teamTwoItems;
            Transform teamContent = isTeamOne ? teamOneContent : teamTwoContent;
            
            // Si el jugador ya tiene un item en el panel, actualizarlo
            if (teamItems.TryGetValue(actorNumber, out GameObject existingItem))
            {
                // Actualizar información del item
                Image heroIcon = existingItem.transform.Find("HeroIcon").GetComponent<Image>();
                Text playerNameText = existingItem.transform.Find("PlayerName").GetComponent<Text>();
                Image readyIndicator = existingItem.transform.Find("ReadyIndicator").GetComponent<Image>();
                
                heroIcon.sprite = selectedHero != null ? selectedHero.heroIcon : null;
                playerNameText.text = player.NickName;
                readyIndicator.gameObject.SetActive(isReady);
            }
            else
            {
                // Crear un nuevo item para el jugador
                GameObject teamItem = Instantiate(teamHeroItemPrefab, teamContent);
                
                // Configurar información
                Image heroIcon = teamItem.transform.Find("HeroIcon").GetComponent<Image>();
                Text playerNameText = teamItem.transform.Find("PlayerName").GetComponent<Text>();
                Image readyIndicator = teamItem.transform.Find("ReadyIndicator").GetComponent<Image>();
                
                heroIcon.sprite = selectedHero != null ? selectedHero.heroIcon : null;
                playerNameText.text = player.NickName;
                readyIndicator.gameObject.SetActive(isReady);
                
                // Guardar referencia
                teamItems[actorNumber] = teamItem;
            }
        }
    }
    
    private void OnReadyButtonClicked()
    {
        if (selectedHero == null) return;
        
        bool currentStatus = HeroSelectionManager.Instance.IsPlayerReady(PhotonNetwork.LocalPlayer.ActorNumber);
        HeroSelectionManager.Instance.SetReady(!currentStatus);
        
        // Actualizar texto del botón
        readyButton.GetComponentInChildren<Text>().text = !currentStatus ? "Cancel" : "Ready";
        
        // Actualizar paneles de equipo
        UpdateTeamPanels();
    }
    
    private void UpdateTimer(float remainingTime)
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    private void OnSelectionFinished()
    {
        readyButton.interactable = false;
        timerText.text = "Starting Game...";
    }
}