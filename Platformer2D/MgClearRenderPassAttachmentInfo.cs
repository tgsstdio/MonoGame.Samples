using Magnesium;
using System;

namespace Platformer2D
{
    public class MgClearRenderPassAttachmentInfo
    {
        public MgFormat Format { get; set; }
        public uint? FirstSubpass { get; set; }
        public MgImageAspectFlagBits Attachment { get; set; }
        public MgImageAspectFlagBits Subpass { get; set; }

        public MgImageAspectFlagBits Usage
        {
            get
            {
                return Attachment | Subpass;
            }
        }

        public MgClearValue Generate(MgColor4f color, float depth, uint stencil)
        {
            var compareMask = MgImageAspectFlagBits.COLOR_BIT;
            if (Usage == compareMask)
            {
                return AsColor(color);
            }

            compareMask = MgImageAspectFlagBits.DEPTH_BIT | MgImageAspectFlagBits.STENCIL_BIT;
            if (Usage == compareMask)
            {
                return AsDepthAndStencil(depth, stencil);
            }

            // just depth
            compareMask = MgImageAspectFlagBits.DEPTH_BIT;
            if (Usage == compareMask)
            {
                return AsDepthOnly(depth);
            }

            // just stencil
            compareMask = MgImageAspectFlagBits.STENCIL_BIT;
            if (Usage == compareMask)
            {
                return AsStencilOnly(stencil);
            }

            throw new NotSupportedException();
        }

        public MgClearValue AsColor(MgColor4f color)
        {
            return MgClearValue.FromColorAndFormat(Format, color);
        }

        public MgClearValue AsDepthOnly(float depth)
        {
            var depthStencil = new MgClearDepthStencilValue { Depth = depth, Stencil = 0 };
            return new MgClearValue { DepthStencil = depthStencil };
        }

        public MgClearValue AsDepthAndStencil(float depth, uint stencil)
        {
            var depthStencil = new MgClearDepthStencilValue { Depth = depth, Stencil = stencil };
            return new MgClearValue { DepthStencil = depthStencil };
        }

        public MgClearValue AsStencilOnly(uint stencil)
        {
            var stencilOnly = new MgClearDepthStencilValue { Stencil = stencil, Depth = 0f };
            return new MgClearValue { DepthStencil = stencilOnly };
        }
    }
    
}
