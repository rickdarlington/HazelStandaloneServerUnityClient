namespace HazelServer
{
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