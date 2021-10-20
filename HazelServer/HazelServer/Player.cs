using System;
using System.Numerics;
using Hazel;
using System.Threading;

namespace HazelServer
{
    internal class Player
    {
        public readonly uint id; 
        public readonly Connection connection;

        private static int _playerCounter = 0;
        
        public Vector2 Position = new Vector2(0, 0);
        public uint lookDirection = 0;
        
        public bool LoggedIn { get; private set; }= false;
        public String name { get; private set; }

        public uint LastProcessedInput { get; private set; } = 0;

        public Player(Connection c)
        {
            connection = c;
            id = (uint)Interlocked.Increment(ref _playerCounter);
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

                    switch (tag)
                    {
                        case MessageTags.LogIn:
                            string playerName = msg.ReadString();
                            Console.WriteLine($"{DateTime.UtcNow} [TRACE] \"{playerName}\" is trying to log in");
                            if (LogIn(playerName))
                            {
                                Send(SendOption.Reliable, MessageTags.LoginSuccess, null);
                            }
                            else
                            {
                                Send(SendOption.Reliable, MessageTags.LoginFailed, $"{playerName} is already logged in!");
                            }
                            break;
                        case MessageTags.ConsoleMessage:
                            //TODO do something with console commands
                            Console.WriteLine($"{DateTime.UtcNow} [INBOUND] console message from player \"{name}\": {msg.ReadString()}");
                            break;
                        case MessageTags.PlayerChat:
                            //TODO implement player chat (send message to all other players?)
                            Console.WriteLine($"{DateTime.UtcNow} [INBOUND] chat message from player \"{name}\": {msg.ReadString()}");
                            break;
                        case MessageTags.PlayerInput:
                            ProcessInput(msg);
                            break;
                        default:
                            Console.WriteLine($"{DateTime.UtcNow} [ERROR] unhandled message type [{msg.Tag}]");
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
            if (Game.Instance.GetPlayerByName(name) == null)
            {
                this.name = name;
                Console.WriteLine($"{DateTime.UtcNow} [DEBUG] \"{name}\" logged in successfully");
                this.LoggedIn = true;
                return true;
            }
            else
            {
                Console.WriteLine($"{DateTime.UtcNow} [ERROR] {connection.EndPoint.Address} is trying to log in as \"{name}\" who is already logged in");
                return false;
            }
        }

        private void ProcessInput(MessageReader msg)
        {
            uint sequenceNumber = msg.ReadPackedUInt32();
            bool[] input = new[] { msg.ReadBoolean(), msg.ReadBoolean(), msg.ReadBoolean(), msg.ReadBoolean() };
            
            //TODO move player code goes here.

            if (sequenceNumber > LastProcessedInput)
            {
                LastProcessedInput = sequenceNumber;
            }
            else
            {
                //TODO we should never get here since inputs are sent reliably.  this is more of an indicator to investigate
                Console.WriteLine($"{DateTime.UtcNow} [TRACE] processing out of order player input {sequenceNumber} > {LastProcessedInput}");    
            }

            Console.WriteLine($"{DateTime.UtcNow} [TRACE] player input ({sequenceNumber}): {input[0]} {input[1]} {input[2]} {input[3]}");
        }
        
        //TODO how do we genericize this?  we want to send errors with strings, but sometimes just tags.  we don't 
        //really want to keep track on both client and server whether we can read the error string from the message
        public void Send(SendOption option, MessageTags tag, string messageString)
        {
            var msg = MessageWriter.Get(option);
            msg.StartMessage((byte)tag);

            if (messageString != null)
            {
                msg.Write(messageString);
            }
            
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