using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityClient.Utilities
{
    public class ConsoleBehaviour : MonoBehaviour
    {
        [SerializeField] private string prefix = string.Empty;
        [SerializeField] private ConsoleCommand[] commands = new ConsoleCommand[0];

        [Header("UI")] 
        [SerializeField] private GameObject uiCanvas = null;
        [SerializeField] private InputField _consoleInputField = null;

        private static ConsoleBehaviour instance;
        private Console _console;

        private Console Console
        {
            get
            {
                if (_console != null)
                {
                    return _console; 
                }

                return _console = new Console(prefix, commands);
            }
        }

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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("pressed tilde");
                Toggle();
            }
        }

        private void Toggle()
        {
            
            if (uiCanvas.activeSelf)
            {
                uiCanvas.SetActive(false);
            }
            else
            {
                uiCanvas.SetActive(true);
                _consoleInputField.ActivateInputField();
            }
        }

        public void ProcessCommand(String inputValue)
        {
            Console.ProcessCommand(inputValue);
            _consoleInputField.text = string.Empty;
        }
    }
}