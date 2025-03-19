using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class TeamManager : MonoBehaviourPunCallbacks
{
    public static TeamManager Instance;
    
    private List<PlayerNetwork> teamOnePlayers = new List<PlayerNetwork>();
    private List<PlayerNetwork> teamTwoPlayers = new List<PlayerNetwork>();
    
    // Colores para los equipos
    [SerializeField] private Color teamOneColor = Color.blue;
    [SerializeField] private Color teamTwoColor = Color.red;
    
    public Color TeamOneColor => teamOneColor;
    public Color TeamTwoColor => teamTwoColor;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    public void RegisterPlayer(PlayerNetwork player, int team)
    {
        if (team == 0)
        {
            if (!teamOnePlayers.Contains(player))
            {
                teamOnePlayers.Add(player);
            }
        }
        else
        {
            if (!teamTwoPlayers.Contains(player))
            {
                teamTwoPlayers.Add(player);
            }
        }
    }
    
    public void UnregisterPlayer(PlayerNetwork player, int team)
    {
        if (team == 0)
        {
            teamOnePlayers.Remove(player);
        }
        else
        {
            teamTwoPlayers.Remove(player);
        }
    }
    
    public int[] GetTeamCounts()
    {
        return new int[] { teamOnePlayers.Count, teamTwoPlayers.Count };
    }
    
    public List<PlayerNetwork> GetTeamPlayers(int team)
    {
        return team == 0 ? teamOnePlayers : teamTwoPlayers;
    }
    
    // Para determinar el equipo de un jugador específico
    public int GetPlayerTeam(PlayerNetwork player)
    {
        return teamOnePlayers.Contains(player) ? 0 : 1;
    }
    
    // Comprobar si dos jugadores son aliados
    public bool AreAllies(PlayerNetwork player1, PlayerNetwork player2)
    {
        if (player1 == null || player2 == null) return false;
        
        return GetPlayerTeam(player1) == GetPlayerTeam(player2);
    }
    
    // Comprobar si un jugador es enemigo
    public bool IsEnemy(PlayerNetwork player1, PlayerNetwork player2)
    {
        if (player1 == null || player2 == null) return false;
        
        return GetPlayerTeam(player1) != GetPlayerTeam(player2);
    }
    
    // Obtener el color del equipo
    public Color GetTeamColor(int team)
    {
        return team == 0 ? teamOneColor : teamTwoColor;
    }
}