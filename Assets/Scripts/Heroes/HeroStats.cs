using UnityEngine;
using System.Collections.Generic;

public class HeroStats : MonoBehaviour
{
    // Estadísticas base
    public float MaxHealth { get; private set; }
    public float MaxMana { get; private set; }
    public float AttackDamage { get; private set; }
    public float AttackSpeed { get; private set; }
    public float MoveSpeed { get; private set; }
    public float HealthRegen { get; private set; }
    public float ManaRegen { get; private set; }
    
    // Estadísticas críticas
    public float CritChance { get; private set; }
    public float CritDamage { get; private set; }
    
    // Resistencias
    public float PhysicalResistance { get; private set; }
    public float MagicalResistance { get; private set; }
    
    // Modificadores de stats temporales (buffs/debuffs)
    private Dictionary<string, StatModifier> statModifiers = new Dictionary<string, StatModifier>();
    
    // Referencia a la definición del héroe
    private HeroDefinition heroDefinition;
    
    public void Initialize(HeroDefinition definition)
    {
        heroDefinition = definition;
        
        // Inicializar estadísticas desde la definición
        MaxHealth = definition.baseHealth;
        MaxMana = definition.baseMana;
        AttackDamage = definition.baseAttackDamage;
        AttackSpeed = definition.baseAttackSpeed;
        MoveSpeed = definition.baseMoveSpeed;
        
        // Establecer valores por defecto para regeneración si no están en la definición
        HealthRegen = definition.baseHealthRegen;
        ManaRegen = definition.baseManaRegen;
        
        // Inicializar valores críticos
        CritChance = 0.05f; // 5% por defecto
        CritDamage = 1.5f;  // 150% por defecto
        
        // Valores por defecto para resistencias
        PhysicalResistance = 0;
        MagicalResistance = 0;
        
        // Limpiar modificadores
        statModifiers.Clear();
    }
    
    // Calcular la reducción de daño basada en resistencias
    public float CalculateDamageReduction(float incomingDamage, DamageType damageType = DamageType.Physical)
    {
        float resistance = damageType == DamageType.Physical ? PhysicalResistance : MagicalResistance;
        
        // Fórmula simple: damage * (100 / (100 + resistance))
        float damageMultiplier = 100f / (100f + resistance);
        return incomingDamage * damageMultiplier;
    }
    
    // Añadir un modificador de estadísticas
    public void AddStatModifier(string id, StatModifier modifier)
    {
        statModifiers[id] = modifier;
        RecalculateStats();
    }
    
    // Eliminar un modificador de estadísticas
    public void RemoveStatModifier(string id)
    {
        if (statModifiers.ContainsKey(id))
        {
            statModifiers.Remove(id);
            RecalculateStats();
        }
    }
    
    // Recalcular todas las estadísticas teniendo en cuenta los modificadores
    private void RecalculateStats()
    {
        // Reiniciar a valores base desde la definición
        MaxHealth = heroDefinition.baseHealth;
        MaxMana = heroDefinition.baseMana;
        AttackDamage = heroDefinition.baseAttackDamage;
        AttackSpeed = heroDefinition.baseAttackSpeed;
        MoveSpeed = heroDefinition.baseMoveSpeed;
        HealthRegen = heroDefinition.baseHealthRegen;
        ManaRegen = heroDefinition.baseManaRegen;
        CritChance = 0.05f;
        CritDamage = 1.5f;
        
        // Aplicar modificadores
        foreach (StatModifier modifier in statModifiers.Values)
        {
            // Primero aplicamos modificadores flat
            MaxHealth += modifier.healthFlat;
            MaxMana += modifier.manaFlat;
            AttackDamage += modifier.attackDamageFlat;
            AttackSpeed += modifier.attackSpeedFlat;
            MoveSpeed += modifier.moveSpeedFlat;
            HealthRegen += modifier.healthRegenFlat;
            ManaRegen += modifier.manaRegenFlat;
            PhysicalResistance += modifier.physicalResistanceFlat;
            MagicalResistance += modifier.magicalResistanceFlat;
            CritChance += modifier.critChanceFlat;
            CritDamage += modifier.critDamageFlat;
        }
        
        // Luego aplicamos los modificadores porcentuales
        float healthPercent = 1f;
        float manaPercent = 1f;
        float attackDamagePercent = 1f;
        float attackSpeedPercent = 1f;
        float moveSpeedPercent = 1f;
        float healthRegenPercent = 1f;
        float manaRegenPercent = 1f;
        float critChancePercent = 1f;
        float critDamagePercent = 1f;
        
        foreach (StatModifier modifier in statModifiers.Values)
        {
            healthPercent += modifier.healthPercent;
            manaPercent += modifier.manaPercent;
            attackDamagePercent += modifier.attackDamagePercent;
            attackSpeedPercent += modifier.attackSpeedPercent;
            moveSpeedPercent += modifier.moveSpeedPercent;
            healthRegenPercent += modifier.healthRegenPercent;
            manaRegenPercent += modifier.manaRegenPercent;
            critChancePercent += modifier.critChancePercent;
            critDamagePercent += modifier.critDamagePercent;
        }
        
        // Aplicar porcentajes
        MaxHealth *= healthPercent;
        MaxMana *= manaPercent;
        AttackDamage *= attackDamagePercent;
        AttackSpeed *= attackSpeedPercent;
        MoveSpeed *= moveSpeedPercent;
        HealthRegen *= healthRegenPercent;
        ManaRegen *= manaRegenPercent;
        CritChance *= critChancePercent;
        CritDamage *= critDamagePercent;
    }
    
    // Aumentar stats por nivel
    public void LevelUp(int newLevel)
    {
        // Incrementar estadísticas base según crecimiento por nivel
        MaxHealth += heroDefinition.healthGrowth;
        MaxMana += heroDefinition.manaGrowth;
        AttackDamage += heroDefinition.attackDamageGrowth;
        AttackSpeed += heroDefinition.attackSpeedGrowth;
        
        // Recalcular con los nuevos valores base
        RecalculateStats();
    }
}

// Enum para tipos de daño
public enum DamageType
{
    Physical,
    Magical,
    True // Daño verdadero que ignora resistencias
}

// Clase para modificadores de estadísticas
public class StatModifier
{
    // Modificadores flat (se suman directamente)
    public float healthFlat = 0f;
    public float manaFlat = 0f;
    public float attackDamageFlat = 0f;
    public float attackSpeedFlat = 0f;
    public float moveSpeedFlat = 0f;
    public float healthRegenFlat = 0f;
    public float manaRegenFlat = 0f;
    public float physicalResistanceFlat = 0f;
    public float magicalResistanceFlat = 0f;
    public float critChanceFlat = 0f;
    public float critDamageFlat = 0f;
    
    // Modificadores porcentuales (1.0 = 100%)
    public float healthPercent = 0f;
    public float manaPercent = 0f;
    public float attackDamagePercent = 0f;
    public float attackSpeedPercent = 0f;
    public float moveSpeedPercent = 0f;
    public float healthRegenPercent = 0f;
    public float manaRegenPercent = 0f;
    public float critChancePercent = 0f;
    public float critDamagePercent = 0f;
    
    // Duración del modificador (0 = permanente hasta que se elimine)
    public float duration = 0f;
}