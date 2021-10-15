using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

namespace UnityClient
{
    public class PlayerInputBehaviour : MonoBehaviour
    {

        private void FixedUpdate()
        {
            bool[] inputs = new bool[4];
            inputs[0] = Keyboard.current.wKey.isPressed;
            inputs[1] = Keyboard.current.aKey.isPressed;
            inputs[2] = Keyboard.current.sKey.isPressed;
            inputs[3] = Keyboard.current.dKey.isPressed;
            //inputs[4] = Keyboard.current.wKey.isPressed
            
            //_messageHandler.SendReliableInput(inputs);            
        }
    }
}