using System;
using System.Threading;

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
                //SendPlayerStateDataUpdates();
                
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
    }
}