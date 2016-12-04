﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace SilverSim.ServiceInterfaces.Avatar
{
    [Serializable]
    public class AvatarUpdateFailedException : Exception
    {
        public AvatarUpdateFailedException()
        { 
        }

        public AvatarUpdateFailedException(string message)
            : base(message)
        {

        }

        protected AvatarUpdateFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public AvatarUpdateFailedException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    public abstract class AvatarServiceInterface
    {
        #region Constructor
        public AvatarServiceInterface()
        {

        }
        #endregion

        public abstract Dictionary<string, string> this[UUID avatarID]
        {
            get;
            set; /* setting null means remove of avatar settings */
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract string this[UUID avatarID, string itemKey]
        {
            get;
            set;
        }

        public abstract bool TryGetValue(UUID avatarID, string itemKey, out string value);

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract List<string> this[UUID avatarID, IList<string> itemKeys]
        {
            get;
            set;
        }

        public abstract void Remove(UUID avatarID, IList<string> nameList);
        public abstract void Remove(UUID avatarID, string name);

        public void StoreAppeanceInfo(UUID avatarID, AppearanceInfo aInfo)
        {
            Dictionary<string, string> vals = new Dictionary<string, string>();
            vals.Add("Serial", aInfo.Serial.ToString());
            vals.Add("AvatarHeight", aInfo.AvatarHeight.ToString(CultureInfo.InvariantCulture));
            string visualParams = string.Empty;
            bool firstVp = true;
            StringBuilder sb = new StringBuilder();
            foreach(byte b in aInfo.VisualParams)
            {
                if(!firstVp)
                {
                    sb.Append(",");
                }
                firstVp = false;
                sb.Append(b.ToString());
            }
            vals.Add("VisualParams", sb.ToString());

            Dictionary<WearableType, List<AgentWearables.WearableInfo>> wearables = aInfo.Wearables.All;
            foreach (KeyValuePair<WearableType, List<AgentWearables.WearableInfo>> kvp in wearables)
            {
                int no = 0;
                foreach(AgentWearables.WearableInfo wi in kvp.Value)
                {
                    if (wi.AssetID != UUID.Zero)
                    {
                        vals.Add(string.Format("Wearable {0}:{1}", (int)kvp.Key, no), string.Format("{0}:{1}", wi.ItemID.ToString(), wi.AssetID.ToString()));
                    }
                    else
                    {
                        vals.Add(string.Format("Wearable {0}:{1}", (int)kvp.Key, no), wi.ItemID.ToString());
                    }
                    ++no;
                }
            }
            foreach(KeyValuePair<AttachmentPoint, RwLockedDictionary<UUID, UUID>> kvp in aInfo.Attachments)
            {
                List<string> itemIds = new List<string>();
                foreach (KeyValuePair<UUID, UUID> kvpAttachment in kvp.Value)
                {
                    itemIds.Add(kvpAttachment.Key.ToString());
                }
                vals.Add("_ap_" + ((int)kvp.Key).ToString(), string.Join(",", itemIds));
            }
            this[avatarID] = vals;
        }

        public bool TryGetAppearanceInfo(UUID avatarID, out AppearanceInfo aInfo)
        {
            Dictionary<string, string> items = this[avatarID];

            aInfo = new AppearanceInfo();

            if (items.Count == 0)
            {
                return false;
            }

            string val;
            uint uintval;
            if (items.TryGetValue("Serial", out val) && uint.TryParse(val, out uintval))
            {
                aInfo.Serial = uintval;
            }
            double realval;
            if(items.TryGetValue("AvatarHeight", out val) && double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out realval))
            {
                aInfo.AvatarHeight = realval;
            }
            else
            {
                aInfo.AvatarHeight = 2;
            }
            if(items.TryGetValue("VisualParams", out val))
            {
                string[] vals = val.Split(',');
                byte[] vp = new byte[vals.Length];
                for(int i = 0; i < vals.Length; ++i)
                {
                    vp[i] = byte.Parse(vals[i]);
                }
                aInfo.VisualParams = vp;
            }

            /* default avatar textures here */
            for (int i = 0; i < AppearanceInfo.AvatarTextureData.TextureCount; ++i)
            {
                aInfo.AvatarTextures[i] = new UUID("c228d1cf-4b5d-4ba8-84f4-899a0796aa97");
            }

            Regex wearable_match = new Regex(@"/^Wearable ([0-9]*)\:([0-9]*)$/");
            Dictionary<WearableType, List<AgentWearables.WearableInfo>> wearables = new Dictionary<WearableType, List<AgentWearables.WearableInfo>>();
            foreach (KeyValuePair<string, string> kvp in items)
            {
                Match m = wearable_match.Match(kvp.Key);
                if(m.Success)
                {
                    int pos = int.Parse(m.Groups[1].Value);
                    int no = int.Parse(m.Groups[2].Value);
                    string[] va = kvp.Value.Split(':');
                    AgentWearables.WearableInfo wi = new AgentWearables.WearableInfo();
                    wi.ItemID = UUID.Parse(va[0]);
                    if(va.Length > 1)
                    {
                        wi.AssetID = UUID.Parse(va[1]);
                    }
                    List<AgentWearables.WearableInfo> wiList;
                    if(!wearables.TryGetValue((WearableType)pos, out wiList))
                    {
                        wiList = new List<AgentWearables.WearableInfo>();
                        wearables.Add((WearableType)pos, wiList);
                    }
                    wiList.Add(wi);
                }
                else if(kvp.Key.StartsWith("_ap_"))
                {
                    aInfo.Attachments[(AttachmentPoint)int.Parse(kvp.Key.Substring(4))].Add(UUID.Parse(kvp.Value), UUID.Zero);
                }
            }
            aInfo.Wearables.All = wearables;

            return true;
        }
    }
}
