using System.Collections.Generic;

namespace HazelServer
{
    public readonly struct PlayerInputStruct
    {
        public readonly uint sequenceNumber;
        public readonly float dt;
        public readonly bool[] inputs;

        public PlayerInputStruct(uint sequence, bool[] ins, float deltaTime = 0.01666667f)
        {
            sequenceNumber = sequence;
            dt = deltaTime;
            inputs = ins;
        }
    }
    
    public readonly struct GameUpdateStruct 
    {
        public readonly uint updateCount;
        public readonly uint serverTick;
        public readonly List<PositionStruct> positions;
                
        public GameUpdateStruct(uint count, uint tick, List<PositionStruct> pos)
        {
            updateCount = count;
            serverTick = tick;
            positions = pos;
        }
    }

    public readonly struct PositionStruct
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