// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Object;

namespace SilverSim.Scene.Types.Script.Events
{
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
        }
    }
}
