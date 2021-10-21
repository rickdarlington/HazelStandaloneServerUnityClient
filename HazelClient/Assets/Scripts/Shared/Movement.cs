using System.Numerics;

namespace HazelServer.Shared
{
    public class Movement
    {
        //FixedUpdate is bound to 0.01666667 in Unity>Edit>Project Settings>Time>Fixed Timestep

        private static int moveSpeed = 1;
        private static float dt = 0.01666667f;
        
        public static Vector2 ApplyInput(Vector2 position, bool[] input)
        {
            if (input[0]) position.Y += moveSpeed * dt;
            if (input[2]) position.Y -= moveSpeed * dt;

            if (input[1]) position.X -= moveSpeed * dt;
            if (input[3]) position.X += moveSpeed * dt;

            return position;
        }
    }
}