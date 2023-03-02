using System;
using SpencerDevv.Core;
using SpencerDevv.DataTypes;
using UnityEngine;

namespace SpencerDevv.Player.MovementTypes
{
    [RequireComponent(typeof(CharacterController))]
    public class ClientSidePredictor : MonoBehaviour
    {
        private const int BUFFER_SIZE = 1024;
        
        private InputPayload[] inputBuffer;
        private StatePayload[] stateBuffer;
        private StatePayload latestServerState;
        private StatePayload lastProcessedState;
        
        [SerializeField] private float gravity;
        [SerializeField] private CharacterController controller;
        [SerializeField] private float movementSpeed;
        [SerializeField] private Transform camProxy;
        [SerializeField] private float jumpHeight;
        
        private float gravityAcceleration;
        private float moveSpeed;
        private float jumpSpeed;
        private float yVelocity;

        private void OnValidate()
        {
            if (controller == null)
                controller = GetComponent<CharacterController>();
            
            Initialize();
        }

        private void Initialize()
        {
            gravityAcceleration = gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
            moveSpeed = movementSpeed * Time.fixedDeltaTime;
            jumpSpeed = Mathf.Sqrt(jumpHeight * -2f * gravityAcceleration);
        }

        private void Start()
        {
            inputBuffer = new InputPayload[BUFFER_SIZE];
            stateBuffer = new StatePayload[BUFFER_SIZE];
            
            for (var index = stateBuffer.Length - 1; index >= 0; index--)
            {
                var payload = stateBuffer[index];
                payload.position = transform.position;
            }
            
            Initialize();
        }

        private Vector3 FlattenVector3(Vector3 vector)
        {
            vector.y = 0;
            return vector;
        }
        
        public void NewUpdate(ushort tick, Vector3 position)
        {
            latestServerState = new StatePayload
            {
                tick = tick,
                position = position
            };
        }
        
        public void SetInputs(ushort tick, bool[] inputs, Vector3 camForward)
        {
            if (!latestServerState.Equals(default(StatePayload)) &&
                (lastProcessedState.Equals(default(StatePayload)) ||
                 !latestServerState.Equals(lastProcessedState)))
            {
                HandleServerReconciliation();
            }
            
            var bufferIndex = tick % BUFFER_SIZE;
            
            var inputPayload = new InputPayload
            {
                tick = tick,
                inputs = inputs,
                camForward = camForward
            };
            
            inputBuffer[bufferIndex] = inputPayload;
            stateBuffer[bufferIndex] = ProcessMovement(inputPayload);
        }

        private StatePayload ProcessMovement(InputPayload inputPayload)
        {
            var inputDirection = Vector2.zero;
            if (inputPayload.inputs[0])
                inputDirection.y += 1;
            if (inputPayload.inputs[1])
                inputDirection.y -= 1;
            if (inputPayload.inputs[2])
                inputDirection.x -= 1;
            if (inputPayload.inputs[3])
                inputDirection.x += 1;

            var moveDirection = Vector3.Normalize(camProxy.right * inputDirection.x + Vector3.Normalize(FlattenVector3(camProxy.forward)) * inputDirection.y);
            moveDirection *= moveSpeed;

            if (controller.isGrounded)
            {
                yVelocity = 0f;
                if (inputPayload.inputs[4])
                {
                    yVelocity = jumpSpeed;
                }
            }
            yVelocity += gravityAcceleration;
            moveDirection.y = yVelocity;
            controller.Move(moveDirection);

            return new StatePayload
            {
                tick = inputPayload.tick,
                position = transform.position
            };
        }

        private void HandleServerReconciliation()
        {
            lastProcessedState = latestServerState;
            
            var serverStateBufferIndex = latestServerState.tick % BUFFER_SIZE;
            var positionError = Vector3.Distance(latestServerState.position, stateBuffer[serverStateBufferIndex].position);
            if (!(positionError > 0.001f)) return;
            
            Debug.Log("-- RECONCILIATION --");
            Debug.Log($"Position Error: {positionError}");
            Debug.Log($"Server Tick: {latestServerState.tick}");
            Debug.Log($"Client Tick: {TickManager.Singleton.ServerTick}");
            Debug.Log("--------------------");
            
            transform.position = latestServerState.position;
            stateBuffer[serverStateBufferIndex] = latestServerState;

            var ticksToProcess = latestServerState.tick + 1;
            while (ticksToProcess < TickManager.Singleton.ServerTick)
            {
                var bufferIndex = ticksToProcess % BUFFER_SIZE;

                var statePayload = ProcessMovement(inputBuffer[bufferIndex]);
                stateBuffer[bufferIndex] = statePayload;
                    
                ticksToProcess++;
            }
        }
    }
}