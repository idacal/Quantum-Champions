using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Hero))]
public class PlayerController : MonoBehaviourPun
{
    private Hero hero;
    private HeroMovement movement;
    private Camera mainCamera;
    
    [Header("Settings")]
    [SerializeField] private float targetingRange = 10f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask targetableLayer;
    
    // Estado del controlador
    private bool isAbilityCastingMode = false;
    private bool isTargetingMode = false;
    private GameObject currentTargetObject;
    
    private void Awake()
    {
        hero = GetComponent<Hero>();
        movement = GetComponent<HeroMovement>();
        mainCamera = Camera.main;
    }
    
    private void Update()
    {
        // Sólo procesar input para el jugador local
        if (!photonView.IsMine) return;
        
        // Manejar movimiento y selección de objetivo
        if (!isAbilityCastingMode)
        {
            HandleMovementInput();
            HandleTargetingInput();
        }
        
        // Manejar input de habilidades básicas (A, S) por ejemplo autoataques
        HandleBasicAbilityInput();
    }
    
    private void HandleMovementInput()
    {
        // Manejar click derecho para mover
        if (Input.GetMouseButtonDown(1))
        {
            // Raycast para determinar punto de destino
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, groundLayer))
            {
                // Mover al punto del mundo
                movement.MoveToPosition(hit.point);
                
                // Resetear targeting
                isTargetingMode = false;
                currentTargetObject = null;
            }
        }
    }
    
    private void HandleTargetingInput()
    {
        // Si presionamos A, entramos en modo targeting
        if (Input.GetKeyDown(KeyCode.A))
        {
            isTargetingMode = true;
        }
        
        // En modo targeting, verificar targets con click izquierdo
        if (isTargetingMode && Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, targetableLayer))
            {
                // Verificar si es un objetivo válido (p.ej. un héroe enemigo)
                Hero targetHero = hit.collider.GetComponent<Hero>();
                if (targetHero != null && targetHero != hero)
                {
                    // Verificar equipo
                    TeamAssignment targetTeam = targetHero.GetComponent<TeamAssignment>();
                    TeamAssignment myTeam = GetComponent<TeamAssignment>();
                    
                    if (targetTeam != null && myTeam != null && targetTeam.TeamId != myTeam.TeamId)
                    {
                        // Establecer como objetivo actual
                        currentTargetObject = targetHero.gameObject;
                        
                        // Calcular distancia
                        float distance = Vector3.Distance(transform.position, currentTargetObject.transform.position);
                        
                        // Si está en rango, mantener posición y atacar
                        if (distance <= targetingRange)
                        {
                            // Detener movimiento
                            movement.StopMovement();
                            
                            // Rotar hacia el objetivo
                            movement.RotateTowards(currentTargetObject.transform.position);
                            
                            // Iniciar ataque automático
                            // Aquí se implementaría la lógica de autoataque
                        }
                        // Si está fuera de rango, moverse hacia el objetivo
                        else
                        {
                            movement.MoveToPosition(currentTargetObject.transform.position);
                        }
                    }
                }
            }
        }
    }
    
    private void HandleBasicAbilityInput()
    {
        // Implementar autoataques u otras habilidades básicas
        // Por ejemplo, presionar S para un ataque rápido
        if (Input.GetKeyDown(KeyCode.S) && currentTargetObject != null)
        {
            // Verificar rango
            float distance = Vector3.Distance(transform.position, currentTargetObject.transform.position);
            if (distance <= targetingRange)
            {
                // Ejecutar ataque
                // Aquí se implementaría la lógica de ataque
            }
        }
    }
    
    // Método para cambiar entre modo normal y modo de casting de habilidades
    public void SetAbilityCastingMode(bool isCasting)
    {
        isAbilityCastingMode = isCasting;
        
        // Si estamos saliendo del modo casting, restablecer movimiento
        if (!isCasting && movement != null)
        {
            movement.StopMovement();
        }
    }
    
    // Método para obtener el objeto seleccionado actualmente
    public GameObject GetCurrentTarget()
    {
        return currentTargetObject;
    }
    
    // Método para seleccionar un objetivo específico
    public void SetTarget(GameObject target)
    {
        currentTargetObject = target;
        isTargetingMode = (target != null);
    }
}