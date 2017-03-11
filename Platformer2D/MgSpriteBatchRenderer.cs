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
    public class MgSpriteBatchRenderer
    {
        private List<MgSpriteVertexDataItem> mItems;
        private List<MgSpriteMaterialData> mMaterials;
        private IMgThreadPartition mPartition;
        private bool _beginCalled = false;
        private MgSpriteBatchBuffer mBatchBuffer;
        private MgDrawIndexedIndirectCommand mCommand;

        public MgSpriteBatchRenderer(IMgThreadPartition partition, MgSpriteBatchBuffer batchBuffer, MgDrawIndexedIndirectCommand command)
        {
            mPartition = partition;
            mBatchBuffer = batchBuffer;
            mCommand = command;

            mItems = new List<MgSpriteVertexDataItem>();
            mMaterials = new List<MgSpriteMaterialData>();
        }

        public void Begin()
        {
            if (_beginCalled)
                throw new InvalidOperationException("Begin cannot be called again until End has been successfully called.");

            mItems.Clear();
            mMaterials.Clear();
            mBatchBuffer.Reset();

            ResetCommands();

            _beginCalled = true;
        }

        private void ResetCommands()
        {
            mCommand.IndexCount = 0;
            mCommand.InstanceCount = 0;
        }

        public void DrawInternal(MgSpriteBatchCreateInfo parameter)
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

            mItems.Add(item);
            mMaterials.Add(material);
        }

        public MgViewport Viewport { get; set; }
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
                
        public void End()
        {
            _beginCalled = false;
        }

        public void Update()
        {
            SetBufferData(mPartition.Device, mBatchBuffer, mItems, mMaterials);
        }

        private static void SetBufferData
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

            // INDEX DATA

            {
                var totalBytes = 6 * batchBuffer.Indices.Stride;
                var indices = new byte[totalBytes];
                
                IntPtr indicesDst = IntPtr.Add(bufferDst, (int)batchBuffer.Indices.Offset);
                var offset = (int)batchBuffer.Indices.Offset;

                // ALWAYS 6 INDICES - TRIANGLE ORDER IS SAME
                if (batchBuffer.IndexType == MgIndexType.UINT16)
                {
                    var uShorts = new ushort[6];
                    ushort multiple = 0;                    

                    foreach (var quad in items)
                    {
                        uShorts[0] = multiple;
                        uShorts[1] = (ushort) (multiple + 1);
                        uShorts[2] = (ushort) (multiple + 2);

                        uShorts[3] = (ushort) (multiple + 1);
                        uShorts[4] = (ushort) (multiple + 3);
                        uShorts[5] = (ushort) (multiple + 2);                        

                        Buffer.BlockCopy(uShorts, 0, indices, 0, totalBytes);

                        IntPtr dest = IntPtr.Add(indicesDst, offset);
                        Marshal.Copy(indices, 0, dest, totalBytes);

                        offset += totalBytes;
                        multiple += 4;
                    }
                }
                else
                {
                    var uShorts = new uint[6];
                    uint multiple = 0;

                    foreach (var quad in items)
                    {
                        uShorts[0] = multiple;
                        uShorts[1] = multiple + 1U;
                        uShorts[2] = multiple + 2U;

                        uShorts[3] = multiple + 1U;
                        uShorts[4] = multiple + 3U;
                        uShorts[5] = multiple + 2U;

                        Buffer.BlockCopy(uShorts, 0, indices, 0, totalBytes);

                        IntPtr dest = IntPtr.Add(indicesDst, offset);
                        Marshal.Copy(indices, 0, dest, totalBytes);

                        offset += totalBytes;
                        multiple += 4;
                    }
                }

                // FYI - OFFSET is always 0               
            }

            // VERTEX DATA
            {
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

            // MATERIAL DATA 
            {
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

            // INSTANCE DATA - BASED ON THE NUMBER OF MATERIALS
            {
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

            // INDIRECT DATA - IGNORE           

            batchBuffer.DeviceMemory.UnmapMemory(device);
        }
    }
}
