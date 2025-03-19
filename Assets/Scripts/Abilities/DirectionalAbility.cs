using UnityEngine;

// Habilidad dirigida en una dirección
public class DirectionalAbility : BaseAbility
{
    public DirectionalAbility(AbilityDefinition definition, Hero caster) : base(definition, caster) { }
    
    public override bool Use(Vector3 targetPosition, GameObject targetObject = null)
    {
        // Calcular dirección
        Vector3 direction = (targetPosition - caster.transform.position).normalized;
        
        // Si hay un proyectil, instanciarlo
        if (Definition.projectilePrefab != null)
        {
            GameObject projectile = Object.Instantiate(
                Definition.projectilePrefab,
                caster.transform.position,
                Quaternion.LookRotation(direction)
            );
            
            // Configurar proyectil
            Projectile projectileComponent = projectile.GetComponent<Projectile>();
            if (projectileComponent != null)
            {
                projectileComponent.Initialize(caster, Definition.baseDamage, Definition.range, direction);
            }
        }
        // Si no hay proyectil, aplicar efectos inmediatos
        else
        {
            // Implementar raycast en la dirección para detectar objetivos
            RaycastHit hit;
            if (Physics.Raycast(caster.transform.position, direction, out hit, Definition.range))
            {
                Hero target = hit.collider.GetComponent<Hero>();
                if (target != null)
                {
                    // Aplicar daño
                    if (Definition.baseDamage > 0)
                    {
                        target.TakeDamage(Definition.baseDamage, caster);
                    }
                    
                    // Aplicar efectos visuales en el punto de impacto
                    if (Definition.effectPrefab != null)
                    {
                        GameObject effect = Object.Instantiate(
                            Definition.effectPrefab,
                            hit.point,
                            Quaternion.LookRotation(hit.normal)
                        );
                        
                        Object.Destroy(effect, Definition.duration > 0 ? Definition.duration : 2f);
                    }
                }
            }
        }
        
        return true;
    }
}