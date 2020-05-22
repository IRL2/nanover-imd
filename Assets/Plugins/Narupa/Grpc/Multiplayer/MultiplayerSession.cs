﻿// Copyright (c) Intangible Realities Lab. All rights reserved.
// Licensed under the GPL. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Narupa.Network;
using UnityEngine;
using System.Linq;
using Narupa.Core;
using Narupa.Core.Async;
using Narupa.Core.Math;
using Narupa.Grpc;
using Narupa.Grpc.Multiplayer;
using Narupa.Grpc.Stream;
using Narupa.Protocol.State;

namespace Narupa.Session
{
    /// <summary>
    /// Manages the state of a single user engaging in multiplayer. Tracks
    /// local and remote avatars and maintains a copy of the latest values in
    /// the shared key/value store.
    /// </summary>
    public sealed class MultiplayerSession : IDisposable
    {
        public const string SimulationPoseKey = "scene";
        
        protected string Token { get; }
        
        public MultiplayerAvatars Avatars { get; }

        public MultiplayerSession()
        {
            Token = Guid.NewGuid().ToString();
            
            Avatars = new MultiplayerAvatars(this);
            
            SimulationPose =
                new MultiplayerResource<Transformation>(this, SimulationPoseKey, PoseFromObject,
                                                        PoseToObject);
        }

        /// <summary>
        /// The transformation of the simulation box.
        /// </summary>
        public readonly MultiplayerResource<Transformation> SimulationPose;

        /// <summary>
        /// Dictionary of the currently known shared state.
        /// </summary>
        public Dictionary<string, object> SharedStateDictionary { get; } =
            new Dictionary<string, object>();

        /// <summary>
        /// Is there an open client on this session?
        /// </summary>
        public bool IsOpen => client != null;

        /// <summary>
        /// Has this session created a user?
        /// </summary>
        public bool HasPlayer => PlayerId != null;

        /// <summary>
        /// Username of the current player, if any.
        /// </summary>
        public string PlayerName { get; private set; }

        /// <summary>
        /// ID of the current player, if any.
        /// </summary>
        public string PlayerId { get; private set; }

        /// <summary>
        /// How many milliseconds to put between sending our requested value
        /// changes.
        /// </summary>
        public int ValuePublishInterval { get; set; } = 1000 / 30;

        private MultiplayerClient client;

        private BidirectionalStream<UpdateStateRequest, StateUpdate> ValueUpdates { get; set; }

        private Dictionary<string, object> pendingValues
            = new Dictionary<string, object>();

        private List<string> pendingRemovals
            = new List<string>();

        private Task valueFlushingTask;

        public event Action<string, object> SharedStateDictionaryKeyUpdated;

        public event Action<string> SharedStateDictionaryKeyRemoved;

        public event Action MultiplayerJoined;

        private bool awaitingUpdate = false;

        /// <summary>
        /// The index of the update you are about to send.
        /// </summary>
        public int SendIndex { get; private set; } = 0;

        /// <summary>
        /// The index of the update that you last recieved.
        /// </summary>
        public int RecieveIndex { get; private set; } = -1;

        /// <summary>
        /// Connect to a Multiplayer service over the given connection. 
        /// Closes any existing client.
        /// </summary>
        public void OpenClient(GrpcConnection connection)
        {
            CloseClient();

            client = new MultiplayerClient(connection);
            
            ValueUpdates = client.SubscribeStateUpdates();
            ValueUpdates.MessageReceived += OnResourceValuesUpdateReceived;
            ValueUpdates.StartStreams().AwaitInBackgroundIgnoreCancellation();
            
            if (valueFlushingTask == null)
            {
                valueFlushingTask = CallbackInterval(FlushValues, ValuePublishInterval);
                valueFlushingTask.AwaitInBackground();
            }
        }

        /// <summary>
        /// Close the current Multiplayer client and dispose all streams.
        /// </summary>
        public void CloseClient()
        {
            Avatars.CloseClient();
            FlushValues();
            
            client?.CloseAndCancelAllSubscriptions();
            client?.Dispose();
            client = null;

            PlayerName = null;
            PlayerId = null;

            ClearSharedState();
            pendingValues.Clear();
            pendingRemovals.Clear();
        }

