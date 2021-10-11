using System;
using Hazel;
using System.Collections.Generic;
using System.Threading;

namespace HazelServer
{
    internal class Game
    {
        public List<Player> PlayerList = new();
        
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
            return PlayerList.Count;
        }

        //TODO optimize, maybe use a dictionary/etc for getting player by name
        public Player GetPlayerByName(string name)
        {
            Player p = null;
            lock (PlayerList)
            {
                foreach (var player in PlayerList)
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
            lock (PlayerList)
            {
                PlayerList.Add(newPlayer);
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
            lock (PlayerList)
            {
                PlayerList.Remove(p);
            }
        }

        public void Broadcast(MessageWriter msg)
        {
            //TODO implement better locking
            // It's possible to create this method entirely lock-free, but too tricky 
            // for this example! Even a ReaderWriterLockSlim would be an improvement.
            lock (PlayerList)
            {
                foreach (var player in PlayerList)
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