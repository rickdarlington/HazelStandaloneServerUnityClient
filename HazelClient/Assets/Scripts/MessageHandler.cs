using System;
using Hazel;
using UnityEngine;
using UnityEngine.InputSystem;

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
            try
            {
                while (obj.Message.Position < obj.Message.Length)
                {
                    // Remember from the server code that sub-messages aren't pooled,
                    // they share the parent message's buffer. So don't recycle them!
                    var msg = obj.Message.ReadMessage();

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
                        case MessageTags.GameData:
                            ReceiveGameData(msg);
                            break;
                        default:
                            Debug.Log($"[DEBUG] unhandled message type [{msg.Tag}]");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[EXCEPTION] exception in MessageHandler.HandleMessage: {e.Message}");
            }
            finally
            {
                //TODO do we need cleanup here?
            }
        }

        private void ServerInitResponse(MessageReader reader)
        {
            int myId = reader.ReadInt32();
            Debug.Log($"[INFO] connected to server with player id: {myId}");
            _networkManager.PlayerId = myId;

            //TODO this is where you want to send your login information
            Debug.Log($"[DEBUG] sending log in message for {_networkManager.PlayerName}");
            Send(SendOption.Reliable, MessageTags.LogIn, _networkManager.PlayerName);
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
            UIMenuBehavior.Instance.ConnectionLost(msg.ReadString());
        }

        private void HandleServerMessage(MessageReader msg)
        {
            Debug.Log($"Received Server Message: {msg.ReadString()}");
        }

        private void ReceiveGameData(MessageReader msg)
        {
            var updates = msg.ReadPackedUInt32();
            var serverTick = msg.ReadPackedUInt32();
            
            //Debug.Log($"Processing ({updates+1}) update(s).");
            
            var i = 0;
            while (i < updates+1)
            {
                //TODO put these "packets" into a queue or something for processing by Update()
                PositionPacket packet = new PositionPacket(msg.ReadPackedUInt32(), msg.ReadSingle(), msg.ReadSingle(),
                    msg.ReadPackedUInt32());

                if (packet.playerId == _networkManager.PlayerId)
                {
                    Debug.Log($"Server tick: {serverTick} player: {packet.playerId} position: {packet.X} . {packet.Y}");
                }

                i++;
            }
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
    }
}