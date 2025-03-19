using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Epoch Legends/Ability Definition")]
public class AbilityDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string abilityName;
    public string description;
    public Sprite icon;
    
    [Header("Costs and Cooldown")]
    public float manaCost;
    public float cooldownTime;
    
    [Header("Targeting")]
    public TargetingType targetingType;
    public float range;
    public float areaOfEffect;
    
    [Header("Effects")]
    public float baseDamage;
    public float damageScaling;
    public float baseHealing;
    public float healingScaling;
    public float duration;
    
    [Header("Visual")]
    public GameObject effectPrefab;
    public GameObject projectilePrefab;
    
    public enum TargetingType
    {
        Self,
        Target,
        Direction,
        Area,
        Passive
    }
}