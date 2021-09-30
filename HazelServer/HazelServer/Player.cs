using System;
using Hazel;
using System.Threading;

namespace HazelServer
{
    internal class Player
    {
        public readonly int id; 
        public readonly Connection connection;
        
        private static int _playerCounter = 0;
        private readonly ServerProgram _server;

        private String _name;
        
        public Player(ServerProgram server, Connection c, String name)
        {
            //TODO should server just be a singleton get from Program.cs?
            _server = server;
            _name = name;
            connection = c;
            id = Interlocked.Increment(ref _playerCounter);
        }

        public void HandleMessage(DataReceivedEventArgs obj)
        {
            Console.WriteLine("new message");
            try
            {
                // This pattern allows us to pack and handle multiple messages
                // This creates really good packet efficiency, but watch out for MTU.
                while (obj.Message.Position < obj.Message.Length)
                {
                    // Okay, I lied. You won't need to recycle any message from ReadMessage!
                    // They share the internal MessageReader.Buffer with the parent, so there's no new buffer to pool!
                    var msg = obj.Message.ReadMessage();
                    var tag = (PlayerMessageTags)msg.Tag;
                    
                    Console.WriteLine(tag.ToString());
                    
                    switch (tag)
                    {
                        case PlayerMessageTags.JoinGame:
                            var message = MessageWriter.Get(SendOption.Reliable);
                            message.StartMessage((byte)PlayerMessageTags.JoinGame);

                            lock (this)
                            {
                                _server.game.AddPlayer(this);
                                message.Write(id);
                            }

                            message.EndMessage();
                            try
                            {
                                obj.Sender.Send(message);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"[EXCEPTION] {e.Message}");
                                Console.WriteLine($"Error sending player join message to player with id: {this.id}");
                            }

                            message.Recycle();
                            break;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error in Player.HandleMessage");
            }
            finally
            {
                obj.Message.Recycle();
            }
        }
        
        public void HandleDisconnect(object sender, DisconnectedEventArgs e)
        {
            Console.WriteLine($"[DEBUG] Disconnecting player id: {id}");
            _server.game.removePlayer(this);
            //TODO shoudn't we destroy the player 
            // There's actually nothing to do in this simple case!
            // If HandleDisconnect is called, then dispose is also guaranteed to be called.
            // Feel free to log e.Reason, clean up anything associated with a player disconnecting, etc.
        }
    }
}