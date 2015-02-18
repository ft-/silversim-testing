/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types.Asset;
using System;
using System.Threading;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        private const int IMAGE_PACKET_SIZE = 1000;
        private const int IMAGE_FIRST_PACKET_SIZE = 600;

        #region Texture Download Thread
        private void TextureDownloadThread(object param)
        {
            Thread.CurrentThread.Name = string.Format("LLUDP:Texture Downloader for CircuitCode {0} / IP {1}", CircuitCode, RemoteEndPoint.ToString());

            while(true)
            {
                Messages.Image.RequestImage req;
                if(!m_TextureDownloadThreadRunning)
                {
                    return;
                }
                try
                {
                    req = m_TextureDownloadQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                foreach (Messages.Image.RequestImage.RequestImageEntry imageRequest in req.RequestImageList)
                {
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
                            if (m_Server.LogAssetFailures)
                            {
                                m_Log.DebugFormat("Failed to download image {0} (RequestImage): {1} or {2}\nA: {3}\nB: {4}", imageRequest.ImageID, e1.Message, e2.Message, e1.StackTrace.ToString(), e2.StackTrace.ToString());
                            }
                            Messages.Image.ImageNotInDatabase failres = new Messages.Image.ImageNotInDatabase();
                            failres.ID = imageRequest.ImageID;
                            SendMessage(failres);
                            continue;
                        }
                    }

                    Messages.Image.ImageCodec codec;

                    switch (asset.Type)
                    {
                        case AssetType.ImageJPEG:
                            codec = Messages.Image.ImageCodec.JPEG;
                            break;

                        case AssetType.ImageTGA:
                        case AssetType.TextureTGA:
                            codec = Messages.Image.ImageCodec.TGA;
                            break;

                        case AssetType.Texture:
                            codec = Messages.Image.ImageCodec.J2C;
                            break;

                        default:
                            Messages.Image.ImageNotInDatabase failres = new Messages.Image.ImageNotInDatabase();
                            failres.ID = imageRequest.ImageID;
                            SendMessage(failres);
                            continue;
                    }

                    Messages.Image.ImageData res = new Messages.Image.ImageData();
                    res.Codec = codec;
                    res.ID = imageRequest.ImageID;
                    res.Size = (uint)asset.Data.Length;

                    if (asset.Data.Length > IMAGE_FIRST_PACKET_SIZE)
                    {
                        if (imageRequest.Packet == 0)
                        {
                            res.Data = new byte[IMAGE_FIRST_PACKET_SIZE];
                            uint numpackets = 1 + ((uint)asset.Data.Length - 1) / IMAGE_PACKET_SIZE;
                            res.Packets = (ushort)numpackets;

                            Buffer.BlockCopy(asset.Data, 0, res.Data, 0, IMAGE_FIRST_PACKET_SIZE);
                            SendMessage(res);
                        } 
                        res = null;


                        int offset = IMAGE_FIRST_PACKET_SIZE;
                        ushort packetno = 0;
                        while(offset < asset.Data.Length)
                        {
                            Messages.Image.ImagePacket ip = new Messages.Image.ImagePacket();
                            ip.ID = imageRequest.ImageID;
                            ip.Packet = ++packetno;
                            if(asset.Data.Length - offset > IMAGE_PACKET_SIZE)
                            {
                                ip.Data = new byte[IMAGE_PACKET_SIZE];
                            }
                            else
                            {
                                ip.Data = new byte[asset.Data.Length - offset];
                            }

                            Buffer.BlockCopy(asset.Data, offset, ip.Data, 0, ip.Data.Length);
                            SendMessage(ip);
                            offset += IMAGE_PACKET_SIZE;
                        }
                    }
                    else
                    {
                        res.Data = asset.Data;
                        res.Packets = 1;
                        SendMessage(res);
                    }
                }
            }
        }
        #endregion

    }
}
