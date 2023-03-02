using System.Collections.Generic;
using SpencerDevv.Core;
using SpencerDevv.DataTypes;
using UnityEngine;

namespace SpencerDevv.Player.MovementTypes
{
    public class Interpolator : MonoBehaviour
    {
        [SerializeField] private float timeElapsed = 0f;
        [SerializeField] private float timeToReachTarget = 0.05f;
        [SerializeField] private float movementThreshold = 0.05f;
        
        private readonly List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();
        
        private float squareMovementThreshold;
        private TransformUpdate to;
        private TransformUpdate from;
        private TransformUpdate previous;
        
        private void Start()
        {
            squareMovementThreshold = movementThreshold * movementThreshold;
            
            to = new TransformUpdate(TickManager.Singleton.ServerTick, transform);
            from = new TransformUpdate(TickManager.Singleton.InterpolationTick, transform);
            previous = new TransformUpdate(TickManager.Singleton.InterpolationTick, transform);
        }
        
        private void Update()
        {
            for (var i = 0; i < futureTransformUpdates.Count; i++)
            {
                if (TickManager.Singleton.ServerTick < futureTransformUpdates[i].Tick) continue;
                
                previous = to;
                to = futureTransformUpdates[i];
                from = new TransformUpdate(TickManager.Singleton.InterpolationTick, transform);

                futureTransformUpdates.RemoveAt(i);
                i--;
                timeElapsed = 0f;
                timeToReachTarget = (to.Tick - from.Tick) * Time.fixedDeltaTime;
            }

            timeElapsed += Time.deltaTime;
            InterpolatePosition(timeElapsed / timeToReachTarget);
            InterpolateRotation(timeElapsed / timeToReachTarget);
        }

        private void InterpolatePosition(float lerpAmount)
        {
            if ((to.Position - previous.Position).sqrMagnitude < squareMovementThreshold)
            {
                if (to.Position != from.Position)
                    transform.position = Vector3.Lerp(from.Position, to.Position, lerpAmount);

                return;
            }

            transform.position = Vector3.LerpUnclamped(from.Position, to.Position, lerpAmount);
        }

        private void InterpolateRotation(float lerpAmount)
        {
            if (to.Forward == previous.Forward)
            {
                if (to.Forward != from.Forward)
                    transform.forward = Vector3.Lerp(from.Forward, to.Forward, lerpAmount);

                return;
            }

            transform.forward = Vector3.LerpUnclamped(from.Forward, to.Forward, lerpAmount);
        }

        public void NewUpdate(ushort tick, Vector3 position, Vector3 forward)
        {
            if (tick <= TickManager.Singleton.InterpolationTick)
                return;

            for (var i = 0; i < futureTransformUpdates.Count; i++)
            {
                if (tick >= futureTransformUpdates[i].Tick) continue;
                
                futureTransformUpdates.Insert(i, new TransformUpdate(tick, position, forward));
                return;
            }

            futureTransformUpdates.Add(new TransformUpdate(tick, position, forward));
        }
    }
}