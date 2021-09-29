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
        
        // Let's tell the existing players about the new player
        // And tell the new player about all the existing players
        public void AddPlayer(Player newPlayer)
        {
            var msg = MessageWriter.Get(SendOption.Reliable);
            msg.StartMessage((byte)PlayerMessageTags.PlayerJoined);
            msg.WritePacked(newPlayer.id);
            msg.EndMessage();
            Broadcast(msg);

            lock (_playerList)
            {
                msg.Clear(SendOption.Reliable);
                msg.StartMessage((byte)PlayerMessageTags.PlayersInGame);
                foreach (var player in _playerList)
                {
                    msg.WritePacked(player.id);
                }
                msg.EndMessage();

                _playerList.Add(newPlayer);
            }

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