/*

ArribaSim is distributed under the terms of the
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

using System.Collections.Generic;

namespace ArribaSim.Linden.Messages
{
    public class UDPPacketDecoder
    {
        public delegate Message PacketDecoderDelegate(UDPPacket p);
        public readonly Dictionary<MessageType, PacketDecoderDelegate> PacketTypes = new Dictionary<MessageType,PacketDecoderDelegate>();

        public UDPPacketDecoder()
        {
            /* Circuit */
            PacketTypes.Add(MessageType.CompleteAgentMovement, Circuit.CompleteAgentMovement.Decode);
            PacketTypes.Add(MessageType.LogoutRequest, Circuit.LogoutRequest.Decode);

            /* Script */
            PacketTypes.Add(MessageType.ScriptDialogReply, Script.ScriptDialogReply.Decode);
            
            /* Parcel */
            PacketTypes.Add(MessageType.ParcelPropertiesRequest, Parcel.ParcelPropertiesRequest.Decode);
            PacketTypes.Add(MessageType.ParcelPropertiesRequestByID, Parcel.ParcelPropertiesRequestByID.Decode);
            PacketTypes.Add(MessageType.ParcelPropertiesUpdate, Parcel.ParcelPropertiesUpdate.Decode);
            PacketTypes.Add(MessageType.ParcelReturnObjects, Parcel.ParcelReturnObjects.Decode);
            PacketTypes.Add(MessageType.ParcelSetOtherCleanTime, Parcel.ParcelSetOtherCleanTime.Decode);
            PacketTypes.Add(MessageType.ParcelSelectObjects, Parcel.ParcelSelectObjects.Decode);
            
            /* Estate */
            PacketTypes.Add(MessageType.EstateCovenantRequest, Estate.EstateCovenantRequest.Decode);

            /* Lookup */
            PacketTypes.Add(MessageType.UUIDNameRequest, Lookup.UUIDNameRequest.Decode);
            PacketTypes.Add(MessageType.UUIDGroupNameRequest, Lookup.UUIDGroupNameRequest.Decode);

        }
    }
}
