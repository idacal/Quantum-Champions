using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Hero caster;
    private float damage;
    private float maxDistance;
    private Vector3 startPosition;
    private Vector3 direction;
    private float speed = 10f;
    
    private bool hasHit = false;
    
    public void Initialize(Hero caster, float damage, float maxDistance, Vector3 direction)
    {
        this.caster = caster;
        this.damage = damage;
        this.maxDistance = maxDistance;
        this.direction = direction;
        this.startPosition = transform.position;
    }
    
    private void Update()
    {
        if (hasHit) return;
        
        // Mover proyectil
        transform.position += direction * speed * Time.deltaTime;
        
        // Verificar distancia máxima
        float traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= maxDistance)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        
        // Comprobar si golpeamos algo que puede recibir daño
        Hero targetHero = other.GetComponent<Hero>();
        
        if (targetHero != null && targetHero != caster)
        {
            hasHit = true;
            
            // Aplicar daño
            targetHero.TakeDamage(damage, caster);
            
            // Instanciar efecto de impacto si es necesario
            // Aquí podrías instanciar un efecto visual
            
            // Destruir proyectil
            Destroy(gameObject);
        }
        else if (other.CompareTag("Obstacle"))
        {
            hasHit = true;
            
            // Instanciar efecto de impacto en obstáculo si es necesario
            // Aquí podrías instanciar un efecto visual
            
            // Destruir proyectil
            Destroy(gameObject);
        }
    }
}