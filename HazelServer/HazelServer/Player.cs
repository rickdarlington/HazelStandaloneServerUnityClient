using System;
using System.Collections.Generic;
using System.Numerics;
using Hazel;
using System.Threading;
using UnityClient;

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

        private Queue<PlayerInputStruct> _playerInputs = new Queue<PlayerInputStruct>();

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
                            StoreInput(msg);
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

        private void StoreInput(MessageReader msg)
        {
            uint inputsCount = msg.ReadPackedUInt32();

            int i = 0;
            while (i < inputsCount)
            {
                var sequenceNumber = msg.ReadPackedUInt32();
                var dt = msg.ReadSingle();
                bool[] input = new[] { msg.ReadBoolean(), msg.ReadBoolean(), msg.ReadBoolean(), msg.ReadBoolean() };
                _playerInputs.Enqueue(new PlayerInputStruct(sequenceNumber, input, dt));
                i++;
            }
        }

        //NOTE should ONLY be called by GameStateUpdateLogic
        public void ApplyInputs()
        {
            //TODO check if player is sending "too many" inputs.  eg. more than they could actually generate at the fixedupdate rate
            // fixedUpdate only allows for sending 6 inputs per fixedUpdate tick, but these might have been delayed/etc
            // how? add up all dts and ensure they're less than 0.0167?
            
            //TODO we're allowing the player some authority here by letting them tell us what their dt was for a given input
            int i = 0;
            int toProcessCount = _playerInputs.Count;

            while (i < toProcessCount)
            {
                var playerInputStruct = _playerInputs.Dequeue();
                Position = Movement.ApplyInput(Position, playerInputStruct.inputs, 0.01666667f);
                
                //Console.WriteLine($"{DateTime.UtcNow} [TRACE] processing input seq {playerInputStruct.sequenceNumber}");
                
                if(playerInputStruct.sequenceNumber > LastProcessedInput) {
                    LastProcessedInput = playerInputStruct.sequenceNumber;
                }
                i++;
            }
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