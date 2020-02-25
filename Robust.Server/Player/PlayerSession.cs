﻿using Robust.Server.Interfaces.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Server.GameObjects;
using System;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Server.Player
{
    /// <summary>
    /// This is the session of a connected client.
    /// </summary>
    internal class PlayerSession : IPlayerSession
    {
        private readonly PlayerManager _playerManager;
        public readonly PlayerState PlayerState;

        public PlayerSession(PlayerManager playerManager, INetChannel client, PlayerData data)
        {
            _playerManager = playerManager;
            SessionId = client.SessionId;
            _data = data;

            PlayerState = new PlayerState
            {
                SessionId = client.SessionId,
            };

            ConnectedClient = client;

            UpdatePlayerState();
        }

        [ViewVariables] public INetChannel ConnectedClient { get; }

        [ViewVariables] public IEntity AttachedEntity { get; private set; }

        [ViewVariables] public EntityUid? AttachedEntityUid => AttachedEntity?.Uid;

        private SessionStatus _status = SessionStatus.Connecting;

        /// <inheritdoc />
        public string Name => SessionId.Username;

        /// <inheritdoc />
        [ViewVariables]
        public SessionStatus Status
        {
            get => _status;
            set
            {
                if (_status == value)
                    return;

                var old = _status;
                _status = value;
                UpdatePlayerState();

                PlayerStatusChanged?.Invoke(this, new SessionStatusEventArgs(this, old, value));
            }
        }

        /// <inheritdoc />
        public DateTime ConnectedTime { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public NetSessionId SessionId { get; }

        private readonly PlayerData _data;
        [ViewVariables] public IPlayerData Data => _data;

        /// <inheritdoc />
        public event EventHandler<SessionStatusEventArgs> PlayerStatusChanged;

        /// <inheritdoc />
        public void AttachToEntity(IEntity a)
        {
            DetachFromEntity();

            if (a == null)
            {
                return;
            }

            var actorComponent = a.AddComponent<BasicActorComponent>();
            actorComponent.playerSession = this;
            AttachedEntity = a;
            a.SendMessage(actorComponent, new PlayerAttachedMsg(this));
            a.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PlayerAttachSystemMessage(a, this));
            SetAttachedEntityName();
            UpdatePlayerState();
        }

        /// <inheritdoc />
        public void DetachFromEntity()
        {
            if (AttachedEntity == null)
                return;

            if (AttachedEntity.Deleted)
            {
                throw new InvalidOperationException("Tried to detach player, but my entity does not exist!");
            }

            if (AttachedEntity.TryGetComponent<BasicActorComponent>(out var actor))
            {
                AttachedEntity.SendMessage(actor, new PlayerDetachedMsg(this));
                AttachedEntity.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PlayerDetachedSystemMessage(AttachedEntity));
                AttachedEntity.RemoveComponent<BasicActorComponent>();
                AttachedEntity = null;
                UpdatePlayerState();
            }
            else
            {
                throw new InvalidOperationException("Tried to detach player, but entity does not have ActorComponent!");
            }
        }

        /// <inheritdoc />
        public void OnConnect()
        {
            ConnectedTime = DateTime.Now;
            Status = SessionStatus.Connected;
            UpdatePlayerState();
        }

        /// <inheritdoc />
        public void OnDisconnect()
        {
            Status = SessionStatus.Disconnected;

            DetachFromEntity();
            UpdatePlayerState();
        }

        private void SetAttachedEntityName()
        {
            if (Name != null && AttachedEntity != null)
            {
                AttachedEntity.Name = Name;
            }
        }

        /// <summary>
        ///     Causes the session to switch from the lobby to the game.
        /// </summary>
        public void JoinGame()
        {
            if (ConnectedClient == null || Status == SessionStatus.InGame)
                return;

            Status = SessionStatus.InGame;
            UpdatePlayerState();
        }

        private void UpdatePlayerState()
        {
            PlayerState.Status = Status;
            PlayerState.Name = Name;
            if (AttachedEntity == null)
                PlayerState.ControlledEntity = null;
            else
                PlayerState.ControlledEntity = AttachedEntity.Uid;

            _playerManager.Dirty();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return SessionId.ToString();
        }
    }
}
