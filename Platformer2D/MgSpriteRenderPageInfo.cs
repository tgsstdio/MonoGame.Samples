using Magnesium;
using MonoGame.Graphics;
using System;
using System.Diagnostics;

namespace Platformer2D
{
    public class MgSpriteRenderPageInfo : IDisposable
    {
        public IMgPipeline Pipeline { get; set; }
        public IMgDescriptorSet DescriptorSet { get; set; }
        public IMgPipelineLayout Layout { get; set; }
        public MgClearValue[] ClearValues { get; set; }
        public MgRect2D RenderArea { get; set; }
        public IMgRenderPass RenderPass { get; set; }
        private MgSpriteBatchBuffer mBatchBuffer;
        private MgIndirectBufferSpriteInfo mIndirect;

        public MgSpriteRenderPageInfo(
            MgSpriteBatchBuffer batchBuffer,
            MgIndirectBufferSpriteInfo indirect
            )
        {
            mBatchBuffer = batchBuffer;
            mIndirect = indirect;
        }

        ~MgSpriteRenderPageInfo()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool mIsDisposed = false;
        protected void Dispose(bool dispose)
        {
            if (mIsDisposed)
                return;
                
            if (mIndirect != null)
            {
                mIndirect.Dispose();
            }

            mIsDisposed = true;
        }

        public void Compose(IMgCommandBuffer cmdBuf, IMgFramebuffer frame)
        {
            if (cmdBuf == null)
                throw new ArgumentNullException(nameof(cmdBuf));

            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            Debug.Assert(RenderPass != null);

            var renderPassCreateInfo = new MgRenderPassBeginInfo
            {
                RenderArea = RenderArea,
                Framebuffer = frame,
                ClearValues = ClearValues,
                RenderPass = RenderPass,
            };

            cmdBuf.CmdBeginRenderPass(renderPassCreateInfo, MgSubpassContents.INLINE);

            cmdBuf.CmdBindDescriptorSets(MgPipelineBindPoint.GRAPHICS, Layout, 0, 1, new[] { DescriptorSet }, null);
            cmdBuf.CmdBindPipeline(MgPipelineBindPoint.GRAPHICS, Pipeline);
            cmdBuf.CmdBindIndexBuffer(mBatchBuffer.Buffer, mBatchBuffer.Indices.Offset, mBatchBuffer.IndexType);
            cmdBuf.CmdBindVertexBuffers(0, new[] { mBatchBuffer.Buffer, mBatchBuffer.Buffer }, new[] { mBatchBuffer.Vertices.Offset, mBatchBuffer.Instances.Offset });

            cmdBuf.CmdDrawIndexedIndirect(mIndirect.Buffer, mIndirect.BindOffset, 1, mIndirect.Stride);

            cmdBuf.CmdEndRenderPass();
        }
    }
    
}
