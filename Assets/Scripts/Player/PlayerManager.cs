using System;
using System.Collections.Generic;
using Riptide;
using SpencerDevv.Core;
using SpencerDevv.Player.MovementTypes;
using UnityEngine;

namespace SpencerDevv.Player
{
    public class PlayerManager : MonoBehaviour
    {
        internal static readonly Dictionary<ushort, PlayerManager> List = new Dictionary<ushort, PlayerManager>();

        internal ushort ID;
        internal string UserName;
        internal bool IsLocal;

        [SerializeField] private Interpolator Interpolator;
        [SerializeField] private ClientSidePredictor predictor;
        
        private void OnDestroy()
        {
            List.Remove(ID);
        }

        private void Move(ushort tick, Vector3 newPosition, Vector3 forward)
        {
            if (IsLocal)
            {
                predictor.NewUpdate(tick, newPosition);
                return;
            }
            
            Interpolator.NewUpdate(tick, newPosition, forward);
        }
        
        internal static void Spawn(ushort _ID, string _UserName, Vector3 _position, bool shouldSendSpawn = false)
        {
            var isLocal = _ID == NetworkManager.Singleton.Client.Id;
            var gameObject = isLocal ? Instantiate(NetworkManager.Singleton.LocalPlayerPrefab, _position, Quaternion.identity) : Instantiate(NetworkManager.Singleton.PlayerPrefab, _position, Quaternion.identity);
            var player = gameObject.GetComponent<PlayerManager>();
            
            player.ID = _ID;
            player.UserName = _UserName;
            player.IsLocal = isLocal;
            player.name = $"Player {_ID} ({_UserName})";
            
            List.Add(_ID, player);
            if (shouldSendSpawn)
                player.SendSpawn();
        }

        #region Messages
        private void SendSpawn()
        {
            var message = Message.Create(MessageSendMode.Reliable, MessageId.SpawnPlayer);
            message.AddUShort(ID);
            message.AddString(UserName);
            message.AddVector3(transform.position);
            NetworkManager.Singleton.Client.Send(message);
        }
        
        [MessageHandler((ushort)MessageId.SpawnPlayer)]
        private static void SpawnPlayer(Message message)
        {
            var id = message.GetUShort();
            var userName = message.GetString();
            var position = message.GetVector3();
            
            Spawn(id, userName, position);
        }
        
        internal void SendSpawn(ushort newPlayerId)
        {
            var message = Message.Create(MessageSendMode.Reliable, MessageId.SpawnPlayer);
            message.AddUShort(ID);
            message.AddString(UserName);
            message.AddVector3(transform.position);
            NetworkManager.Singleton.Server.Send(message, newPlayerId);
        }

        [MessageHandler((ushort)MessageId.PlayerMovement)]
        private static void PlayerMovement(Message message)
        {
            var playerId = message.GetUShort();
            var tick = message.GetUShort();
            var position = message.GetVector3();
            var forward = message.GetVector3();
            
            if (List.TryGetValue(playerId, out var player))
                player.Move(tick, position, forward);
        }
        #endregion
    }
}