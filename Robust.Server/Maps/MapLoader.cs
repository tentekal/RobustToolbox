﻿using System;
using System.Collections.Generic;
using System.IO;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Maps;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;
using System.Globalization;
using Robust.Shared.Interfaces.GameObjects;
using System.Linq;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects.Components.Map;
using YamlDotNet.Core;

namespace Robust.Server.Maps
{
    /// <summary>
    ///     Saves and loads maps to the disk.
    /// </summary>
    public class MapLoader : IMapLoader
    {
        private const int MapFormatVersion = 2;

        [Dependency]
#pragma warning disable 649
        private readonly IResourceManager _resMan;

        [Dependency]
        private readonly IMapManagerInternal _mapManager;

        [Dependency]
        private readonly ITileDefinitionManager _tileDefinitionManager;

        [Dependency]
        private readonly IServerEntityManagerInternal _serverEntityManager;

        [Dependency] private readonly IPauseManager _pauseManager;
        [Dependency] private readonly IComponentManager _componentManager;
        [Dependency] private readonly IComponentFactory _componentFactory;
#pragma warning restore 649

        /// <inheritdoc />
        public void SaveBlueprint(GridId gridId, string yamlPath)
        {
            var grid = _mapManager.GetGrid(gridId);

            var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager, _pauseManager, _componentFactory, _componentManager);
            context.RegisterGrid(grid);
            var root = context.Serialize();
            var document = new YamlDocument(root);

            var resPath = new ResourcePath(yamlPath).ToRootedPath();
            _resMan.UserData.CreateDir(resPath.Directory);

            using (var file = _resMan.UserData.Open(resPath, FileMode.Create))
            {
                using (var writer = new StreamWriter(file))
                {
                    var stream = new YamlStream();

                    stream.Add(document);
                    stream.Save(new YamlMappingFix(new Emitter(writer)), false);
                }
            }
        }

        /// <inheritdoc />
        public IMapGrid LoadBlueprint(MapId mapId, string path)
        {
            TextReader reader;
            var resPath = new ResourcePath(path).ToRootedPath();

            // try user
            if (!_resMan.UserData.Exists(resPath))
            {
                Logger.InfoS("map", $"No user blueprint path: {resPath}");

                // fallback to content
                if (_resMan.TryContentFileRead(resPath, out var contentReader))
                {
                    reader = new StreamReader(contentReader);
                }
                else
                {
                    Logger.ErrorS("map", $"No blueprint found: {resPath}");
                    return null;
                }
            }
            else
            {
                var file = _resMan.UserData.Open(resPath, FileMode.Open);
                reader = new StreamReader(file);
            }

            IMapGrid grid;
            using (reader)
            {
                Logger.InfoS("map", $"Loading Grid: {resPath}");

                var data = new MapData(reader);

                if (data.GridCount != 1)
                {
                    throw new InvalidDataException("Cannot instance map with multiple grids as blueprint.");
                }

                var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager, _pauseManager, _componentFactory, _componentManager, (YamlMappingNode)data.RootNode, mapId);
                context.Deserialize();
                grid = context.Grids[0];

                if (!context.MapIsPostInit && _pauseManager.IsMapInitialized(mapId))
                {
                    foreach (var entity in context.Entities)
                    {
                        entity.RunMapInit();
                    }
                }
            }

