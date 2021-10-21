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
                foreach (var pos in update.positions)
                {
                    //special actions for this player
                    if (pos.playerId == HazelNetworkManager.Instance.PlayerId)
                    {
                        removeAckedInputs(pos.lastProcessedInput);
                        
                        //TODO need GameObjects
                        //reconciliate(pos, myPlayer);
                        
                        Debug.Log($"my position: {pos.X} . {pos.Y}");
                    }
                }
                i++;
            }
        }

        private void reconciliate(PositionStruct pos, GameObject myPlayer)
        {
            //TODO implement me!
            //update myPlayer position with Movement.ApplyInput
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