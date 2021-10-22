using HazelServer;
using UnityEngine;

namespace UnityClient
{
    public class EntityMetadata
    {
        public uint playerId { get; private set; } //just in case
        public GameObject gameObject { get; private set; }
        public PositionStruct previousPosition { get; private set; }
        public PositionStruct nextPosition { get; private set; }

        public EntityMetadata(uint pid, GameObject g, PositionStruct previous, PositionStruct next)
        {
            playerId = pid;
            gameObject = g;
            previousPosition = previous;
            nextPosition = next;
        }

        public void updatePositionBuffer(PositionStruct newPos)
        {
            previousPosition = nextPosition;
            nextPosition = newPos;
        }
    }
}