using Riptide;
using SpencerDevv.Core;
using SpencerDevv.Player.MovementTypes;
using UnityEngine;

namespace SpencerDevv.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerManager player;
        [SerializeField] private Transform camTransform;
        [SerializeField] private ClientSidePredictor predictor;
        private bool[] inputs;
        
        private void Start()
        {
            inputs = new bool[5];
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.W))
                inputs[0] = true;

            if (Input.GetKey(KeyCode.S))
                inputs[1] = true;

            if (Input.GetKey(KeyCode.A))
                inputs[2] = true;

            if (Input.GetKey(KeyCode.D))
                inputs[3] = true;

            if (Input.GetKey(KeyCode.Space))
                inputs[4] = true;
        }
        
        private void FixedUpdate()
        {
            predictor.SetInputs(TickManager.Singleton.ServerTick, inputs, camTransform.forward);
            SendInput();

            for (var i = 0; i < inputs.Length; i++)
            {
                inputs[i] = false;
            }
        }

        #region Messages
        private void SendInput()
        {
            var message = Message.Create(MessageSendMode.Unreliable, MessageId.PlayerMovement);
            message.AddUShort(player.ID);
            message.AddUShort(TickManager.Singleton.ServerTick);
            message.AddVector3(transform.position);
            message.AddVector3(transform.forward);
            NetworkManager.Singleton.Client.Send(message);
        }
        #endregion
    }
}