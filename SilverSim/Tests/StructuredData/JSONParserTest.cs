/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Main.Common;
using SilverSim.StructuredData.JSON;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using System.IO;
using System.Text;

namespace SilverSim.Tests.StructuredData
{
    public class JSONParserTest : ITest
    {
        
        const string JSONInput = "{\"jsonrpc\":\"2.0\",\"result\":{\"UserId\":\"11111111-2222-3333-4444-fedcba987654\",\"PartnerId\":\"00000000-0000-0000-0000-000000000000\",\"PublishProfile\":false,\"PublishMature\":false,\"WebUrl\":\"\",\"WantToMask\":0,\"WantToText\":\"\",\"SkillsMask\":0,\"SkillsText\":\"\",\"Language\":\"\",\"ImageId\":\"00000000-0000-0000-0000-000000000000\",\"AboutText\":\"\",\"FirstLifeImageId\":\"00000000-0000-0000-0000-000000000000\",\"FirstLifeText\":\"\"},\"id\":\"a96f1bd0-c079-40a4-a012-d43c845d3b99\"}";

        public bool Run()
        {
            IValue iv = JSON.Deserialize(new MemoryStream(UTF8NoBOM.GetBytes(JSONInput)));
            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
