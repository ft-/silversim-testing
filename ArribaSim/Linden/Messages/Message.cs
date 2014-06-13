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

using System;

namespace ArribaSim.Linden.Messages
{
    public class Message
    {
        #region Message Type
        public enum MessageType
        {
            High,
            Medium,
            Low
        }

        protected static readonly UInt32 LOW = 0xFFFF0000;
        protected static readonly UInt32 MEDIUM = 0xFF00;
        protected static readonly UInt32 HIGH = 0;

        public MessageType Type
        {
            get
            {
                if(Number <= 0xFE)
                {
                    return MessageType.High;
                }
                else if (Number <= 0xFFFE)
                {
                    return MessageType.Medium;
                }
                else
                {
                    return MessageType.Low;
                }
            }
        }
        #endregion

        #region Overloaded methods
        public virtual bool ZeroFlag
        {
            get
            {
                return false;
            }
        }

        public virtual UInt32 Number
        {
            get
            {
                return 0;
            }
        }
        #endregion
    }
}
