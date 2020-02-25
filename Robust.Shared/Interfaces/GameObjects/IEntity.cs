﻿using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Timing;

namespace Robust.Shared.Interfaces.GameObjects
{
    public interface IEntity
    {
        GameTick LastModifiedTick { get; }

        /// <summary>
        /// The Entity Manager that controls this entity.
        /// </summary>
        IEntityManager EntityManager { get; }

        /// <summary>
        ///     The name of this entity.
        ///     This is the actual IC display name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        ///     The unique ID of this entity.
        ///     Unique IDs are unique per entity,
        ///     and correspond to counterparts across the network.
        /// </summary>
        EntityUid Uid { get; }

        /// <summary>
        ///     Whether this entity has fully initialized.
        /// </summary>
        bool Initialized { get; }

        bool Initializing { get; }

        /// <summary>
        ///     True if the entity has been deleted.
        /// </summary>
        bool Deleted { get; }

        /// <summary>
        ///     The prototype that was used to create this entity.
        /// </summary>
        EntityPrototype Prototype { get; }

        /// <summary>
        /// The string that describes this entity via examine
        /// </summary>
        string Description { get; }

        /// <summary>
        ///     Determines if this entity is still valid.
        /// </summary>
        /// <returns>True if this entity is still valid.</returns>
        bool IsValid();

        /// <summary>
        ///     The entity's transform component.
        /// </summary>
        ITransformComponent Transform { get; }

        /// <summary>
        ///     The MetaData Component of this entity.
        /// </summary>
        IMetaDataComponent MetaData { get; }

        /// <summary>
        ///     Public method to add a component to an entity.
        ///     Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <typeparam name="T">The component type to add.</typeparam>
        /// <returns>The newly added component.</returns>
        T AddComponent<T>()
            where T : Component, new();

        /// <summary>
        ///     Removes the component with the specified reference type,
        ///     Without needing to have the component itself.
        /// </summary>
        /// <typeparam name="T">The component reference type to remove.</typeparam>
        void RemoveComponent<T>();

        /// <summary>
        ///     Checks to see if the entity has a component of the specified type.
        /// </summary>
        /// <typeparam name="T">The component reference type to check.</typeparam>
        /// <returns>True if the entity has a component of type <typeparamref name="T" />, false otherwise.</returns>
        bool HasComponent<T>();

        /// <summary>
        ///     Checks to see ift he entity has a component of the specified type.
        /// </summary>
        /// <param name="t">The component reference type to check.</param>
        /// <returns></returns>
        bool HasComponent(Type type);

        /// <summary>
        ///     Retrieves the component of the specified type.
        /// </summary>
        /// <typeparam name="T">The component reference type to fetch.</typeparam>
        /// <returns>The retrieved component.</returns>
        /// <exception cref="Shared.GameObjects.UnknownComponentException">
        ///     Thrown if there is no component with the specified type.
        /// </exception>
        T GetComponent<T>();

        /// <summary>
        ///     Retrieves the component of the specified type.
        /// </summary>
        /// <param name="type">The component reference type to fetch.</param>
        /// <returns>The retrieved component.</returns>
        /// <exception cref="Shared.GameObjects.UnknownComponentException">
        ///     Thrown if there is no component with the specified type.
        /// </exception>
        IComponent GetComponent(Type type);

        /// <summary>
        ///     Retrieves the component with the specified network ID.
        /// </summary>
        /// <param name="netID">The net ID of the component to retrieve.</param>
        /// <returns>The component with the provided net ID.</returns>
        /// <seealso cref="IComponent.NetID" />
        /// <exception cref="Shared.GameObjects.UnknownComponentException">
        ///     Thrown if there is no component with the specified net ID.
        /// </exception>
        IComponent GetComponent(uint netID);

        /// <summary>
        ///     Attempt to retrieve the component with specified type,
        ///     writing it to the <paramref name="component" /> out parameter if it was found.
        /// </summary>
        /// <typeparam name="T">The component reference type to attempt to fetch.</typeparam>
        /// <param name="component">The component, if it was found. Null otherwise.</param>
        /// <returns>True if a component with specified type was found.</returns>
        bool TryGetComponent<T>(out T component);

        /// <summary>
        ///     Attempt to retrieve the component with specified type,
        ///     writing it to the <paramref name="component" /> out parameter if it was found.
        /// </summary>
        /// <param name="type">The component reference type to attempt to fetch.</param>
        /// <param name="component">The component, if it was found. Null otherwise.</param>
        /// <returns>True if a component with specified type was found.</returns>
        bool TryGetComponent(Type type, out IComponent component);

        /// <summary>
        ///     Attempt to retrieve the component with specified network ID,
        ///     writing it to the <paramref name="component" /> out parameter if it was found.
        /// </summary>
        /// <param name="type">The component net ID to attempt to fetch.</param>
        /// <param name="component">The component, if it was found. Null otherwise.</param>
        /// <returns>True if a component with specified net ID was found.</returns>
        bool TryGetComponent(uint netID, out IComponent component);

        /// <summary>
        ///     Used by the entity manager to delete the entity.
        ///     Do not call directly. If you want to delete entities,
        ///     see <see cref="Delete" />.
        /// </summary>
        void Shutdown();

        /// <summary>
        ///     Deletes this entity.
        /// </summary>
        void Delete();

        /// <summary>
        ///     Returns all components on the entity.
        /// </summary>
        /// <returns>An enumerable of components on the entity.</returns>
        IEnumerable<IComponent> GetAllComponents();

        /// <summary>
        ///     Returns all components that are assignable to <typeparamref name="T"/>.
        ///     This does not go by component references.
        /// </summary>
        /// <typeparam name="T">The type that components must implement.</typeparam>
        /// <returns>An enumerable over the found components.</returns>
        IEnumerable<T> GetAllComponents<T>();

        /// <summary>
        ///     Sends a message to all other components in this entity.
        /// </summary>
        /// <param name="owner">Object that sent the event.</param>
        /// <param name="message">Message to send.</param>
        void SendMessage(IComponent owner, ComponentMessage message);

        /// <summary>
        ///     Sends a message over the network to the counterpart component. This works both ways.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="message">Message to send.</param>
        void SendNetworkMessage(IComponent owner, ComponentMessage message, INetChannel channel = null);

        void Dirty();
    }
}
