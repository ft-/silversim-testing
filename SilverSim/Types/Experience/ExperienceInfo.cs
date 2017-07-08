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

using SilverSim.Types.Grid;
using SilverSim.Types.Script;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.IO;

namespace SilverSim.Types.Experience
{
    [Flags]
    public enum ExperiencePropertyFlags
    {
        None = 0,
        Invalid = 1 << 0,
        Privileged = 1 << 3,
        Grid = 1 << 4,
        Private = 1 << 5,
        Disabled = 1 << 6,
        Suspended = 1 << 7
    }

    public class ExperienceInfo
    {
        public UUID ID = UUID.Zero;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public ExperiencePropertyFlags Properties = ExperiencePropertyFlags.None;
        public UUI Creator = UUI.Unknown;
        public UGI Group = UGI.Unknown;
        public RegionAccess Maturity;
        public string Marketplace = string.Empty;
        public UUID LogoID = UUID.Zero;
        public string SlUrl = string.Empty;

        public string ExtendedMetadata
        {
            get
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    LlsdXml.Serialize(new Map
                    {
                        ["marketplace"] = (AString)Marketplace,
                        ["logo"] = LogoID
                    }, ms);
                    return ms.ToArray().FromUTF8Bytes();
                }
            }
            set
            {
                using (MemoryStream ms = new MemoryStream(value.ToUTF8Bytes()))
                {
                    IValue iv;
                    Map m = (Map)LlsdXml.Deserialize(ms);
                    Marketplace = m.TryGetValue("marketplace", out iv) ? iv.ToString() : string.Empty;
                    m.TryGetValue("logo", out LogoID);
                }
            }
        }

        public ExperienceInfo()
        {
        }

        public ExperienceInfo(ExperienceInfo info)
        {
            ID = info.ID;
            Name = info.Name;
            Description = info.Description;
            Properties = info.Properties;
            Creator = new UUI(info.Creator);
            Group = new UGI(info.Group);
            Maturity = info.Maturity;
            Marketplace = info.Marketplace;
            LogoID = info.LogoID;
            SlUrl = info.SlUrl;
        }
    }
}
