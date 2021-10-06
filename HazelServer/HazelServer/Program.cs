using Hazel;
using Hazel.Udp;
using System;
using System.Net;
using System.Threading;

namespace HazelServer
{
    internal enum PlayerMessageTags
    {
        JoinGame,       // 0
        LeaveGame,      // 1
        PlayerInit,     // 2
        PlayerJoined,   // 3
        PlayerLeft,     // 4
        PlayersInGame,  // 5
        ServerMessage,  // 6
        GameData        // 7
    }

    internal class Server
    {
        private const int ServerPort = 30003;

        private bool _amService;
        public GameData _game { get; private set; }

        private UdpConnectionListener _udpServerRef;

        private static void Main(string[] args)
        {
            bool amService = false;
            if (args.Length > 0) bool.TryParse(args[0], out amService);

            Server server = new Server(amService);
            server.Run();
        }

        public Server(bool amService)
        {
            _amService = amService;
        }
        
        private void Run()
        {
            _game = new GameData();
            
            using (var udpServer = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, ServerPort), IPMode.IPv4))
            {
                Console.WriteLine($"{DateTime.UtcNow} [START] Starting server");
                
                _udpServerRef = udpServer;
                udpServer.NewConnection += HandleNewConnection;
                udpServer.Start();

                var running = true;
                while (running)
                {
                    //spawn game logic thread
                    Thread StateManagerThread = new Thread(StateUpdateThread);
                    StateManagerThread.Start();
                    
                    if (_amService)
                    {
                        // When running as a service, the main thread really doesn't need to do anything
                        // But it can be really useful to poll for configuration changes or log periodic statistics
                        Thread.Sleep(60000);

                        // For example, if you suspect you have a MessageReader/Writer leak, try outputting these counters.
                        // If the NumberInUse and NumberCreated keep going up, you probably forgot to recycle. (Or maybe you have a deadlock!)
                        // If you try to recycle a pooled object twice, the pool will throw. (Which sucks, but tends to be very easy to debug.)
                        // If you pool something that didn't come from the pool, the pool will throw.
                        // I may make the exceptions only happen in debug builds someday since they do have perf cost. (Very, very small)
                        Console.WriteLine($"${DateTime.UtcNow} Readers: {MessageReader.ReaderPool.NumberInUse}/{MessageReader.ReaderPool.NumberCreated}/{MessageReader.ReaderPool.Size}");
                        Console.WriteLine($"${DateTime.UtcNow} Writers: {MessageWriter.WriterPool.NumberInUse}/{MessageWriter.WriterPool.NumberCreated}/{MessageWriter.WriterPool.Size}");
                    }
                    else
                    {
                        //TODO Readline seems to have a bug (maybe ubuntu terminal specific) which causes 'backspace' key to break the terminal :(Console.Write(".");
                        var input = Console.ReadLine();
                        if (input.Equals(":q"))
                        {
                            Console.WriteLine($"{DateTime.UtcNow} > goodbye for now...");
                            return;
                        }

                        ServerCommand(input);
                    }
                }
            }
        }

        private void ServerCommand(String input)
        {
            switch (input)
            {
                case "bc":
                case "broadcast":
                    var msg = MessageWriter.Get(SendOption.Reliable);
                    msg.StartMessage((byte)PlayerMessageTags.ServerMessage);
                    msg.Write("hi");
                    msg.EndMessage();
                    _game.Broadcast(msg);
                    Console.WriteLine($"> broadcast sent");
                    break;
                case "pc":
                case "player count":
                    Console.WriteLine($"> Players online: {_game.PlayerCount()}");
                    break;
                case "sc":
                case "show connections":
                    Console.WriteLine($"> udp connections: " + _udpServerRef.ConnectionCount);
                    break;
                default: 
                    Console.WriteLine($"> input error");
                    break;
            }
        }

        // From here down, you must be thread-safe!
        private void HandleNewConnection(NewConnectionEventArgs obj)
        {
            Console.WriteLine($"{DateTime.UtcNow} [DEBUG] new connection from {obj.Connection.EndPoint.Address}");
            try
            {
                if (obj.HandshakeData.Length <= 0)
                {
                    // If the handshake is invalid, let's disconnect them!
                    Console.WriteLine($"{DateTime.UtcNow} [ERROR] disconnecting {obj.Connection.EndPoint.Address} due to bad handshake.");
                    return;
                }

                // Make sure this client version is compatible with this server and/or other clients!
                var clientVersion = obj.HandshakeData.ReadInt32();
                Console.WriteLine($"{DateTime.UtcNow} [DEBUG] connect from clientVersion {clientVersion}");

                //TODO update to pass name in handshake data
                //var playerName = obj.HandshakeData.ReadString();

                var player = new Player(this, obj.Connection, "test");
                _game.AddPlayer(player);
                
                //TODO is this even working?  HandleDisconnect never gets invoked
                obj.Connection.DataReceived += player.HandleMessage;
                obj.Connection.Disconnected += player.HandleDisconnect;
            }
            finally
            {
                // Always recycle messages!
                obj.HandshakeData.Recycle();
            }
        }
        
        public void StateUpdateThread()
        {
            Console.WriteLine($"{DateTime.UtcNow} [START] Starting game update loop");
            //update loop 
            long tickRate = 100; //10 ticks per second
            int dt = 0;
            while (true)
            {
                long startTimeMS = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                
                
                //UpdatePlayerPositions(_game);
                //SendPlayerStateDataUpdates();
                
                long finishTimeMS = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                long elapsedTimeMS = finishTimeMS - startTimeMS;
                
                if (elapsedTimeMS < tickRate)
                {
                    dt = (int)tickRate - (int)elapsedTimeMS;
                    Thread.Sleep(dt);
                }
                else
                {
                    Console.WriteLine($"Position update took longer ({elapsedTimeMS}) than tick {tickRate}");
                }
            }
        }
    }
}
