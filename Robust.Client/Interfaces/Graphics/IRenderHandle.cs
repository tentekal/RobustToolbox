using Robust.Client.Graphics.Drawing;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.Interfaces.Graphics
{
    internal interface IRenderHandle
    {
        DrawingHandleScreen DrawingHandleScreen { get; }
        DrawingHandleWorld DrawingHandleWorld { get; }

        void SetScissor(UIBox2i? scissorBox);
        void DrawEntity(IEntity entity, Vector2 position, Vector2 scale, Direction? overrideDirection);
    }
}