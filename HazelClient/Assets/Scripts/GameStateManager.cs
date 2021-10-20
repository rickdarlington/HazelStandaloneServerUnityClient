using System.Collections.Generic;
using HazelServer;
using UnityEngine;

namespace UnityClient
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance => instance;
        private static GameStateManager instance;
        
        public Queue<GameUpdateStruct> GameUpdates = new Queue<GameUpdateStruct>();
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != null)
            {
                Debug.Log("[TODO] why would you ever get here?  eliminate this");
                Destroy(this);
            }
        }
        
        private void FixedUpdate()
        {
            int updatesToProcess = GameUpdates.Count;
            if (updatesToProcess == 0) return;
            
            //Debug.Log($"processing {updatesToProcess} updates");
            int i = 0;
            while (i < updatesToProcess)
            {
                var update = GameUpdates.Dequeue();
                //TODO actually do something with this update
                foreach (var p in update.positions)
                {
                    //if this is me, update debugging info
                    if (p.playerId == HazelNetworkManager.Instance.PlayerId)
                    {
                        Debug.Log($"my position: {p.X} . {p.Y}");
                    }
                }
                i++;
            }
        }
    }
}