using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AbilityUIManager : MonoBehaviour
{
    [SerializeField] private Transform abilityBarContainer;
    [SerializeField] private GameObject abilitySlotPrefab;
    [SerializeField] private GameObject targetingIndicatorPrefab;
    
    private AbilityController abilityController;
    private PlayerController playerController;
    
    private GameObject activeTargetingIndicator;
    private int selectedAbilityIndex = -1;
    
    private void Start()
    {
        // Buscar jugador local
        PlayerNetwork localPlayer = PlayerManager.Instance.LocalPlayer;
        if (localPlayer != null)
        {
            abilityController = localPlayer.GetComponent<AbilityController>();
            playerController = localPlayer.GetComponent<PlayerController>();
            
            if (abilityController != null)
            {
                // Inicializar UI de habilidades
                InitializeAbilityBar();
            }
        }
    }
    
    private void Update()
    {
        if (abilityController == null) return;
        
        // Manejar input de habilidades
        HandleAbilityInput();
        
        // Manejar targeting
        if (selectedAbilityIndex >= 0)
        {
            UpdateTargetingIndicator();
        }
    }
    
    private void InitializeAbilityBar()
    {
        // Limpiar slots existentes
        foreach (Transform child in abilityBarContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Crear un slot para cada habilidad
        List<BaseAbility> abilities = abilityController.GetAbilities();
        for (int i = 0; i < abilities.Count; i++)
        {
            GameObject slotObject = Instantiate(abilitySlotPrefab, abilityBarContainer);
            
            // Configurar el slot
            AbilitySlotUI slotUI = slotObject.GetComponent<AbilitySlotUI>();
            if (slotUI != null)
            {
                slotUI.Initialize(abilities[i], i);
                
                // Configurar evento de click
                Button button = slotObject.GetComponent<Button>();
                if (button != null)
                {
                    int index = i;  // Capturar índice para el closure
                    button.onClick.AddListener(() => OnAbilitySelected(index));
                }
            }
        }
    }
    
    private void HandleAbilityInput()
    {
        // Mapeo de teclas a índices de habilidad
        KeyCode[] abilityKeys = new KeyCode[] { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };
        
        // Comprobar pulsaciones de teclas
        for (int i = 0; i < abilityKeys.Length; i++)
        {
            if (Input.GetKeyDown(abilityKeys[i]))
            {
                OnAbilitySelected(i);
                break;
            }
        }
        
        // Manejar click para confirmar objetivo
        if (selectedAbilityIndex >= 0 && Input.GetMouseButtonDown(0))
        {
            // No activar si estamos sobre un elemento de UI
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                ConfirmAbilityTarget();
            }
        }
        
        // Cancelar con click derecho
        if (selectedAbilityIndex >= 0 && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            CancelAbilityCasting();
        }
    }
    
    private void OnAbilitySelected(int abilityIndex)
    {
        List<BaseAbility> abilities = abilityController.GetAbilities();
        
        // Verificar índice válido
        if (abilityIndex < 0 || abilityIndex >= abilities.Count) return;
        
        // Verificar cooldown
        if (abilityController.GetAbilityCooldown(abilityIndex) > 0) return;
        
        // Verificar si es una habilidad pasiva
        BaseAbility ability = abilities[abilityIndex];
        if (ability is PassiveAbility) return;
        
        // Establecer habilidad seleccionada
        selectedAbilityIndex = abilityIndex;
        
        // Crear indicador de targeting
        if (activeTargetingIndicator == null)
        {
            activeTargetingIndicator = Instantiate(targetingIndicatorPrefab);
        }
        
        // Configurar indicador según tipo de habilidad
        ConfigureTargetingIndicator(ability.Definition.targetingType);
        
        // Desactivar movimiento normal
        if (playerController != null)
        {
            playerController.SetAbilityCastingMode(true);
        }
    }
    
    private void ConfigureTargetingIndicator(AbilityDefinition.TargetingType targetingType)
    {
        if (activeTargetingIndicator == null) return;
        
        // Configurar componentes según tipo
        TargetingIndicator indicator = activeTargetingIndicator.GetComponent<TargetingIndicator>();
        if (indicator != null)
        {
            // Obtener la definición de la habilidad
            List<BaseAbility> abilities = abilityController.GetAbilities();
            AbilityDefinition definition = abilities[selectedAbilityIndex].Definition;
            
            // Configurar basado en tipo
            switch (targetingType)
            {
                case AbilityDefinition.TargetingType.Self:
                    indicator.SetupAsSelfCast(definition.areaOfEffect);
                    break;
                    
                case AbilityDefinition.TargetingType.Target:
                    indicator.SetupAsTargetCast(definition.range);
                    break;
                    
                case AbilityDefinition.TargetingType.Direction:
                    indicator.SetupAsDirectionalCast(definition.range);
                    break;
                    
                case AbilityDefinition.TargetingType.Area:
                    indicator.SetupAsAreaCast(definition.range, definition.areaOfEffect);
                    break;
            }
        }
    }
    
    private void UpdateTargetingIndicator()
    {
        if (activeTargetingIndicator == null) return;
        
        // Obtener posición del ratón en el mundo
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 targetPosition;
        
        if (Physics.Raycast(ray, out hit))
        {
            targetPosition = hit.point;
        }
        else
        {
            // Si no hay hit, proyectar en un plano arbitrario
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                targetPosition = ray.GetPoint(distance);
            }
            else
            {
                return;
            }
        }
        
        // Actualizar posición del indicador
        TargetingIndicator indicator = activeTargetingIndicator.GetComponent<TargetingIndicator>();
        if (indicator != null)
        {
            indicator.UpdatePosition(targetPosition, PlayerManager.Instance.LocalPlayer.transform.position);
        }
    }
    
    private void ConfirmAbilityTarget()
    {
        if (selectedAbilityIndex < 0 || activeTargetingIndicator == null) return;
        
        // Obtener posición objetivo
        TargetingIndicator indicator = activeTargetingIndicator.GetComponent<TargetingIndicator>();
        Vector3 targetPosition = indicator.GetTargetPosition();
        
        // Obtener objeto objetivo si es relevante
        GameObject targetObject = indicator.GetTargetObject();
        int targetNetworkId = -1;
        
        if (targetObject != null)
        {
            // Si el objetivo tiene un PhotonView, obtener su ID
            Photon.Pun.PhotonView view = targetObject.GetComponent<Photon.Pun.PhotonView>();
            if (view != null)
            {
                targetNetworkId = view.ViewID;
            }
        }
        
        // Activar la habilidad
        PlayerNetwork playerNetwork = PlayerManager.Instance.LocalPlayer;
        if (playerNetwork != null)
        {
            playerNetwork.CallAbilityRPC(selectedAbilityIndex, targetPosition, targetNetworkId);
        }
        
        // Limpiar estado de casting
        ClearCastingState();
    }
    
    private void CancelAbilityCasting()
    {
        ClearCastingState();
    }
    
    private void ClearCastingState()
    {
        // Destruir indicador
        if (activeTargetingIndicator != null)
        {
            Destroy(activeTargetingIndicator);
            activeTargetingIndicator = null;
        }
        
        // Resetear selección
        selectedAbilityIndex = -1;
        
        // Restaurar movimiento normal
        if (playerController != null)
        {
            playerController.SetAbilityCastingMode(false);
        }
    }
}

