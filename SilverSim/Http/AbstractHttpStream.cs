// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;

namespace SilverSim.Http
{
    public class HttpHeaderFormatException : Exception
    {
        public HttpHeaderFormatException()
        {

        }
    }

    public abstract class AbstractHttpStream : Stream
    {
        public AbstractHttpStream()
        {
        }

        public abstract string ReadHeaderLine();
    }
}
