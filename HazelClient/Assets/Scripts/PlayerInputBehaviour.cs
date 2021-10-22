using System;
using System.Collections.Generic;
using System.Linq;
using HazelServer;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = System.Numerics.Vector2;

namespace UnityClient
{
    public class PlayerInputBehaviour : MonoBehaviour
    {
        private uint inputSequenceNumber = 0;
        
        private Queue<PlayerInputStruct> _batchedInputs = new Queue<PlayerInputStruct>();

        private void Update()
        {
            //TODO is there an efficiency hit here?
            var networkManager = HazelNetworkManager.Instance;
            var gameStateManager = GameStateManager.Instance;
            
            if (networkManager == null || !networkManager.LoggedIn)
            {
                return;
            }

            bool[] inputs = new bool[4];
            inputs[0] = Keyboard.current.wKey.isPressed;
            inputs[1] = Keyboard.current.aKey.isPressed;
            inputs[2] = Keyboard.current.sKey.isPressed;
            inputs[3] = Keyboard.current.dKey.isPressed;

            if (inputs.Contains(true))
            {
                _batchedInputs.Enqueue(new PlayerInputStruct(inputSequenceNumber, inputs, Time.deltaTime));
                inputSequenceNumber++;
                
                //apply the input to the player (this is "prediction")
                GameObject player = gameStateManager.getPlayerGameObject(networkManager.PlayerId);
                if (player == null)
                {
                    Debug.Log("WTF did you do?");
                    return;
                }
                Vector2 pos = new Vector2(player.transform.position.x, player.transform.position.y);
                Vector2 newPos = Movement.ApplyInput(pos, inputs, Time.deltaTime);
                player.transform.position = new Vector3(newPos.X, newPos.Y, 0);
            }
        }

        private void FixedUpdate()
        {
            //FixedUpdate is bound to 0.01666667 in Unity>Edit>Project Settings>Time>Fixed Timestep
            int count = _batchedInputs.Count;
            int i = 0;

            PlayerInputStruct[] inputs = new PlayerInputStruct[count];
            while (i < count)
            {
                inputs[i] = _batchedInputs.Dequeue();
                GameStateManager.Instance.SentInputs.Enqueue(inputs[i]);
                i++;
            }
            
            MessageHandler.Instance.SendReliableInput(inputs);
        }
    }
}