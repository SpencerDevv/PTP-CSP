using UnityEngine;

namespace SpencerDevv.DataTypes
{
    public struct InputPayload
    {
        public int tick;
        public bool[] inputs;
        public Vector3 camForward;
    }
    
    public struct StatePayload
    {
        public int tick;
        public Vector3 position;
    }
}