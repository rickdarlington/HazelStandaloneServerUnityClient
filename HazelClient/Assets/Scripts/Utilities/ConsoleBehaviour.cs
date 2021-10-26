using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityClient.Utilities
{
    public class ConsoleBehaviour : MonoBehaviour
    {
        [SerializeField] private string prefix = string.Empty;

        [Header("UI")] 
        [SerializeField] private TMP_InputField _consoleInputField = null;

        private static ConsoleBehaviour instance;

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

                if (_consoleInputField.isFocused)
                {
                    _consoleInputField.DeactivateInputField();
                }
                else
                {
                    _consoleInputField.Select();
                    _consoleInputField.ActivateInputField();
                }
            }
        }

        public void Send(InputAction.CallbackContext context)
        {
            if (context.action.triggered)
            {
                string text = _consoleInputField.text;
                if (text.StartsWith(prefix))
                {
                    ProcessCommand(text);
                }
                else
                {
                    PlayerChat(text);
                }

                _consoleInputField.text = string.Empty;
            }
        }

        private void ProcessCommand(string text)
        {
            text = text.Remove(0, prefix.Length);
            var command = text.Split()[0];
            var rest = text.Remove(0, command.Length).TrimStart();

            switch (command)
            {
                case "ss":
                case "sendserver":
                    MessageHandler.Instance.SendConsoleToServer(rest);
                    break;
                default:
                    Debug.Log("[ERROR] /invalid command.");
                    break;

            }

        }

        private void PlayerChat(string text)
        {
            MessageHandler.Instance.PlayerChat(text);
        }
    }
}