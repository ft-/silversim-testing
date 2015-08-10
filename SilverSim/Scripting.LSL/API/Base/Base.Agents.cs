// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        public int llGetRegionAgentCount(ScriptInstance Instance)
        {
            return Instance.Part.ObjectGroup.Scene.Agents.Count;
        }

        [APILevel(APIFlags.LSL)]
        public const int DATA_ONLINE = 1;
        [APILevel(APIFlags.LSL)]
        public const int DATA_NAME = 2;
        [APILevel(APIFlags.LSL)]
        public const int DATA_BORN = 3;
        [APILevel(APIFlags.LSL)]
        public const int DATA_RATING = 4;
        [APILevel(APIFlags.LSL)]
        public const int DATA_PAYINFO = 8;

        [APILevel(APIFlags.LSL)]
        public const int PAYMENT_INFO_ON_FILE = 0x1;
        [APILevel(APIFlags.LSL)]
        public const int PAYMENT_INFO_USED = 0x2;

        [APILevel(APIFlags.LSL)]
        public LSLKey llRequestAgentData(ScriptInstance Instance, LSLKey id, int data)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llRequestDisplayName(ScriptInstance Instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llRequestUsername(ScriptInstance Instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public string llGetDisplayName(ScriptInstance Instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public string llKey2Name(ScriptInstance Instance, LSLKey id)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetAgentSize(ScriptInstance Instance, LSLKey id)
        {
            lock (Instance)
            {
                IAgent agent;
                try
                {
                    agent = Instance.Part.ObjectGroup.Scene.Agents[id];
                }
                catch
                {
                    return Vector3.Zero;
                }

                if (agent.IsInScene(Instance.Part.ObjectGroup.Scene))
                {
                    return agent.Size;
                }
                return Vector3.Zero;
            }
        }

        #region osGetAvatarList
        [APILevel(APIFlags.OSSL)]
        public AnArray osGetAvatarList(ScriptInstance Instance)
        {
            AnArray res = new AnArray();

            lock (Instance)
            {
                foreach (IAgent agent in Instance.Part.ObjectGroup.Scene.Agents)
                {
                    if (agent.ID == Instance.Part.ObjectGroup.Scene.Owner.ID)
                    {
                        continue;
                    }
                    res.Add(new LSLKey(agent.ID));
                    res.Add(agent.GlobalPosition);
                    res.Add(agent.Name);
                }
            }
            return res;
        }
        #endregion

        #region osGetAgents
        [APILevel(APIFlags.OSSL)]
        public AnArray osGetAgents(ScriptInstance Instance)
        {
            AnArray res = new AnArray();

            lock (Instance)
            {
                foreach (IAgent agent in Instance.Part.ObjectGroup.Scene.Agents)
                {
                    res.Add(agent.Name);
                }
            }
            return res;
        }
        #endregion
    }
}
