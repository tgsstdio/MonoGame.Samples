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

        public void Initialise()
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
        }

        #region Create methods

        public IMgSpriteBatch Create(Color clearColor, EffectVariant variant, EffectPipelineDescriptorSet descriptorSet)
        {
            SetupBoundDescriptorSet(descriptorSet);

            return InitializeSpriteBatch(clearColor, variant, descriptorSet);
        }

        private MgSpriteBatchRenderer InitializeSpriteBatch(Color clearColor, EffectVariant variant, EffectPipelineDescriptorSet descriptorSet)
        {
            IMgAllocationCallbacks allocator = null;
            var indirect = new MgIndirectBufferSpriteInfo(mGraphicsConfiguration.Partition, mSerializer, allocator);

            var graphicsDevice = variant.GraphicsDevice;

            var passInfo = new MgClearRenderPassInfo(graphicsDevice.RenderpassInfo);
            var clearValues = ExtractClearValues(clearColor, passInfo);

            var createInfo = new MgSpriteBatchRendererCreateInfo
            {
                BatchBuffer = mBatchBuffer,
                Indirect = indirect,
                ClearValues = clearValues.ToArray(),
                PipelineDescriptorSet = descriptorSet,
                Pipeline = variant.Pipeline,
                RenderArea = graphicsDevice.Scissor,
                RenderPass = graphicsDevice.Renderpass,
                Viewport = graphicsDevice.CurrentViewport,
            };

            return new MgSpriteBatchRenderer(mGraphicsConfiguration.Device, createInfo);
        }

        private void SetupBoundDescriptorSet(EffectPipelineDescriptorSet descriptorSet)
        {
            mDescriptorSet = descriptorSet;

            var constantBufferInfo = new MgDescriptorBufferInfo[]
            {
                new MgDescriptorBufferInfo
                {
                    Buffer = mBatchBuffer.Buffer,
                    Offset = mBatchBuffer.Materials.Offset,
                    Range = mBatchBuffer.Materials.ArraySize,
                }
            };

            const uint FIRST_CONSTANT_BUFFER_BINDING = 1U;
            descriptorSet.Begin();
            descriptorSet.SetConstantBuffers(FIRST_CONSTANT_BUFFER_BINDING, 0, constantBufferInfo);
        }

        private static List<MgClearValue> ExtractClearValues(Color clearColor, MgClearRenderPassInfo passInfo)
        {
            var clearValues = new List<MgClearValue>();
            foreach (var attachment in passInfo.Attachments)
            {
                if (attachment.Usage == MgImageAspectFlagBits.COLOR_BIT)
                {
                    var clear = attachment.AsColor(AsColor4f(clearColor));
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

        private static MgColor4f AsColor4f(Color value)
        {
            var color = value.ToVector4();
            return new MgColor4f { R = color.X, G = color.Y, B = color.Z, A = color.W };
        }

        private static Vector4 AsVector4(Color value)
        {
            var color = value.ToVector4();
            return new Vector4(color.X, color.Y, color.Z, color.W);
        }

        #endregion

        private EffectPipelineDescriptorSet mDescriptorSet;
        private EffectVariant[] mVariants;

        public EffectVariant[] VariantEffects
        {
            get
            {
                return mVariants;
            }
        }

        private MgSpriteBatchBuffer mBatchBuffer;
        private EffectDescriptorPool mPool;

        public EffectDescriptorPool DescriptorPool
        {
            get
            {
                return mPool;
            }
        }

        private DefaultSpriteBatchConfiguration mConfiguration;
    }
}
