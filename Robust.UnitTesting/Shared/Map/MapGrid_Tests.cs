﻿using System.Linq;
using Moq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.Timing;
using Robust.Server.Timing;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using MapGrid = Robust.Shared.Map.MapGrid;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, TestOf(typeof(MapGrid))]
    class MapGrid_Tests : RobustUnitTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            var mapMan = IoCManager.Resolve<IMapManager>();

            mapMan.Initialize();
            mapMan.Startup();
        }

        [Test]
        public void GetTileRefCoords()
        {
            var grid = MapGridFactory(new GridId(1));
            grid.SetTile(new MapIndices(-9, -1), new Tile(1, 2));

            var result = grid.GetTileRef(new MapIndices(-9, -1));

            Assert.That(grid.ChunkCount, Is.EqualTo(1));
            Assert.That(grid.GetMapChunks().Keys.ToList()[0], Is.EqualTo(new MapIndices(-2, -1)));
            Assert.That(result, Is.EqualTo(new TileRef(new MapId(5), new GridId(1), new MapIndices(-9,-1), new Tile(1, 2))));
        }

        /// <summary>
        ///     Verifies that the world Bounds of the grid properly expand when tiles are placed.
        /// </summary>
        [Test]
        public void BoundsExpansion()
        {
            var grid = MapGridFactory(new GridId(1));

            grid.SetTile(new MapIndices(-1, -2), new Tile(1));
            grid.SetTile(new MapIndices(1, 2), new Tile(1));

            var bounds = grid.WorldBounds;

            // this is world, so add the grid world pos
            Assert.That(bounds.Bottom, Is.EqualTo(-2+5));
            Assert.That(bounds.Left, Is.EqualTo(-1+3));
            Assert.That(bounds.Top, Is.EqualTo(3+5));
            Assert.That(bounds.Right, Is.EqualTo(2+3));
        }

        /// <summary>
        ///     Verifies that the world bounds of the grid properly contract when a tile is removed.
        /// </summary>
        [Test]
        public void BoundsContract()
        {
            var grid = MapGridFactory(new GridId(1));

            grid.SetTile(new MapIndices(-1, -2), new Tile(1));
            grid.SetTile(new MapIndices(1, 2), new Tile(1));

            grid.SetTile(new MapIndices(1, 2), Tile.Empty);

            var bounds = grid.WorldBounds;

            // this is world, so add the grid world pos
            Assert.That(bounds.Bottom, Is.EqualTo(-2+5));
            Assert.That(bounds.Left, Is.EqualTo(-1+3));
            Assert.That(bounds.Top, Is.EqualTo(-1+5));
            Assert.That(bounds.Right, Is.EqualTo(0+3));
        }

        [Test]
        public void GridTileToChunkIndices()
        {
            var grid = MapGridFactory(new GridId(1));

            var result = grid.GridTileToChunkIndices(new MapIndices(-9, -1));

            Assert.That(result, Is.EqualTo(new MapIndices(-2, -1)));
        }

        /// <summary>
        ///     Verifies that the local position is centered on the tile, instead of bottom left.
        /// </summary>
        [Test]
        public void ToLocalCentered()
        {
            var grid = MapGridFactory(new GridId(1));

            var result = grid.GridTileToLocal(new MapIndices(0, 0)).Position;

            Assert.That(result.X, Is.EqualTo(0.5f));
            Assert.That(result.Y, Is.EqualTo(0.5f));
        }

        [Test]
        public void TryGetTileRefNoTile()
        {
            var grid = MapGridFactory(new GridId(1));

            var foundTile = grid.TryGetTileRef(new MapIndices(-9, -1), out var tileRef);

            Assert.That(foundTile, Is.False);
            Assert.That(tileRef, Is.EqualTo(new TileRef()));
            Assert.That(grid.ChunkCount, Is.EqualTo(0));
        }

        [Test]
        public void TryGetTileRefTileExists()
        {
            var grid = MapGridFactory(new GridId(1));
            grid.SetTile(new MapIndices(-9, -1), new Tile(1, 2));

            var foundTile = grid.TryGetTileRef(new MapIndices(-9, -1), out var tileRef);

            Assert.That(foundTile, Is.True);
            Assert.That(grid.ChunkCount, Is.EqualTo(1));
            Assert.That(grid.GetMapChunks().Keys.ToList()[0], Is.EqualTo(new MapIndices(-2, -1)));
            Assert.That(tileRef, Is.EqualTo(new TileRef(new MapId(5), new GridId(1), new MapIndices(-9, -1), new Tile(1, 2))));
        }

        [Test]
        public void PointCollidesWithGrid()
        {
            var grid = MapGridFactory(new GridId(1));
            grid.SetTile(new MapIndices(19, 23), new Tile(1));

            var result = grid.CollidesWithGrid(new MapIndices(19, 23));

            Assert.That(result, Is.True);
        }

        [Test]
        public void PointNotCollideWithGrid()
        {
            var grid = MapGridFactory(new GridId(1));
            grid.SetTile(new MapIndices(19, 23), new Tile(1));

            var result = grid.CollidesWithGrid(new MapIndices(19, 24));

            Assert.That(result, Is.False);
        }

        private static IMapGridInternal MapGridFactory(GridId id)
        {
            var entMan = (ServerEntityManager)IoCManager.Resolve<IEntityManager>();
            entMan.CullDeletionHistory(GameTick.MaxValue);

            var mapId = new MapId(5);
            var mapMan = IoCManager.Resolve<IMapManager>();

            if(mapMan.MapExists(mapId))
                mapMan.DeleteMap(mapId);

            mapMan.CreateMap(mapId);

            if(mapMan.GridExists(id))
                mapMan.DeleteGrid(id);

            var newGrid = mapMan.CreateGrid(mapId, id, 8, 1);
            newGrid.WorldPosition = new Vector2(3, 5);

            return (IMapGridInternal)newGrid;
        }
    }
}
