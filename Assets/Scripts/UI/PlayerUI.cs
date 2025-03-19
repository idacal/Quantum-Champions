using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Añadida directiva using para List<>

public class PlayerUI : MonoBehaviour
{
    [Header("Health & Mana")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider manaBar;
    [SerializeField] private Text healthText;
    [SerializeField] private Text manaText;
    
    [Header("Abilities")]
    [SerializeField] private Transform abilitiesContainer;
    [SerializeField] private GameObject abilitySlotPrefab;
    
    [Header("Level & Stats")]
    [SerializeField] private Text levelText;
    [SerializeField] private Text statsText;
    
    private Hero playerHero;
    private AbilityController abilityController;
    
    private void Start()
    {
        // Buscar al jugador local
        PlayerNetwork localPlayer = PlayerManager.Instance.LocalPlayer;
        if (localPlayer != null)
        {
            playerHero = localPlayer.GetComponent<Hero>();
            abilityController = localPlayer.GetComponent<AbilityController>();
            
            if (playerHero != null)
            {
                // Inicializar UI
                InitializeAbilitySlots();
                UpdateUI();
            }
        }
    }
    
    private void Update()
    {
        if (playerHero != null)
        {
            UpdateUI();
        }
    }
    
    private void InitializeAbilitySlots()
    {
        if (abilityController == null) return;
        
        // Limpiar slots existentes
        foreach (Transform child in abilitiesContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Crear un slot para cada habilidad
        List<BaseAbility> abilities = abilityController.GetAbilities();
        for (int i = 0; i < abilities.Count; i++)
        {
            GameObject slotObject = Instantiate(abilitySlotPrefab, abilitiesContainer);
            
            // Configurar el slot
            AbilitySlotUI slotUI = slotObject.GetComponent<AbilitySlotUI>();
            if (slotUI != null)
            {
                slotUI.Initialize(abilities[i], i);
            }
        }
    }
    
    private void UpdateUI()
    {
        // Actualizar barras de vida y maná
        float healthPercent = playerHero.CurrentHealth / playerHero.GetComponent<HeroStats>().MaxHealth;
        float manaPercent = playerHero.CurrentMana / playerHero.GetComponent<HeroStats>().MaxMana;
        
        healthBar.value = healthPercent;
        manaBar.value = manaPercent;
        
        healthText.text = $"{Mathf.Ceil(playerHero.CurrentHealth)}/{Mathf.Ceil(playerHero.GetComponent<HeroStats>().MaxHealth)}";
        manaText.text = $"{Mathf.Ceil(playerHero.CurrentMana)}/{Mathf.Ceil(playerHero.GetComponent<HeroStats>().MaxMana)}";
        
        // Actualizar nivel
        levelText.text = $"Level {playerHero.CurrentLevel}";
        
        // Actualizar estadísticas
        HeroStats stats = playerHero.GetComponent<HeroStats>();
        statsText.text = $"ATK: {stats.AttackDamage}\n" +
                         $"SPD: {stats.MoveSpeed}\n" +
                         $"ARM: {stats.PhysicalResistance}";
    }
}

// Clase para los slots de habilidad
public class AbilitySlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Text cooldownText;
    [SerializeField] private Text keyText;
    
    private BaseAbility ability;
    private int abilityIndex;
    private AbilityController abilityController;
    
    public void Initialize(BaseAbility ability, int index)
    {
        this.ability = ability;
        this.abilityIndex = index;
        
        // Mostrar icono
        iconImage.sprite = ability.Definition.icon;
        
        // Mostrar tecla (Q, W, E, R)
        char[] keys = new char[] { 'Q', 'W', 'E', 'R' };
        if (index < keys.Length)
        {
            keyText.text = keys[index].ToString();
        }
        
        // Buscar controlador
        abilityController = PlayerManager.Instance.LocalPlayer.GetComponent<AbilityController>();
    }
    
    private void Update()
    {
        if (abilityController == null) return;
        
        // Actualizar cooldown
        float cooldown = abilityController.GetAbilityCooldown(abilityIndex);
        float maxCooldown = ability.Definition.cooldownTime;
        
        if (cooldown > 0)
        {
            // Mostrar overlay de cooldown
            cooldownOverlay.gameObject.SetActive(true);
            cooldownOverlay.fillAmount = cooldown / maxCooldown;
            
            // Mostrar texto de cooldown
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = Mathf.Ceil(cooldown).ToString();
        }
        else
        {
            // Ocultar indicadores de cooldown
            cooldownOverlay.gameObject.SetActive(false);
            cooldownText.gameObject.SetActive(false);
        }
    }
}