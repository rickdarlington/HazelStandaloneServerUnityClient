using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UnityClient
{
    public class UIMenuBehavior : MonoBehaviour
    {
        [Header("UI")] 
        [SerializeField] private GameObject uiCanvas = null;
        [SerializeField] private InputField playerNameInputField = null;
        [SerializeField] private Button loginButton = null;

        public static UIMenuBehavior instance { get; private set; }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;

            DontDestroyOnLoad(gameObject);
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
            Debug.Log("clicked connect");
            uiCanvas.SetActive(false);
            playerNameInputField.interactable = false;
            loginButton.interactable = false;
            HazelNetworkManager.instance.ConnectToServer(playerNameInputField.text);
        }

        public void ConnectionLost(string message)
        {
            Debug.Log($"[ERROR] disconnected: {message}");
            //TODO why doesn't the MenuCanvasBackground become active when we hit here?
            uiCanvas.SetActive(true);
        }
    }
}