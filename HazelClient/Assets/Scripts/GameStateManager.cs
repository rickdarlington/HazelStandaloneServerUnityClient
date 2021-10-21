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

        public Queue<PlayerInputStruct> SentInputs = new Queue<PlayerInputStruct>();
        
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
            
            int i = 0;
            while (i < updatesToProcess)
            {
                var update = GameUpdates.Dequeue();
                //TODO actually do something with this update
                foreach (var p in update.positions)
                {
                    //special actions for this player
                    if (p.playerId == HazelNetworkManager.Instance.PlayerId)
                    {
                        removeAckedInputs(p.lastProcessedInput);
                        //TODO do reconciliation with what's left in SentInputs
                        
                        Debug.Log($"my position: {p.X} . {p.Y}");
                    }
                }
                i++;
            }
        }

        private void removeAckedInputs(uint lastProcessedInput)
        {
            while (SentInputs.Count > 0)
            {
                PlayerInputStruct i = SentInputs.Peek();
                if (i.sequenceNumber <= lastProcessedInput)
                {
                    SentInputs.Dequeue();
                }
                else
                {
                    return;
                }
            }
        }
    }
}