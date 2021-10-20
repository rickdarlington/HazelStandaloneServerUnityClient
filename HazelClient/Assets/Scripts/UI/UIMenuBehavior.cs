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
        [SerializeField] private TMP_Text connectionStatusField = null;


        public static UIMenuBehavior Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ToggleMenu(InputAction.CallbackContext context)
        {
            if (context.action.triggered)
            {
                //deactivate menu, unless we're not connected
                var isConnected = HazelNetworkManager.Instance.IsConnected();
                if (uiCanvas.activeSelf && isConnected)
                {
                    DeactivateMenu();
                }
                else
                {
                    ActivateMenu();
                }
            }
        }

        private void ActivateMenu()
        {
            uiCanvas.SetActive(true);
            if (HazelNetworkManager.Instance.IsConnected() && HazelNetworkManager.Instance.LoggedIn)
            {
                var netman = HazelNetworkManager.Instance;
                playerNameInputField.interactable = false;
                loginButton.interactable = false;

                connectionStatusField.text = $"Connected to {netman.ServerAddress}:{netman.ServerPort} as {netman.PlayerName}";
            }
            else
            {
                connectionStatusField.text = "Disconnected...";
                playerNameInputField.interactable = true;
                loginButton.interactable = true;
            }
        }

        private void DeactivateMenu()
        {
            uiCanvas.SetActive(false);
        }

        public void ConnectClicked()
        {
            uiCanvas.SetActive(false);
            playerNameInputField.interactable = false;
            loginButton.interactable = false;
            HazelNetworkManager.Instance.ConnectToServer(playerNameInputField.text);
        }

        //FYI this can NOT be called directly, ONLY from the event system (so the main thread) 
        public void ConnectionLost(string message)
        {
            Debug.Log($"[ERROR] disconnected: {message}");
            //TODO tell the player why this failed, don't just debug log.
            ActivateMenu();
        }
    }
}