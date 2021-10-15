namespace UnityClient
{
    public struct PositionStruct
    {
        public readonly uint playerId;
        public readonly float X;
        public readonly float Y;
        public readonly uint lookDirection;

        public PositionStruct(uint p, float x, float y, uint l)
        {
            playerId = p;
            X = x;
            Y = y;
            lookDirection = l;
        }
    }
}