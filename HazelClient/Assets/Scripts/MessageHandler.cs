using System;
using Hazel;
using UnityEditor.VersionControl;
using UnityEngine;

namespace UnityClient
{
    public class MessageHandler
    {
        private HazelNetworkManager _networkManager;

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
            try
            {
                while (obj.Message.Position < obj.Message.Length)
                {
                    // Remember from the server code that sub-messages aren't pooled,
                    // they share the parent message's buffer. So don't recycle them!
                    var msg = obj.Message.ReadMessage();

                    Debug.Log($"[TRACE] message type [{(MessageTags)msg.Tag}]");
                    switch ((MessageTags)msg.Tag)
                    {
                        case MessageTags.ServerInit:
                            ServerInitResponse(msg);
                            break;
                        case MessageTags.LoginFailed:
                            ServerLoginFailure(msg);
                            break;
                        case MessageTags.LoginSuccess:
                            ServerLoginResponse(msg);
                            break;
                        case MessageTags.ServerMessage:
                            HandleServerMessage(msg);
                            break;

                        //TODO implement the rest eg:
                        //case PlayerMessageTags.PlayerJoined:
                        //case PlayerMessageTags.GameData (etc)

                        default:
                            Debug.Log($"[DEBUG] unhandled message type [{msg.Tag}]");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[EXEPTION] exception in MessageHandler.HandleMessage: {e.Message}");
            }
            finally
            {
                //TODO do we need cleanup here?
            }
        }

        //TODO make some cleaner "send message to server" methods and cleanup below duplication
        private void ServerInitResponse(MessageReader reader)
        {
            int myId = reader.ReadInt32();
            Debug.Log($"[INFO] connected to server with player id: {myId}");

            //TODO this is where you want to send your login information
            Debug.Log($"[DEBUG] sending log in message for {_networkManager.PlayerName}");
            var msg = MessageWriter.Get(SendOption.Reliable);
            msg.StartMessage((byte)MessageTags.LogIn);
            msg.Write(_networkManager.PlayerName);
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
                Debug.Log($"[ERROR] Caught exception in LogIn for connection {_networkManager.Connection.EndPoint.Address}");
                Debug.Log($"[EXCEPTION] {e.Message}");
            }
            msg.Recycle();
        }

        private void ServerLoginResponse(MessageReader msg)
        {
            Debug.Log($"[INFO] Login success");
            _networkManager.LoggedIn = true;
        }

        private void ServerLoginFailure(MessageReader msg)
        {
            Debug.Log($"[ERROR] login failed with error: {msg.ReadString()}");
            _networkManager.LoggedIn = false;
            //TODO boot to login screen
            UIMenuBehavior.instance.ConnectionLost(msg.ReadString());
        }

        private void HandleServerMessage(MessageReader msg)
        {
            Debug.Log($"Received Server Message: {msg.ReadString()}");
        }
        
        //TODO extract below methods to appropriate class
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
            var msg = MessageWriter.Get(SendOption.Reliable);
            msg.StartMessage((byte)MessageTags.ConsoleMessage);
            msg.Write(message);
            
            msg.EndMessage();

            try
            {
                _networkManager.Connection.Send(msg);
            }
            catch(Exception e)
            {
                Debug.Log($"[ERROR] Caught exception in console message send");
                Debug.Log($"[EXCEPTION] {e.Message}");
            }
            msg.Recycle();
        }

        public void PlayerChat(string message)
        {
            if (!_networkManager.IsConnected())
            {
                Debug.Log("[ERROR] you can't chat if you're not connected");
                return;
            }
            
            var msg = MessageWriter.Get(SendOption.Reliable);
            msg.StartMessage((byte)MessageTags.PlayerChat);
            msg.Write(message);
            
            msg.EndMessage();

            try
            {
                _networkManager.Connection.Send(msg);
            }
            catch(Exception e)
            {
                Debug.Log($"[ERROR] Caught exception in chat message send");
                Debug.Log($"[EXCEPTION] {e.Message}");
            }
            msg.Recycle();
        }
    }
}