using Magnesium;
using Microsoft.Xna.Framework;
using MonoGame.Core;
using MonoGame.Graphics;
using System;

namespace Platformer2D
{
    public interface IMgSpriteBatch : IDisposable
    {
        void Compile(IMgCommandBuffer cmd, IMgFramebuffer frame);

        void Begin();

        void Draw(IMgTexture texture, Vector2 position, Rectangle? p, Color color, float v1, Vector2 origin, float v2, SpriteEffects flip, float v3);
        void Draw(IMgTexture texture, Vector2 position, Color white);

        void Draw(uint textureSlot, Vector2 zero, Color white);
        void Draw(uint textureSlot, Vector2 position, object p, Color color, float v1, Vector2 origin, float v2, SpriteEffects none, float v3);

        void DrawString(SpriteFont font, string value, Vector2 vector2, Color black);
        void End();
    }
}