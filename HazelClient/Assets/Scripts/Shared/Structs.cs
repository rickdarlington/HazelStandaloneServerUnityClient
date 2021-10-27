using System;
using System.Collections.Generic;

namespace HazelServer
{
    public readonly struct PlayerInputStruct
    {
        public readonly uint sequenceNumber;
        public readonly float deltaTime;
        public readonly bool[] inputs;

        public PlayerInputStruct(uint sequenceNumber, bool[] inputs, float deltaTime = 0.01666667f)
        {
            this.sequenceNumber = sequenceNumber;
            this.inputs = inputs;
            this.deltaTime = deltaTime;
        }
    }
    
    public struct GameUpdateStruct 
    {
        public readonly uint updateCount;
        public readonly uint serverTick;
        public readonly List<PositionStruct> positions;
                
        public GameUpdateStruct(uint updateCount, uint serverTick, List<PositionStruct> positions)
        {
            this.updateCount = updateCount;
            this.serverTick = serverTick;
            this.positions = positions;
        }
    }

    public class PositionStruct
    {
        public readonly uint playerId;
        public readonly float X;
        public readonly float Y;
        public readonly uint lookDirection;

        //only used in interpolation
        public long renderTime;
        
        //TODO refactor this.  should only send it to the relevant player
        public readonly uint lastProcessedInput;

        public PositionStruct(uint p, uint seq, float x, float y, uint l)
        {
            playerId = p;
            lastProcessedInput = seq;
            X = x;
            Y = y;
            lookDirection = l;
            renderTime = DateTime.MinValue.Ticks/TimeSpan.TicksPerMillisecond;
        }
    }

    public class ChatMessageStruct
    {
        public readonly uint playerId;
        public readonly string playerName;
        public readonly string message;

        public ChatMessageStruct(uint playerId, string playerName, string message)
        {
            this.playerId = playerId;
            this.playerName = playerName;
            this.message = message;
        }
    }
}