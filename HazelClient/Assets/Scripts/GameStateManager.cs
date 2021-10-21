using System;
using System.Collections.Generic;
using HazelServer;
using Unity.Mathematics;
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

                    GameObject g = null;
                    if(characters.TryGetValue(pos.playerId, out g))
                    {
                        //special actions for this player
                        if (pos.playerId == HazelNetworkManager.Instance.PlayerId)
                        {
                            RemoveAckedInputs(pos.lastProcessedInput);
                            Reconciliate(pos, g);
                            Debug.Log($"my position: {pos.X} . {pos.Y}");
                        }
                        
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
            System.Numerics.Vector2 predictedPosition = new System.Numerics.Vector2(pos.X, pos.Y);

            foreach (PlayerInputStruct input in SentInputs)
            {
                predictedPosition = Movement.ApplyInput(predictedPosition, input.inputs);
            }

            myPlayer.transform.position = new Vector3(predictedPosition.X, predictedPosition.Y, 0); 
            //TODO we need something to manage rendering, animate the sprite, etc
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