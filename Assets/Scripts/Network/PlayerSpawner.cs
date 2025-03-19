using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] teamOneSpawnPoints;
    [SerializeField] private Transform[] teamTwoSpawnPoints;
    
    private Dictionary<int, Transform> playerSpawnPoints = new Dictionary<int, Transform>();
    
    private void Start()
    {
        if (!PhotonNetwork.IsConnected) return;
        
        SpawnPlayer();
    }
    
    private void SpawnPlayer()
    {
        // Obtener el héroe seleccionado en la fase de selección
        string selectedHeroName = PlayerPrefs.GetString("SelectedHero_" + PhotonNetwork.LocalPlayer.ActorNumber, string.Empty);
        GameObject heroToSpawn = playerPrefab; // Por defecto
        
        // Si tenemos un héroe seleccionado, buscar su prefab
        if (!string.IsNullOrEmpty(selectedHeroName))
        {
            // Aquí deberías buscar el prefab correcto según el nombre
            // Por ahora usamos el prefab por defecto
        }
        
        // Asignar equipo (simplificado - podría basarse en datos de sala)
        int playerTeam = DetermineTeam();
        
        // Determinar punto de spawn
        Transform spawnPoint = GetSpawnPoint(playerTeam);
        
        // Instanciar el jugador a través de la red
        GameObject playerObj = PhotonNetwork.Instantiate(
            heroToSpawn.name, 
            spawnPoint.position, 
            spawnPoint.rotation
        );
        
        // Configurar datos de equipo
        PlayerNetwork playerNetwork = playerObj.GetComponent<PlayerNetwork>();
        photonView.RPC("SetPlayerTeam", RpcTarget.AllBuffered, playerNetwork.photonView.ViewID, playerTeam);
    }
    
    [PunRPC]
    private void SetPlayerTeam(int viewID, int team)
    {
        PhotonView view = PhotonView.Find(viewID);
        if (view != null)
        {
            // Asignar equipo al jugador
            TeamAssignment teamAssignment = view.GetComponent<TeamAssignment>();
            if (teamAssignment != null)
            {
                teamAssignment.SetTeam(team);
            }
        }
    }
    
    private int DetermineTeam()
    {
        // Lógica para determinar a qué equipo debe ir el jugador
        // Por simplicidad, usamos un enfoque básico de balance
        int[] teamCounts = TeamManager.Instance.GetTeamCounts();
        return teamCounts[0] <= teamCounts[1] ? 0 : 1;
    }
    
    private Transform GetSpawnPoint(int team)
    {
        Transform[] spawnPoints = team == 0 ? teamOneSpawnPoints : teamTwoSpawnPoints;
        
        // Elegir un punto de spawn aleatorio para el equipo
        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index];
    }
}