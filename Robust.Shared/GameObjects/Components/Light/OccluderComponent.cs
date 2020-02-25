using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    public class OccluderComponent : Component
    {
        public sealed override string Name => "Occluder";
        public sealed override uint? NetID => NetIDs.OCCLUDER;

        private bool _enabled = true;
        private Box2 _boundingBox = new Box2(-0.5f, -0.5f, 0.5f, 0.5f);

        [ViewVariables(VVAccess.ReadWrite)]
        public Box2 BoundingBox
        {
            get => _boundingBox;
            set
            {
                _boundingBox = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                Dirty();
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _enabled, "enabled", true);
            serializer.DataField(ref _boundingBox, "boundingBox", new Box2(-0.5f, -0.5f, 0.5f, 0.5f));
        }

        public override ComponentState GetComponentState()
        {
            return new OccluderComponentState(Enabled, BoundingBox);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
            {
                return;
            }

            var cast = (OccluderComponentState) curState;

            Enabled = cast.Enabled;
            BoundingBox = cast.BoundingBox;
        }

        [NetSerializable, Serializable]
        private sealed class OccluderComponentState : ComponentState
        {
            public bool Enabled { get; }
            public Box2 BoundingBox { get; }

            public OccluderComponentState(bool enabled, Box2 boundingBox) : base(NetIDs.OCCLUDER)
            {
                Enabled = enabled;
                BoundingBox = boundingBox;
            }
        }
    }
}
