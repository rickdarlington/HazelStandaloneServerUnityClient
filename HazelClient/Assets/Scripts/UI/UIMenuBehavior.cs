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
            if (HazelNetworkManager.Instance.IsConnected())
            {
                var netman = HazelNetworkManager.Instance;
                playerNameInputField.interactable = false;
                loginButton.interactable = false;
                Debug.Log(netman.ServerAddress);
                Debug.Log(netman.ServerPort);
                Debug.Log(netman.PlayerName);

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

        public void ConnectionLost(string message)
        {
            Debug.Log($"[ERROR] disconnected: {message}");
            ActivateMenu();
        }
    }
}