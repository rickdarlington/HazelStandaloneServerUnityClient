﻿using Hazel;
using Hazel.Udp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace UnityClient
{
    // Maybe you want to use another lib to keep this in sync?
    // They usually don't change enough for copy-paste to be a problem, though.
    internal enum MessageTags
    {
        ServerInit,     // 0
        LogIn,          // 1
        LoginSuccess,   // 2
        LoginFailed,    // 3
        ServerMessage,  // 4 
        GameData
    }
    
    internal class HazelNetworkManager : MonoBehaviour
    {
        public static HazelNetworkManager instance;
        
        private const int _serverPort = 30003;
        //TODO we can optimize by removing this, as we only have one "game" running at a time
        private const int _gameId = 333;
        
        // Unity gets very grumpy if you start messing with GameObjects on threads
        // other than the main one. So while sending/receiving messages can be multithreaded,
        // we need a queue to hold events until a Update/FixedUpdate method can handle them.
        public List<Action> eventQueue = new List<Action>();

        // How many seconds between batched messages
        public float minSendInterval = .1f;
        
        private UdpClientConnection _connection;

        // This will hold a reliable and an unreliable "channel", so you can batch 
        // messages to the server every MinSendInterval seconds.
        private MessageWriter[] _streams;

        private float timer = 0;

        private bool _loggedIn = false;
        private bool _connectInProgress = false;

        private string _playerName = "nobody";

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != null)
            {
                Debug.Log("[TODO] why would you ever get here?  eliminate this");
                Destroy(this);
            }
        }

        public void Update()
        {
            //if update is called before connecting, _streams will be null
            if (_streams == null)
            {
                return;
            }
            
            lock (eventQueue)
            {
                foreach (var evt in eventQueue)
                {
                    evt();
                }

                eventQueue.Clear();
            }

            timer += Time.fixedDeltaTime;
            if (timer < minSendInterval)
            {
                // Unless you are making a highly competitive action game, you don't need updates
                // every frame. And many network connections cannot handle that kind of traffic.
                return;
            }

            timer = 0;

            foreach (var msg in _streams)
            {
                try
                {
                    //TODO we can remove GameId here, as we only have one game running at a time
                    // Right now:
                    // 7 = Tag (1) + MessageLength (2) + GameId (4)
                    // Ideally, no magic calculation, just msg.HasMessages
                    if (!msg.HasBytes(7)) continue;
                    msg.EndMessage();

                    _connection.Send(msg);
                }
                catch 
                {
                    Debug.Log("[TODO] handle this catch in Update()");
                    // Logging, probably
                }

                msg.Clear(msg.SendOption);
                msg.StartMessage((byte)MessageTags.GameData);
                msg.Write(_gameId);
            }
        }

        //this is a coroutine, hence the "Co in CoConnect"
        public IEnumerator CoConnect()
        {
            Debug.Log("[EXEC] CoConnect");
            _connectInProgress = true;
            // Initialize streams (once)
            if (_streams == null)
            {
                _streams = new MessageWriter[2];
                for (int i = 0; i < _streams.Length; ++i)
                {
                    _streams[i] = MessageWriter.Get((SendOption)i);
                }
            }

            // Clear any existing data, and prep them for batching
            for (int i = 0; i < _streams.Length; ++i)
            {
                var stream = _streams[i];
                stream.Clear((SendOption)i);
                stream.StartMessage((byte)MessageTags.GameData);
                stream.Write(_gameId);
            }

            //TODO update to not force loopback connection
            _connection = new UdpClientConnection(new IPEndPoint(IPAddress.Loopback, _serverPort));
            _connection.DataReceived += HandleMessage;
            _connection.Disconnected += HandleDisconnect;
            
            Debug.Log("[DEBUG] client connection configured, calling connect.");

            // If you block in a Unity Coroutine, it'll hang the game!
            _connection.ConnectAsync(GetConnectionData());

            while (_connection != null && _connection.State != ConnectionState.Connected)
            {
                yield return null;
            }

            _connectInProgress = false;
        }

        // Remember this is on a new thread.
        private void HandleDisconnect(object sender, DisconnectedEventArgs e)
        {
            Debug.Log($"[INFO] server disconnected");
            lock (eventQueue)
            {
                eventQueue.Clear();
                eventQueue.Add(serverDisconnected);
            }
        }
        
        private void HandleMessage(DataReceivedEventArgs obj)
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
            catch
            {
                Debug.Log("[TODO] handle errors in HandleMessage()"); // Error logging
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

            //TODO this is where you want to send your login information
            Debug.Log($"[DEBUG] sending log in message for {_playerName}");
            var msg = MessageWriter.Get(SendOption.Reliable);
            msg.StartMessage((byte)MessageTags.LogIn);
            msg.Write(_playerName);
            msg.EndMessage();

            try
            {
                while (_connectInProgress)
                {
                    //TODO FIXME
                    //wait while _connectInProgress is true.
                    // There's a race condition here such that you might try to call _connection.Send
                    // before the connection is actually ready.  WTF.
                }
                _connection.Send(msg);
            }
            catch(Exception e)
            {
                Debug.Log($"[ERROR] Caught exception in LogIn for connection {_connection.EndPoint.Address}");
                Debug.Log($"[EXCEPTION] {e.Message}");
            }
            msg.Recycle();
        }

        private void ServerLoginResponse(MessageReader msg)
        {
            Debug.Log($"[INFO] Login success");
            _loggedIn = true;
        }

        private void ServerLoginFailure(MessageReader msg)
        {
            Debug.Log($"[ERROR] login failed with error: {msg.ReadString()}");
            _loggedIn = false;
            //TODO boot to login screen
            UIMenuBehavior.instance.ConnectionLost(msg.ReadString());
        }

        private void HandleServerMessage(MessageReader msg)
        {
            Debug.Log($"Received Server Message: {msg.ReadString()}");
        }

        private static byte[] GetConnectionData()
        {
            // A version code. Could be anything though.
            return new byte[] { 1, 0, 0, 0 };
        }

        public void ConnectToServer(string playerName)
        {
            if (playerName != "")
            {
                _playerName = playerName;
            }

            StartCoroutine(CoConnect());
        }

        public bool IsConnected()
        {
            if (_connection == null)
            {
                Debug.Log("_connection is null, not connected");
                return false;
            }

            Debug.Log($"connection state: {_connection.State}");

            return (_connection.State == ConnectionState.Connected);
        }

        /// <summary>
        /// actions that need to be handled by the main thread
        /// </summary>
        public Action serverDisconnected = () =>
        {
            UIMenuBehavior.instance.ConnectionLost("server connection lost");   
        };
    }
}
