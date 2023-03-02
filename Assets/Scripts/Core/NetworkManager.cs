using System;
using System.Linq;
using Riptide;
using Riptide.Utils;
using UnityEngine;
using DisconnectedEventArgs = Riptide.DisconnectedEventArgs;
using SpencerDevv.Player;

namespace SpencerDevv.Core
{
    internal enum MessageId : ushort
    {
        SyncTick = 1,
        SpawnPlayer,
        PlayerMovement,
    }

    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager _singleton;
        public static NetworkManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }

        [SerializeField] private ushort port;
        [SerializeField] private ushort maxConnections;
        
        internal Server Server { get; private set; }
        internal Client Client { get; private set; }
        public bool isHost { get; private set; } = false;

        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject localPlayerPrefab;
        public GameObject PlayerPrefab => playerPrefab;
        public GameObject LocalPlayerPrefab => localPlayerPrefab;
        
        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

            Server = new Server();
            Server.ClientConnected += PlayerAdded;
            Server.RelayFilter = new MessageRelayFilter(typeof(MessageId), MessageId.SyncTick, MessageId.SpawnPlayer, MessageId.PlayerMovement);
            
            Client = new Client();
            Client.Connected += DidConnect;
            Client.ConnectionFailed += FailedToConnect;
            Client.ClientDisconnected += PlayerLeft;
            Client.Disconnected += DidDisconnect;
        }

        private void FixedUpdate()
        {
            if (Server.IsRunning)
                Server.Update();
            
            Client.Update();
        }

        private void OnApplicationQuit()
        {
            Server.Stop();
            Client.Disconnect();
            TickManager.Singleton.Started = false;
        }

        internal void StartHost()
        {
            Server.Start(port, maxConnections);
            Client.Connect($"127.0.0.1:{port}");
            isHost = true;
            TickManager.Singleton.Started = true;
        }
        
        internal void JoinGame(string ipString)
        {
            Client.Connect($"{ipString}:{port}");
            isHost = false;
            TickManager.Singleton.Started = true;
        }
        
        internal void LeaveGame()
        {
            Server.Stop();
            Client.Disconnect();
            TickManager.Singleton.Started = false;
        }

        private void DidConnect(object sender, EventArgs e)
        {
            PlayerManager.Spawn(Client.Id, UIManager.Singleton.Username, Vector3.zero, true);
            TickManager.Singleton.Started = true;
        }
        
        private void DidDisconnect(object sender, DisconnectedEventArgs e)
        {
            foreach (var player in PlayerManager.List.Values)
                Destroy(player.gameObject);

            UIManager.Singleton.BackToMain();
            TickManager.Singleton.Started = false;
        }

        private void FailedToConnect(object sender, ConnectionFailedEventArgs e)
        {
            UIManager.Singleton.BackToMain();
            TickManager.Singleton.Started = false;
        }

        private void PlayerAdded(object sender, ServerConnectedEventArgs e)
        {
            foreach (var player in PlayerManager.List.Values.Where(player => player.ID != e.Client.Id))
            {
                player.SendSpawn(e.Client.Id);
            }
        }
        
        private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
        {
            Destroy(PlayerManager.List[e.Id].gameObject);
        }
    }
}