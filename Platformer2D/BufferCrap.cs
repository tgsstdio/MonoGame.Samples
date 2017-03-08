using Magnesium;
using MonoGame.Content;
using MonoGame.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Platformer2D
{
    class BufferCrap
    {
        private IMgGraphicsConfiguration mGraphicsConfiguration;
        private IMgGraphicsDevice mGraphicsDevice;
        private IShaderContentStreamer mShaderContent;

        public BufferCrap(
            IMgGraphicsConfiguration graphicsConfiguration,
            IMgGraphicsDevice graphicsDevice,
            IShaderContentStreamer shaderContent
            )
        {
            mGraphicsConfiguration = graphicsConfiguration;
            mGraphicsDevice = graphicsDevice;
            mShaderContent = shaderContent;
        }

        public void Initialise(MgColor4f clearColor)
        {
            // SPRITEBATCH
            mClearColor = clearColor;

            var batch = new DefaultSpriteBatch(mGraphicsConfiguration.Partition, mShaderContent);

            var createInfo = new MgSpriteBatchBufferCreateInfo
            {
                IndexType = MgIndexType.UINT16,
                IndicesCount = 200,
                InstancesCount = 200,
                MaterialsCount = 32,
                VerticesCount = 200,
            };

            mBatchBuffer = new MgSpriteBatchBuffer(mGraphicsConfiguration.Partition, createInfo);

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

            mVariants = batch.Load(NO_OFTEXTURE_SLOTS, seeds);

            const uint NO_OF_DESCRIPTOR_SETS = 3;
            var pool = batch.CreateDescriptorPool(NO_OF_DESCRIPTOR_SETS);

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
        }

        private EffectPipelineDescriptorSet mDescriptorSet;
        private EffectVariant[] mVariants;
        private MgSpriteBatchBuffer mBatchBuffer;
        private MgColor4f mClearColor;

        public void Update(MgTexture[] textures)
        {
            const uint BINDING = 0U;
            UpdateIndirectCommand();

            mDescriptorSet.SetTextures(BINDING, 0, textures);
        }

        private void UpdateIndirectCommand()
        {
            MgIndexedIndirectCommandSerializer serializer = null;

            var indirectInfo = new MgDrawIndexedIndirectCommand
            {

            };

            var render = new SpriteRenderInfo
            {

            };

            IMgDeviceMemory memory = null;
            IntPtr dest;
            var err = memory.MapMemory(mGraphicsConfiguration.Device, 0UL, render.IndirectStride, 0, out dest);
            serializer.Serialize(dest, 0, new[] { indirectInfo });
            memory.UnmapMemory(mGraphicsConfiguration.Device);
        }

        public class SpriteRenderInfo
        {
            public IMgGraphicsDevice GraphicsDevice { get; set; }
            public IMgPipeline Pipeline { get; set; }
            public IMgDescriptorSet DescriptorSet { get; set; } 
            public IMgPipelineLayout Layout { get; set; }

            public IMgBuffer Indirect { get; set; }
            public uint IndirectStride { get; set; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VkDrawIndexedIndirectCommand
        {
            public uint IndexCount { get; set; }
            public uint InstanceCount { get; set; }
            public uint FirstIndex { get; set; }
            public int VertexOffset { get; set; }
            public uint FirstInstance { get; set; }
        };

        public class MgDrawIndexedIndirectCommand
        {
            public uint IndexCount { get; set; }
            public uint InstanceCount { get; set; }
            public uint FirstIndex { get; set; }
            public int VertexOffset { get; set; }
            public uint FirstInstance { get; set; }
        };

        public interface MgIndexedIndirectCommandSerializer
        {
            int GetStride();
            void Serialize(IntPtr dest, uint offset, MgDrawIndexedIndirectCommand[] commands);
        }

        public class VkIndexedIndirectCommandSerializer : MgIndexedIndirectCommandSerializer
        {
            public int GetStride()
            {
                return Marshal.SizeOf(typeof(VkDrawIndexedIndirectCommand));
            }

            public void Serialize(IntPtr dest, uint offset, MgDrawIndexedIndirectCommand[] commands)
            {
                int destOffset = (int)offset;
                int stride = GetStride();
                foreach (var command in commands)
                {                 
                    IntPtr bufferDest = IntPtr.Add(dest, destOffset);

                    // ALL OF THIS IS TO HANDLE VERTEX OFFSET BEING int instead of uint for Metal / OpenGL
                        // IMPLEMENTATION for vk - copy data to uint struct and marshal that object instead.

                    var structData = new VkDrawIndexedIndirectCommand
                    {
                        IndexCount = command.IndexCount,
                        FirstIndex = command.FirstIndex,
                        FirstInstance = command.FirstInstance,
                        VertexOffset = command.VertexOffset,
                        InstanceCount = command.InstanceCount,
                    };

                    Marshal.StructureToPtr(structData, bufferDest, false);                    

                    destOffset += stride;
                }
            }
        }

        public void Setup()
        {
            var serializer = new VkIndexedIndirectCommandSerializer();
            IntPtr indirect = Marshal.AllocHGlobal(serializer.GetStride());
        }

        public void Compile()
        {
            var renderables = new SpriteRenderInfo[2];

            IMgCommandBuffer cmdBuf = null;

            foreach (var render in renderables)
            {


     
                Render(cmdBuf, render, 0);

            }
        }

        public class MgClearAttachmentInfo
        {
            public MgImageAspectFlagBits AspectMask { get; set; }
            public MgFormat Format { get; set; }

            public MgClearValue Generate(MgColor4f color, float depth, uint stencil)
            {
                var mask = MgImageAspectFlagBits.COLOR_BIT;
                if (AspectMask == mask)
                {
                    return MgClearValue.FromColorAndFormat(Format, color);                    
                }

                mask = MgImageAspectFlagBits.DEPTH_BIT | MgImageAspectFlagBits.STENCIL_BIT;
                if (AspectMask == mask)
                {
                    var depthStencil = new MgClearDepthStencilValue { Depth = depth, Stencil = stencil };
                    return new MgClearValue { DepthStencil = depthStencil };
                }

                // just depth
                mask = MgImageAspectFlagBits.DEPTH_BIT;
                if (AspectMask == mask)
                {
                    var depthStencil = new MgClearDepthStencilValue { Depth = depth, Stencil = 0 };
                    return new MgClearValue { DepthStencil = depthStencil };
                }

                // just stencil
                mask = MgImageAspectFlagBits.STENCIL_BIT;
                if (AspectMask == mask)
                {
                    var stencilOnly = new MgClearDepthStencilValue { Stencil = stencil, Depth = 0f };
                    return new MgClearValue { DepthStencil = stencilOnly };
                }

                throw new NotSupportedException();
            }
        }       

        public class ClearInfo
        {
            public uint? FirstSubpass { get; set; }
            public MgImageAspectFlagBits ImageAspect { get; set; }
        }

        public void Render(IMgCommandBuffer cmdBuf, SpriteRenderInfo item, int index)
        {
            var info = item.GraphicsDevice.RenderpassInfo;

            var minNoOfClearValues = 0;
            foreach(var attachment in info.Attachments)
            {
                if (attachment.LoadOp == MgAttachmentLoadOp.CLEAR || attachment.StencilLoadOp == MgAttachmentLoadOp.CLEAR)
                {
                    minNoOfClearValues += 1;
                }
            }

            var clearValueInfos = new ClearInfo[minNoOfClearValues];

            // SHOULD BE GREATER THAN ONE ... I think
            if (info.Subpasses != null)
            {
                for (var i = 0U; i < info.Subpasses.Length; i += 1)
                {
                    var subpass = info.Subpasses[i];
                    if (subpass != null)
                    {
                        if (subpass.ColorAttachments != null)
                        {
                            foreach (var colorAttach in subpass.ColorAttachments)
                            {
                                if (colorAttach.Attachment < minNoOfClearValues)
                                {
                                    var current = clearValueInfos[colorAttach.Attachment];
                                    if (!current.FirstSubpass.HasValue)
                                    {
                                        current.FirstSubpass = i;
                                    }
                                    current.ImageAspect |= MgImageAspectFlagBits.COLOR_BIT;
                                }
                            }
                        }

                        var dsAttachment = subpass.DepthStencilAttachment;
                        if (dsAttachment != null)
                        {
                            if (dsAttachment.Attachment < minNoOfClearValues)
                            {
                                var current = clearValueInfos[dsAttachment.Attachment];
                                if (!current.FirstSubpass.HasValue)
                                {
                                    current.FirstSubpass = i;
                                }
                                current.ImageAspect |= MgImageAspectFlagBits.DEPTH_BIT | MgImageAspectFlagBits.STENCIL_BIT;
                            }
                        }
                    }
                }
            }

            var renderPassCreateInfo = new MgRenderPassBeginInfo
            {
                RenderArea = item.GraphicsDevice.Scissor,
                Framebuffer = item.GraphicsDevice.Framebuffers[index],
                // TODO : need to get clear values struct somehow to clear on renderpass
                ClearValues = null,
                RenderPass = item.GraphicsDevice.Renderpass,
            };

            cmdBuf.CmdBeginRenderPass(renderPassCreateInfo, MgSubpassContents.INLINE);

            cmdBuf.CmdBindDescriptorSets(MgPipelineBindPoint.GRAPHICS, item.Layout, 0, 1, new[] { item.DescriptorSet }, null);
            cmdBuf.CmdBindPipeline(MgPipelineBindPoint.GRAPHICS, item.Pipeline);
            cmdBuf.CmdBindIndexBuffer(mBatchBuffer.Buffer, mBatchBuffer.Indices.Offset, mBatchBuffer.IndexType);
            cmdBuf.CmdBindVertexBuffers(0, new[] { mBatchBuffer.Buffer, mBatchBuffer.Buffer }, new[] { mBatchBuffer.Vertices.Offset, mBatchBuffer.Instances.Offset });

            cmdBuf.CmdDrawIndexedIndirect(item.Indirect, 0UL, 1, item.IndirectStride);

            cmdBuf.CmdEndRenderPass();
        }
    }
}
