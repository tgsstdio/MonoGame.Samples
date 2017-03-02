using Magnesium;
using Microsoft.Xna.Framework;
using MonoGame.Graphics;
using System;
using System.Diagnostics;

namespace Platformer2D
{
    public class DefaultSwapchain : IDisposable
    {
        private IMgPresentationLayer mPresentationLayer;
        private IMgGraphicsConfiguration mGraphicsConfiguration;
        private IMgCommandBuffer[] mBarrierCommands;

        public DefaultSwapchain(IMgPresentationLayer presentationLayer, IMgGraphicsConfiguration graphicsConfiguration)
        {
            mPresentationLayer = presentationLayer;
            mGraphicsConfiguration = graphicsConfiguration;
        }

        public IMgCommandBuffer PrePresentBarrierCmd { get; private set; }
        public IMgCommandBuffer PostPresentBarrierCmd { get; private set; }
        public IMgSemaphore PresentComplete { get; private set; }
        public IMgSemaphore RenderComplete { get; private set; }
        public GraphRenderer Graph { get; private set; }

        public void Initialize()
        {
            const int NO_OF_BUFFERS = 2;
            mBarrierCommands = new IMgCommandBuffer[NO_OF_BUFFERS];
            var pAllocateInfo = new MgCommandBufferAllocateInfo
            {
                CommandBufferCount = NO_OF_BUFFERS,
                CommandPool = mGraphicsConfiguration.Partition.CommandPool,
                Level = MgCommandBufferLevel.PRIMARY,
            };

            mGraphicsConfiguration.Device.AllocateCommandBuffers(pAllocateInfo, mBarrierCommands);

            PrePresentBarrierCmd = mBarrierCommands[0];
            PostPresentBarrierCmd = mBarrierCommands[1];

            // Create a semaphore used to synchronize image presentation
            // Ensures that the image is displayed before we start submitting new commands to the queu
            IMgSemaphore presentComplete;
            var err = mGraphicsConfiguration.Device.CreateSemaphore(new MgSemaphoreCreateInfo(), null, out presentComplete);
            Debug.Assert(err == Result.SUCCESS);
            PresentComplete = presentComplete;

            // Create a semaphore used to synchronize command submission
            // Ensures that the image is not presented until all commands have been sumbitted and executed
            IMgSemaphore renderComplete;
            err = mGraphicsConfiguration.Device.CreateSemaphore(new MgSemaphoreCreateInfo(), null, out renderComplete);
            Debug.Assert(err == Result.SUCCESS);
            RenderComplete = renderComplete;

            Graph = new GraphRenderer();
        }

        public void Render(GameTime gameTime)
        {
            Debug.Assert(PostPresentBarrierCmd != null);
            Debug.Assert(mPresentationLayer != null);
            Debug.Assert(PrePresentBarrierCmd != null);
            Debug.Assert(RenderComplete != null);

            uint frameIndex = mPresentationLayer.BeginDraw(PostPresentBarrierCmd, PresentComplete);            

            Graph.Render(mGraphicsConfiguration.Queue, gameTime, frameIndex);

            // should use semaphores instead
            mGraphicsConfiguration.Queue.QueueWaitIdle();

            mPresentationLayer.EndDraw(new uint[] { frameIndex }, PrePresentBarrierCmd, new[] { RenderComplete });
        }

        private bool mIsDisposed = false;
        public void Dispose()
        {
            if (mIsDisposed)
                return;

            if (RenderComplete != null)
            {
                RenderComplete.DestroySemaphore(mGraphicsConfiguration.Device, null);
                RenderComplete = null;
            }

            if (PresentComplete != null)
            {
                PresentComplete.DestroySemaphore(mGraphicsConfiguration.Device, null);
                PresentComplete = null;
            }

            if (mBarrierCommands != null)
            {
                mGraphicsConfiguration.Device.FreeCommandBuffers(mGraphicsConfiguration.Partition.CommandPool, mBarrierCommands);
                mBarrierCommands = null;
            }

            mIsDisposed = true;
        }
    }
}
