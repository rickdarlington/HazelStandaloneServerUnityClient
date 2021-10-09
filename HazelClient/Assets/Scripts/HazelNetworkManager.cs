using Hazel;
using Hazel.Udp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace UnityClient
{

    internal class HazelNetworkManager : MonoBehaviour
    {
        //TODO we can optimize by removing this, as we only have one "game" running at a time
        private const int _gameId = 333;
        
        private static HazelNetworkManager instance;
        public static HazelNetworkManager Instance => instance;
        public readonly IPAddress ServerAddress = IPAddress.Loopback;
        public readonly int ServerPort = 30003;
        
        // Unity gets very grumpy if you start messing with GameObjects on threads
        // other than the main one. So while sending/receiving messages can be multithreaded,
        // we need a queue to hold events until a Update/FixedUpdate method can handle them.
        public readonly List<Action> eventQueue = new List<Action>();

        // How many seconds between batched messages
        public float minSendInterval = .1f;
        
        public UdpClientConnection Connection { get; private set; }

        // This will hold a reliable and an unreliable "channel", so you can batch 
        // messages to the server every MinSendInterval seconds.
        private MessageWriter[] _streams;
        
        private float timer = 0;
        public bool LoggedIn { get; set; } = false;
        
        public bool ConnectInProgress { get; private set; } = false;

        public string PlayerName { get; private set; } = "nobody"; 

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

                    Connection.Send(msg);
                }
                catch 
                {
                    Debug.Log("[TODO] handle this catch in Update()");
                }

                msg.Clear(msg.SendOption);
                msg.StartMessage((byte)MessageTags.GameData);
                msg.Write(_gameId);
            }
        }

        //this is a coroutine, hence the "Co" in CoConnect
        public IEnumerator CoConnect()
        {
            Debug.Log("[EXEC] CoConnect");
            ConnectInProgress = true;
            // only initialize streams once
            if (_streams == null)
            {
                _streams = new MessageWriter[2];
                for (int i = 0; i < _streams.Length; ++i)
                {
                    _streams[i] = MessageWriter.Get((SendOption)i);
                }
            }

            //clear any existing data, and prep them for batching
            for (int i = 0; i < _streams.Length; ++i)
            {
                var stream = _streams[i];
                stream.Clear((SendOption)i);
                stream.StartMessage((byte)MessageTags.GameData);
                stream.Write(_gameId);
            }

            Connection = new UdpClientConnection(new IPEndPoint(ServerAddress, ServerPort));
            Connection.DataReceived += MessageHandler.Instance.HandleMessage;
            Connection.Disconnected += HandleDisconnect;
            
            Debug.Log("[DEBUG] client connection configured, calling connect.");

            // If you block in a Unity Coroutine, it'll hang the game!
            Connection.ConnectAsync(GetConnectionData());

            while (Connection != null && Connection.State != ConnectionState.Connected)
            {
                yield return null;
            }

            ConnectInProgress = false;
        }

        // Remember this is on a new thread, anything you need to do to gameobjects has to be in the eventQueue
        private void HandleDisconnect(object sender, DisconnectedEventArgs e)
        {
            Debug.Log($"[INFO] server disconnected");
            lock (eventQueue)
            {
                eventQueue.Clear();
                eventQueue.Add(serverDisconnected);
            }
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
                PlayerName = playerName;
            }

            StartCoroutine(CoConnect());
        }

        public bool IsConnected()
        {
            if (Connection == null)
            {
                return false;
            }
            
            return (Connection.State == ConnectionState.Connected);
        }

        /// <summary>
        /// actions that need to be handled by the main thread
        /// </summary>
        public Action serverDisconnected = () =>
        {
            UIMenuBehavior.Instance.ConnectionLost("server connection lost");   
        };
    }
}
