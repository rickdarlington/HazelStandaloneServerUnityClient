using System.Numerics;

namespace UnityClient
{
    public static class Movement
    {
        //FixedUpdate is bound to 0.01666667 in Unity>Edit>Project Settings>Time>Fixed Timestep

        private static float moveSpeed = 1.0f;

        public static Vector2 ApplyInput(float x, float y, bool[] input, float deltatime)
        {
            return ApplyInput(new Vector2(x, y), input, deltatime);
        }

        public static Vector2 ApplyInput(Vector2 position, bool[] input, float deltaTime)
        {
            if (input[0]) position.Y += moveSpeed * deltaTime;
            if (input[2]) position.Y -= moveSpeed * deltaTime;

            if (input[1]) position.X -= moveSpeed * deltaTime;
            if (input[3]) position.X += moveSpeed * deltaTime;

            return position;
        }
    }
}