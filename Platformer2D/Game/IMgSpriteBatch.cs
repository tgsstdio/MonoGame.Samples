using Microsoft.Xna.Framework;
using MonoGame.Core;
using MonoGame.Graphics;

namespace Platformer2D
{
    public interface IMgSpriteBatch
    {
        void Draw(IMgTexture texture, Vector2 position, object p, Color color, float v1, Vector2 origin, float v2, SpriteEffects none, float v3);
        void Draw(IMgTexture mgTexture2D, Vector2 zero, Color white);
        void DrawString(SpriteFont font, string value, Vector2 vector2, Color black);
    }
}