namespace HazelServer
{
    public class PositionPacket
    {
        public readonly uint playerId;
        public readonly float X;
        public readonly float Y;
        public readonly uint lookDirection;

        public PositionPacket(uint p, float x, float y, uint l)
        {
            playerId = p;
            X = x;
            Y = y;
            lookDirection = l;
        }
    }
}