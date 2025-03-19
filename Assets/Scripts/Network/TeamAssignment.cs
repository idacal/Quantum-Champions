using Photon.Pun;
using UnityEngine;

public class TeamAssignment : MonoBehaviourPun
{
    [SerializeField] private int teamId = -1; // -1 = sin asignar, 0 = equipo 1, 1 = equipo 2
    [SerializeField] private Renderer[] teamColorRenderers; // Renderers que cambiarán de color según el equipo
    [SerializeField] private Material teamOneMaterial;
    [SerializeField] private Material teamTwoMaterial;
    
    public int TeamId => teamId;
    
    private void Start()
    {
        // Si el equipo ya está asignado, aplicar el color correspondiente
        if (teamId != -1)
        {
            ApplyTeamVisuals();
        }
    }
    
    public void SetTeam(int newTeamId)
    {
        teamId = newTeamId;
        ApplyTeamVisuals();
        
        // Registrar este jugador en el TeamManager
        PlayerNetwork playerNetwork = GetComponent<PlayerNetwork>();
        if (playerNetwork != null)
        {
            TeamManager.Instance.RegisterPlayer(playerNetwork, teamId);
        }
    }
    
    private void ApplyTeamVisuals()
    {
        // Cambiar materiales según el equipo
        Material teamMaterial = teamId == 0 ? teamOneMaterial : teamTwoMaterial;
        
        if (teamMaterial != null)
        {
            foreach (Renderer renderer in teamColorRenderers)
            {
                if (renderer != null)
                {
                    // Guardar una copia del material para no modificar el original compartido
                    renderer.material = teamMaterial;
                }
            }
        }
        
        // Alternativamente, podemos usar colores en lugar de materiales
        Color teamColor = TeamManager.Instance.GetTeamColor(teamId);
        foreach (Renderer renderer in teamColorRenderers)
        {
            if (renderer != null)
            {
                // Aplicar color al material existente
                renderer.material.color = teamColor;
            }
        }
    }
    
    private void OnDestroy()
    {
        // Al destruir el objeto, desregistrar del TeamManager
        PlayerNetwork playerNetwork = GetComponent<PlayerNetwork>();
        if (playerNetwork != null && teamId != -1 && TeamManager.Instance != null)
        {
            TeamManager.Instance.UnregisterPlayer(playerNetwork, teamId);
        }
    }
}