using System.Collections.Generic;

namespace HazelServer
{
    public struct PlayerInputStruct
    {
        public uint sequenceNumber;
        public float dt;
        public bool[] inputs;

        public PlayerInputStruct(uint sequence, bool[] ins, float deltaTime = 0.01666667f)
        {
            sequenceNumber = sequence;
            dt = deltaTime;
            inputs = ins;
        }
    }
    
    public struct GameUpdateStruct 
    {
        public uint updateCount;
        public uint serverTick;
        public List<PositionStruct> positions;
                
        public GameUpdateStruct(uint count, uint tick, List<PositionStruct> pos)
        {
            updateCount = count;
            serverTick = tick;
            positions = pos;
        }
    }

    public struct PositionStruct
    {
        public readonly uint playerId;
        public readonly float X;
        public readonly float Y;
        public readonly uint lookDirection;

        //TODO refactor this.  should only send it to the relevant player
        public readonly uint lastProcessedInput;

        public PositionStruct(uint p, uint seq, float x, float y, uint l)
        {
            playerId = p;
            lastProcessedInput = seq;
            X = x;
            Y = y;
            lookDirection = l;
        }
    }
}