        /// <summary>
        /// Create a new multiplayer with the given username, subscribe avatar 
        /// and value updates, and begin publishing our avatar.
        /// </summary>
        public async Task JoinMultiplayer(string playerName)
        {
            if (HasPlayer)
                throw new InvalidOperationException($"Multiplayer already joined as {PlayerName}");

            var response = await client.CreatePlayer(playerName);

            PlayerName = playerName;
            PlayerId = response.PlayerId;

            MultiplayerJoined?.Invoke();
        }
        
        /// <summary>
        /// Set the given key in the shared state dictionary, which will be
        /// sent to the server according in the future according to the publish 
        /// interval.
        /// </summary>
        public void SetSharedState(string key, object value)
        {
            pendingValues[key] = value.ToProtobufValue();
            pendingRemovals.Remove(key);
        }
        
        /// <summary>
        /// Remove the given key from the shared state dictionary, which will be
        /// sent to the server according in the future according to the publish 
        /// interval.
        /// </summary>
        public void RemoveSharedStateKey(string key)
        {
            pendingValues.Remove(key);
            pendingRemovals.Add(key);
        }


        /// <summary>
        /// Get a key in the shared state dictionary.
        /// </summary>
        public object GetSharedState(string key)
        {
            return SharedStateDictionary.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Attempt to gain exclusive write access to the shared value of the given key.
        /// </summary>
        public async Task<bool> LockResource(string id)
        {
            return await client.UpdateLocks(Token, new Dictionary<string, float>
                                            {
                                                [id] = 1f
                                            },
                                            new string[0]);
        }

        /// <summary>
        /// Release the lock on the given object of a given key.
        /// </summary>
        public async Task<bool> ReleaseResource(string id)
        {
            return await client.UpdateLocks(Token, new Dictionary<string, float>(), new string[]
            {
                id
            });
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            FlushValues();

            CloseClient();
        }

        private void ClearSharedState()
        {
            var keys = SharedStateDictionary.Keys.ToList();
            SharedStateDictionary.Clear();

            foreach (var key in keys)
            {
                try
                {
                    SharedStateDictionaryKeyRemoved?.Invoke(key);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private void OnResourceValuesUpdateReceived(StateUpdate update)
        {
            RecieveIndex++;
            try
            {
                if (update.ChangedKeys != null)
                {
                    foreach (var (key, value1) in update.ChangedKeys.Fields)
                    {
                        var value = value1.ToObject();
                        if (value == null)
                        {
                            SharedStateDictionary.Remove(key);
                            SharedStateDictionaryKeyRemoved?.Invoke(key);
                        }
                        else
                        {
                            SharedStateDictionary[key] = value;
                            SharedStateDictionaryKeyUpdated?.Invoke(key, value);
                        }
                    }
                }
            }
            finally
            {
                Debug.Log("Recieve Update");
                awaitingUpdate = false;
            }
        }

        private void FlushValues()
        {
            if (!IsOpen)
                return;

            if (awaitingUpdate)
                return;
            
            var request = new UpdateStateRequest
            {
                AccessToken = Token,
                Update = MultiplayerClient.CreateStateUpdate(pendingValues, pendingRemovals)
            };
            
            ValueUpdates.QueueMessageAsync(request).AwaitInBackgroundIgnoreCancellation();
            Debug.Log("Send Update");
            SendIndex++;
            pendingRemovals.Clear();
            pendingValues.Clear();
            awaitingUpdate = true;
        }

        private static async Task CallbackInterval(Action callback, int interval)
        {
            while (true)
            {
                callback();
                await Task.Delay(interval);
            }
        }

        private static object PoseToObject(Transformation pose)
        {
            var data = new object[]
            {
                pose.Position.x, pose.Position.y, pose.Position.z, pose.Rotation.x, pose.Rotation.y,
                pose.Rotation.z, pose.Rotation.w, pose.Scale.x, pose.Scale.y, pose.Scale.z,
            };

            return data;
        }

        private static Transformation PoseFromObject(object @object)
        {
            if (@object is List<object> list)
            {
                var values = list.Select(value => Convert.ToSingle(value)).ToList();
                var position = list.GetVector3(0);
                var rotation = list.GetQuaternion(3);
                var scale = list.GetVector3(7);

                return new Transformation(position, rotation, scale);
            }

            throw new ArgumentOutOfRangeException();
        }

        public MultiplayerResource<object> GetSharedResource(string key)
        {
            return new MultiplayerResource<object>(this, key);
        }
    }
}