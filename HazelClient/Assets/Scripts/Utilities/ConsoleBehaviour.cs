using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityClient.Utilities
{
    public class ConsoleBehaviour : MonoBehaviour
    {
        [SerializeField] private string prefix = string.Empty;
        [SerializeField] private ConsoleCommand[] commands = new ConsoleCommand[0];

        [Header("UI")] 
        [SerializeField] private GameObject uiCanvas = null;
        [SerializeField] private TMP_InputField _consoleInputField = null;

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

        //called by input actions (new input system)
        public void Toggle(InputAction.CallbackContext context)
        {
            if (context.action.triggered)
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
        }

        public void ProcessCommand(String inputValue)
        {
            Console.ProcessCommand(inputValue);
            _consoleInputField.text = string.Empty;
        }
    }
}