            return grid;
        }

        /// <inheritdoc />
        public void SaveMap(MapId mapId, string yamlPath)
        {
            var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager, _pauseManager, _componentFactory, _componentManager);
            foreach (var grid in _mapManager.GetAllMapGrids(mapId))
            {
                context.RegisterGrid(grid);
            }

            var document = new YamlDocument(context.Serialize());

            var resPath = new ResourcePath(yamlPath).ToRootedPath();
            _resMan.UserData.CreateDir(resPath.Directory);

            using (var file = _resMan.UserData.Open(resPath, FileMode.Create))
            {
                using (var writer = new StreamWriter(file))
                {
                    var stream = new YamlStream();

                    stream.Add(document);
                    stream.Save(new YamlMappingFix(new Emitter(writer)), false);
                }
            }
        }

        /// <inheritdoc />
        public void LoadMap(MapId mapId, string path)
        {
            TextReader reader;
            var resPath = new ResourcePath(path).ToRootedPath();

            // try user
            if (!_resMan.UserData.Exists(resPath))
            {
                Logger.InfoS("map", $"No user blueprint found: {resPath}");

                // fallback to content
                if (_resMan.TryContentFileRead(resPath, out var contentReader))
                {
                    reader = new StreamReader(contentReader);
                }
                else
                {
                    Logger.ErrorS("map", $"No blueprint found: {resPath}");
                    return;
                }
            }
            else
            {
                var file = _resMan.UserData.Open(resPath, FileMode.Open);
                reader = new StreamReader(file);
            }

            using (reader)
            {
                Logger.InfoS("map", $"Loading Map: {resPath}");

                var data = new MapData(reader);

                if (data.GridCount != 1)
                {
                    throw new InvalidDataException("Cannot instance map with multiple grids as blueprint.");
                }

                var context = new MapContext(_mapManager, _tileDefinitionManager, _serverEntityManager, _pauseManager, _componentFactory, _componentManager, (YamlMappingNode)data.RootNode, mapId);
                context.Deserialize();

                if (!context.MapIsPostInit && _pauseManager.IsMapInitialized(mapId))
                {
                    foreach (var entity in context.Entities)
                    {
                        entity.RunMapInit();
                    }
                }
            }
        }

        /// <summary>
        ///     Handles the primary bulk of state during the map serialization process.
        /// </summary>
        private class MapContext : YamlObjectSerializer.Context, IEntityLoadContext
        {
            private readonly IMapManagerInternal _mapManager;
            private readonly ITileDefinitionManager _tileDefinitionManager;
            private readonly IServerEntityManagerInternal _serverEntityManager;
            private readonly IPauseManager _pauseManager;
            private readonly IComponentFactory _componentFactory;
            private readonly IComponentManager _componentManager;

            private readonly Dictionary<GridId, int> GridIDMap = new Dictionary<GridId, int>();
            public readonly List<IMapGrid> Grids = new List<IMapGrid>();

            private readonly Dictionary<EntityUid, int> EntityUidMap = new Dictionary<EntityUid, int>();
            private readonly Dictionary<int, EntityUid> UidEntityMap = new Dictionary<int, EntityUid>();
            public readonly List<IEntity> Entities = new List<IEntity>();

            private readonly List<(IEntity, YamlMappingNode)> _entitiesToDeserialize
                = new List<(IEntity, YamlMappingNode)>();

            private bool IsBlueprintMode => GridIDMap.Count == 1;

            private int uidCounter;

            private readonly YamlMappingNode RootNode;
            private readonly MapId TargetMap;

            private Dictionary<string, YamlMappingNode> CurrentReadingEntityComponents;

            private string CurrentWritingComponent;
            private IEntity CurrentWritingEntity;

            private Dictionary<ushort, string> _tileMap;

            public bool MapIsPostInit { get; private set; }

            public MapContext(IMapManagerInternal maps, ITileDefinitionManager tileDefs, IServerEntityManagerInternal entities, IPauseManager pauseManager, IComponentFactory componentFactory, IComponentManager componentManager)
            {
                _mapManager = maps;
                _tileDefinitionManager = tileDefs;
                _serverEntityManager = entities;
                _pauseManager = pauseManager;
                _componentFactory = componentFactory;
                _componentManager = componentManager;

                RootNode = new YamlMappingNode();
            }

            public MapContext(IMapManagerInternal maps, ITileDefinitionManager tileDefs, IServerEntityManagerInternal entities,
                IPauseManager pauseManager, IComponentFactory componentFactory, IComponentManager componentManager, YamlMappingNode node, MapId targetMapId)
            {
                _mapManager = maps;
                _tileDefinitionManager = tileDefs;
                _serverEntityManager = entities;
                _pauseManager = pauseManager;
                _componentFactory = componentFactory;
                _componentManager = componentManager;

                RootNode = node;
                TargetMap = targetMapId;
            }

            // Deserialization
            public void Deserialize()
            {
                // First we load map meta data like version.
                ReadMetaSection();

                // Load grids.
                ReadTileMapSection();
                ReadGridSection();

                // Entities are first allocated. This allows us to know the future UID of all entities on the map before
                // even ExposeData is loaded. This allows us to resolve serialized EntityUid instances correctly.
                AllocEntities();

                // Actually instance components and run ExposeData on them.
                FinishEntitiesLoad();

                // Clear the net tick numbers so that components from prototypes (not modified by map)
                // aren't sent over the wire initially.
                ResetNetTicks();

                // Grid entities were NOT created inside ReadGridSection().
                // We have to fix the created grids up with the grid entities deserialized from the map.
                FixMapEntities();

                // We have to attach grids to the target map here.
                // If we don't, initialization & startup can fail for some entities.
                AttachMapEntities();

                // Run Initialize on all components.
                FinishEntitiesInitialization();

                // Run Startup on all components.
                FinishEntitiesStartup();
            }

            private void ResetNetTicks()
            {
                foreach (var (entity, data) in _entitiesToDeserialize)
                {
                    if (!data.TryGetNode("components", out YamlSequenceNode componentList))
                    {
                        continue;
                    }

                    if (entity.Prototype == null)
                    {
                        continue;
                    }

                    foreach (var component in _componentManager.GetNetComponents(entity.Uid))
                    {
                        var castComp = (Component) component;

                        if (componentList.Any(p => p["type"].AsString() == component.Name))
                        {
                            if (entity.Prototype.Components.ContainsKey(component.Name))
                            {
                                // This component is modified by the map so we have to send state.
                                // Though it's still in the prototype itself so creation doesn't need to be sent.
                                castComp.ClearCreationTick();
                            }
                            else
                            {
                                // New component that the prototype normally does not have, need to sync full data.
                                continue;
                            }
                        }

                        // This component is not modified by the map file,
                        // so the client will have the same data after instantiating it from prototype ID.
                        castComp.ClearTicks();
                    }
                }
            }

            private void AttachMapEntities()
            {
                var mapEntity = _mapManager.GetMapEntity(TargetMap);

                foreach (var grid in Grids)
                {
                    var entity = _serverEntityManager.GetEntity(grid.GridEntityId);
                    entity.Transform.AttachParent(mapEntity);
                }
            }

            private void FixMapEntities()
            {
                foreach (var entity in Entities)
                {
                    if (entity.TryGetComponent(out IMapGridComponent grid))
                    {
                        var castGrid = (MapGrid) grid.Grid;
                        castGrid.GridEntityId = entity.Uid;
                    }
                }
            }

            private void ReadMetaSection()
            {
                var meta = RootNode.GetNode<YamlMappingNode>("meta");
                var ver = meta.GetNode("format").AsInt();
                if (ver != MapFormatVersion)
                {
                    throw new InvalidDataException("Cannot handle this map file version.");
                }

                if (meta.TryGetNode("postmapinit", out var mapInitNode))
                {
                    MapIsPostInit = mapInitNode.AsBool();
                }
                else
                {
                    MapIsPostInit = true;
                }
            }

            private void ReadTileMapSection()
            {
                // Load tile mapping so that we can map the stored tile IDs into the ones actually used at runtime.
                _tileMap = new Dictionary<ushort, string>();

                var tileMap = RootNode.GetNode<YamlMappingNode>("tilemap");
                foreach (var (key, value) in tileMap)
                {
                    var tileId = (ushort) key.AsInt();
                    var tileDefName = value.AsString();
                    _tileMap.Add(tileId, tileDefName);
                }
            }

            private void ReadGridSection()
            {
                var grids = RootNode.GetNode<YamlSequenceNode>("grids");

                foreach (var grid in grids)
                {
                    var newId = new GridId?();
                    YamlGridSerializer.DeserializeGrid(
                        _mapManager, TargetMap, ref newId,
                        (YamlMappingNode)grid["settings"],
                        (YamlSequenceNode)grid["chunks"],
                        _tileMap,
                        _tileDefinitionManager
                    );

                    if (newId != null)
                    {
                        Grids.Add(_mapManager.GetGrid(newId.Value));
                    }
                }
            }

            private void AllocEntities()
            {
                var entities = RootNode.GetNode<YamlSequenceNode>("entities");
                foreach (var entityDef in entities.Cast<YamlMappingNode>())
                {
                    string type = null;
                    if (entityDef.TryGetNode("type", out var typeNode))
                    {
                        type = typeNode.AsString();
                    }

                    var uid = Entities.Count;
                    if (entityDef.TryGetNode("uid", out var uidNode))
                    {
                        uid = uidNode.AsInt();
                    }

                    var entity = _serverEntityManager.AllocEntity(type);
                    Entities.Add(entity);
                    UidEntityMap.Add(uid, entity.Uid);
                    _entitiesToDeserialize.Add((entity, entityDef));
                }
            }

            private void FinishEntitiesLoad()
            {
                foreach (var (entity, data) in _entitiesToDeserialize)
                {
                    CurrentReadingEntityComponents = new Dictionary<string, YamlMappingNode>();
                    if (data.TryGetNode("components", out YamlSequenceNode componentList))
                    {
                        foreach (var compData in componentList)
                        {
                            CurrentReadingEntityComponents[compData["type"].AsString()] = (YamlMappingNode)compData;
                        }
                    }

                    _serverEntityManager.FinishEntityLoad(entity, this);
                }
            }

            private void FinishEntitiesInitialization()
            {
                foreach (var entity in Entities)
                {
                    _serverEntityManager.FinishEntityInitialization(entity);
                }
            }

            private void FinishEntitiesStartup()
            {
                foreach (var entity in Entities)
                {
                    _serverEntityManager.UpdateEntityTree(entity);
                }

                foreach (var entity in Entities)
                {
                    _serverEntityManager.FinishEntityStartup(entity);
                }

                foreach (var entity in Entities)
                {
                    _serverEntityManager.UpdateEntityTree(entity);
                }
            }

            // Serialization
            public void RegisterGrid(IMapGrid grid)
            {
                if (GridIDMap.ContainsKey(grid.Index))
                {
                    throw new InvalidOperationException();
                }

                Grids.Add(grid);
                GridIDMap.Add(grid.Index, GridIDMap.Count);
            }

            public YamlNode Serialize()
            {
                WriteMetaSection();
                WriteTileMapSection();
                WriteGridSection();

                PopulateEntityList();
                WriteEntitySection();

                return RootNode;
            }

            private void WriteMetaSection()
            {
                var meta = new YamlMappingNode();
                RootNode.Add("meta", meta);
                meta.Add("format", MapFormatVersion.ToString(CultureInfo.InvariantCulture));
                // TODO: Make these values configurable.
                meta.Add("name", "DemoStation");
                meta.Add("author", "Space-Wizards");

                var isPostInit = false;
                foreach (var grid in Grids)
                {
                    if (_pauseManager.IsMapInitialized(grid.ParentMapId))
                    {
                        isPostInit = true;
                        break;
                    }
                }

                meta.Add("postmapinit", isPostInit ? "true" : "false");
            }

            private void WriteTileMapSection()
            {
                var tileMap = new YamlMappingNode();
                RootNode.Add("tilemap", tileMap);
                foreach (var tileDefinition in _tileDefinitionManager)
                {
                    tileMap.Add(tileDefinition.TileId.ToString(CultureInfo.InvariantCulture), tileDefinition.Name);
                }
            }

            private void WriteGridSection()
            {
                var grids = new YamlSequenceNode();
                RootNode.Add("grids", grids);

                foreach (var grid in Grids)
                {
                    var entry = YamlGridSerializer.SerializeGrid(grid);
                    grids.Add(entry);
                }
            }

            private void PopulateEntityList()
            {
                foreach (var entity in _serverEntityManager.GetEntities())
                {
                    if (IsMapSavable(entity))
                    {
                        var uid = uidCounter++;
                        EntityUidMap.Add(entity.Uid, uid);
                        Entities.Add(entity);
                    }
                }
            }

            private void WriteEntitySection()
            {
                var entities = new YamlSequenceNode();
                RootNode.Add("entities", entities);

                foreach (var entity in Entities)
                {
                    CurrentWritingEntity = entity;
                    var mapping = new YamlMappingNode
                    {
                        {"uid", EntityUidMap[entity.Uid].ToString(CultureInfo.InvariantCulture)}
                    };

                    if (entity.Prototype != null)
                    {
                        mapping.Add("type", entity.Prototype.ID);
                    }

                    var components = new YamlSequenceNode();
                    // See engine#636 for why the Distinct() call.
                    foreach (var component in entity.GetAllComponents())
                    {
                        var compMapping = new YamlMappingNode();
                        CurrentWritingComponent = component.Name;
                        var compSerializer = YamlObjectSerializer.NewWriter(compMapping, this);

                        component.ExposeData(compSerializer);

                        // Don't need to write it if nothing was written!
                        if (compMapping.Children.Count != 0)
                        {
                            // Something actually got written!
                            compMapping.Add("type", component.Name);
                            components.Add(compMapping);
                        }
                    }

                    if (components.Children.Count != 0)
                    {
                        mapping.Add("components", components);
                    }

                    entities.Add(mapping);
                }
            }

            public override bool TryNodeToType(YamlNode node, Type type, out object obj)
            {
                if (type == typeof(GridId))
                {
                    if (node.AsString() == "null")
                    {
                        obj = GridId.Invalid;
                        return true;
                    }

                    var val = node.AsInt();
                    if (val >= Grids.Count)
                    {
                        Logger.ErrorS("map", "Error in map file: found local grid ID '{0}' which does not exist.", val);
                    }
                    else
                    {
                        obj = Grids[val].Index;
                        return true;
                    }
                }
                if (type == typeof(EntityUid))
                {
                    if (node.AsString() == "null")
                    {
                        obj = EntityUid.Invalid;
                        return true;
                    }

                    var val = node.AsInt();
                    if (val >= Entities.Count)
                    {
                        Logger.ErrorS("map", "Error in map file: found local entity UID '{0}' which does not exist.", val);
                    }
                    else
                    {
                        obj = UidEntityMap[val];
                        return true;
                    }
                }
                if (typeof(IEntity).IsAssignableFrom(type))
                {
                    var val = node.AsInt();
                    if (val >= Entities.Count)
                    {
                        Logger.ErrorS("map", "Error in map file: found local entity UID '{0}' which does not exist.", val);
                    }
                    else
                    {
                        obj = Entities[val];
                        return true;
                    }
                }
                obj = null;
                return false;
            }

            public override bool TryTypeToNode(object obj, out YamlNode node)
            {
                switch (obj)
                {
                    case GridId gridId:
                        if (!GridIDMap.TryGetValue(gridId, out var gridMapped))
                        {
                            Logger.WarningS("map", "Cannot write grid ID '{0}', falling back to nullspace.", gridId);
                            break;
                        }
                        else
                        {
                            node = new YamlScalarNode(gridMapped.ToString(CultureInfo.InvariantCulture));
                            return true;
                        }

                    case EntityUid entityUid:
                        if (!EntityUidMap.TryGetValue(entityUid, out var entityUidMapped))
                        {
                            // Terrible hack to mute this warning on the grids themselves when serializing blueprints.
                            if (!IsBlueprintMode || !CurrentWritingEntity.HasComponent<MapGridComponent>() ||
                                CurrentWritingComponent != "Transform")
                            {
                                Logger.WarningS("map", "Cannot write entity UID '{0}'.", entityUid);
                            }

                            node = new YamlScalarNode("null");
                            return true;
                        }
                        else
                        {
                            node = new YamlScalarNode(entityUidMapped.ToString(CultureInfo.InvariantCulture));
                            return true;
                        }

                    case IEntity entity:
                        if (!EntityUidMap.TryGetValue(entity.Uid, out var entityMapped))
                        {
                            Logger.WarningS("map", "Cannot write entity UID '{0}'.", entity.Uid);
                            break;
                        }
                        else
                        {
                            node = new YamlScalarNode(entityMapped.ToString(CultureInfo.InvariantCulture));
                            return true;
                        }
                }
                node = null;
                return false;
            }

            // Create custom object serializers that will correctly allow data to be overriden by the map file.
            ObjectSerializer IEntityLoadContext.GetComponentSerializer(string componentName, YamlMappingNode protoData)
            {
                if (CurrentReadingEntityComponents == null)
                {
                    throw new InvalidOperationException();
                }

                if (CurrentReadingEntityComponents.TryGetValue(componentName, out var mapping))
                {
                    var list = new List<YamlMappingNode> {mapping};
                    if (protoData != null)
                    {
                        list.Add(protoData);
                    }
                    return YamlObjectSerializer.NewReader(list, this);
                }

                return YamlObjectSerializer.NewReader(protoData, this);
            }

            public IEnumerable<string> GetExtraComponentTypes()
            {
                return CurrentReadingEntityComponents.Keys;
            }

            public override bool IsValueDefault<T>(string field, T value)
            {
                if (CurrentWritingEntity.Prototype == null)
                {
                    // No prototype, can't be default.
                    return false;
                }

                if (!CurrentWritingEntity.Prototype.Components.TryGetValue(CurrentWritingComponent, out var compData))
                {
                    // This component was added mid-game.
                    return false;
                }

                var testSer = YamlObjectSerializer.NewReader(compData);
                if (testSer.TryReadDataFieldCached(field, out T prototypeVal))
                {
                    if (value == null)
                    {
                        return prototypeVal == null;
                    }

                    return value.Equals(prototypeVal);
                }

                return false;
            }

            private bool IsMapSavable(IEntity entity)
            {
                if (entity.Prototype?.MapSavable == false || !GridIDMap.ContainsKey(entity.Transform.GridID))
                {
                    return false;
                }

                // Don't serialize things parented to un savable things.
                // For example clothes inside a person.
                var current = entity.Transform;
                while (current.Parent != null)
                {
                    if (current.Parent.Owner.Prototype?.MapSavable == false)
                    {
                        return false;
                    }

                    current = current.Parent;
                }

                return true;
            }
        }

        /// <summary>
        ///     Does basic pre-deserialization checks on map file load.
        ///     For example, let's not try to use maps with multiple grids as blueprints, shall we?
        /// </summary>
        private class MapData
        {
            public YamlNode RootNode { get; }
            public int GridCount { get; }

            public MapData(TextReader reader)
            {
                var stream = new YamlStream();
                stream.Load(reader);

                if (stream.Documents.Count < 1)
                {
                    throw new InvalidDataException("Stream has no YAML documents.");
                }

                // Kinda wanted to just make this print a warning and pick [0] but screw that.
                // What is this, a hug box?
                if (stream.Documents.Count > 1)
                {
                    throw new InvalidDataException("Stream too many YAML documents. Map files store exactly one.");
                }

                RootNode = stream.Documents[0].RootNode;
                GridCount = ((YamlSequenceNode)RootNode["grids"]).Children.Count;
            }
        }
    }
}
