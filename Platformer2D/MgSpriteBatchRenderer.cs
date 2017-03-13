using Magnesium;
using Microsoft.Xna.Framework;
using MonoGame.Core;
using MonoGame.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Platformer2D
{
    public class MgSpriteBatchRenderer : IMgSpriteBatch
    {
        #region Private fields
        private List<MgSpriteVertexDataItem> mVertexDataItems;
        private List<MgSpriteMaterialData> mMaterials;
        private List<IMgTexture> mTextures;
        private IMgDevice mDevice;
        private bool _beginCalled = false;
        private MgSpriteBatchBuffer mBatchBuffer;
        private MgDrawIndexedIndirectCommand mCommand;
        private MgIndirectBufferSpriteInfo mIndirect;
        #endregion

        public MgSpriteBatchRenderer(IMgDevice device, MgSpriteBatchRendererCreateInfo createInfo)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            if (createInfo == null)
                throw new ArgumentNullException(nameof(createInfo));

            if (createInfo.BatchBuffer == null)
                throw new ArgumentNullException(nameof(createInfo.BatchBuffer));

            if (createInfo.Indirect == null)
                throw new ArgumentNullException(nameof(createInfo.Indirect));

            if (createInfo.Pipeline == null)
                throw new ArgumentNullException(nameof(createInfo.PipelineDescriptorSet));

            if (createInfo.PipelineDescriptorSet ==  null)
                throw new ArgumentNullException(nameof(createInfo.PipelineDescriptorSet));

            if (createInfo.ClearValues == null)
                throw new ArgumentNullException(nameof(createInfo.ClearValues));

            if (createInfo.RenderPass == null)
                throw new ArgumentNullException(nameof(createInfo.RenderPass));

            mDevice = device;
            mBatchBuffer = createInfo.BatchBuffer;
            mIndirect = createInfo.Indirect;
            Pipeline = createInfo.Pipeline;
            PipelineDescriptorSet = createInfo.PipelineDescriptorSet;
            ClearValues = createInfo.ClearValues;
            RenderPass = createInfo.RenderPass;

            RenderArea = createInfo.RenderArea;
            Viewport = createInfo.Viewport;

            mCommand = new MgDrawIndexedIndirectCommand();

            mVertexDataItems = new List<MgSpriteVertexDataItem>();
            mMaterials = new List<MgSpriteMaterialData>();
            mTextures = new List<IMgTexture>();
        }

        #region IDisposable methods

        ~MgSpriteBatchRenderer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool mIsDisposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (mIsDisposed)
                return;

            ReleaseUnmanagedResources();

            if (disposing)
            {
                ReleaseManagedResources();
            }

            mIsDisposed = true;
        }

        private void ReleaseManagedResources()
        {
            mVertexDataItems.Clear();
            mMaterials.Clear();
        }

        private void ReleaseUnmanagedResources()
        {
            if (mIndirect != null)
            {
                mIndirect.Dispose();
            }
        }

        #endregion

        #region Public properties

        public IMgPipeline Pipeline { get; private set; }
        public EffectPipelineDescriptorSet PipelineDescriptorSet { get; private set; }
        public MgClearValue[] ClearValues { get; private set; }
        public MgRect2D RenderArea { get; private set; }
        public IMgRenderPass RenderPass { get; private set; }
        public MgViewport Viewport { get; private set; }

        #endregion

        public void Compile(IMgCommandBuffer cmdBuf, IMgFramebuffer frame)
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

            cmdBuf.CmdBindDescriptorSets(MgPipelineBindPoint.GRAPHICS, PipelineDescriptorSet.PipelineLayout, 0, 1, new[] { PipelineDescriptorSet.DescriptorSet }, null);
            cmdBuf.CmdBindPipeline(MgPipelineBindPoint.GRAPHICS, Pipeline);
            cmdBuf.CmdBindIndexBuffer(mBatchBuffer.Buffer, mBatchBuffer.Indices.Offset, mBatchBuffer.IndexType);
            cmdBuf.CmdBindVertexBuffers(0, new[] { mBatchBuffer.Buffer, mBatchBuffer.Buffer }, new[] { mBatchBuffer.Vertices.Offset, mBatchBuffer.Instances.Offset });

            cmdBuf.CmdDrawIndexedIndirect(mIndirect.Buffer, mIndirect.BindOffset, 1, mIndirect.Stride);

            cmdBuf.CmdEndRenderPass();
        }

        #region Begin methods

        public void Begin()
        {
            if (_beginCalled)
                throw new InvalidOperationException("Begin cannot be called again until End has been successfully called.");

            mVertexDataItems.Clear();
            mMaterials.Clear();
            mBatchBuffer.Reset();

            PipelineDescriptorSet.Begin();

            ResetCommands();

            _beginCalled = true;
        }

        private void ResetCommands()
        {
            mCommand.IndexCount = 0;
            mCommand.InstanceCount = 0;
        }

        #endregion

        #region Draw methods

        public void Draw(IMgTexture texture, Vector2 position, Rectangle? sourceRect, Color color, float rotation, Vector2 origin, float scale, SpriteEffects flip, float depth)
        {
            var slotIndex = (uint)mTextures.Count;
            mTextures.Add(texture);

            // SHOULD BE Transform -> Rotate - Scale

            var scaleM = Matrix.CreateScale(scale);
            var rotateM = Matrix.Identity;
            var translateM = Matrix.CreateTranslation(new Vector3(position.X, position.Y, depth));
            var final = Matrix.Multiply(translateM, Matrix.Multiply(rotateM, scaleM));

            var w = texture.Width * scale;
            var h = texture.Height * scale;
            if (sourceRect.HasValue)
            {
                w = sourceRect.Value.Width * scale;
                h = sourceRect.Value.Height * scale;
            }

            var item = new MgSpriteBatchCreateInfo
            {
                TextureSlotId = slotIndex,
                Width = texture.Width,
                Height = texture.Height,
                SourceRect = sourceRect,
                DestinationRect = new Vector4(position.X, position.Y, w, h),
                Color = AsVector4(color),
                Origin = origin,
                Transform = final,
                Scale = scale,
                Depth = depth,
                Rotation = rotation,
                SpriteEffect = flip,
            };
            DrawInternal(item);
        }

        public void Draw(IMgTexture texture, Vector2 position, Color mask)
        {
            var slotIndex = (uint)mTextures.Count;
            mTextures.Add(texture);

            // SHOULD BE Transform -> Rotate - Scale

            var scaleM = Matrix.Identity;
            var rotateM = Matrix.Identity;
            var translateM = Matrix.CreateTranslation(new Vector3(position.X, position.Y, 0));
            var final = Matrix.Multiply(translateM, Matrix.Multiply(rotateM, scaleM));

            var w = texture.Width;
            var h = texture.Height;

            var item = new MgSpriteBatchCreateInfo
            {
                TextureSlotId = slotIndex,
                Width = texture.Width,
                Height = texture.Height,
                DestinationRect = new Vector4(position.X, position.Y, w, h),
                SourceRect = null,
                TopLeft = new Vector2(0, 0),
                BottomRight = new Vector2(1, 1),
                Color = AsVector4(mask),
                Origin = Vector2.Zero,
                Transform = final,
                Scale = 1f,
                Depth = 0f,
                Rotation = 0f,
                SpriteEffect = 0,
            };
            DrawInternal(item);
        }

        public void Draw(uint textureSlot, Vector2 position, Color mask)
        {
            var scaleM = Matrix.Identity;
            var rotateM = Matrix.Identity;
            var translateM = Matrix.CreateTranslation(new Vector3(position.X, position.Y, 0));
            var final = Matrix.Multiply(translateM, Matrix.Multiply(rotateM, scaleM));

            var item = new MgSpriteBatchCreateInfo
            {
                TextureSlotId = textureSlot,
                SourceRect = null,
                TopLeft = new Vector2(0, 0),
                BottomRight = new Vector2(1, 1),
                Color = AsVector4(mask),
                Origin = Vector2.Zero,
                Transform = final,
                Scale = 1f,
                Depth = 0f,
                Rotation = 0f,
                SpriteEffect = 0,
            };
            DrawInternal(item);
        }

        public void Draw(uint textureSlot, Vector2 position, object p, Color color, float v1, Vector2 origin, float v2, SpriteEffects none, float v3)
        {
            throw new NotImplementedException();
        }

        public void DrawString(SpriteFont font, string value, Vector2 vector2, Color black)
        {
            throw new NotImplementedException();
        }

        private static Vector4 AsVector4(Color value)
        {
            var color = value.ToVector4();
            return new Vector4(color.X, color.Y, color.Z, color.W);
        }

        private void DrawInternal(MgSpriteBatchCreateInfo parameter)
        {
            Vector2 _texCoordTL = new Vector2(0, 0);
            Vector2 _texCoordBR = new Vector2(0, 0);

            if (parameter.SourceRect.HasValue)
            {
                Rectangle _tempRect = parameter.SourceRect.Value;

                _texCoordTL.X = _tempRect.X / (float)parameter.Width;
                _texCoordTL.Y = _tempRect.Y / (float)parameter.Height;
                _texCoordBR.X = (_tempRect.X + _tempRect.Width) / (float)parameter.Width;
                _texCoordBR.Y = (_tempRect.Y + _tempRect.Height) / (float)parameter.Height;
            }
            else
            {
                _texCoordTL = parameter.TopLeft;
                _texCoordBR = parameter.BottomRight;
            }

            if ((parameter.SpriteEffect & SpriteEffects.FlipVertically) != 0)
            {
                var temp = _texCoordBR.Y;
                _texCoordBR.Y = _texCoordTL.Y;
                _texCoordTL.Y = temp;
            }
            if ((parameter.SpriteEffect & SpriteEffects.FlipHorizontally) != 0)
            {
                var temp = _texCoordBR.X;
                _texCoordBR.X = _texCoordTL.X;
                _texCoordTL.X = temp;
            }

            var destinationRectangle = parameter.DestinationRect;

            var item = new MgSpriteVertexDataItem();
            item.Set(destinationRectangle.X,
                    destinationRectangle.Y,
                    parameter.Depth,
                    -parameter.Origin.X,
                    -parameter.Origin.Y,
                    destinationRectangle.Z,
                    destinationRectangle.W,
                    (float)Math.Sin(parameter.Rotation),
                    (float)Math.Cos(parameter.Rotation),
                    parameter.Color,
                    _texCoordTL,
                    _texCoordBR);

            // CommandBuffer

            //mBatchBuffer.Vertices.NoOfItems += 4;

            mCommand.IndexCount += 6;
            mCommand.InstanceCount += 1;

            // materials
            var material = new MgSpriteMaterialData
            {
                Texture = parameter.TextureSlotId,
                Color = parameter.Color,
                Transform = GenerateTransform(Viewport, parameter),
            };

            mVertexDataItems.Add(item);
            mMaterials.Add(material);
        }

        private static Matrix GenerateTransform(MgViewport viewport, MgSpriteBatchCreateInfo parameter)
        {
            Matrix projection;
            Matrix.CreateOrthographicOffCenter(0f, viewport.Width, viewport.Height, 0f, -1, 0, out projection);

            // GL requires a half pixel offset to match DX.
            projection.M41 += -0.5f * projection.M11;
            projection.M42 += -0.5f * projection.M22;

            Matrix globalTransform = parameter.Transform;
            Matrix transform;
            Matrix.Multiply(ref globalTransform, ref projection, out transform);
            return transform;
        }

        #endregion

        #region End methods

        public void End()
        {
            if (PipelineDescriptorSet == null)
            {
                return;
            }

            UpdateTextureSlots();

            PipelineDescriptorSet.End();

            UpdateBoundConstantBuffer();

            if (mIndirect != null)
            {
                mIndirect.Update(mCommand);
            }

            _beginCalled = false;
        }

        private void UpdateTextureSlots()
        {
            const uint FIRST_TEXTURE_BINDING = 0;
            if (mTextures.Count > 0)
            {
                PipelineDescriptorSet.SetTextures(FIRST_TEXTURE_BINDING, 0, mTextures.ToArray());
            }
        }

        public void UpdateBoundConstantBuffer()
        {
            SetBoundConstantBuffer(mDevice, mBatchBuffer, mVertexDataItems, mMaterials);
        }

        private static void SetBoundConstantBuffer
        (
            IMgDevice device,
            MgSpriteBatchBuffer batchBuffer,
            List<MgSpriteVertexDataItem> items,
            List<MgSpriteMaterialData> materials
        )
        {
            // var quads = new MgSpriteVertexDataItem[1];

            IntPtr bufferDst = IntPtr.Zero;
            var err = batchBuffer.DeviceMemory.MapMemory(device, 0, batchBuffer.BufferSize, 0, out bufferDst);
            Debug.Assert(err == Result.SUCCESS, err + " != Result.SUCCESS");

            ProcessIndices(batchBuffer, items, bufferDst);

            ProcessVertexData(batchBuffer, items, bufferDst);

            ProcessMaterials(batchBuffer, materials, bufferDst);

            ProcessInstanceArray(batchBuffer, materials, bufferDst);

            // INDIRECT DATA - IGNORE           

            batchBuffer.DeviceMemory.UnmapMemory(device);
        }

        private static void ProcessInstanceArray(MgSpriteBatchBuffer batchBuffer, List<MgSpriteMaterialData> materials, IntPtr bufferDst)
        {
            // INSTANCE DATA - BASED ON THE NUMBER OF MATERIALS

            IntPtr instanceDst = IntPtr.Add(bufferDst, (int)batchBuffer.Instances.Offset);

            var instances = new uint[materials.Count];
            var byteLength = batchBuffer.Instances.Stride * instances.Length;
            var byteData = new byte[byteLength];

            for (var i = 0U; i < instances.Length; ++i)
            {
                instances[i] = i;
            }

            Buffer.BlockCopy(instances, 0, byteData, 0, byteLength);
            Marshal.Copy(byteData, 0, instanceDst, byteLength);
        }

        private static void ProcessMaterials(MgSpriteBatchBuffer batchBuffer, List<MgSpriteMaterialData> materials, IntPtr bufferDst)
        {
            // MATERIAL DATA 

            IntPtr materialDst = IntPtr.Add(bufferDst, (int)batchBuffer.Materials.Offset);

            var stride = batchBuffer.Materials.Stride;
            var offset = 0;
            foreach (var material in materials)
            {
                IntPtr dest = IntPtr.Add(materialDst, offset);
                Marshal.StructureToPtr(material, dest, false);
                offset += stride;
            }
        }

        private static void ProcessVertexData(MgSpriteBatchBuffer batchBuffer, List<MgSpriteVertexDataItem> items, IntPtr bufferDst)
        {
            // VERTEX DATA

            IntPtr verticesDst = IntPtr.Add(bufferDst, (int)batchBuffer.Vertices.Offset);

            var stride = batchBuffer.Vertices.Stride;
            var offset = 0;
            foreach (var quad in items)
            {
                IntPtr dest = IntPtr.Add(verticesDst, offset);
                Marshal.StructureToPtr(quad, dest, false);
                offset += stride;
            }
        }

        private static void ProcessIndices(MgSpriteBatchBuffer batchBuffer, List<MgSpriteVertexDataItem> items, IntPtr bufferDst)
        {
            // INDEX DATA
            const int NO_OF_INDICES_PER_QUAD = 6;
            const int NO_OF_VERTICES_PER_QUAD = 4;

            var totalBytes = NO_OF_INDICES_PER_QUAD * batchBuffer.Indices.Stride;
            var indices = new byte[totalBytes];

            IntPtr indicesDst = IntPtr.Add(bufferDst, (int)batchBuffer.Indices.Offset);
            var offset = (int)batchBuffer.Indices.Offset;

            // ALWAYS 6 INDICES - TRIANGLE ORDER IS SAME
            if (batchBuffer.IndexType == MgIndexType.UINT16)
            {
                var uShorts = new ushort[NO_OF_INDICES_PER_QUAD];
                ushort quad_array_offset = 0;

                foreach (var quad in items)
                {
                    uShorts[0] = quad_array_offset;
                    uShorts[1] = (ushort)(quad_array_offset + 1);
                    uShorts[2] = (ushort)(quad_array_offset + 2);

                    uShorts[3] = (ushort)(quad_array_offset + 1);
                    uShorts[4] = (ushort)(quad_array_offset + 3);
                    uShorts[5] = (ushort)(quad_array_offset + 2);

                    Buffer.BlockCopy(uShorts, 0, indices, 0, totalBytes);

                    IntPtr dest = IntPtr.Add(indicesDst, offset);
                    Marshal.Copy(indices, 0, dest, totalBytes);

                    offset += totalBytes;
                    quad_array_offset += NO_OF_VERTICES_PER_QUAD;
                }
            }
            else
            {
                var uShorts = new uint[NO_OF_INDICES_PER_QUAD];
                uint quad_array_offset = 0;

                foreach (var quad in items)
                {
                    uShorts[0] = quad_array_offset;
                    uShorts[1] = quad_array_offset + 1U;
                    uShorts[2] = quad_array_offset + 2U;

                    uShorts[3] = quad_array_offset + 1U;
                    uShorts[4] = quad_array_offset + 3U;
                    uShorts[5] = quad_array_offset + 2U;

                    Buffer.BlockCopy(uShorts, 0, indices, 0, totalBytes);

                    IntPtr dest = IntPtr.Add(indicesDst, offset);
                    Marshal.Copy(indices, 0, dest, totalBytes);

                    offset += totalBytes;
                    quad_array_offset += NO_OF_VERTICES_PER_QUAD;
                }
            }

            // FYI - OFFSET is always 0               
        }

        #endregion
    }
}
