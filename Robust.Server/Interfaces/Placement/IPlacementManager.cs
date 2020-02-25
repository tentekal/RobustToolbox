﻿using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Network.Messages;

namespace Robust.Server.Interfaces.Placement
{
    public interface IPlacementManager
    {
        /// <summary>
        ///     Initializes this manager into a usable state.
        /// </summary>
        void Initialize();

        /// <summary>
        ///  Handles placement related client messages.
        /// </summary>
        void HandleNetMessage(MsgPlacement msg);

        void HandlePlacementRequest(MsgPlacement msg);

        /// <summary>
        ///  Places mob in entity placement mode with given settings.
        /// </summary>
        void SendPlacementBegin(IEntity mob, int range, string objectType, string alignOption);

        /// <summary>
        ///  Places mob in tile placement mode with given settings.
        /// </summary>
        void SendPlacementBeginTile(IEntity mob, int range, string tileType, string alignOption);

        /// <summary>
        ///  Cancels object placement mode for given mob.
        /// </summary>
        void SendPlacementCancel(IEntity mob);

        /// <summary>
        ///  Gives Mob permission to place entity and places it in object placement mode.
        /// </summary>
        void StartBuilding(IEntity mob, int range, string objectType, string alignOption);

        /// <summary>
        ///  Gives Mob permission to place tile and places it in object placement mode.
        /// </summary>
        void StartBuildingTile(IEntity mob, int range, string tileType, string alignOption);

        /// <summary>
        ///  Revokes open placement Permission and cancels object placement mode.
        /// </summary>
        void CancelBuilding(IEntity mob);

        /// <summary>
        ///  Gives a mob a permission to place a given Entity.
        /// </summary>
        void AssignBuildPermission(IEntity mob, int range, string objectType, string alignOption);

        /// <summary>
        ///  Gives a mob a permission to place a given Tile.
        /// </summary>
        void AssignBuildPermissionTile(IEntity mob, int range, string tileType, string alignOption);

        /// <summary>
        ///  Removes all building Permissions for given mob.
        /// </summary>
        void RevokeAllBuildPermissions(IEntity mob);

        Func<MsgPlacement, bool> AllowPlacementFunc { get; set; }
    }
}
