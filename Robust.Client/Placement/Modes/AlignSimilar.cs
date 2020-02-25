﻿using System.Linq;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Placement.Modes
{
    public class AlignSimilar : PlacementMode
    {
        private const uint SnapToRange = 50;

        public AlignSimilar(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);
            var mapGrid = pManager.MapManager.GetGrid(MouseCoords.GridID);
            CurrentTile = mapGrid.GetTileRef(MouseCoords);

            if (pManager.CurrentPermission.IsTile)
            {
                return;
            }

            if (!RangeCheck(MouseCoords))
            {
                return;
            }

            var manager = IoCManager.Resolve<IClientEntityManager>();

            var snapToEntities = manager.GetEntitiesInRange(MouseCoords, SnapToRange)
                .Where(entity => entity.Prototype == pManager.CurrentPrototype && entity.Transform.MapID == mapGrid.ParentMapId)
                .OrderBy(entity => (entity.Transform.WorldPosition - MouseCoords.ToMapPos(pManager.MapManager)).LengthSquared)
                .ToList();

            if (snapToEntities.Count == 0)
            {
                return;
            }

            var closestEntity = snapToEntities[0];
            if (!closestEntity.TryGetComponent<ISpriteComponent>(out var component))
            {
                return;
            }

            var closestBounds = component.BaseRSI.Size;

            var closestRect =
                Box2.FromDimensions(
                    closestEntity.Transform.WorldPosition.X - closestBounds.X / 2f,
                    closestEntity.Transform.WorldPosition.Y - closestBounds.Y / 2f,
                    closestBounds.X, closestBounds.Y);

            var sides = new[]
            {
                new Vector2(closestRect.Left + closestRect.Width / 2f, closestRect.Top - closestBounds.Y / 2f),
                new Vector2(closestRect.Left + closestRect.Width / 2f, closestRect.Bottom + closestBounds.Y / 2f),
                new Vector2(closestRect.Left - closestBounds.X / 2f, closestRect.Top + closestRect.Height / 2f),
                new Vector2(closestRect.Right + closestBounds.X / 2f, closestRect.Top + closestRect.Height / 2f)
            };

            var closestSide =
                (from Vector2 side in sides orderby (side - MouseCoords.Position).LengthSquared select side).First();

            MouseCoords = new GridCoordinates(closestSide, MouseCoords.GridID);
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            if (pManager.CurrentPermission.IsTile)
            {
                return false;
            }
            else if (!RangeCheck(position))
            {
                return false;
            }
            else if (IsColliding(position))
            {
                return false;
            }

            return true;
        }
    }
}
