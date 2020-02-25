﻿using System.Collections.Generic;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     A square section of a <see cref="IMapGrid"/>.
    /// </summary>
    public interface IMapChunk : IEnumerable<TileRef>
    {
        /// <summary>
        ///     The number of tiles per side of the square chunk.
        /// </summary>
        ushort ChunkSize { get; }

        /// <summary>
        ///     The X index of this chunk inside the <see cref="IMapGrid"/>.
        /// </summary>
        int X { get; }

        /// <summary>
        ///     The Y index of this chunk inside the <see cref="IMapGrid"/>.
        /// </summary>
        int Y { get; }

        /// <summary>
        ///     The positional indices of this chunk in the <see cref="IMapGrid"/>.
        /// </summary>
        MapIndices Indices { get; }

        /// <summary>
        ///     Returns the tile at the given indices.
        /// </summary>
        /// <param name="xIndex">The X tile index relative to the chunk origin.</param>
        /// <param name="yIndex">The Y tile index relative to the chunk origin.</param>
        /// <returns>A reference to a tile.</returns>
        TileRef GetTileRef(ushort xIndex, ushort yIndex);

        /// <summary>
        ///     Returns the tile reference at the given indices.
        /// </summary>
        /// <param name="indices">The tile indices relative to the chunk origin.</param>
        /// <returns>A reference to a tile.</returns>
        TileRef GetTileRef(MapIndices indices);

        Tile GetTile(ushort xIndex, ushort yIndex);

        /// <summary>
        ///     Returns all of the tiles in the chunk, while optionally filtering empty files.
        ///     Returned order is guaranteed to be row-major.
        /// </summary>
        /// <param name="ignoreEmpty">Will empty (space) tiles be added to the collection?</param>
        /// <returns></returns>
        IEnumerable<TileRef> GetAllTiles(bool ignoreEmpty = true);

        /// <summary>
        ///     Replaces a single tile inside of the chunk.
        /// </summary>
        /// <param name="xIndex">The X tile index relative to the chunk.</param>
        /// <param name="yIndex">The Y tile index relative to the chunk.</param>
        /// <param name="tile">The new tile to insert.</param>
        void SetTile(ushort xIndex, ushort yIndex, Tile tile);

        /// <summary>
        ///     Transforms Tile indices relative to the grid into tile indices relative to this chunk.
        /// </summary>
        /// <param name="gridTile">Tile indices relative to the grid.</param>
        /// <returns>Tile indices relative to this chunk.</returns>
        MapIndices GridTileToChunkTile(MapIndices gridTile);

        /// <summary>
        ///     Translates chunk tile indices to grid tile indices.
        /// </summary>
        /// <param name="chunkTile">The indices relative to the chunk origin.</param>
        /// <returns>The indices relative to the grid origin.</returns>
        MapIndices ChunkTileToGridTile(MapIndices chunkTile);

        IEnumerable<SnapGridComponent> GetSnapGridCell(ushort xCell, ushort yCell, SnapGridOffset offset);

        void AddToSnapGridCell(ushort xCell, ushort yCell, SnapGridOffset offset, SnapGridComponent snap);
        void RemoveFromSnapGridCell(ushort xCell, ushort yCell, SnapGridOffset offset, SnapGridComponent snap);

        Box2i CalcLocalBounds();

        Box2 CalcWorldBounds();

        /// <summary>
        /// Tests if a point is on top of a non-empty tile.
        /// </summary>
        /// <param name="localIndices">Local tile indices</param>
        bool CollidesWithChunk(MapIndices localIndices);
    }
}
