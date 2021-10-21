using System;
using System.Collections.Generic;
using Hazel;
using HazelServer;
using UnityEngine;

namespace UnityClient
{
    public class MessageHandler
    {
        private readonly HazelNetworkManager _networkManager;

        private static MessageHandler instance = null;
        
        private MessageHandler()
        {
            _networkManager = HazelNetworkManager.Instance;
        }

        public static MessageHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MessageHandler();
                }

                return instance;
            }
        }
        
        public void HandleMessage(DataReceivedEventArgs obj)
        {
            MessageTags tag = MessageTags.None;
            try
            {
                while (obj.Message.Position < obj.Message.Length)
                {
                    // Remember from the server code that sub-messages aren't pooled,
                    // they share the parent message's buffer. So don't recycle them!
                    var msg = obj.Message.ReadMessage();
                    tag = (MessageTags)msg.Tag;
                    
                    switch (tag)
                    {
                        case MessageTags.ServerInit:
                            Debug.Log($"[TRACE] HandleMessage:serverInit");
                            ServerInitResponse(msg);
                            break;
                        case MessageTags.LoginFailed:
                            Debug.Log($"[TRACE] HandleMessage:loginFailed");
                            ServerLoginFailure(msg.ReadString());
                            break;
                        case MessageTags.LoginSuccess:
                            Debug.Log($"[TRACE] HandleMessage:LoginSuccess");
                            ServerLoginResponse();
                            break;
                        case MessageTags.ServerMessage:
                            if (_networkManager.LoggedIn) HandleServerMessage(msg);
                            break;
                        case MessageTags.GameData:
                            if (_networkManager.LoggedIn) ReceiveGameData(msg);
                            break;
                        default:
                            Debug.Log($"[DEBUG] unhandled message type [{msg.Tag}]");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[EXCEPTION] exception in MessageHandler.HandleMessage: {e.Message} for messageType {tag}");
            }
            finally
            {
                //TODO do we need cleanup here?
            }
        }

        private void ServerInitResponse(MessageReader reader)
        {
            uint myId = reader.ReadUInt32();
            Debug.Log($"[INFO] connected to server with player id: {myId}");
            HazelNetworkManager.Instance.PlayerId = myId;

            //TODO this is where you want to send your login information
            Debug.Log($"[DEBUG] sending log in message for {_networkManager.PlayerName}");
            Send(SendOption.Reliable, MessageTags.LogIn, _networkManager.PlayerName);
        }

        private void ServerLoginResponse()
        {
            Debug.Log($"[INFO] Login success");
            _networkManager.LoggedIn = true;
        }

        private void ServerLoginFailure(String errorMessage)
        {
            Debug.Log($"[ERROR] login failed with error: {errorMessage}");
            _networkManager.LoggedIn = false;
            _networkManager.AddEvent(_networkManager.serverDisconnectedAction);
        }

        private void HandleServerMessage(MessageReader msg)
        {
            Debug.Log($"Received Server Message: {msg.ReadString()}");
        }

        private void ReceiveGameData(MessageReader msg)
        {
            var updates = msg.ReadPackedUInt32();
            var serverTick = msg.ReadPackedUInt32();
            List<PositionStruct> positions = new List<PositionStruct>();
            
            var i = 0;
            while (i < updates+1)
            {
                PositionStruct pos = new PositionStruct(msg.ReadPackedUInt32(), msg.ReadPackedUInt32(), msg.ReadSingle(), msg.ReadSingle(),
                    msg.ReadPackedUInt32());

                positions.Add(pos);
                i++;
            }

            var update = new GameUpdateStruct(updates, serverTick, positions);

            GameStateManager.Instance.GameUpdates.Enqueue(update);
        }
        
        public void SendConsoleToServer(string message)
        {
            if (!_networkManager.IsConnected())
            {
                Debug.Log("[ERROR] you can't send commands to the server if you're not connected");
                return;
            }

            if (message.Length == 0)
            {
                return;
            }
            
            Debug.Log($"[DEBUG] sending console message to server: \"{message}\"");
            Send(SendOption.Reliable, MessageTags.ConsoleMessage, message);
        }

        public void PlayerChat(string message)
        {
            if (!_networkManager.IsConnected())
            {
                Debug.Log("[ERROR] you can't chat if you're not connected");
                return;
            }
            
            Send(SendOption.Reliable, MessageTags.PlayerChat, message);
        }

        private void Send(SendOption option, MessageTags tag, string message = null)
        {
            var msg = MessageWriter.Get(option);
            msg.StartMessage((byte)tag);
            if (message != null)
            {
                msg.Write(message);
            }

            msg.EndMessage();

            try
            {
                while (_networkManager.ConnectInProgress)
                {
                    //TODO FIXME
                    //wait while _connectInProgress is true.
                    // There's a race condition here such that you might try to call _connection.Send
                    // before the connection is actually ready.  WTF.
                    Debug.Log($"[WARNING] waiting for connection to complete before sending login info");
                }
                _networkManager.Connection.Send(msg);
            }
            catch(Exception e)
            {
                Debug.Log($"[ERROR] Caught exception in SendReliable for connection {_networkManager.Connection.EndPoint.Address}");
                Debug.Log($"[EXCEPTION] {e.Message}");
            }
            msg.Recycle();
        }

        public void SendReliableInput(uint inputSequenceNumber, bool[] input)
        {
            if (!_networkManager.LoggedIn)
            {
                return;
            }
            
            var msg = MessageWriter.Get(SendOption.Reliable);
            msg.StartMessage((byte)MessageTags.PlayerInput);
            msg.WritePacked(inputSequenceNumber);
            msg.Write(input[0]);
            msg.Write(input[1]);
            msg.Write(input[2]);
            msg.Write(input[3]);
            msg.EndMessage();

            try
            {
                _networkManager.Connection.Send(msg);
            }
            catch(Exception e)
            {
                Debug.Log($"[ERROR] Caught exception in SendInput");
                Debug.Log($"[EXCEPTION] {e.Message}");
            }
            msg.Recycle();
        }
    }
}