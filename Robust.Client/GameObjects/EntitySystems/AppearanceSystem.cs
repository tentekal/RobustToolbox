﻿using System.Collections.Generic;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.GameObjects.EntitySystems
{
    internal sealed class AppearanceSystem : EntitySystem
    {
        private readonly Queue<AppearanceComponent> _updatesQueued = new Queue<AppearanceComponent>();

        public override void FrameUpdate(float frameTime)
        {
            while (_updatesQueued.TryDequeue(out var appearance))
            {
                UpdateComponent(appearance);
                appearance.UnmarkDirty();
            }
        }

        private static void UpdateComponent(AppearanceComponent component)
        {
            foreach (var visualizer in component.Visualizers)
            {
                switch (visualizer)
                {
                    case AppearanceComponent.SpriteLayerToggle spriteLayerToggle:
                        UpdateSpriteLayerToggle(component, spriteLayerToggle);
                        break;

                    default:
                        visualizer.OnChangeData(component);
                        break;
                }
            }
        }

        private static void UpdateSpriteLayerToggle(AppearanceComponent component, AppearanceComponent.SpriteLayerToggle toggle)
        {
            component.TryGetData(toggle.Key, out bool visible);
            var sprite = component.Owner.GetComponent<SpriteComponent>();
            sprite.LayerSetVisible(toggle.SpriteLayer, visible);
        }

        public void EnqueueAppearanceUpdate(AppearanceComponent component)
        {
            _updatesQueued.Enqueue(component);
        }
    }
}
