using Hazel;
using Hazel.Udp;
using System;
using System.Net;
using System.Threading;

namespace HazelServer
{
    internal class Server
    {
        private readonly bool _amService;
        private const int ServerPort = 30003;
        private UdpConnectionListener UdpServerInstance;

        private readonly GameStateUpdateLogic _gameStateUpdateLogic;
        //TODO private readonly AIUpdateLogic _aiUpdateLogic;
        
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
            _gameStateUpdateLogic = new();
        }
        
        private void Run()
        {
            UdpServerInstance = new UdpConnectionListener(new IPEndPoint(IPAddress.Any, ServerPort), IPMode.IPv4);
            using (UdpServerInstance)
            {
                Console.WriteLine($"{DateTime.UtcNow} [START] Starting server"); 
                UdpServerInstance.NewConnection += HandleNewConnection; 
                UdpServerInstance.Start();
                
                //spawn game logic thread
                Thread stateLogicThread = new Thread(_gameStateUpdateLogic.thread);
                stateLogicThread.Start();
                
                var running = true;
                //spawn server threads and run
                while (running)
                {
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

        private void ServerCommand(string input)
        {
            switch (input)
            {
                case "bc":
                case "broadcast":
                    var msg = MessageWriter.Get(SendOption.Reliable);
                    msg.StartMessage((byte)MessageTags.ServerMessage);
                    msg.Write("hi");
                    msg.EndMessage();
                    Game.Instance.Broadcast(msg);
                    Console.WriteLine($"> broadcast sent");
                    break;
                case "pc":
                case "player count":
                    Console.WriteLine($"> Players online: {Game.Instance.PlayerCount()}");
                    break;
                case "sc":
                case "show connections":
                    Console.WriteLine($"> udp connections: " + UdpServerInstance.ConnectionCount);
                    break;
                case "rw": 
                case "readers": 
                case "writers":
                    Console.WriteLine($"> Readers: {MessageReader.ReaderPool.NumberInUse}/{MessageReader.ReaderPool.NumberCreated}/{MessageReader.ReaderPool.Size}");
                    Console.WriteLine($"> Writers: {MessageWriter.WriterPool.NumberInUse}/{MessageWriter.WriterPool.NumberCreated}/{MessageWriter.WriterPool.Size}");
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
                Console.WriteLine($"{DateTime.UtcNow} [DEBUG] {obj.Connection.EndPoint.Address} is connecting with clientVersion {clientVersion}");

                var player = new Player(obj.Connection);
                Game.Instance.AddPlayer(player);
                
                obj.Connection.DataReceived += player.HandleMessage;
                
                //TODO is this even working? hazel bug?  HandleDisconnect never gets invoked
                obj.Connection.Disconnected += player.HandleDisconnect;
            }
            finally
            {
                // Always recycle messages!
                obj.HandshakeData.Recycle();
            }
        }
    }
}
