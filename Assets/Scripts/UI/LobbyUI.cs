using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviourPunCallbacks
{
    [Header("Room Creation")]
    [SerializeField] private InputField roomNameInput;
    [SerializeField] private Toggle privateRoomToggle;
    [SerializeField] private Button createRoomButton;
    
    [Header("Room List")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private GameObject roomListItemPrefab;
    
    [Header("Room Panel")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private Text roomNameText;
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private Button startGameButton;
    
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomListItems = new Dictionary<string, GameObject>();
    private Dictionary<int, GameObject> playerListItems = new Dictionary<int, GameObject>();
    
    private void Start()
    {
        // Iniciar conexión a Photon
        EpochNetworkManager.Instance.Connect();
        
        // Configurar botones
        createRoomButton.onClick.AddListener(CreateRoom);
        startGameButton.onClick.AddListener(StartGame);
        
        // Inicialmente el panel de sala está oculto
        roomPanel.SetActive(false);
    }
    
    private void CreateRoom()
    {
        string roomName = string.IsNullOrEmpty(roomNameInput.text) 
            ? "Room " + Random.Range(1000, 10000) 
            : roomNameInput.text;
            
        bool isPrivate = privateRoomToggle.isOn;
        
        EpochNetworkManager.Instance.CreateRoom(roomName, isPrivate);
    }
    
    private void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.HeroSelection);
        }
    }
    
    public void JoinRoom(string roomName)
    {
        EpochNetworkManager.Instance.JoinRoom(roomName);
    }
    
    // Actualiza la lista de salas
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
        UpdateRoomListUI();
    }
    
    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }
    }
    
    private void UpdateRoomListUI()
    {
        // Limpiar items existentes
        foreach (var item in roomListItems.Values)
        {
            Destroy(item);
        }
        roomListItems.Clear();
        
        // Crear nuevos items para cada sala
        foreach (var roomInfo in cachedRoomList.Values)
        {
            if (roomInfo.IsVisible && roomInfo.IsOpen)
            {
                GameObject roomItem = Instantiate(roomListItemPrefab, roomListContent);
                
                // Configurar UI del item
                Text roomNameText = roomItem.transform.Find("RoomNameText").GetComponent<Text>();
                Text playerCountText = roomItem.transform.Find("PlayerCountText").GetComponent<Text>();
                Button joinButton = roomItem.transform.Find("JoinButton").GetComponent<Button>();
                
                roomNameText.text = roomInfo.Name;
                playerCountText.text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";
                joinButton.onClick.AddListener(() => JoinRoom(roomInfo.Name));
                
                roomListItems[roomInfo.Name] = roomItem;
            }
        }
    }
    
    // Cuando se une a una sala
    public override void OnJoinedRoom()
    {
        // Mostrar panel de sala
        roomPanel.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        
        // Actualizar lista de jugadores
        UpdatePlayerList();
        
        // Solo el master client puede iniciar el juego
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }
    
    // Cuando alguien se une o sale de la sala
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }
    
    // Cuando cambia el master client
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }
    
    private void UpdatePlayerList()
    {
        // Limpiar items existentes
        foreach (var item in playerListItems.Values)
        {
            Destroy(item);
        }
        playerListItems.Clear();
        
        // Crear un item para cada jugador en la sala
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject playerItem = Instantiate(playerListItemPrefab, playerListContent);
            
            Text playerNameText = playerItem.transform.Find("PlayerNameText").GetComponent<Text>();
            Text playerReadyText = playerItem.transform.Find("ReadyStatusText").GetComponent<Text>();
            
            playerNameText.text = player.NickName;
            playerReadyText.text = "Not Ready"; // Implementar sistema de Ready más adelante
            
            if (player.IsMasterClient)
            {
                playerNameText.text += " (Host)";
            }
            
            playerListItems[player.ActorNumber] = playerItem;
        }
    }
    
    // Al salir de una sala
    public override void OnLeftRoom()
    {
        roomPanel.SetActive(false);
    }
}