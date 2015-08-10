// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.StructuredData.JSON;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SilverSim.Tests.StructuredData
{
    class JSON20RPCParserTest : ITest
    {
        const string JSONInput = "{\"jsonrpc\":\"2.0\",\"result\":{\"UserId\":\"11111111-2222-3333-4444-fedcba987654\",\"PartnerId\":\"00000000-0000-0000-0000-000000000000\",\"PublishProfile\":false,\"PublishMature\":false,\"WebUrl\":\"\",\"WantToMask\":0,\"WantToText\":\"\",\"SkillsMask\":0,\"SkillsText\":\"\",\"Language\":\"\",\"ImageId\":\"00000000-0000-0000-0000-000000000000\",\"AboutText\":\"\",\"FirstLifeImageId\":\"00000000-0000-0000-0000-000000000000\",\"FirstLifeText\":\"\"},\"id\":\"a96f1bd0-c079-40a4-a012-d43c845d3b99\"}";

        public bool Run()
        {
            IValue iv = JSON20RPC.DeserializeResponse(new MemoryStream(UTF8NoBOM.GetBytes(JSONInput)));
            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
