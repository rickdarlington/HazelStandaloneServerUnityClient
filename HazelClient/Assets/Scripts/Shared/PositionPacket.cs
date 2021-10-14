namespace UnityClient
{
    public class PositionPacket
    {
        public uint playerId;
        public float X;
        public float Y;
        public uint lookDirection;

        public PositionPacket(uint p, float x, float y, uint l)
        {
            playerId = p;
            X = x;
            Y = y;
            lookDirection = l;
        }
    }
}