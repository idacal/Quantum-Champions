using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using ExitGames.Client.Photon;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;



    public class PlayerListEntry : MonoBehaviour
    {
        [Header("UI References")]
        public Text PlayerNameText;

        public Image PlayerColorImage;
        public Button PlayerReadyButton;
        public Image PlayerReadyImage;

        private int ownerId;
        private bool isPlayerReady;

        #region UNITY

        public void OnEnable()
        {
            PlayerNumbering.OnPlayerNumberingChanged += OnPlayerNumberingChanged;
        }

        public void Start()
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber != ownerId)
            {
                PlayerReadyButton.gameObject.SetActive(false);
            }
            else
            {
                // Inicializar propiedades del jugador
                Hashtable initialProps = new Hashtable() 
                { 
                    {GameManager.PLAYER_READY, isPlayerReady}
                };
                
                PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
                PhotonNetwork.LocalPlayer.SetScore(0);

                PlayerReadyButton.onClick.AddListener(() =>
                {
                    isPlayerReady = !isPlayerReady;
                    SetPlayerReady(isPlayerReady);

                    Hashtable props = new Hashtable() {{GameManager.PLAYER_READY, isPlayerReady}};
                    PhotonNetwork.LocalPlayer.SetCustomProperties(props);

                    if (PhotonNetwork.IsMasterClient)
                    {
                        // Buscar LobbyMainPanel y notificar cambio de propiedades
                        LobbyMainPanel[] lobbies = FindObjectsOfType<LobbyMainPanel>();
                        if (lobbies != null && lobbies.Length > 0)
                        {
                            lobbies[0].LocalPlayerPropertiesUpdated();
                        }
                    }
                });
            }
        }

        public void OnDisable()
        {
            PlayerNumbering.OnPlayerNumberingChanged -= OnPlayerNumberingChanged;
        }

        #endregion

        public void Initialize(int playerId, string playerName)
        {
            ownerId = playerId;
            PlayerNameText.text = playerName;
        }

        private void OnPlayerNumberingChanged()
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == ownerId)
                {
                    // Obtener color del equipo si está asignado
                    object teamObj;
                    if (p.CustomProperties.TryGetValue(GameManager.PLAYER_TEAM, out teamObj))
                    {
                        int team = (int)teamObj;
                        PlayerColorImage.color = GetTeamColor(team);
                    }
                    else
                    {
                        // Si no hay equipo asignado, usar color por defecto basado en número de jugador
                        PlayerColorImage.color = GetDefaultColor(p.GetPlayerNumber());
                    }
                }
            }
        }

        public void SetPlayerReady(bool playerReady)
        {
            PlayerReadyButton.GetComponentInChildren<Text>().text = playerReady ? "Ready!" : "Ready?";
            PlayerReadyImage.enabled = playerReady;
        }
        
        // Obtener color basado en el equipo asignado (0=azul, 1=rojo)
        private Color GetTeamColor(int teamId)
        {
            return teamId == 0 ? new Color(0.0f, 0.2f, 1.0f) : new Color(1.0f, 0.0f, 0.0f);
        }
        
        // Colores por defecto basados en el número de jugador (para compatibilidad)
        private Color GetDefaultColor(int playerNumber)
        {
            switch(playerNumber % 4)
            {
                case 0: return new Color(0.0f, 0.2f, 1.0f); // Azul
                case 1: return new Color(1.0f, 0.0f, 0.0f); // Rojo
                case 2: return new Color(0.0f, 0.8f, 0.2f); // Verde
                case 3: return new Color(1.0f, 1.0f, 0.0f); // Amarillo
                default: return Color.gray;
            }
        }
    }
