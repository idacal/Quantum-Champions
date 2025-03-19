using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class EpochNetworkManager : MonoBehaviourPunCallbacks
{
    public static EpochNetworkManager Instance;
    
    [SerializeField] private string gameVersion = "0.1";
    [SerializeField] private byte maxPlayersPerRoom = 10;
    
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        ConnectingToMaster,
        JoiningLobby,
        InLobby,
        JoiningRoom,
        InRoom,
        CreatingRoom
    }
    
    public ConnectionState CurrentState { get; private set; } = ConnectionState.Disconnected;
    
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
    
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    
    public void Connect()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby();
            CurrentState = ConnectionState.JoiningLobby;
        }
        else
        {
            CurrentState = ConnectionState.Connecting;
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = gameVersion;
        }
    }
    
    public void CreateRoom(string roomName, bool isPrivate = false)
    {
        if (!PhotonNetwork.IsConnected) return;
        
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = !isPrivate
        };
        
        CurrentState = ConnectionState.CreatingRoom;
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    
    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnected) return;
        
        CurrentState = ConnectionState.JoiningRoom;
        PhotonNetwork.JoinRoom(roomName);
    }
    
    public void JoinRandomRoom()
    {
        if (!PhotonNetwork.IsConnected) return;
        
        CurrentState = ConnectionState.JoiningRoom;
        PhotonNetwork.JoinRandomRoom();
    }
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server");
        CurrentState = ConnectionState.ConnectingToMaster;
        PhotonNetwork.JoinLobby();
    }
    
    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        CurrentState = ConnectionState.InLobby;
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        CurrentState = ConnectionState.InRoom;
    }
    
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Room creation failed: {message}");
        CurrentState = ConnectionState.InLobby;
    }
    
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join room failed: {message}");
        CurrentState = ConnectionState.InLobby;
    }
    
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join random room failed: {message}");
        CurrentState = ConnectionState.InLobby;
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Disconnected: {cause}");
        CurrentState = ConnectionState.Disconnected;
    }
}