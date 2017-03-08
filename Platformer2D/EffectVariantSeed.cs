using Magnesium;
using MonoGame.Content;

namespace Platformer2D
{
    public class EffectVariantSeed
    {
        public IMgGraphicsDevice GraphicsDevice { get; set; }
        public AssetIdentifier VertexShader { get; set; }
        public AssetIdentifier FragmentShader { get; set; }
    }
}
