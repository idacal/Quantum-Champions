using Photon.Pun;
using UnityEngine;
using System.Collections.Generic;

public class Hero : MonoBehaviourPun
{
    [SerializeField] private HeroDefinition heroDefinition;
    
    // Referencias a componentes
    private HeroStats heroStats;
    private HeroMovement heroMovement;
    private AbilityController abilityController;
    
    // Estado del héroe
    public float CurrentHealth { get; set; }
    public float CurrentMana { get; set; }
    public int CurrentLevel { get; private set; } = 1;
    public HeroDefinition Definition => heroDefinition;
    
    // Eventos
    public delegate void HeroEvent(Hero hero);
    public event HeroEvent OnDeath;
    public event HeroEvent OnRespawn;
    public event HeroEvent OnLevelUp;
    
    private void Awake()
    {
        heroStats = GetComponent<HeroStats>();
        heroMovement = GetComponent<HeroMovement>();
        abilityController = GetComponent<AbilityController>();
    }
    
    private void Start()
    {
        if (heroDefinition != null)
        {
            InitializeHero();
        }
    }
    
    private void InitializeHero()
    {
        // Configurar estadísticas iniciales
        heroStats.Initialize(heroDefinition);
        
        // Inicializar estado del héroe
        CurrentHealth = heroStats.MaxHealth;
        CurrentMana = heroStats.MaxMana;
        
        // Configurar habilidades
        abilityController.Initialize(heroDefinition.abilities);
    }
    
    public void TakeDamage(float damage, Hero attacker = null)
    {
        if (!photonView.IsMine) return;
        
        float actualDamage = heroStats.CalculateDamageReduction(damage);
        CurrentHealth -= actualDamage;
        
        // Comprobar si el héroe ha muerto
        if (CurrentHealth <= 0)
        {
            Die(attacker);
        }
        
        // Sincronizar salud
        photonView.RPC("SyncHealth", RpcTarget.Others, CurrentHealth);
    }
    
    [PunRPC]
    private void SyncHealth(float health)
    {
        CurrentHealth = health;
    }
    
    public void Heal(float amount)
    {
        if (!photonView.IsMine) return;
        
        CurrentHealth = Mathf.Min(CurrentHealth + amount, heroStats.MaxHealth);
        
        // Sincronizar salud
        photonView.RPC("SyncHealth", RpcTarget.Others, CurrentHealth);
    }
    
    public void UseMana(float amount)
    {
        if (!photonView.IsMine) return;
        
        CurrentMana = Mathf.Max(0, CurrentMana - amount);
        
        // Sincronizar maná
        photonView.RPC("SyncMana", RpcTarget.Others, CurrentMana);
    }
    
    [PunRPC]
    private void SyncMana(float mana)
    {
        CurrentMana = mana;
    }
    
    public void GainExperience(float amount)
    {
        if (!photonView.IsMine) return;
        
        // Esta sería una implementación simple, en un sistema real tendrías una curva de experiencia por nivel
        float expNeeded = CurrentLevel * 100; // Por ejemplo
        
        if (amount >= expNeeded)
        {
            LevelUp();
            // Experiencia sobrante
            GainExperience(amount - expNeeded);
        }
    }
    
    private void LevelUp()
    {
        CurrentLevel++;
        
        // Actualizar estadísticas por nivel
        heroStats.LevelUp(CurrentLevel);
        
        // Restaurar salud y maná al subir de nivel
        CurrentHealth = heroStats.MaxHealth;
        CurrentMana = heroStats.MaxMana;
        
        // Sincronizar con otros jugadores
        photonView.RPC("SyncLevelUp", RpcTarget.Others, CurrentLevel);
        
        // Disparar evento de subida de nivel
        OnLevelUp?.Invoke(this);
    }
    
    [PunRPC]
    private void SyncLevelUp(int newLevel)
    {
        CurrentLevel = newLevel;
        heroStats.LevelUp(CurrentLevel);
    }
    
    private void Die(Hero killer = null)
    {
        if (!photonView.IsMine) return;
        
        // Lógica de muerte (temporizador de respawn, etc.)
        photonView.RPC("ShowDeathAnimation", RpcTarget.All);
        
        // Desactivar temporalmente el héroe
        gameObject.SetActive(false);
        
        // Disparar evento de muerte
        OnDeath?.Invoke(this);
        
        // Iniciar temporizador de respawn
        // Esto debería ser gestionado por un sistema de respawn
        RespawnController respawnController = FindObjectOfType<RespawnController>();
        if (respawnController != null)
        {
            respawnController.QueueForRespawn(this);
        }
    }
    
    [PunRPC]
    private void ShowDeathAnimation()
    {
        // Reproducir animación de muerte
        // Aquí implementarías la animación o efectos visuales
    }
    
    public void Respawn(Vector3 position)
    {
        if (!photonView.IsMine) return;
        
        // Restaurar salud y maná
        CurrentHealth = heroStats.MaxHealth;
        CurrentMana = heroStats.MaxMana;
        
        // Reposicionar y reactivar
        transform.position = position;
        gameObject.SetActive(true);
        
        // Sincronizar con otros jugadores
        photonView.RPC("SyncRespawn", RpcTarget.Others, position);
        
        // Disparar evento de respawn
        OnRespawn?.Invoke(this);
    }
    
    [PunRPC]
    private void SyncRespawn(Vector3 position)
    {
        CurrentHealth = heroStats.MaxHealth;
        CurrentMana = heroStats.MaxMana;
        transform.position = position;
        gameObject.SetActive(true);
    }
    
    public void ActivateAbility(int abilityIndex, Vector3 targetPosition, int targetNetworkId = -1)
    {
        abilityController.ActivateAbility(abilityIndex, targetPosition, targetNetworkId);
    }
    
    // Update para regeneración y otros procesos continuos
    private void Update()
    {
        if (photonView.IsMine)
        {
            // Regeneración de vida y maná
            RegenerateHealthAndMana();
        }
    }
    
    private void RegenerateHealthAndMana()
    {
        // Regenerar vida si no está al máximo
        if (CurrentHealth < heroStats.MaxHealth)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + (heroStats.HealthRegen * Time.deltaTime), heroStats.MaxHealth);
        }
        
        // Regenerar maná si no está al máximo
        if (CurrentMana < heroStats.MaxMana)
        {
            CurrentMana = Mathf.Min(CurrentMana + (heroStats.ManaRegen * Time.deltaTime), heroStats.MaxMana);
        }
    }
}