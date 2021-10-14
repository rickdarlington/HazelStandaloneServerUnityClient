using System;
using System.Net.Mime;
using System.Numerics;
using System.Threading;
using Hazel;

namespace HazelServer
{
    public class GameStateUpdateLogic
    {
        public void thread()
        {
            Console.WriteLine($"{DateTime.UtcNow} [START] Starting game update loop");
            //update loop 
            long tickRate = 100; //10 ticks per second
            int dt = 0;
            while (true)
            {
                //Console.WriteLine($"{DateTime.UtcNow} [TRACE] game state update");

                long startTimeMS = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                PositionPacket[] positions = UpdatePlayerPositions();
                SendPlayerStateDataUpdates(positions);
                
                long finishTimeMS = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                long elapsedTimeMS = finishTimeMS - startTimeMS;
                
                if (elapsedTimeMS < tickRate)
                {
                    dt = (int)tickRate - (int)elapsedTimeMS;
                    Thread.Sleep(dt);
                }
                else
                {
                    Console.WriteLine($"Position update took longer ({elapsedTimeMS}) than tick rate ({tickRate})");
                }
            }
        }

        private PositionPacket[] UpdatePlayerPositions()
        {
            var pl = Game.Instance.PlayerList;
            PositionPacket[] positions = new PositionPacket[pl.Count];
            
            int i = 0;
            lock (pl)
            {
                foreach (var player in pl)
                {
                    //compute new player position
                    positions[i] = new PositionPacket(player.id, player.Position.X, player.Position.Y, player.lookDirection);
                    i++;
                }
            }

            return positions;
        }

        private void SendPlayerStateDataUpdates(PositionPacket[] positions)
        {

            var l = positions.Length / 4;
            
            var msg = MessageWriter.Get();
            msg.StartMessage((byte)MessageTags.GameData);
            msg.WritePacked(l);

            foreach (PositionPacket position in positions)
            {
                msg.WritePacked(position.playerId);
                msg.Write(position.X);
                msg.Write(position.Y);
                msg.WritePacked(position.lookDirection);
            }
            
            msg.EndMessage();

            if (msg.Length > 8)
            {
                Console.WriteLine($"{DateTime.UtcNow} [TRACE] position message length: {msg.Length}");
            }

            Game.Instance.Broadcast(msg);
        }
    }
}