using Magnesium;
using MonoGame.Content;
using MonoGame.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System;

namespace Platformer2D
{
    public class SpriteBatchEffect : IDisposable
    {
        private IMgGraphicsConfiguration mGraphicsConfiguration;
        private IMgGraphicsDevice mGraphicsDevice;
        private IShaderContentStreamer mShaderContent;
        private IMgIndexedIndirectCommandSerializer mSerializer;

        public SpriteBatchEffect(
            IMgGraphicsConfiguration graphicsConfiguration,
            IMgGraphicsDevice graphicsDevice,
            IShaderContentStreamer shaderContent,
            IMgIndexedIndirectCommandSerializer serializer
            )
        {
            mGraphicsConfiguration = graphicsConfiguration;
            mGraphicsDevice = graphicsDevice;
            mShaderContent = shaderContent;
            mSerializer = serializer;
        }

        ~SpriteBatchEffect()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool mIsDisposed = false;
        protected void Dispose(bool Dispose)
        {
            if (mIsDisposed)
                return;

            ReleaseUnmanagedResources();

            mIsDisposed = true;
        }

        private void ReleaseUnmanagedResources()
        {
            if (mPool != null)
            {
                mPool.Dispose();
            }

            if (mIndirectBuffer != null)
            {
                mIndirectBuffer.Dispose();
            }

            if (mVariants != null)
            {
                foreach (var pipe in mVariants)
                {
                    pipe.Dispose();
                }
            }

            if (mBatchBuffer != null)
            {
                mBatchBuffer.Dispose();
            }

            if (mConfiguration != null)
            {
                mConfiguration.Dispose();
            }
        }

        public IMgSpriteBatch Initialise()
        {
            // Configuration here

            mConfiguration = new DefaultSpriteBatchConfiguration(mGraphicsConfiguration.Partition, mShaderContent);

            // 50 sprites 
            const uint NO_OF_INDIVIDUAL_SPRITES = 50;

            var createInfo = new MgSpriteBatchBufferCreateInfo
            {
                IndexType = MgIndexType.UINT16,
                IndicesCount = NO_OF_INDIVIDUAL_SPRITES * 6,
                InstancesCount = NO_OF_INDIVIDUAL_SPRITES,
                MaterialsCount = NO_OF_INDIVIDUAL_SPRITES,
                VerticesCount = NO_OF_INDIVIDUAL_SPRITES * 4,
            };

            IMgAllocationCallbacks allocator = null;
            mBatchBuffer = new MgSpriteBatchBuffer(mGraphicsConfiguration.Partition, createInfo, allocator);

            const uint NO_OF_TEXTURE_SLOTS = 16;

            var seeds = new[]
            {
                new EffectVariantSeed
                {
                    GraphicsDevice = mGraphicsDevice,
                    FragmentShader = new AssetIdentifier { AssetId = 0x00000002 },
                    VertexShader = new AssetIdentifier {  AssetId = 0x00000001 },
                }
            };

            mVariants = mConfiguration.Load(NO_OF_TEXTURE_SLOTS, seeds);

            const uint NO_OF_DESCRIPTOR_SETS = 3;
            mPool = mConfiguration.CreateDescriptorPool(NO_OF_DESCRIPTOR_SETS);

            // Sprite batch here

            mDescriptorSet = mPool.CreateDescriptorSet();

            // Sprite batch here

            mIndirectBuffer = new MgIndirectBufferSpriteInfo(mGraphicsConfiguration.Partition, mSerializer, allocator);

            var command = new MgDrawIndexedIndirectCommand();

            mRenderer = new MgSpriteBatchRenderer(mGraphicsConfiguration.Partition, mBatchBuffer, command)
            {
                Viewport = mGraphicsDevice.CurrentViewport,                
            };

            var batchJob = new DefaultSpriteBatcher(mGraphicsConfiguration, mBatchBuffer, mRenderer, mIndirectBuffer, command);
            return batchJob;
        }

        private static List<MgClearValue> ExtractClearValues(Color clearColor, MgClearRenderPassInfo passInfo)
        {
            var clearValues = new List<MgClearValue>();
            foreach (var attachment in passInfo.Attachments)
            {
                if (attachment.Usage == MgImageAspectFlagBits.COLOR_BIT)
                {
                    var color = clearColor.ToVector4();
                    var clear = attachment.AsColor(new MgColor4f { R = color.X, G = color.Y, B = color.Z, A = color.W });
                    clearValues.Add(clear);
                }
                else if ((attachment.Usage | MgImageAspectFlagBits.DEPTH_BIT) > 0 || (attachment.Usage | MgImageAspectFlagBits.STENCIL_BIT) > 0)
                {
                    var depthStencil = attachment.AsDepthAndStencil(0f, 0);
                    clearValues.Add(depthStencil);
                }
            }

            return clearValues;
        }

        private EffectPipelineDescriptorSet mDescriptorSet;
        private EffectVariant[] mVariants;
        private MgSpriteBatchBuffer mBatchBuffer;
        private MgIndirectBufferSpriteInfo mIndirectBuffer;
        private MgSpriteBatchRenderer mRenderer;
        private EffectDescriptorPool mPool;
        private DefaultSpriteBatchConfiguration mConfiguration;

        //public void Update(MgTexture[] textures)
        //{
        //    mRenderer.Update();
        //    UpdateIndirectBuffer();

        //    const uint BINDING = 0U;
        //    mDescriptorSet.SetTextures(BINDING, 0, textures);
        //}

        //private void UpdateIndirectBuffer()
        //{
        //    const uint FLAGS = 0U;

        //    var device = mGraphicsConfiguration.Device;
        //    var deviceMemory = mIndirectBuffer.DeviceMemory;

        //    IntPtr dest;
        //    var err = deviceMemory.MapMemory(device, mIndirectBuffer.BindOffset, mIndirectBuffer.Stride, FLAGS, out dest);
        //    mSerializer.Serialize(dest, mIndirectBuffer.UpdateOffset, new[] { Command });
        //    deviceMemory.UnmapMemory(device);
        //}
    }
}
