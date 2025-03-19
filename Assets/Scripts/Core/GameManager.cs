using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        
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
            case GameState.MainMenu:
                SceneManager.LoadScene(mainMenuScene);
                break;
                
            case GameState.Lobby:
                SceneManager.LoadScene(lobbyScene);
                break;
                
            case GameState.HeroSelection:
                SceneManager.LoadScene(heroSelectionScene);
                break;
                
            case GameState.Loading:
                // Mostrar pantalla de carga antes de pasar al juego
                break;
                
            case GameState.InGame:
                PhotonNetwork.LoadLevel(gameScene);
                break;
                
            case GameState.EndGame:
                // Mostrar resultados
                break;
        }
    }
    
    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        ChangeState(GameState.Loading);
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        
        // Al terminar la carga, cambiar a estado InGame
        ChangeState(GameState.InGame);
    }
    
    public void ReturnToLobby()
    {
        ChangeState(GameState.Lobby);
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
}