using System;
using System.Collections.Generic;
using HazelServer;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace UnityClient.Utilities
{
    public class ConsoleBehaviour : MonoBehaviour
    {
        [SerializeField] private string prefix = string.Empty;

        [Header("UI")] 
        [SerializeField] private TMP_InputField _consoleInputField = null;
        [SerializeField] private TMP_Text _consoleText = null;

        // Unity gets very grumpy if you start messing with GameObjects on threads
        // other than the main one. So while sending/receiving messages can be multithreaded,
        // we need a queue to hold events until a Update/FixedUpdate method can handle them.
        public readonly Queue<ChatMessageStruct> messageQueue = new Queue<ChatMessageStruct>();
        
        public static ConsoleBehaviour Instance => instance;
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

        private void Update()
        {
            //grab messages from queue and display
            int num = messageQueue.Count;

            for (int i = 0; i < num; i++)
            {
                ChatMessageStruct msg = messageQueue.Dequeue();
                Debug.Log($"[TRACE] incoming chat message: {msg.message}");
                _consoleText.text += $"[{msg.playerName}] {msg.message}\n";
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
            MessageHandler.Instance.SendPlayerChat(text);
        }

        public void ReceiveChat(uint playerId, string playerName, string text)
        {
            messageQueue.Enqueue(new ChatMessageStruct(playerId, playerName, text));
        }
    }
}