using UnityEngine;
using UnityEngine.UI;

namespace UnityClient
{
    public class UIMenuBehavior : MonoBehaviour
    {
        [Header("UI")] 
        [SerializeField] private GameObject uiCanvas = null;
        [SerializeField] private InputField playerNameInputField = null;

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

        public void ConnectClicked()
        {
            Debug.Log("clicked connect");
            uiCanvas.SetActive(false);
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