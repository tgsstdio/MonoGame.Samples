using Magnesium;
using MonoGame.Content;
using MonoGame.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System;

namespace Platformer2D
{
    partial class BufferCrap
    {
        private IMgGraphicsConfiguration mGraphicsConfiguration;
        private IMgGraphicsDevice mGraphicsDevice;
        private IShaderContentStreamer mShaderContent;
        private IMgIndexedIndirectCommandSerializer mSerializer;

        public BufferCrap(
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

        public void Initialise(Color clearColor)
        {
            // Configuration here

            var config = new DefaultSpriteBatchConfiguration(mGraphicsConfiguration.Partition, mShaderContent);

            var createInfo = new MgSpriteBatchBufferCreateInfo
            {
                IndexType = MgIndexType.UINT16,
                IndicesCount = 200,
                InstancesCount = 200,
                MaterialsCount = 32,
                VerticesCount = 200,
            };

            IMgAllocationCallbacks allocator = null;
            mBatchBuffer = new MgSpriteBatchBuffer(mGraphicsConfiguration.Partition, createInfo, allocator);

            const uint NO_OFTEXTURE_SLOTS = 3;

            var seeds = new[]
            {
                new EffectVariantSeed
                {
                    GraphicsDevice = mGraphicsDevice,
                    FragmentShader = new AssetIdentifier { },
                    VertexShader = new AssetIdentifier { },
                }
            };

            mVariants = config.Load(NO_OFTEXTURE_SLOTS, seeds);

            const uint NO_OF_DESCRIPTOR_SETS = 3;
            var pool = config.CreateDescriptorPool(NO_OF_DESCRIPTOR_SETS);

            // Sprite batch here

            mDescriptorSet = pool.CreateDescriptorSet();
            var bufferInfos = new MgDescriptorBufferInfo[]
            {
                new MgDescriptorBufferInfo
                {
                    Buffer = mBatchBuffer.Buffer,
                    Offset = mBatchBuffer.Materials.Offset,
                    Range = mBatchBuffer.Materials.ArraySize,
                }
            };
            mDescriptorSet.SetConstantBuffers(0, 0, bufferInfos);

            // Sprite batch here

            var effectDs = mDescriptorSet;

            var descriptorSet = effectDs.DescriptorSet;
            var pipelineLayout = effectDs.PipelineLayout;
            var variantPipeline = mVariants[0];
            var stride = (uint)mSerializer.GetStride();
            mIndirectBuffer = new MgIndirectBufferSpriteInfo(mGraphicsConfiguration.Partition, mSerializer, allocator);

            mRenderer = new MgSpriteBatchRenderer(mGraphicsConfiguration.Partition, mBatchBuffer, Command)
            {
                Viewport = mGraphicsDevice.CurrentViewport,                
            };

            var batchJob = new DefaultSpriteBatcher(mGraphicsConfiguration, mBatchBuffer, mRenderer, mIndirectBuffer, mSerializer);
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

        private MgSpriteRenderPageInfo mRenderPage;

        private EffectPipelineDescriptorSet mDescriptorSet;
        private EffectVariant[] mVariants;
        private MgSpriteBatchBuffer mBatchBuffer;
        private MgIndirectBufferSpriteInfo mIndirectBuffer;
        private MgSpriteBatchRenderer mRenderer;

        public MgDrawIndexedIndirectCommand Command { get; private set; }

        public void Update(MgTexture[] textures)
        {
            mRenderer.Update();
            UpdateIndirectBuffer();

            const uint BINDING = 0U;
            mDescriptorSet.SetTextures(BINDING, 0, textures);
        }

        private void UpdateIndirectBuffer()
        {
            const uint FLAGS = 0U;

            var device = mGraphicsConfiguration.Device;
            var deviceMemory = mIndirectBuffer.DeviceMemory;

            IntPtr dest;
            var err = deviceMemory.MapMemory(device, mIndirectBuffer.BindOffset, mIndirectBuffer.Stride, FLAGS, out dest);
            mSerializer.Serialize(dest, mIndirectBuffer.UpdateOffset, new[] { Command });
            deviceMemory.UnmapMemory(device);
        }

        public void Compile(IMgCommandBuffer cmdBuf, int index)
        {
            IMgFramebuffer frame = mGraphicsDevice.Framebuffers[index];
            mRenderPage.Compose(cmdBuf, frame);
        }
    }
}
