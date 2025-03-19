using UnityEngine;

/// <summary>
/// Interfaz para objetos que pueden recibir daño en el juego.
/// Implementar esta interfaz en cualquier objeto que pueda ser dañado,
/// como héroes, creeps, estructuras u objetivos especiales.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Obtiene la salud máxima del objeto.
    /// </summary>
    float MaxHealth { get; }
    
    /// <summary>
    /// Obtiene la salud actual del objeto.
    /// </summary>
    float CurrentHealth { get; }
    
    /// <summary>
    /// Aplica daño al objeto.
    /// </summary>
    /// <param name="damage">Cantidad de daño a aplicar.</param>
    /// <param name="attacker">Referencia al héroe que causa el daño, si lo hay.</param>
    void TakeDamage(float damage, Hero attacker);
}