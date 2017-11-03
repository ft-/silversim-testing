// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.Threading;
using SilverSim.Viewer.Messages.Image;
using SilverSim.Viewer.Messages;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        private const int IMAGE_PACKET_SIZE = 1000;
        private const int IMAGE_FIRST_PACKET_SIZE = 600;

        #region Texture Download Thread
        public bool LogUDPTextureDownloads;
        private void TextureDownloadThread(object param)
        {
            Thread.CurrentThread.Name = string.Format("LLUDP:Texture Downloader for CircuitCode {0} / IP {1}", CircuitCode, RemoteEndPoint.ToString());
            var bakedReqs = new Queue<RequestImage.RequestImageEntry>();
            var normalReqs = new Queue<RequestImage.RequestImageEntry>();
            var activeRequestImages = new HashSet<UUID>();
#warning Implement Priority handling

            while(true)
            {
                if(!m_TextureDownloadThreadRunning)
                {
                    return;
                }
                try
                {
                    var req = (bakedReqs.Count != 0 || normalReqs.Count != 0) ?
                        (RequestImage)m_TextureDownloadQueue.Dequeue(0) :
                        (RequestImage)m_TextureDownloadQueue.Dequeue(1000);

                    foreach(var imageRequest in req.RequestImageList)
                    {
                        if(imageRequest.DiscardLevel < 0)
                        {
                            /* skip discard level < 0 */
                            continue;
                        }
                        if (!activeRequestImages.Contains(imageRequest.ImageID))
                        {
                            activeRequestImages.Add(imageRequest.ImageID);
                            if (imageRequest.Type == RequestImage.ImageType.Baked ||
                                imageRequest.Type == RequestImage.ImageType.ServerBaked)
                            {
                                bakedReqs.Enqueue(imageRequest);
                            }
                            else
                            {
                                normalReqs.Enqueue(imageRequest);
                            }
                        }
                    }
                }
                catch
                {
                    if (bakedReqs.Count == 0 && normalReqs.Count == 0)
                    {
                        continue;
                    }
                }

                if (bakedReqs.Count != 0 || normalReqs.Count != 0)
                {
                    RequestImage.RequestImageEntry imageRequest;
                    try
                    {
                        imageRequest = bakedReqs.Dequeue();
                    }
                    catch
                    {
                        try
                        {
                            imageRequest = normalReqs.Dequeue();
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if(LogUDPTextureDownloads)
                    {
                        m_Log.InfoFormat("Processing texture {0}", imageRequest.ImageID);
                    }
                    /* let us prefer the scene's asset service */
                    AssetData asset;
                    try
                    {
                        asset = Scene.AssetService[imageRequest.ImageID];
                    }
                    catch(Exception e1)
                    {
                        try
                        {
                            /* now we try the agent's asset service */
                            asset = Agent.AssetService[imageRequest.ImageID];
                            try
                            {
                                /* let us try to store the image locally */
                                asset.Temporary = true;
                                Scene.AssetService.Store(asset);
                            }
                            catch(Exception e3)
                            {
                                m_Log.DebugFormat("Failed to store asset {0} locally (RequestImage): {1}", imageRequest.ImageID, e3.Message);
                            }
                        }
                        catch (Exception e2)
                        {
                            if (Server.LogAssetFailures)
                            {
                                m_Log.DebugFormat("Failed to download image {0} (RequestImage): {1} or {2}\nA: {3}\nB: {4}", imageRequest.ImageID, e1.Message, e2.Message, e1.StackTrace, e2.StackTrace);
                            }
                            var failres = new ImageNotInDatabase
                            {
                                ID = imageRequest.ImageID
                            };
                            SendMessage(failres);
                            if (LogUDPTextureDownloads)
                            {
                                m_Log.InfoFormat("texture {0} not found", imageRequest.ImageID);
                            }
                            activeRequestImages.Remove(imageRequest.ImageID);
                            continue;
                        }
                    }

                    ImageCodec codec;

                    switch (asset.Type)
                    {
                        case AssetType.ImageJPEG:
                            codec = ImageCodec.JPEG;
                            break;

                        case AssetType.ImageTGA:
                        case AssetType.TextureTGA:
                            codec = ImageCodec.TGA;
                            break;

                        case AssetType.Texture:
                            codec = ImageCodec.J2C;
                            break;

                        default:
                            var failres = new ImageNotInDatabase
                            {
                                ID = imageRequest.ImageID
                            };
                            SendMessage(failres);
                            activeRequestImages.Remove(imageRequest.ImageID);
                            if(LogUDPTextureDownloads)
                            {
                                m_Log.InfoFormat("Asset {0} is not a texture", imageRequest.ImageID);
                            }
                            continue;
                    }

                    var res = new ImageData
                    {
                        Codec = codec,
                        ID = imageRequest.ImageID,
                        Size = (uint)asset.Data.Length
                    };
                    if (asset.Data.Length > IMAGE_FIRST_PACKET_SIZE)
                    {
                        List<Message> messages = new List<Message>();
                        res.Data = new byte[IMAGE_FIRST_PACKET_SIZE];

                        Buffer.BlockCopy(asset.Data, 0, res.Data, 0, IMAGE_FIRST_PACKET_SIZE);
                        messages.Add(res);

                        int offset = IMAGE_FIRST_PACKET_SIZE;
                        ushort packetno = 1;
                        while(offset < asset.Data.Length)
                        {
                            var ip = new ImagePacket
                            {
                                ID = imageRequest.ImageID,
                                Packet = packetno++,
                                Data = (asset.Data.Length - offset > IMAGE_PACKET_SIZE) ?
                                new byte[IMAGE_PACKET_SIZE] :
                                new byte[asset.Data.Length - offset]
                            };
                            Buffer.BlockCopy(asset.Data, offset, ip.Data, 0, ip.Data.Length);
                            messages.Add(ip);
                            offset += IMAGE_PACKET_SIZE;
                        }
                        res.Packets = packetno;

                        foreach(Message m in messages)
                        {
                            SendMessage(m);
                        }
                    }
                    else
                    {
                        res.Data = asset.Data;
                        res.Packets = 1;
                        SendMessage(res);
                    }
                    activeRequestImages.Remove(imageRequest.ImageID);
                    if (LogUDPTextureDownloads)
                    {
                        m_Log.InfoFormat("Download of texture {0} finished", imageRequest.ImageID);
                    }
                }
            }
        }
        #endregion

    }
}
