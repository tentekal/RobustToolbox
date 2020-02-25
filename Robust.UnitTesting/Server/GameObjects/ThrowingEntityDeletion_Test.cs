using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Robust.UnitTesting.Server.GameObjects
{
    [TestFixture]
    public class ThrowingEntityDeletion_Test : RobustUnitTest
    {
        private IServerEntityManager EntityManager;
        private IComponentFactory _componentFactory;
        private IMapManager MapManager;

        const string PROTOTYPES = @"
- type: entity
  id: throwInAdd
  components:
  - type: ThrowsInAdd
- type: entity
  id: throwInExposeData
  components:
  - type: ThrowsInExposeData
- type: entity
  id: throwsInInitialize
  components:
  - type: ThrowsInInitialize
- type: entity
  id: throwsInStartup
  components:
  - type: ThrowsInStartup
";

        [OneTimeSetUp]
        public void Setup()
        {
            _componentFactory = IoCManager.Resolve<IComponentFactory>();

            _componentFactory.Register<ThrowsInAddComponent>();
            _componentFactory.Register<ThrowsInExposeDataComponent>();
            _componentFactory.Register<ThrowsInInitializeComponent>();
            _componentFactory.Register<ThrowsInStartupComponent>();

            EntityManager = IoCManager.Resolve<IServerEntityManager>();
            MapManager = IoCManager.Resolve<IMapManager>();
            MapManager.Initialize();
            MapManager.Startup();

            MapManager.CreateNewMapEntity(MapId.Nullspace);

            var manager = IoCManager.Resolve<IPrototypeManager>();
            manager.LoadFromStream(new StringReader(PROTOTYPES));
            manager.Resync();

            //NOTE: The grids have not moved, so we can assert worldpos == localpos for the test
        }

        [Test]
        public void Test([Values("throwInAdd", "throwInExposeData", "throwsInInitialize", "throwsInStartup")]
            string prototypeName)
        {
            Assert.That(() => EntityManager.SpawnEntity(prototypeName, MapCoordinates.Nullspace),
                Throws.TypeOf<EntityCreationException>());

            Assert.That(EntityManager.GetEntities().Where(p => p.Prototype?.ID == prototypeName), Is.Empty);
        }

        private sealed class ThrowsInAddComponent : Component
        {
            public override string Name => "ThrowsInAdd";

            public override void OnAdd() => throw new NotSupportedException();
        }

        private sealed class ThrowsInExposeDataComponent : Component
        {
            public override string Name => "ThrowsInExposeData";

            public override void ExposeData(ObjectSerializer serializer) => throw new NotSupportedException();
        }

        private sealed class ThrowsInInitializeComponent : Component
        {
            public override string Name => "ThrowsInInitialize";

            public override void Initialize() => throw new NotSupportedException();
        }

        private sealed class ThrowsInStartupComponent : Component
        {
            public override string Name => "ThrowsInStartup";

            protected override void Startup() => throw new NotSupportedException();
        }
    }
}
