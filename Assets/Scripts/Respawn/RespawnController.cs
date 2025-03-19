using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class RespawnController : MonoBehaviourPunCallbacks
{
    [SerializeField] private float baseRespawnTime = 10f;
    [SerializeField] private float respawnTimeIncreasePerLevel = 2f;
    [SerializeField] private float maxRespawnTime = 60f;
    
    // Lista de puntos de respawn por equipo
    [SerializeField] private List<RespawnPoint> teamOneRespawnPoints = new List<RespawnPoint>();
    [SerializeField] private List<RespawnPoint> teamTwoRespawnPoints = new List<RespawnPoint>();
    
    // Héroes en cola de respawn
    private Dictionary<Hero, float> respawnQueue = new Dictionary<Hero, float>();
    
    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Procesar cola de respawn
        List<Hero> heroesToRespawn = new List<Hero>();
        
        foreach (var pair in respawnQueue)
        {
            Hero hero = pair.Key;
            float respawnTime = pair.Value;
            
            // Actualizar tiempo restante
            respawnQueue[hero] = respawnTime - Time.deltaTime;
            
            // Si es tiempo de respawn, añadir a la lista
            if (respawnQueue[hero] <= 0)
            {
                heroesToRespawn.Add(hero);
            }
        }
        
        // Respawn de héroes
        foreach (Hero hero in heroesToRespawn)
        {
            RespawnHero(hero);
            respawnQueue.Remove(hero);
        }
    }
    
    // Método para poner en cola de respawn
    public void QueueForRespawn(Hero hero)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (hero != null)
        {
            // Calcular tiempo de respawn basado en nivel
            float respawnTime = CalculateRespawnTime(hero.CurrentLevel);
            
            // Añadir a la cola
            respawnQueue[hero] = respawnTime;
            
            // Notificar tiempo de respawn
            photonView.RPC("SyncRespawnTime", RpcTarget.All, hero.photonView.ViewID, respawnTime);
            
            // También podríamos mostrar una notificación UI al jugador
        }
    }
    
    // Método para respawnear a un héroe
    private void RespawnHero(Hero hero)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (hero != null && hero.photonView != null)
        {
            // Determinar punto de respawn según equipo
            Vector3 respawnPosition = GetRespawnPoint(hero);
            
            // Notificar al héroe para respawn
            photonView.RPC("RespawnHeroRPC", hero.photonView.Owner, respawnPosition);
        }
    }
    
    // RPC para sincronizar tiempo de respawn con todos los clientes
    [PunRPC]
    private void SyncRespawnTime(int heroViewID, float respawnTime)
    {
        // Buscar el héroe
        PhotonView heroView = PhotonView.Find(heroViewID);
        if (heroView != null)
        {
            Hero hero = heroView.GetComponent<Hero>();
            if (hero != null)
            {
                // Actualizar UI o mostrar notificación
                // Por ejemplo, si el héroe es del jugador local
                if (heroView.IsMine)
                {
                    // Aquí podríamos actualizar una UI de temporizador
                    // DeathTimerUI.SetTime(respawnTime);
                    
                    // Mostrar notificación
                    NotificationSystem.Instance.ShowNotification($"Reaparecerás en {Mathf.Ceil(respawnTime)} segundos", NotificationType.Warning);
                }
            }
        }
    }
    
    // RPC para respawnear un héroe en un cliente específico
    [PunRPC]
    private void RespawnHeroRPC(Vector3 respawnPosition)
    {
        // Este RPC se llamará en el propietario del héroe
        // Buscar héroe local
        PlayerNetwork localPlayer = PlayerManager.Instance.LocalPlayer;
        if (localPlayer != null)
        {
            Hero hero = localPlayer.GetComponent<Hero>();
            if (hero != null)
            {
                // Respawnear el héroe
                hero.Respawn(respawnPosition);
                
                // Mostrar notificación
                NotificationSystem.Instance.ShowNotification("¡Has reaparecido!", NotificationType.Info);
            }
        }
    }
    
    // Calcular tiempo de respawn basado en nivel
    private float CalculateRespawnTime(int heroLevel)
    {
        float respawnTime = baseRespawnTime + (heroLevel - 1) * respawnTimeIncreasePerLevel;
        return Mathf.Min(respawnTime, maxRespawnTime);
    }
    
    // Obtener un punto de respawn apropiado
    private Vector3 GetRespawnPoint(Hero hero)
    {
        // Determinar equipo
        TeamAssignment teamAssignment = hero.GetComponent<TeamAssignment>();
        if (teamAssignment != null)
        {
            int teamId = teamAssignment.TeamId;
            List<RespawnPoint> respawnPoints = (teamId == 0) ? teamOneRespawnPoints : teamTwoRespawnPoints;
            
            if (respawnPoints.Count > 0)
            {
                // Seleccionar punto de respawn (por ahora aleatorio, podría ser más sofisticado)
                int index = Random.Range(0, respawnPoints.Count);
                RespawnPoint point = respawnPoints[index];
                
                if (point != null)
                {
                    return point.transform.position;
                }
            }
        }
        
        // Punto por defecto (por si acaso)
        return new Vector3(0, 0, 0);
    }
}