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

using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Npc
{
    public partial class NpcAgent
    {
        readonly RwLockedDoubleDictionary<UUID /* ItemID */, UInt32 /* LocalID */, KeyValuePair<UUID /* SceneID */, UUID /* ObjectID */>> m_AttachmentsList = new RwLockedDoubleDictionary<UUID, UInt32, KeyValuePair<UUID, UUID>>();

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
            ObjectGroup grp;
            ObjectPart part;
            if(!CurrentScene.ObjectGroups.TryGetValue(objectKey, out grp))
            {
                return;
            }
            else if(!grp.TryGetValue(linkNum, out part))
            {
                return;
            }
            DetectInfo dInfo = new DetectInfo();
            dInfo.LinkNumber = linkNum;
            dInfo.TouchFace = -1;
            dInfo.Name = Owner.FullName;
            dInfo.GrabOffset = part.LocalPosition * -1f;
            dInfo.Group = Group;
            dInfo.Key = ID;
            dInfo.ObjType = DetectedTypeFlags.Npc;
            dInfo.ObjType |= (SittingOnObject != null) ? DetectedTypeFlags.Passive : DetectedTypeFlags.Active;
            dInfo.Owner = Owner;
            dInfo.Position = GlobalPosition;
            dInfo.Rotation = GlobalRotation;
            dInfo.TouchBinormal = Vector3.Zero;
            dInfo.TouchNormal = Vector3.Zero;
            dInfo.TouchST = new Vector3(-1f, -1f, 0);
            dInfo.TouchUV = new Vector3(-1f, -1f, 0);

            TouchEvent te = new TouchEvent();
            te.Type = TouchEvent.TouchType.Start;
            te.Detected.Add(dInfo);
            part.PostTouchEvent(te);

            te = new TouchEvent();
            te.Type = TouchEvent.TouchType.Continuous;
            te.Detected.Add(dInfo);
            part.PostTouchEvent(te);

            te = new TouchEvent();
            te.Type = TouchEvent.TouchType.End;
            te.Detected.Add(dInfo);
            part.PostTouchEvent(te);
        }

        public void DoSit(UUID objectKey)
        {
            SceneInterface scene = CurrentScene;
            if(scene != null)
            {
                ObjectPart part;
                if(scene.Primitives.TryGetValue(objectKey, out part))
                {
                    ObjectGroup grp = part.ObjectGroup;
                    if (grp != null)
                    {
                        grp.AgentSitting.Sit(this, grp.RootPart != part ? part.LinkNumber : -1);
                    }
                }
            }
        }

        readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, int>> m_ScriptedIMListeners = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, int>>(delegate () { return new RwLockedDictionary<UUID, int>(); });

        public override bool IMSend(GridInstantMessage im)
        {
            if(im.Dialog == GridInstantMessageDialog.MessageFromAgent ||
                im.Dialog == GridInstantMessageDialog.MessageFromObject)
            {
                foreach (KeyValuePair<UUID, RwLockedDictionary<UUID, int>> kvp in m_ScriptedChatListeners)
                {
                    ObjectPart part;
                    ObjectPartInventoryItem item;
                    if (CurrentScene.Primitives.TryGetValue(kvp.Key, out part))
                    {
                        foreach (KeyValuePair<UUID, int> kvpinner in kvp.Value)
                        {
                            if (part.Inventory.TryGetValue(kvpinner.Key, out item))
                            {
                                ScriptInstance instance = item.ScriptInstance;
                                if (null != instance)
                                {
                                    /* Translate IM event to mapped channel */
                                    ListenEvent nev = new ListenEvent();
                                    nev.ButtonIndex = -1;
                                    nev.Channel = kvpinner.Value;
                                    nev.Distance = 0;
                                    nev.GlobalPosition = Vector3.Zero;
                                    nev.ID = im.FromAgent.ID;
                                    nev.Message = im.Message;
                                    nev.Name = im.FromAgent.FullName;
                                    nev.OriginSceneID = UUID.Zero;
                                    nev.OwnerID = im.FromAgent.ID;
                                    nev.SourceType = im.Dialog == GridInstantMessageDialog.MessageFromObject ? ListenEvent.ChatSourceType.Object : ListenEvent.ChatSourceType.Agent;
                                    nev.TargetID = ID;
                                    nev.Type = ListenEvent.ChatType.Say;
                                    instance.PostEvent(nev);
                                }
                            }
                        }
                    }
                    else
                    {
                        m_ScriptedIMListeners.Remove(kvp.Key);
                    }
                }
            }
            return true;
        }

        public void ListenIM(UUID objectid, UUID itemid, int tochannel)
        {
            m_ScriptedIMListeners[objectid][itemid] = tochannel;
        }

        public void UnlistenIM(UUID objectid, UUID itemid)
        {
            RwLockedDictionary<UUID, int> itemlist;
            if (m_ScriptedIMListeners.TryGetValue(objectid, out itemlist))
            {
                itemlist.Remove(itemid);
            }
            m_ScriptedIMListeners.RemoveIf(objectid, delegate (RwLockedDictionary<UUID, int> list) { return list.Count == 0; });
        }

        readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, int>> m_ScriptedChatListeners = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, int>>(delegate() { return new RwLockedDictionary<UUID, int>(); });

        void OnChatReceive(ListenEvent ev)
        {
            foreach(KeyValuePair<UUID, RwLockedDictionary<UUID, int>> kvp in m_ScriptedChatListeners)
            {
                ObjectPart part;
                ObjectPartInventoryItem item;
                if(CurrentScene.Primitives.TryGetValue(kvp.Key, out part))
                {
                    foreach(KeyValuePair<UUID, int> kvpinner in kvp.Value)
                    {
                        if(part.Inventory.TryGetValue(kvpinner.Key, out item))
                        {
                            ScriptInstance instance = item.ScriptInstance;
                            if(null != instance)
                            {
                                /* Translate listen event to mapped channel */
                                ListenEvent nev = new ListenEvent();
                                nev.ButtonIndex = ev.ButtonIndex;
                                nev.Channel = kvpinner.Value;
                                nev.Distance = ev.Distance;
                                nev.GlobalPosition = ev.GlobalPosition;
                                nev.ID = ev.ID;
                                nev.Message = ev.Message;
                                nev.Name = ev.Name;
                                nev.OriginSceneID = ev.OriginSceneID;
                                nev.OwnerID = ev.OwnerID;
                                nev.SourceType = ev.SourceType;
                                nev.TargetID = ev.TargetID;
                                nev.Type = ev.Type;
                                instance.PostEvent(nev);
                            }
                        }
                    }
                }
                else
                {
                    m_ScriptedChatListeners.Remove(kvp.Key);
                }
            }
        }

        public void ListenAsNpc(UUID objectid, UUID itemid, int tochannel)
        {
            m_ScriptedChatListeners[objectid][itemid] = tochannel;
        }

        public void UnlistenAsNpc(UUID objectid, UUID itemid)
        {
            RwLockedDictionary<UUID, int> itemlist;
            if(m_ScriptedChatListeners.TryGetValue(objectid, out itemlist))
            {
                itemlist.Remove(itemid);
            }
            m_ScriptedChatListeners.RemoveIf(objectid, delegate (RwLockedDictionary<UUID, int> list) { return list.Count == 0; });
        }
    }
}
