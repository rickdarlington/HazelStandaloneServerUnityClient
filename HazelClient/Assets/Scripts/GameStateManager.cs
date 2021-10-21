using System.Collections.Generic;
using HazelServer;
using UnityEngine;

namespace UnityClient
{
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField] private GameObject characterPrefab;
        public static GameStateManager Instance => instance;
        private static GameStateManager instance;
        
        public Queue<GameUpdateStruct> GameUpdates = new Queue<GameUpdateStruct>();

        public Queue<PlayerInputStruct> SentInputs = new Queue<PlayerInputStruct>();

        public Dictionary<uint, GameObject> characters = new Dictionary<uint, GameObject>();

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
                    //first time we've seen this player?
                    if (!characters.ContainsKey(pos.playerId))
                    {
                        InstanceCharacter(pos);
                    }
                    
                    //TODO implement de-instance player logic

                    //special actions for this player
                    if (pos.playerId == HazelNetworkManager.Instance.PlayerId)
                    {
                        RemoveAckedInputs(pos.lastProcessedInput);
                        
                        //TODO need GameObjects
                        //Reconciliate(pos, myPlayer);
                        
                        Debug.Log($"my position: {pos.X} . {pos.Y}");
                    }

                    GameObject g = null;
                    if(characters.TryGetValue(pos.playerId, out g))
                    {
                        //TODO you want interpolation here?
                        g.transform.position = new Vector3(pos.X, pos.Y, 0);
                        //TODO also update look direction
                    }
                    else
                    {
                        Debug.Log($"[ERROR] you didn't instance a character with ID {pos.playerId}");
                    }
                }
                i++;
            }
        }

        private void Reconciliate(PositionStruct pos, GameObject myPlayer)
        {
            //TODO implement me!
            //update myPlayer position with Movement.ApplyInput
        }

        private void RemoveAckedInputs(uint lastProcessedInput)
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

        public void InstanceCharacter(PositionStruct pos)
        {
            //TODO actually set rotation based on pos.lookDirection
            var c = Instantiate(characterPrefab, new Vector3(pos.X, pos.Y, 0), Quaternion.identity);
            characters.Add(pos.playerId, c);
        }
    }
}