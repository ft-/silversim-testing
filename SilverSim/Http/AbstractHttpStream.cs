// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;
using System.Runtime.Serialization;

namespace SilverSim.Http
{
    [Serializable]
    public class HttpHeaderFormatException : Exception
    {
        public HttpHeaderFormatException()
        {

        }
    }

    [Serializable]
    public abstract class AbstractHttpStream : Stream
    {
        public AbstractHttpStream()
        {
        }

        public abstract string ReadHeaderLine();
    }
}