// Clase auxiliar para el indicador de targeting
public class TargetingIndicator : MonoBehaviour
{
    [SerializeField] private GameObject selfCastIndicator;
    [SerializeField] private GameObject targetCastIndicator;
    [SerializeField] private GameObject directionalCastIndicator;
    [SerializeField] private GameObject areaCastIndicator;
    
    private AbilityDefinition.TargetingType currentType;
    private float range;
    private float areaSize;
    
    private Vector3 targetPosition;
    private GameObject targetObject;
    
    public void SetupAsSelfCast(float areaSize)
    {
        currentType = AbilityDefinition.TargetingType.Self;
        this.areaSize = areaSize;
        
        // Activar sólo el indicador correcto
        selfCastIndicator.SetActive(true);
        targetCastIndicator.SetActive(false);
        directionalCastIndicator.SetActive(false);
        areaCastIndicator.SetActive(false);
        
        // Configurar tamaño
        selfCastIndicator.transform.localScale = new Vector3(areaSize, areaSize, areaSize);
    }
    
    public void SetupAsTargetCast(float range)
    {
        currentType = AbilityDefinition.TargetingType.Target;
        this.range = range;
        
        // Activar sólo el indicador correcto
        selfCastIndicator.SetActive(false);
        targetCastIndicator.SetActive(true);
        directionalCastIndicator.SetActive(false);
        areaCastIndicator.SetActive(false);
    }
    
