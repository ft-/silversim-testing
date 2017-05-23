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
using System.Globalization;

namespace SilverSim.Scene.Types.Script.Events
{
    public interface IListenEventLocalization
    {
        string Localize(ListenEvent le, CultureInfo currentCulture);
    }

    public struct ListenEvent : IScriptEvent
    {
        public enum ChatType : byte
        {
            Whisper = 0,
            Say = 1,
            Shout = 2,
            StartTyping = 4,
            StopTyping = 5,
            DebugChannel = 6,
            Region = 7,
            OwnerSay = 8,
            Broadcast = 0xFF
        }

        public enum ChatSourceType : byte
        {
            System = 0,
            Agent = 1,
            Object = 2,
        }

        public const int PUBLIC_CHANNEL = 0;
        public const int DEBUG_CHANNEL = 0x7FFFFFFF;

        #region Extension Fields for Chat Router
        public Vector3 GlobalPosition;
        public UUID TargetID; /* SayTo when not UUID.Zero */
        #endregion

        public ChatType Type;
        public ChatSourceType SourceType;
        public int Channel;
        public string Name;
        public UUID ID;
        public UUID OwnerID;
        public string Message;
        public int ButtonIndex;
        public UUID OriginSceneID; /* used for Origin when doing sim neighbor passing */
        public double Distance;
        public IListenEventLocalization Localization;

        public ListenEvent(ListenEvent le)
        {
            GlobalPosition = le.GlobalPosition;
            TargetID = le.TargetID;
            Type = le.Type;
            SourceType = le.SourceType;
            Channel = le.Channel;
            Name = le.Name;
            ID = le.ID;
            OwnerID = le.OwnerID;
            Message = le.Message;
            ButtonIndex = le.ButtonIndex;
            OriginSceneID = le.OriginSceneID;
            Distance = le.Distance;
            Localization = le.Localization;
        }
    }
}
