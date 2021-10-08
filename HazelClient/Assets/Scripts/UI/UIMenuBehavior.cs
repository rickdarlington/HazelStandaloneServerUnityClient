using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UnityClient
{
    public class UIMenuBehavior : MonoBehaviour
    {
        [Header("UI")] 
        [SerializeField] private GameObject uiCanvas = null;
        [SerializeField] private TMP_InputField playerNameInputField = null;
        [SerializeField] private Button loginButton = null;

        public static UIMenuBehavior instance { get; private set; }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Update()
        {
            //this is hacky, we should be processing the eventqueue in some game logic somewhere
            var eventQueue = HazelNetworkManager.instance.eventQueue;
            lock (eventQueue)
            {
                foreach (var action in eventQueue)
                {
                    if (action.Method.Name.StartsWith("UI_"))
                    {
                        
                    }
                }
            }
        }

        public void ToggleMenu(InputAction.CallbackContext context)
        {
            if (context.action.triggered)
            {
                //deactivate menu, unless we're not connected
                var isConnected = HazelNetworkManager.instance.IsConnected();
                if (uiCanvas.activeSelf && isConnected)
                {
                    uiCanvas.SetActive(false);
                }
                else
                {
                    //activate menu
                    uiCanvas.SetActive(true);
                    if (isConnected)
                    {
                        playerNameInputField.interactable = false;
                        loginButton.interactable = false;
                    }
                    else
                    {
                        playerNameInputField.interactable = true;
                        loginButton.interactable = true;
                    }
                }
            }
        }

        public void ConnectClicked()
        {
            uiCanvas.SetActive(false);
            playerNameInputField.interactable = false;
            loginButton.interactable = false;
            HazelNetworkManager.instance.ConnectToServer(playerNameInputField.text);
        }

        public void ConnectionLost(string message)
        {
            try
            {
                Debug.Log($"[ERROR] disconnected: {message}");
                //TODO why doesn't the MenuCanvasBackground become active when we hit here?
                uiCanvas.SetActive(true);
                playerNameInputField.interactable = true;
                loginButton.interactable = true;
            }
            catch (Exception e)
            {
                Debug.Log($"error after connection lost {e.Message}");
            }
        }
    }
}