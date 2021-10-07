using System;
using System.Numerics;
using Hazel;
using System.Threading;

namespace HazelServer
{
    internal class Player
    {
        public readonly int id; 
        public readonly Connection connection;

        private static int _playerCounter = 0;
        public Vector2 position = new Vector2(0, 0);

        private bool loggedIn = false;
        private String _name;

        public Player(Connection c)
        {
            connection = c;
            id = Interlocked.Increment(ref _playerCounter);
        }

        public void HandleMessage(DataReceivedEventArgs obj)
        {
            try
            {
                // This pattern allows us to pack and handle multiple messages
                // This creates really good packet efficiency, but watch out for MTU.
                while (obj.Message.Position < obj.Message.Length)
                {
                    // Okay, I lied. You won't need to recycle any message from ReadMessage!
                    // They share the internal MessageReader.Buffer with the parent, so there's no new buffer to pool!
                    var msg = obj.Message.ReadMessage();
                    var tag = (MessageTags)msg.Tag;

                    Console.WriteLine($"{DateTime.UtcNow} [TRACE] HandleMessage: {tag.ToString()}");

                    switch (tag)
                    {
                        case MessageTags.LogIn:
                            string playerName = msg.ReadString();
                            Console.WriteLine($"{DateTime.UtcNow} [TRACE] \"{playerName}\" is logging in.");
                            if (LogIn(playerName))
                            {
                                SendReliable(MessageTags.LoginSuccess);
                                //TODO login success message
                            }
                            else
                            {
                                SendReliable(MessageTags.LoginFailed);
                                //TODO send login failed message
                            }

                            break;
                    }
                }
            }
            catch
            {
                Console.WriteLine($"{DateTime.UtcNow} [ERROR] Error in Player.HandleMessage");
            }
            finally
            {
                obj.Message.Recycle();
            }
        }

        public void HandleDisconnect(object sender, DisconnectedEventArgs e)
        {
            Console.WriteLine($"{DateTime.UtcNow} [DEBUG] Disconnecting player id: {id}");
            Game.Instance.removePlayer(this);
            //TODO shouldn't we destroy the player 
            // There's actually nothing to do in this simple case!
            // If HandleDisconnect is called, then dispose is also guaranteed to be called.
            // Feel free to log e.Reason, clean up anything associated with a player disconnecting, etc.
        }

        private bool LogIn(string name)
        {
            //TODO check if name is in use, password, etc
            _name = name;
            return true;
        }
        
        
        public void SendReliable(MessageTags tag)
        {
            var msg = MessageWriter.Get(SendOption.Reliable);
            msg.StartMessage((byte)tag);
            msg.EndMessage();
            try
            {
                connection.Send(msg);
            }
            catch
            {
                Console.WriteLine($"{DateTime.UtcNow} [ERROR] Error in Player.Send()");
            }
            finally
            {
                msg.Recycle();
            }
        }
    }
}