using Hazel;
using Hazel.Udp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

// Obviously this isn't a real Unity project, but we'll just pretend.
// I don't want to create the external buttons and stuff, so that's totally up to you.
namespace UnityClient
{
    // Maybe you want to use another lib to keep this in sync?
    // They usually don't change enough for copy-paste to be a problem, though.
    internal enum PlayerMessageTags
    {
        JoinGame,       // 0
        LeaveGame,      // 1
        PlayerJoined,   // 2
        PlayerLeft,     // 3
        PlayersInGame,  // 4
        ServerMessage,  // 5
        GameData        // 6
    }

    // Usually this kind of class should be a singleton, but everyone has 
    // their own way of doing that. So I leave it up to you.
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
                    // TODO: In hazel, I need to change this so it makes sense
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
                msg.StartMessage((byte)PlayerMessageTags.GameData);
                msg.Write(_gameId);
            }
        }

        public void JoinGame(int gameId)
        {
            if (_connection == null) return;

            Console.WriteLine($"Connecting {_connection.EndPoint.Address}");
            
            var msg = MessageWriter.Get(SendOption.Reliable);
            msg.StartMessage((byte)PlayerMessageTags.JoinGame);
            msg.Write(gameId);
            msg.EndMessage();

            try { _connection.Send(msg); } catch { Console.WriteLine($"Caught exception in JoinGame for connection {_connection.EndPoint.Address}"); }
            msg.Recycle();
        }

        //this is a coroutine, hence the "Co in CoConnect"
        public IEnumerator CoConnect()
        {
            // Don't leak connections!
            if (_connection != null) yield break;

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
                stream.StartMessage((byte)PlayerMessageTags.GameData);
                stream.Write(_gameId);
            }

            //TODO update to not force loopback connection
            _connection = new UdpClientConnection(new IPEndPoint(IPAddress.Loopback, _serverPort));
            _connection.DataReceived += HandleMessage;
            _connection.Disconnected += HandleDisconnect;

            // If you block in a Unity Coroutine, it'll hang the game!
            _connection.ConnectAsync(GetConnectionData());

            //TODO implement a connection timeout
            while (_connection != null && _connection.State != ConnectionState.Connected)
            {
                yield return null;
            }
        }

        // Remember this is on a new thread.
        private void HandleDisconnect(object sender, DisconnectedEventArgs e)
        {
            lock (eventQueue)
            {
                eventQueue.Clear();
                // Maybe something like:
                // EventQueue.Add(ChangeToMainMenuSceneWithError(e.Reason));

                //TODO handle server disconnections
                Console.WriteLine($"[TODO] handle server disconnections");
                UIManager.instance.ConnectionLost();
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
                    switch ((PlayerMessageTags)msg.Tag)
                    {
                        case PlayerMessageTags.JoinGame:
                            HandleJoinGameResponse(msg);
                            break; 
                        case PlayerMessageTags.ServerMessage:
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

        private void HandleJoinGameResponse(MessageReader msg)
        {
            int myId = msg.ReadInt32();
            Debug.Log($"[info] joined game as player id: {myId}");
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

        public void ConnectToServer()
        {
            StartCoroutine(CoConnect());
        }
    }
}
