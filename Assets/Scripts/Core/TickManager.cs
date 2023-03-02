using System;
using Riptide;
using UnityEngine;

namespace SpencerDevv.Core
{
    public class TickManager : MonoBehaviour
    {
        private static TickManager _singleton;
        public static TickManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(TickManager)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }
        
        
        public ushort InterpolationTick { get; private set; }
        
        [SerializeField] private ushort _serverTick;
        public ushort ServerTick
        {
            get => _serverTick;
            private set
            {
                _serverTick = value;
                InterpolationTick = (ushort)(value - TicksBetweenPositionUpdates);
            }
        }
        
        private ushort _ticksBetweenPositionUpdates = 2;
        public ushort TicksBetweenPositionUpdates
        {
            get => _ticksBetweenPositionUpdates;
            private set
            {
                _ticksBetweenPositionUpdates = value;
                InterpolationTick = (ushort)(ServerTick - value);
            }
        }
        
        [SerializeField] private ushort tickDivergenceTolerence = 1;

        public bool Started;
        
        private void Awake()
        {
            Singleton = this;
        }
        
        private void SetTick(ushort serverTick)
        {
            if (Mathf.Abs(ServerTick - serverTick) <= tickDivergenceTolerence) return;
            
            Debug.Log($"Client Tick: {ServerTick} -> {serverTick}");
            ServerTick = serverTick;
        }
        
        private void FixedUpdate()
        {
            if (!Started) return;
            
            if (ServerTick % 200 == 0 && NetworkManager.Singleton.isHost)
                SendSync();
            
            ServerTick++;
        }
        
        #region Messages
        private void SendSync()
        {
            var message = Message.Create(MessageSendMode.Unreliable, MessageId.SyncTick);
            message.AddUShort(ServerTick);
            NetworkManager.Singleton.Server.SendToAll(message, NetworkManager.Singleton.Client.Id);
        }
        
        [MessageHandler((ushort)MessageId.SyncTick)]
        private static void SyncTick(Message message)
        {
            var serverTick  = message.GetUShort();
            Singleton.SetTick(serverTick);
        }
        #endregion
    }
}