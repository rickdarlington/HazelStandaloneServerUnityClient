using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

namespace UnityClient
{
    public class PlayerInputBehaviour : MonoBehaviour
    {
        private HazelNetworkManager _networkManager;
        
        private void Awake()
        {
            _networkManager = HazelNetworkManager.Instance;    
        }

        private void FixedUpdate()
        {
            if (!_networkManager.LoggedIn)
            {
                return;
            }
            
            if (Keyboard.current.wKey.isPressed)
            {
                Debug.Log("up");
            }
        }
    }
}