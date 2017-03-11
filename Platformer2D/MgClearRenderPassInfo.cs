using Magnesium;
using System.Collections.Generic;

namespace Platformer2D
{
    public class MgClearRenderPassInfo
    {
        public MgClearRenderPassAttachmentInfo[] Attachments { get; private set; }
        public MgClearRenderPassInfo(MgRenderPassCreateInfo info)
        {
            Initialize(info);
        }

        private void Initialize(MgRenderPassCreateInfo info)
        {
            Attachments = InitializeAttachments(info);
            ExtractSubpassInfo(Attachments.Length, info);
        }

        private MgClearRenderPassAttachmentInfo[] InitializeAttachments(MgRenderPassCreateInfo info)
        {
            var attachmentInfos = new List<MgClearRenderPassAttachmentInfo>();
            foreach (var attachment in info.Attachments)
            {
                if (attachment.LoadOp == MgAttachmentLoadOp.CLEAR || attachment.StencilLoadOp == MgAttachmentLoadOp.CLEAR)
                {
                    MgImageAspectFlagBits attachmentAspect = 0;

                    if (attachment.LoadOp == MgAttachmentLoadOp.CLEAR)
                    {
                        attachmentAspect |= MgImageAspectFlagBits.COLOR_BIT;
                    }

                    if (attachment.StencilLoadOp == MgAttachmentLoadOp.CLEAR)
                    {
                        MgImageAspectFlagBits formatAspect = 0;

                        switch (attachment.Format)
                        {
                            case MgFormat.S8_UINT:
                                formatAspect = MgImageAspectFlagBits.STENCIL_BIT;
                                break;
                            case MgFormat.D16_UNORM_S8_UINT:
                            case MgFormat.D24_UNORM_S8_UINT:
                            case MgFormat.D32_SFLOAT_S8_UINT:
                                formatAspect = MgImageAspectFlagBits.STENCIL_BIT | MgImageAspectFlagBits.DEPTH_BIT;
                                break;
                            case MgFormat.D16_UNORM:
                            case MgFormat.D32_SFLOAT:
                            case MgFormat.X8_D24_UNORM_PACK32:
                                formatAspect = MgImageAspectFlagBits.DEPTH_BIT;
                                break;
                            default:
                                break;
                        }

                        attachmentAspect |= formatAspect;
                    }

                    var clearItem = new MgClearRenderPassAttachmentInfo
                    {
                        Format = attachment.Format,
                        Attachment = attachmentAspect,
                    };

                    attachmentInfos.Add(clearItem);
                }
            }

            return attachmentInfos.ToArray();
        }

        private void ExtractSubpassInfo(int minNoOfClearValues, MgRenderPassCreateInfo info)
        {
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
                                    var current = Attachments[colorAttach.Attachment];
                                    if (!current.FirstSubpass.HasValue)
                                    {
                                        current.FirstSubpass = i;
                                    }
                                    current.Subpass |= MgImageAspectFlagBits.COLOR_BIT;
                                }
                            }
                        }

                        var dsAttachment = subpass.DepthStencilAttachment;
                        if (dsAttachment != null)
                        {
                            if (dsAttachment.Attachment < minNoOfClearValues)
                            {
                                var current = Attachments[dsAttachment.Attachment];
                                if (!current.FirstSubpass.HasValue)
                                {
                                    current.FirstSubpass = i;
                                }
                                current.Subpass |= MgImageAspectFlagBits.DEPTH_BIT | MgImageAspectFlagBits.STENCIL_BIT;
                            }
                        }
                    }
                }
            }
        }
    }
    
}
