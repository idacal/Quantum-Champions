using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;

public class HeroSelectionManager : MonoBehaviourPunCallbacks
{
    public static HeroSelectionManager Instance;
    
    [SerializeField] private float selectionTime = 60f;
    [SerializeField] private HeroRegistry heroRegistry;
    
    private Dictionary<int, HeroDefinition> playerSelections = new Dictionary<int, HeroDefinition>();
    private Dictionary<int, bool> playerReadyStatus = new Dictionary<int, bool>();
    
    private float remainingTime;
    private bool selectionFinished = false;
    
    public event System.Action<float> OnTimerUpdated;
    public event System.Action OnSelectionFinished;
    
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
    
    private void Start()
    {
        // Inicializar temporizador
        remainingTime = selectionTime;
        
        // Registrar todos los jugadores
        foreach (var player in PhotonNetwork.PlayerList)
        {
            playerSelections[player.ActorNumber] = null;
            playerReadyStatus[player.ActorNumber] = false;
        }
        
        // Iniciar countdown
        StartCoroutine(CountdownCoroutine());
    }
    
    private IEnumerator CountdownCoroutine()
    {
        while (remainingTime > 0 && !selectionFinished)
        {
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
            
            // Notificar sobre el tiempo restante
            OnTimerUpdated?.Invoke(remainingTime);
            
            // Si somos el master client, sincronizamos el tiempo
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("SyncTimer", RpcTarget.Others, remainingTime);
            }
            
            // Verificar si todos están listos
            CheckAllReady();
        }
        
        // Si se agotó el tiempo, finalizar selección
        if (!selectionFinished)
        {
            FinishSelection();
        }
    }
    
    [PunRPC]
    private void SyncTimer(float time)
    {
        remainingTime = time;
        OnTimerUpdated?.Invoke(remainingTime);
    }
    
    public void SelectHero(HeroDefinition hero)
    {
        if (!PhotonNetwork.IsConnected) return;
        
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        playerSelections[actorNumber] = hero;
        
        // Sincronizar selección con otros jugadores
        if (hero != null)
        {
            photonView.RPC("SyncHeroSelection", RpcTarget.Others, actorNumber, hero.name);
        }
    }
    
    [PunRPC]
    private void SyncHeroSelection(int actorNumber, string heroName)
    {
        // Buscar la definición de héroe por nombre
        HeroDefinition hero = heroRegistry.GetHeroByName(heroName);
        playerSelections[actorNumber] = hero;
    }
    
    public void SetReady(bool ready)
    {
        if (!PhotonNetwork.IsConnected) return;
        
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        playerReadyStatus[actorNumber] = ready;
        
        // Sincronizar estado con otros jugadores
        photonView.RPC("SyncReadyStatus", RpcTarget.Others, actorNumber, ready);
        
        // Verificar si todos están listos
        CheckAllReady();
    }
    
    [PunRPC]
    private void SyncReadyStatus(int actorNumber, bool ready)
    {
        playerReadyStatus[actorNumber] = ready;
        CheckAllReady();
    }
    
    private void CheckAllReady()
    {
        if (selectionFinished) return;
        
        bool allReady = true;
        foreach (var status in playerReadyStatus.Values)
        {
            if (!status)
            {
                allReady = false;
                break;
            }
        }
        
        // Si todos están listos, terminar selección
        if (allReady)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                FinishSelection();
                photonView.RPC("RPCFinishSelection", RpcTarget.Others);
            }
        }
    }
    
    [PunRPC]
    private void RPCFinishSelection()
    {
        FinishSelection();
    }
    
    private void FinishSelection()
    {
        if (selectionFinished) return;
        
        selectionFinished = true;
        
        // Guardar selecciones para usarlas al iniciar el juego
        foreach (var entry in playerSelections)
        {
            int actorNumber = entry.Key;
            HeroDefinition hero = entry.Value;
            
            // Si el jugador no seleccionó, asignar uno aleatorio
            if (hero == null)
            {
                hero = heroRegistry.GetRandomHero();
                playerSelections[actorNumber] = hero;
            }
            
            // Guardar la selección para usarla en el juego
            PlayerPrefs.SetString("SelectedHero_" + actorNumber, hero.heroName);
        }
        
        // Notificar que la selección ha terminado
        OnSelectionFinished?.Invoke();
        
        // Si somos el host, iniciar el juego
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(StartGameAfterDelay(2f));
        }
    }
    
    private IEnumerator StartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        GameManager.Instance.ChangeState(GameManager.GameState.InGame);
    }
    
    public HeroDefinition GetPlayerSelection(int actorNumber)
    {
        if (playerSelections.TryGetValue(actorNumber, out HeroDefinition hero))
        {
            return hero;
        }
        return null;
    }
    
    public bool IsPlayerReady(int actorNumber)
    {
        if (playerReadyStatus.TryGetValue(actorNumber, out bool ready))
        {
            return ready;
        }
        return false;
    }
    
    // Manejar cuando un jugador se une durante la selección
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        playerSelections[newPlayer.ActorNumber] = null;
        playerReadyStatus[newPlayer.ActorNumber] = false;
    }
    
    // Manejar cuando un jugador se va durante la selección
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        playerSelections.Remove(otherPlayer.ActorNumber);
        playerReadyStatus.Remove(otherPlayer.ActorNumber);
        
        // Verificar si todos los jugadores restantes están listos
        CheckAllReady();
    }
}