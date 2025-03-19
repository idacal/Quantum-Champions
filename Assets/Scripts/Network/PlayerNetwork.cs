using Photon.Pun;
using UnityEngine;

public class PlayerNetwork : MonoBehaviourPun, IPunObservable
{
    private PlayerController playerController;
    private Hero heroComponent;
    
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        heroComponent = GetComponent<Hero>();
    }
    
    private void Start()
    {
        if (photonView.IsMine)
        {
            // Configurar este jugador como controlable localmente
            playerController.enabled = true;
            // Registrar este jugador localmente
            PlayerManager.Instance.RegisterLocalPlayer(this);
        }
        else
        {
            // Desactivar el control local para jugadores remotos
            playerController.enabled = false;
        }
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Datos a enviar a otros jugadores
            stream.SendNext(heroComponent.CurrentHealth);
            stream.SendNext(heroComponent.CurrentMana);
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // Datos recibidos de otros jugadores
            heroComponent.CurrentHealth = (float)stream.ReceiveNext();
            heroComponent.CurrentMana = (float)stream.ReceiveNext();
            
            // Para una interpolación suave, puedes usar:
            Vector3 networkPosition = (Vector3)stream.ReceiveNext();
            Quaternion networkRotation = (Quaternion)stream.ReceiveNext();
            
            // Implementar lógica de interpolación aquí
            transform.position = networkPosition;
            transform.rotation = networkRotation;
        }
    }
    
    // Método para enviar RPC (llamadas a procedimiento remoto)
    [PunRPC]
    public void UseAbility(int abilityIndex, Vector3 targetPosition, int targetNetworkId = -1)
    {
        heroComponent.ActivateAbility(abilityIndex, targetPosition, targetNetworkId);
    }
    
    // Usa este método para llamar a la habilidad localmente y sincronizarla
    public void CallAbilityRPC(int abilityIndex, Vector3 targetPosition, int targetNetworkId = -1)
    {
        if (photonView.IsMine)
        {
            photonView.RPC("UseAbility", RpcTarget.All, abilityIndex, targetPosition, targetNetworkId);
        }
    }
    
    // RPC para sincronizar daño
    [PunRPC]
    public void TakeDamageRPC(float damage, int attackerViewID)
    {
        // Buscar el atacante si se proporcionó un ID
        Hero attacker = null;
        if (attackerViewID >= 0)
        {
            PhotonView attackerView = PhotonView.Find(attackerViewID);
            if (attackerView != null)
            {
                attacker = attackerView.GetComponent<Hero>();
            }
        }
        
        // Aplicar daño al héroe
        if (heroComponent != null)
        {
            heroComponent.TakeDamage(damage, attacker);
        }
    }
    
    // Método para aplicar daño a través de la red
    public void ApplyDamage(float damage, Hero attacker = null)
    {
        int attackerID = -1;
        if (attacker != null && attacker.photonView != null)
        {
            attackerID = attacker.photonView.ViewID;
        }
        
        photonView.RPC("TakeDamageRPC", RpcTarget.All, damage, attackerID);
    }
}