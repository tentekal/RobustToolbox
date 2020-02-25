﻿using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Interfaces.GameObjects;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public sealed class ClientEntityManager : EntityManager, IClientEntityManager, IDisposable
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#if EXCEPTION_TOLERANCE
        [Dependency] private readonly IRuntimeLog _runtimeLog;
#endif
#pragma warning restore 649

        private int _nextClientEntityUid = EntityUid.ClientUid + 1;

        public override void Startup()
        {
            base.Startup();

            if (Started)
            {
                throw new InvalidOperationException("Startup() called multiple times");
            }

            EntitySystemManager.Initialize();
            Started = true;
        }

        public void ApplyEntityStates(List<EntityState> curEntStates, IEnumerable<EntityUid> deletions,
            List<EntityState> nextEntStates)
        {
            var toApply = new Dictionary<IEntity, (EntityState, EntityState)>();
            var toInitialize = new List<Entity>();
            deletions ??= new EntityUid[0];

            if (curEntStates != null && curEntStates.Count != 0)
            {
                foreach (var es in curEntStates)
                {
                    //Known entities
                    if (Entities.TryGetValue(es.Uid, out var entity))
                    {
                        toApply.Add(entity, (es, null));
                    }
                    else //Unknown entities
                    {
                        var metaState =
                            (MetaDataComponentState) es.ComponentStates.First(c => c.NetID == NetIDs.META_DATA);
                        var newEntity = CreateEntity(metaState.PrototypeId, es.Uid);
                        toApply.Add(newEntity, (es, null));
                        toInitialize.Add(newEntity);
                    }
                }
            }

            if (nextEntStates != null && nextEntStates.Count != 0)
            {
                foreach (var es in nextEntStates)
                {
                    if (Entities.TryGetValue(es.Uid, out var entity))
                    {
                        if (toApply.TryGetValue(entity, out var state))
                        {
                            toApply[entity] = (state.Item1, es);
                        }
                        else
                        {
                            toApply[entity] = (null, es);
                        }
                    }
                }
            }

            // Make sure this is done after all entities have been instantiated.
            foreach (var kvStates in toApply)
            {
                var ent = kvStates.Key;
                var entity = (Entity) ent;
                HandleEntityState(entity.EntityManager.ComponentManager, entity, kvStates.Value.Item1,
                    kvStates.Value.Item2);
            }

            foreach (var kvp in toApply)
            {
                UpdateEntityTree(kvp.Key);
            }

            foreach (var id in deletions)
            {
                DeleteEntity(id);
            }

#if EXCEPTION_TOLERANCE
            HashSet<Entity> brokenEnts = new HashSet<Entity>();
#endif

            foreach (var entity in toInitialize)
            {
#if EXCEPTION_TOLERANCE
                try
                {
#endif
                    InitializeEntity(entity);
#if EXCEPTION_TOLERANCE
                }
                catch (Exception e)
                {
                    Logger.ErrorS("state", $"Server entity threw in Init: uid={entity.Uid}, proto={entity.Prototype}\n{e}");
                    brokenEnts.Add(entity);
                }
#endif
            }

            foreach (var entity in toInitialize)
            {
#if EXCEPTION_TOLERANCE
                if(brokenEnts.Contains(entity))
                    continue;

                try
                {
#endif
                    StartEntity(entity);
#if EXCEPTION_TOLERANCE
                }
                catch (Exception e)
                {
                    Logger.ErrorS("state", $"Server entity threw in Start: uid={entity.Uid}, proto={entity.Prototype}\n{e}");
                    brokenEnts.Add(entity);
                }
#endif
            }

            foreach (var entity in toInitialize)
            {
#if EXCEPTION_TOLERANCE
                if(brokenEnts.Contains(entity))
                    continue;
#endif
                UpdateEntityTree(entity);
            }
#if EXCEPTION_TOLERANCE
            foreach (var entity in brokenEnts)
            {
                entity.Delete();
            }
#endif
        }

        public void Dispose()
        {
            Shutdown();
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string prototypeName)
        {
            return CreateEntity(prototypeName);
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string prototypeName, GridCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName, GenerateEntityUid());
            if (coordinates.GridID != GridId.Invalid)
            {
                var gridEntityId = _mapManager.GetGrid(coordinates.GridID).GridEntityId;
                newEntity.Transform.AttachParent(GetEntity(gridEntityId));
                newEntity.Transform.LocalPosition = coordinates.Position;
            }

            return newEntity;
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string prototypeName, MapCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName, GenerateEntityUid());
            newEntity.Transform.AttachParent(_mapManager.GetMapEntity(coordinates.MapId));
            newEntity.Transform.WorldPosition = coordinates.Position;
            return newEntity;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntity(string protoName, GridCoordinates coordinates)
        {
            var newEnt = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity((Entity) newEnt);
            UpdateEntityTree(newEnt);
            return newEnt;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntity(string protoName, MapCoordinates coordinates)
        {
            var entity = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity((Entity) entity);
            UpdateEntityTree(entity);
            return entity;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntityNoMapInit(string protoName, GridCoordinates coordinates)
        {
            return SpawnEntity(protoName, coordinates);
        }


        protected override EntityUid GenerateEntityUid()
        {
            return new EntityUid(_nextClientEntityUid++);
        }

        private void HandleEntityState(IComponentManager compMan, IEntity entity, EntityState curState,
            EntityState nextState)
        {
            var compStateWork = new Dictionary<uint, (ComponentState curState, ComponentState nextState)>();
            var entityUid = entity.Uid;

            if (curState?.ComponentChanges != null)
            {
                foreach (var compChange in curState.ComponentChanges)
                {
                    if (compChange.Deleted)
                    {
                        if (compMan.TryGetComponent(entityUid, compChange.NetID, out var comp))
                        {
                            compMan.RemoveComponent(entityUid, comp);
                        }
                    }
                    else
                    {
                        if (compMan.HasComponent(entityUid, compChange.NetID))
                            continue;

                        var newComp = (Component) IoCManager.Resolve<IComponentFactory>()
                            .GetComponent(compChange.ComponentName);
                        newComp.Owner = entity;
                        compMan.AddComponent(entity, newComp, true);
                    }
                }
            }

            if (curState?.ComponentStates != null)
            {
                foreach (var compState in curState.ComponentStates)
                {
                    compStateWork[compState.NetID] = (compState, null);
                }
            }

            if (nextState?.ComponentStates != null)
            {
                foreach (var compState in nextState.ComponentStates)
                {
                    if (compStateWork.TryGetValue(compState.NetID, out var state))
                    {
                        compStateWork[compState.NetID] = (state.curState, compState);
                    }
                    else
                    {
                        compStateWork[compState.NetID] = (null, compState);
                    }
                }
            }

            foreach (var kvStates in compStateWork)
            {
                if (!compMan.TryGetComponent(entityUid, kvStates.Key, out var component))
                {
                    DebugTools.Assert("Component does not exist for state.");
                    continue;
                }

                try
                {
                    component.HandleComponentState(kvStates.Value.curState, kvStates.Value.nextState);
                }
                catch (Exception e)
                {
                    var wrapper = new ComponentStateApplyException(
                        $"Failed to apply comp state: entity={component.Owner}, comp={component.Name}", e);
#if EXCEPTION_TOLERANCE
                    _runtimeLog.LogException(wrapper, "Component state apply");
#else
                    throw wrapper;
#endif
                }
            }
        }
    }
}
