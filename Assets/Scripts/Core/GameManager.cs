using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

// Resolución de ambigüedad
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    
    public enum GameState
    {
        MainMenu,
        Lobby,
        HeroSelection,
        Loading,
        InGame,
        EndGame
    }
    
    public GameState CurrentState { get; private set; }
    
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string lobbyScene = "Lobby";
    [SerializeField] private string heroSelectionScene = "HeroSelection";
    [SerializeField] private string gameScene = "GameScene";
    
    // Constantes para propiedades de sala
    public const string PLAYER_READY = "PlayerReady";
    public const string PLAYER_TEAM = "PlayerTeam";
    public const string PLAYER_HERO_SELECTION = "PlayerHeroSelection";
    
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
        
        // Asegurarse de que se sincronicen escenas
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        
        Debug.Log($"GameManager: Cambiando de estado {CurrentState} a {newState}");
        
        // Ejecutar lógica de salida del estado actual
        switch (CurrentState)
        {
            case GameState.InGame:
                // Limpiar recursos de juego
                break;
        }
        
        // Cambiar al nuevo estado
        CurrentState = newState;
        
        // Ejecutar lógica de entrada al nuevo estado
        switch (newState)
        {

                
            case GameState.Lobby:
                // Si estamos en la red de Photon, usar PhotonNetwork para cargar escenas
                if (PhotonNetwork.IsConnected)
                {
                    PhotonNetwork.LoadLevel(lobbyScene);
                }
                else
                {
                    SceneManager.LoadScene(lobbyScene);
                }
                break;
                
            case GameState.HeroSelection:
                // Usar PhotonNetwork.LoadLevel para escenas en red
                if (!PhotonNetwork.IsMasterClient) return;
                
                // Cerrar la sala para que no entren más jugadores
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.LoadLevel(heroSelectionScene);
                break;
                
            case GameState.Loading:
                // Mostrar pantalla de carga antes de pasar al juego
                StartCoroutine(ShowLoadingScreen());
                break;
                
            case GameState.InGame:
                if (!PhotonNetwork.IsMasterClient) return;
                PhotonNetwork.LoadLevel(gameScene);
                break;
                
            case GameState.EndGame:
                // Mostrar resultados
                ShowEndGameScreen();
                break;
        }
    }
    
    // Coroutine para mostrar pantalla de carga
    private IEnumerator ShowLoadingScreen()
    {
        // Activar UI de pantalla de carga
        // LoadingUI.Instance.Show();
        
        yield return new WaitForSeconds(1.5f);
        
        // Al terminar la carga, cambiar a estado InGame
        ChangeState(GameState.InGame);
    }
    
    // Método para comenzar el juego después de hero selection
    public void StartGameAfterHeroSelection()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        ChangeState(GameState.Loading);
    }
    
    // Método para volver al lobby desde cualquier escena
    public void ReturnToLobby()
    {
        ChangeState(GameState.Lobby);
    }
    
    // Método para salir del juego
    public void QuitGame()
    {
        Application.Quit();
    }
    
    private void ShowEndGameScreen()
    {
        // Lógica para mostrar pantalla final
        Debug.Log("Mostrando pantalla de fin de juego");
    }
    
    // Asignar equipos automáticamente a los jugadores
    public void AssignTeamsAutomatically()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        Player[] players = PhotonNetwork.PlayerList;
        int teamZeroCount = 0;
        int teamOneCount = 0;
        
        // Contar jugadores ya asignados a equipos
        foreach (Player player in players)
        {
            object teamObj;
            if (player.CustomProperties.TryGetValue(PLAYER_TEAM, out teamObj))
            {
                int team = (int)teamObj;
                if (team == 0)
                    teamZeroCount++;
                else
                    teamOneCount++;
            }
        }
        
        // Asignar equipo a jugadores sin equipo
        foreach (Player player in players)
        {
            if (!player.CustomProperties.ContainsKey(PLAYER_TEAM))
            {
                // Asignar al equipo con menos jugadores
                int team = (teamZeroCount <= teamOneCount) ? 0 : 1;
                
                // Actualizar contadores
                if (team == 0)
                    teamZeroCount++;
                else
                    teamOneCount++;
                
                // Establecer propiedad de equipo
                Hashtable properties = new Hashtable();
                properties.Add(PLAYER_TEAM, team);
                player.SetCustomProperties(properties);
                
                Debug.Log($"Jugador {player.NickName} asignado al equipo {team}");
            }
        }
    }
    
    #region PHOTON CALLBACKS
    
    public override void OnJoinedRoom()
    {
        Debug.Log("GameManager: Unido a sala " + PhotonNetwork.CurrentRoom.Name);
        
        // Si estamos en el estado de Lobby, cambiar la UI para mostrar la sala
        if (CurrentState == GameState.Lobby)
        {
            // El panel de lobby se encarga de esto
        }
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"GameManager: Jugador {newPlayer.NickName} ha entrado a la sala");
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"GameManager: Jugador {otherPlayer.NickName} ha salido de la sala");
    }
    
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"GameManager: Cambio de Master Client a {newMasterClient.NickName}");
    }
    
    public override void OnLeftRoom()
    {
        Debug.Log("GameManager: Has salido de la sala");
        // Si salimos de una sala, volver al lobby
        if (CurrentState != GameState.MainMenu)
        {
            ChangeState(GameState.Lobby);
        }
    }
    
    #endregion
}