    public void SetupAsDirectionalCast(float range)
    {
        currentType = AbilityDefinition.TargetingType.Direction;
        this.range = range;
        
        // Activar sólo el indicador correcto
        selfCastIndicator.SetActive(false);
        targetCastIndicator.SetActive(false);
        directionalCastIndicator.SetActive(true);
        areaCastIndicator.SetActive(false);
        
        // Configurar longitud
        directionalCastIndicator.transform.localScale = new Vector3(1, 1, range);
    }
    
    public void SetupAsAreaCast(float range, float areaSize)
    {
        currentType = AbilityDefinition.TargetingType.Area;
        this.range = range;
        this.areaSize = areaSize;
        
        // Activar sólo el indicador correcto
        selfCastIndicator.SetActive(false);
        targetCastIndicator.SetActive(false);
        directionalCastIndicator.SetActive(false);
        areaCastIndicator.SetActive(true);
        
        // Configurar tamaño
        areaCastIndicator.transform.localScale = new Vector3(areaSize, areaSize, areaSize);
    }
    
    public void UpdatePosition(Vector3 targetPos, Vector3 casterPos)
    {
        targetPosition = targetPos;
        
        switch (currentType)
        {
            case AbilityDefinition.TargetingType.Self:
                // Para autocast, siempre sigue al lanzador
                transform.position = casterPos;
                break;
                
            case AbilityDefinition.TargetingType.Target:
                // Para target, seguir el ratón pero limitar por rango
                Vector3 direction = targetPos - casterPos;
                float distance = direction.magnitude;
                
                if (distance > range)
                {
                    // Limitar al rango máximo
                    targetPos = casterPos + direction.normalized * range;
                }
                
                transform.position = targetPos;
                
                // Buscar posible objetivo bajo el cursor
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit))
                {
                    // Verificar si es un objetivo válido (por ejemplo, un héroe)
                    Hero hero = hit.collider.GetComponent<Hero>();
                    if (hero != null)
                    {
                        targetObject = hero.gameObject;
                    }
                    else
                    {
                        targetObject = null;
                    }
                }
                break;
                
            case AbilityDefinition.TargetingType.Direction:
                // Para direccional, orientar hacia el objetivo
                transform.position = casterPos;
                
                // Rotar para mirar hacia el objetivo (ignorando Y)
                Vector3 flatDirection = targetPos - casterPos;
                flatDirection.y = 0;
                
                if (flatDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(flatDirection);
                }
                break;
                
            case AbilityDefinition.TargetingType.Area:
                // Para área, seguir el ratón pero limitar por rango
                Vector3 areaDirection = targetPos - casterPos;
                float areaDistance = areaDirection.magnitude;
                
                if (areaDistance > range)
                {
                    // Limitar al rango máximo
                    targetPos = casterPos + areaDirection.normalized * range;
                }
                
                transform.position = targetPos;
                break;
        }
    }
    
    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }
    
    public GameObject GetTargetObject()
    {
        return targetObject;
    }
}