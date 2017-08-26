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
        private readonly RwLockedDoubleDictionary<UUID /* ItemID */, UInt32 /* LocalID */, KeyValuePair<UUID /* SceneID */, UUID /* ObjectID */>> m_AttachmentsList = new RwLockedDoubleDictionary<UUID, UInt32, KeyValuePair<UUID, UUID>>();

        public void DoSay(int channel, string text)
        {
            ChatServiceInterface chatService = CurrentScene.GetService<ChatServiceInterface>();
            chatService.Send(new ListenEvent()
            {
                ID = ID,
                Type = ListenEvent.ChatType.Say,
                Channel = channel,
                GlobalPosition = GlobalPosition,
                Name = Name,
                Message = text,
                TargetID = UUID.Zero,
                SourceType = ListenEvent.ChatSourceType.Agent,
                OwnerID = ID
            });
        }

        public void DoSay(string text)
        {
            DoSay(0, text);
        }

        public new void SendAnimations()
        {
            base.SendAnimations();
        }

        public void DoShout(int channel, string text)
        {
            var chatService = CurrentScene.GetService<ChatServiceInterface>();
            chatService.Send(new ListenEvent()
            {
                ID = ID,
                Type = ListenEvent.ChatType.Shout,
                Channel = channel,
                GlobalPosition = GlobalPosition,
                Name = Name,
                Message = text,
                TargetID = UUID.Zero,
                SourceType = ListenEvent.ChatSourceType.Agent,
                OwnerID = ID
            });
        }

        public void DoShout(string text)
        {
            DoShout(0, text);
        }

        public void DoWhisper(int channel, string text)
        {
            ChatServiceInterface chatService = CurrentScene.GetService<ChatServiceInterface>();
            chatService.Send(new ListenEvent()
            {
                ID = ID,
                Type = ListenEvent.ChatType.Whisper,
                Channel = channel,
                GlobalPosition = GlobalPosition,
                Name = Name,
                Message = text,
                TargetID = UUID.Zero,
                SourceType = ListenEvent.ChatSourceType.Agent,
                OwnerID = ID
            });
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
            var dInfo = new DetectInfo()
            {
                LinkNumber = linkNum,
                TouchFace = -1,
                Name = Owner.FullName,
                GrabOffset = part.LocalPosition * -1f,
                Group = Group,
                Key = ID,
                ObjType = DetectedTypeFlags.Npc | ((SittingOnObject != null) ? DetectedTypeFlags.Passive : DetectedTypeFlags.Active),
                Owner = Owner,
                Position = GlobalPosition,
                Rotation = GlobalRotation,
                TouchBinormal = Vector3.Zero,
                TouchNormal = Vector3.Zero,
                TouchST = new Vector3(-1f, -1f, 0),
                TouchUV = new Vector3(-1f, -1f, 0)
            };
            var te = new TouchEvent()
            {
                Type = TouchEvent.TouchType.Start
            };
            te.Detected.Add(dInfo);
            part.PostTouchEvent(te);

            te = new TouchEvent()
            {
                Type = TouchEvent.TouchType.Continuous
            };
            te.Detected.Add(dInfo);
            part.PostTouchEvent(te);

            te = new TouchEvent()
            {
                Type = TouchEvent.TouchType.End
            };
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

        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, int>> m_ScriptedIMListeners = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, int>>(() => new RwLockedDictionary<UUID, int>());
        private readonly RwLockedDictionary<UUID, UUID> m_ScriptedIMSessions = new RwLockedDictionary<UUID, UUID>();

        public UUID GetOrCreateIMSession(UUID targetid) =>
            m_ScriptedIMSessions.GetOrAddIfNotExists(targetid, () => UUID.Random);

        public override bool IMSend(GridInstantMessage im)
        {
            if(im.Dialog == GridInstantMessageDialog.MessageFromAgent)
            {
                m_ScriptedIMSessions.Add(im.FromAgent.ID, im.IMSessionID);
            }

            if (im.Dialog == GridInstantMessageDialog.MessageFromAgent ||
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

                                /* Translate IM event to mapped channel */
                                instance?.PostEvent(new ListenEvent()
                                {
                                    ButtonIndex = -1,
                                    Channel = kvpinner.Value,
                                    Distance = 0,
                                    GlobalPosition = Vector3.Zero,
                                    ID = im.FromAgent.ID,
                                    Message = im.Message,
                                    Name = im.FromAgent.FullName,
                                    OriginSceneID = UUID.Zero,
                                    OwnerID = im.FromAgent.ID,
                                    SourceType = im.Dialog == GridInstantMessageDialog.MessageFromObject ? ListenEvent.ChatSourceType.Object : ListenEvent.ChatSourceType.Agent,
                                    TargetID = ID,
                                    Type = ListenEvent.ChatType.Say
                                });
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
            m_ScriptedIMListeners.RemoveIf(objectid, (RwLockedDictionary<UUID, int> list) => list.Count == 0);
        }

        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, int>> m_ScriptedChatListeners = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, int>>(() => new RwLockedDictionary<UUID, int>());

        private void OnChatReceive(ListenEvent ev)
        {
            if(ev.ID == ID)
            {
                /* if it is this npc, ignore the chat event */
                return;
            }

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

                            /* Translate listen event to mapped channel */
                            instance?.PostEvent(new ListenEvent()
                            {
                                ButtonIndex = ev.ButtonIndex,
                                Channel = kvpinner.Value,
                                Distance = ev.Distance,
                                GlobalPosition = ev.GlobalPosition,
                                ID = ev.ID,
                                Message = ev.Message,
                                Name = ev.Name,
                                OriginSceneID = ev.OriginSceneID,
                                OwnerID = ev.OwnerID,
                                SourceType = ev.SourceType,
                                TargetID = ev.TargetID,
                                Type = ev.Type
                            });
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
            m_ScriptedChatListeners.RemoveIf(objectid, (RwLockedDictionary<UUID, int> list) => list.Count == 0);
        }
    }
}
