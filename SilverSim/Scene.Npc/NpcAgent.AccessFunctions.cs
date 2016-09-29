// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;

namespace SilverSim.Scene.Npc
{
    public partial class NpcAgent
    {
        public void DoSay(int channel, string text)
        {
            ChatServiceInterface chatService = CurrentScene.GetService<ChatServiceInterface>();
            ListenEvent ev = new ListenEvent();
            ev.ID = ID;
            ev.Type = ListenEvent.ChatType.Say;
            ev.Channel = channel;
            ev.GlobalPosition = GlobalPosition;
            ev.Name = Name;
            ev.TargetID = UUID.Zero;
            ev.SourceType = ListenEvent.ChatSourceType.Agent;
            ev.OwnerID = ID;
            chatService.Send(ev);
        }

        public void DoSay(string text)
        {
            DoSay(0, text);
        }

        public void DoShout(int channel, string text)
        {
            ChatServiceInterface chatService = CurrentScene.GetService<ChatServiceInterface>();
            ListenEvent ev = new ListenEvent();
            ev.ID = ID;
            ev.Type = ListenEvent.ChatType.Shout;
            ev.Channel = channel;
            ev.GlobalPosition = GlobalPosition;
            ev.Name = Name;
            ev.TargetID = UUID.Zero;
            ev.SourceType = ListenEvent.ChatSourceType.Agent;
            ev.OwnerID = ID;
            chatService.Send(ev);
        }

        public void DoShout(string text)
        {
            DoShout(0, text);
        }

        public void DoWhisper(int channel, string text)
        {
            ChatServiceInterface chatService = CurrentScene.GetService<ChatServiceInterface>();
            ListenEvent ev = new ListenEvent();
            ev.ID = ID;
            ev.Type = ListenEvent.ChatType.Whisper;
            ev.Channel = channel;
            ev.GlobalPosition = GlobalPosition;
            ev.Name = Name;
            ev.TargetID = UUID.Zero;
            ev.SourceType = ListenEvent.ChatSourceType.Agent;
            ev.OwnerID = ID;
            chatService.Send(ev);
        }

        public void DoWhisper(string text)
        {
            DoWhisper(0, text);
        }

        public void DoTouch(UUID objectKey, int linkNum)
        {

        }
    }
}
