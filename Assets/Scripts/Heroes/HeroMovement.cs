using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HeroMovement : MonoBehaviourPun
{
    [SerializeField] private float rotationSpeed = 5f;
    
    private NavMeshAgent navMeshAgent;
    private HeroStats heroStats;
    private Animator animator;
    
    // Estado de movimiento
    private bool isMoving = false;
    
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        heroStats = GetComponent<HeroStats>();
        animator = GetComponent<Animator>();
    }
    
    private void Start()
    {
        // Configurar velocidad desde las estadísticas
        if (heroStats != null)
        {
            navMeshAgent.speed = heroStats.MoveSpeed;
        }
    }
    
    private void Update()
    {
        // Sólo procesar input para el jugador local
        if (!photonView.IsMine) return;
        
        // Actualizar animaciones
        UpdateAnimations();
    }
    
    // Mover el héroe a una posición del mundo
    public void MoveToPosition(Vector3 position)
    {
        if (!photonView.IsMine) return;
        
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(position);
        isMoving = true;
    }
    
    // Detener el movimiento
    public void StopMovement()
    {
        if (!photonView.IsMine) return;
        
        navMeshAgent.isStopped = true;
        isMoving = false;
    }
    
    // Rotar hacia una posición
    public void RotateTowards(Vector3 position)
    {
        if (!photonView.IsMine) return;
        
        Vector3 direction = (position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    // Comprobar si el héroe ha llegado a su destino
    public bool HasReachedDestination()
    {
        if (!navMeshAgent.pathPending && 
            navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && 
            (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude < 0.1f))
        {
            return true;
        }
        
        return false;
    }
    
    // Actualizar valores del NavMeshAgent cuando cambian las estadísticas
    public void UpdateMovementStats()
    {
        if (heroStats != null)
        {
            navMeshAgent.speed = heroStats.MoveSpeed;
        }
    }
    
    // Actualizar animaciones basadas en el estado de movimiento
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Comprobar si estamos en movimiento
        bool moving = navMeshAgent.velocity.magnitude > 0.1f;
        
        // Actualizar parámetros del animator
        animator.SetBool("IsMoving", moving);
        
        // Si estamos moviendo pero ya casi llegamos, podemos considerar que ya no estamos en movimiento
        if (HasReachedDestination() && isMoving)
        {
            isMoving = false;
            animator.SetBool("IsMoving", false);
        }
    }
}