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

using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System.ComponentModel;
using System.Globalization;

namespace SilverSim.Scene.Chat
{
    [Description("Region Chat Handler")]
    [PluginName("Chat")]
    [ServerParam("Chat.WhisperDistance", DefaultValue = 10.0, Description = "Whisper distance", ParameterType = typeof(double))]
    [ServerParam("Chat.SayDistance", DefaultValue = 20.0, Description = "Say distance", ParameterType = typeof(double))]
    [ServerParam("Chat.ShoutDistance", DefaultValue = 100.0, Description = "Shout distance", ParameterType = typeof(double))]
    public sealed class ChatHandlerFactory : ChatServiceFactoryInterface, IPlugin, IServerParamListener
    {
        private readonly RwLockedDictionary<UUID, double> m_WhisperDistances = new RwLockedDictionary<UUID, double>();
        private readonly RwLockedDictionary<UUID, double> m_SayDistances = new RwLockedDictionary<UUID, double>();
        private readonly RwLockedDictionary<UUID, double> m_ShoutDistances = new RwLockedDictionary<UUID, double>();

        private double GetWhisperDistance(UUID regionid)
        {
            double val;
            if(!m_WhisperDistances.TryGetValue(regionid, out val) &&
                !m_WhisperDistances.TryGetValue(UUID.Zero, out val))
            {
                val = 10;
            }
            return val;
        }

        private double GetSayDistance(UUID regionid)
        {
            double val;
            if (!m_SayDistances.TryGetValue(regionid, out val) &&
                !m_SayDistances.TryGetValue(UUID.Zero, out val))
            {
                val = 20;
            }
            return val;
        }

        private double GetShoutDistance(UUID regionid)
        {
            double val;
            if (!m_ShoutDistances.TryGetValue(regionid, out val) &&
                !m_ShoutDistances.TryGetValue(UUID.Zero, out val))
            {
                val = 100;
            }
            return val;
        }

        private bool TryGetChatHandler(SceneInterface scene, out ChatHandler chat)
        {
            try
            {
                chat = scene.GetService<ChatServiceInterface>() as ChatHandler;
            }
            catch
            {
                chat = null;
            }
            return chat != null;
        }

        [ServerParam("Chat.WhisperDistance")]
        public void HandleChatDistanceWhisperUpdated(UUID regionid, string value)
        {
            double val;
            if(value?.Length == 0)
            {
                m_WhisperDistances.Remove(regionid);
            }
            else if(double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
            {
                m_WhisperDistances[regionid] = val;
            }

            /* update regions */
            if(regionid != UUID.Zero)
            {
                SceneInterface scene;
                if(m_Scenes.TryGetValue(regionid, out scene))
                {
                    ChatHandler chat;
                    if(TryGetChatHandler(scene, out chat))
                    {
                        chat.WhisperDistance = GetWhisperDistance(regionid);
                    }
                }
            }
            else
            {
                foreach(SceneInterface scene in m_Scenes.Values)
                {
                    ChatHandler chat;
                    if (TryGetChatHandler(scene, out chat))
                    {
                        chat.WhisperDistance = GetWhisperDistance(regionid);
                    }
                }
            }
        }

        [ServerParam("Chat.SayDistance")]
        public void HandleChatDistanceSayUpdated(UUID regionid, string value)
        {
            double val;
            if (value?.Length == 0)
            {
                m_SayDistances.Remove(regionid);
            }
            else if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
            {
                m_SayDistances[regionid] = val;
            }

            /* update regions */
            if (regionid != UUID.Zero)
            {
                SceneInterface scene;
                if (m_Scenes.TryGetValue(regionid, out scene))
                {
                    ChatHandler chat;
                    if (TryGetChatHandler(scene, out chat))
                    {
                        chat.SayDistance = GetSayDistance(regionid);
                    }
                }
            }
            else
            {
                foreach (SceneInterface scene in m_Scenes.Values)
                {
                    ChatHandler chat;
                    if (TryGetChatHandler(scene, out chat))
                    {
                        chat.SayDistance = GetSayDistance(regionid);
                    }
                }
            }
        }

        [ServerParam("Chat.ShoutDistance")]
        public void HandleChatDistanceShoutUpdated(UUID regionid, string value)
        {
            double val;
            if (value?.Length == 0)
            {
                m_ShoutDistances.Remove(regionid);
            }
            else if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
            {
                m_ShoutDistances[regionid] = val;
            }

            /* update regions */
            if (regionid != UUID.Zero)
            {
                SceneInterface scene;
                if (m_Scenes.TryGetValue(regionid, out scene))
                {
                    ChatHandler chat;
                    if (TryGetChatHandler(scene, out chat))
                    {
                        chat.ShoutDistance = GetShoutDistance(regionid);
                    }
                }
            }
            else
            {
                foreach (SceneInterface scene in m_Scenes.Values)
                {
                    ChatHandler chat;
                    if (TryGetChatHandler(scene, out chat))
                    {
                        chat.ShoutDistance = GetShoutDistance(regionid);
                    }
                }
            }
        }

        public override ChatServiceInterface Instantiate(UUID regionId) =>
            new ChatHandler(
                GetWhisperDistance(regionId),
                GetSayDistance(regionId),
                GetShoutDistance(regionId));

        private SceneList m_Scenes;

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
        }
    }
}
