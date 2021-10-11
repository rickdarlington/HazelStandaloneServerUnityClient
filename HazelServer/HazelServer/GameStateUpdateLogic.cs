using System;
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

                //UpdatePlayerPositions(_game);
                SendPlayerStateDataUpdates();
                
                long finishTimeMS = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                long elapsedTimeMS = finishTimeMS - startTimeMS;
                
                if (elapsedTimeMS < tickRate)
                {
                    dt = (int)tickRate - (int)elapsedTimeMS;
                    Thread.Sleep(dt);
                }
                else
                {
                    Console.WriteLine($"Position update took longer ({elapsedTimeMS}) than tick {tickRate}");
                }
            }
        }

        private void SendPlayerStateDataUpdates()
        {
            var pl = Game.Instance.PlayerList;
            lock (pl)
            {
                foreach (var player in pl)
                {
                    var msg = MessageWriter.Get();
                    msg.StartMessage((byte)MessageTags.GameData);
                    
                    try
                    {
                        //TODO broadcast also locks on PlayerList.  Can't do this in this way.
                        //Game.Instance.Broadcast(msg);
                        
                        //How should we actually be doing this?  all positions in one message?
                        //This will eventually break MTU in crowded areas.  All positions as
                        //separate messages?  seems like that could make reconciliation and interpolation
                        //really obnoxious to reconstruct later
                        
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
    }
}