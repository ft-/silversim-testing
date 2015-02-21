﻿/*

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

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Agent
{
    public class AgentAnimation : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public struct AnimationEntry
        {
            public UUID AnimID;
            public bool StartAnim;
        }

        public List<AnimationEntry> AnimationEntryList = new List<AnimationEntry>();
        public List<byte[]> PhysicalAvatarEventList = new List<byte[]>();


        public AgentAnimation()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.AgentAnimation;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            AgentAnimation m = new AgentAnimation();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            int n;
            int i;

            n = p.ReadUInt8();
            for (i = 0; i < n; ++i)
            {
                AnimationEntry e = new AnimationEntry();
                e.AnimID = p.ReadUUID();
                e.StartAnim = p.ReadBoolean();
                m.AnimationEntryList.Add(e);
            }

            n = p.ReadUInt8();
            for (i = 0; i < n; ++i)
            {
                m.PhysicalAvatarEventList.Add(p.ReadBytes(p.ReadUInt8()));
            }

            return m;
        }
    }
}
