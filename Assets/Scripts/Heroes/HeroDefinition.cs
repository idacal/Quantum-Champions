using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewHero", menuName = "Epoch Legends/Hero Definition")]
public class HeroDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string heroName;
    public string description;
    public HeroArchetype archetype;
    
    [Header("Visual")]
    public GameObject heroPrefab;
    public Sprite heroPortrait;
    public Sprite heroIcon;
    
    [Header("Base Stats")]
    public float baseHealth;
    public float baseMana;
    public float baseAttackDamage;
    public float baseAttackSpeed;
    public float baseMoveSpeed;
    public float baseHealthRegen;
    public float baseManaRegen;
    
    [Header("Stat Growth per Level")]
    public float healthGrowth;
    public float manaGrowth;
    public float attackDamageGrowth;
    public float attackSpeedGrowth;
    
    [Header("Abilities")]
    public List<AbilityDefinition> abilities = new List<AbilityDefinition>();
}