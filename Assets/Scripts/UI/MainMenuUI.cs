using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject settingsPanel;
    
    private void Start()
    {
        hostButton.onClick.AddListener(OnHostButtonClicked);
        joinButton.onClick.AddListener(OnJoinButtonClicked);
        settingsButton.onClick.AddListener(ToggleSettingsPanel);
        quitButton.onClick.AddListener(OnQuitButtonClicked);
        
        settingsPanel.SetActive(false);
        
        // Desconectar de Photon si estamos conectados
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }
    
    private void OnHostButtonClicked()
    {
        // Establecer nick temporal si no está configurado
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 10000);
        }
        
        // Cambiar a la pantalla de crear sala
        GameManager.Instance.ChangeState(GameManager.GameState.Lobby);
        
        // Al llegar al lobby, el jugador será host
        PlayerPrefs.SetString("PlayerRole", "Host");
    }
    
    private void OnJoinButtonClicked()
    {
        // Establecer nick temporal si no está configurado
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 10000);
        }
        
        // Cambiar a la pantalla de buscar/unirse a sala
        GameManager.Instance.ChangeState(GameManager.GameState.Lobby);
        
        // Al llegar al lobby, el jugador será cliente
        PlayerPrefs.SetString("PlayerRole", "Client");
    }
    
    private void ToggleSettingsPanel()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
    
    private void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}