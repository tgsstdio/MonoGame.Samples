using Magnesium;
using MonoGame.Graphics;

namespace Platformer2D
{
    public class MgSpriteBatchRendererCreateInfo
    {
        public MgSpriteBatchBuffer BatchBuffer { get; set; }
        public MgIndirectBufferSpriteInfo Indirect { get; set; }
        public IMgPipeline Pipeline { get; internal set; }
        public EffectPipelineDescriptorSet PipelineDescriptorSet { get; internal set; }
        public MgClearValue[] ClearValues { get; internal set; }
        public MgRect2D RenderArea { get; internal set; }
        public IMgRenderPass RenderPass { get; internal set; }
        public MgViewport Viewport { get; internal set; }
    }
}
