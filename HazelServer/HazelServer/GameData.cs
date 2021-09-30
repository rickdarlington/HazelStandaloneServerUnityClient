using System;
using Hazel;
using System.Collections.Generic;
using System.Threading;

namespace HazelServer
{
    internal class GameData
    {
        private static int GameCounter = 0;

        public readonly int id = 333;

        private List<Player> _playerList = new List<Player>();

        public int PlayerCount()
        {
            return _playerList.Count;
        }
        
        public void AddPlayer(Player newPlayer)
        {
            lock (_playerList)
            {
                _playerList.Add(newPlayer);
            }
            
            Console.WriteLine($"[DEBUG] adding player with id: {newPlayer.id}");
            
            var msg = MessageWriter.Get(SendOption.Reliable);
            msg.StartMessage((byte)PlayerMessageTags.PlayerInit);
            msg.WritePacked(newPlayer.id);
            msg.EndMessage();
            
            try
            {
                newPlayer.connection.Send(msg);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[EXCEPTION] {e.Message}");
                Console.WriteLine("[ERROR] error in GameData.AddPlayer()");
                //TODO handle "can't send to player" case
            }
        }

        public void removePlayer(Player p)
        {
            Console.WriteLine($"[DEBUG] player {p.id} removed");
            lock (_playerList)
            {
                _playerList.Remove(p);
            }
        }

        public void Broadcast(MessageWriter msg)
        {
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
                        Console.WriteLine("[ERROR] error in GameData.Broadcast()");
                        //TODO handle "can't send to player" case
                        // Maybe you want to disconnect the player if you can't send?
                    }
                }
            }
        }
    }
}