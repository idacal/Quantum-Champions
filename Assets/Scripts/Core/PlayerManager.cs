using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public static PlayerManager Instance;
    
    // Referencia al jugador local
    public PlayerNetwork LocalPlayer { get; private set; }
    
    // Lista de todos los jugadores en el juego
    private Dictionary<int, PlayerNetwork> activePlayers = new Dictionary<int, PlayerNetwork>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    // Registrar el jugador local
    public void RegisterLocalPlayer(PlayerNetwork player)
    {
        LocalPlayer = player;
        RegisterPlayer(player);
    }
    
    // Registrar cualquier jugador
    public void RegisterPlayer(PlayerNetwork player)
    {
        if (player == null || player.photonView == null) return;
        
        int viewID = player.photonView.ViewID;
        if (!activePlayers.ContainsKey(viewID))
        {
            activePlayers.Add(viewID, player);
            Debug.Log($"Jugador registrado: {viewID}");
        }
    }
    
    // Eliminar un jugador del registro
    public void UnregisterPlayer(PlayerNetwork player)
    {
        if (player == null || player.photonView == null) return;
        
        int viewID = player.photonView.ViewID;
        if (activePlayers.ContainsKey(viewID))
        {
            activePlayers.Remove(viewID);
            Debug.Log($"Jugador eliminado: {viewID}");
            
            // Si era el jugador local, limpiar referencia
            if (LocalPlayer == player)
            {
                LocalPlayer = null;
            }
        }
    }
    
    // Obtener un jugador por su ID de vista
    public PlayerNetwork GetPlayerByViewID(int viewID)
    {
        if (activePlayers.TryGetValue(viewID, out PlayerNetwork player))
        {
            return player;
        }
        return null;
    }
    
    // Obtener todos los jugadores activos
    public List<PlayerNetwork> GetAllPlayers()
    {
        return new List<PlayerNetwork>(activePlayers.Values);
    }
    
    // Evento cuando un jugador deja la sala
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // Nota: Es posible que necesitemos limpiar los objetos del jugador que se fue
        Debug.Log($"Jugador salió de la sala: {otherPlayer.NickName}");
    }
}