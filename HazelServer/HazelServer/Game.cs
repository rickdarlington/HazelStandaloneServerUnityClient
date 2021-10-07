﻿using System;
using Hazel;
using System.Collections.Generic;
using System.Threading;

namespace HazelServer
{
    internal class Game
    {
        private static int GameCounter = 0;
        public readonly int id = 333;

        private List<Player> _playerList = new();
        
        private static Game instance;

        public static Game Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Game();
                }

                return instance;
            }
        }

        private Game() {}
        
        public int PlayerCount()
        {
            return _playerList.Count;
        }

        //TODO optimize, maybe use a dictionary/etc for getting player by name
        public Player GetPlayerByName(string name)
        {
            Player p = null;
            lock (_playerList)
            {
                foreach (var player in _playerList)
                {
                    if (player.name == name)
                    {
                        p = player;
                        break;
                    }
                }
            }

            return p;
        }

        //TODO for class: combine all try/connection.send/recycle calls
        public void AddPlayer(Player newPlayer)
        {
            lock (_playerList)
            {
                _playerList.Add(newPlayer);
            }
            
            Console.WriteLine($"{DateTime.UtcNow} [DEBUG] adding player with id: {newPlayer.id}");
            
            var msg = MessageWriter.Get(SendOption.Reliable);
            msg.StartMessage((byte)MessageTags.ServerInit);
            msg.WritePacked(newPlayer.id);
            msg.EndMessage();
            
            try
            {
                newPlayer.connection.Send(msg);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.UtcNow} [EXCEPTION] {e.Message}");
                Console.WriteLine($"{DateTime.UtcNow} [ERROR] error in GameData.AddPlayer()");
                //TODO handle "can't send to player" case
            }
            
            msg.Recycle();
        }

        public void removePlayer(Player p)
        {
            Console.WriteLine($"{DateTime.UtcNow} [DEBUG] player {p.id} removed");
            lock (_playerList)
            {
                _playerList.Remove(p);
            }
        }

        public void ProcessPlayerInputs()
        {
            //TODO implement me
            lock (_playerList)
            {
                foreach (var player in _playerList)
                {
                    try
                    {
                        player.position.X += 0.01f;
                        player.position.Y += 0.01f;
                    }
                    catch
                    {
                        Console.WriteLine($"{DateTime.UtcNow} [ERROR] error processing inputs for player {player.id}");
                    }
                }
            }
        }

        public void SendPlayerPositionUpdate()
        {
            //TODO implement better locking
            lock (_playerList)
            {
                foreach (var player in _playerList)
                {
                    var msg = MessageWriter.Get();
                    
                    try
                    {
                        //these should be sent all together as gamedata?
                        //msg.StartMessage((byte)MessageTags.GameData);
                        //msg.Write();
                        //player.connection.Send(msg);
                    }
                    catch
                    {
                        Console.WriteLine($"{DateTime.UtcNow} [ERROR] error in GameData.Broadcast()");
                        //TODO handle "can't send to player" case
                        // Maybe you want to disconnect the player if you can't send?
                    }
                    
                    msg.Recycle();
                }
            }
        }

        public void Broadcast(MessageWriter msg)
        {
            //TODO implement better locking
            // It's possible to create this method entirely lock-free, but too tricky 
            // for this example! Even a ReaderWriterLockSlim would be an improvement.
            lock (_playerList)
            {
                foreach (var player in _playerList)
                {
                    try
                    {
                        player.connection.Send(msg);
                        
                    }
                    catch
                    {
                        Console.WriteLine($"{DateTime.UtcNow} [ERROR] error in GameData.Broadcast()");
                        //TODO handle "can't send to player" case
                        // Maybe you want to disconnect the player if you can't send?
                    }
                }
                
                msg.Recycle();
            }
        }
    }
}