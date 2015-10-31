// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Profile
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct ProfileProperties
    {
        public UUI User;
        public UUI Partner;
        public bool PublishProfile;
        public bool PublishMature;
        public string WebUrl;
        public uint WantToMask;
        public string WantToText;
        public uint SkillsMask;
        public string SkillsText;
        public string Language;
        public UUID ImageID;
        public string AboutText;
        public UUID FirstLifeImageID;
        public string FirstLifeText;
    }
}
