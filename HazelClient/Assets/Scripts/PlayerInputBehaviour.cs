using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

namespace UnityClient
{
    public class PlayerInputBehaviour : MonoBehaviour
    {

        private void FixedUpdate()
        {
            //TODO refactor
            HazelNetworkManager _manager = HazelNetworkManager.Instance;

            if (_manager == null || !_manager.LoggedIn)
            {
                return;
            }

            bool[] inputs = new bool[4];
            inputs[0] = Keyboard.current.wKey.isPressed;
            inputs[1] = Keyboard.current.aKey.isPressed;
            inputs[2] = Keyboard.current.sKey.isPressed;
            inputs[3] = Keyboard.current.dKey.isPressed;
            //inputs[4] = Keyboard.current.wKey.isPressed

            if (inputs.Contains(true))
            {
                MessageHandler.Instance.SendReliableInput(inputs);    
            }
        }
    }
}