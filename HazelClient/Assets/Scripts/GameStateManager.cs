using System;
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

        public Dictionary<uint, EntityMetadata> characters = new Dictionary<uint, EntityMetadata>();

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

        private void Update()
        {
            interpolateEntities();
        }

        private void interpolateEntities()
        {
            long ts = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            foreach (var kv in characters)
            {
                EntityMetadata e = kv.Value;
                //we don't interpolate ourselves, duh
                if (e.playerId != HazelNetworkManager.Instance.PlayerId)
                {
                    if (e.previousPosition.X == e.nextPosition.X && e.previousPosition.Y == e.nextPosition.Y)
                    {
                        continue;
                    }
                    
                    //TODO from Gambetta, can we simplify to make this more clear?
                    //it's also a bit choppy when the other player changes direction
                    //entity.x = x0 + (x1 - x0) * (render_timestamp - t0) / (t1 - t0);
                    float x = e.previousPosition.X + (e.nextPosition.X - e.previousPosition.X) *
                        (ts - e.previousPosition.renderTime) /
                        (e.nextPosition.renderTime - e.previousPosition.renderTime);
                    
                    float y = e.previousPosition.Y + (e.nextPosition.Y - e.previousPosition.Y) *
                        (ts - e.previousPosition.renderTime) /
                        (e.nextPosition.renderTime - e.previousPosition.renderTime);

                    e.gameObject.transform.position = new Vector3(x, y, 0);
                }
            }
        }

        private void FixedUpdate()
        {
            int updatesToProcess = GameUpdates.Count;
            if (updatesToProcess == 0) return;

            if (updatesToProcess > 1)
            {
                Debug.Log($"Our FixedUpdate() is running slower than server: accumulated updates {updatesToProcess}");
            }

            int i = 0;
            while (i < updatesToProcess)
            {
                GameUpdateStruct update = GameUpdates.Dequeue();
                long renderTime = DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;
                
                foreach (var pos in update.positions)
                {
                    //first time we've seen this player?
                    if (!characters.ContainsKey(pos.playerId))
                    {
                        Debug.Log($"spawning character {pos.playerId}");
                        InstanceCharacter(pos);
                    }
                    
                    //TODO implement de-instance player logic

                    EntityMetadata e;
                    if(characters.TryGetValue(pos.playerId, out e))
                    {
                        if (pos.playerId == HazelNetworkManager.Instance.PlayerId)
                        {
                            //reconciliation for us
                            RemoveAckedInputs(pos.lastProcessedInput);
                            Reconciliate(pos, e.gameObject);
                        }
                        else
                        {
                            pos.renderTime = renderTime;
                            e.updatePositionBuffer(pos);
                        }

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
                predictedPosition = Movement.ApplyInput(predictedPosition, input.inputs, input.deltaTime);
            }
            
            //Debug.Log($"server: {pos.X}, {pos.Y} | predicted: {predictedPosition.X}, {predictedPosition.Y} | current: {myPlayer.transform.position.x}, {myPlayer.transform.position.y}");
            
            if (predictedPosition.X != myPlayer.transform.position.x ||
                predictedPosition.Y != myPlayer.transform.position.y)
            {
                //TODO might want to tune this to avoid snaps under reasonable distance
                Debug.Log($"SNAP: {predictedPosition.X}, {predictedPosition.Y} => {myPlayer.transform.position.x}, {myPlayer.transform.position.y}");
                Debug.Log($"distance: {Vector2.Distance(new Vector2(predictedPosition.X, predictedPosition.Y), myPlayer.transform.position)}");
            }
            
            myPlayer.transform.position = new Vector3(predictedPosition.X, predictedPosition.Y, 0);
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
                    //Debug.Log($"unacked inputs: {SentInputs.Count} {SentInputs.Peek().sequenceNumber}");
                    return;
                }
            }
        }

        public void InstanceCharacter(PositionStruct pos)
        {
            //TODO actually set rotation based on pos.lookDirection
            var c = Instantiate(characterPrefab, new Vector3(pos.X, pos.Y, 0), Quaternion.identity);
            
            //TODO refactor? we set pos twice because this is a struct and previous can't be null
            EntityMetadata e = new EntityMetadata(pos.playerId, c, pos, pos);
            characters.Add(pos.playerId, e);
        }

        public GameObject getPlayerGameObject(uint playerId)
        {

            EntityMetadata e;
            if(!characters.TryGetValue(playerId, out e)) 
            {
                Debug.Log($"[ERROR] can't get GameObject for playerId: {playerId} THIS SHOULD NEVER HAPPEN!!!");
            }

            return e.gameObject;
        }
    }
}