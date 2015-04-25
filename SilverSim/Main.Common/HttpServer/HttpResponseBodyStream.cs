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

using System;
using System.IO;

namespace SilverSim.Main.Common.HttpServer
{
    public class HttpResponseBodyStream : Stream
    {
        private Stream m_Output;
        private long m_RemainingLength;
        private bool m_HasLimitedLength;
        private long m_ContentLength;
        private static readonly byte[] FillBytes = new byte[10240];

        public HttpResponseBodyStream(Stream output, long contentLength)
        {
            m_RemainingLength = contentLength;
            m_Output = output;
            m_ContentLength = contentLength;
            m_HasLimitedLength = true;
        }

        public HttpResponseBodyStream(Stream output)
        {
            m_RemainingLength = 0;
            m_Output = output;
            m_ContentLength = 0;
            m_HasLimitedLength = false;
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek 
        { 
            get
            {
                return false;
            }
        }

        public override bool CanTimeout 
        { 
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override long Length 
        { 
            get
            {
                return m_ContentLength;
            }
        }

        public override long Position
        {
            get
            {
                return m_ContentLength - m_RemainingLength;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public new int WriteTimeout 
        { 
            get
            {
                return m_Output.WriteTimeout;
            }
            set
            {
                m_Output.WriteTimeout = value;
            }
        }

        public new IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        public new void Close()
        {
            if(m_Output != null)
            {
                while(m_RemainingLength > 0)
                {
                    if(m_RemainingLength > FillBytes.Length)
                    {
                        Write(FillBytes, 0, FillBytes.Length);
                    }
                    else
                    {
                        Write(FillBytes, 0, (int)m_RemainingLength);
                    }
                }
                m_Output.Flush();
                m_Output = null;
            }
        }

        protected new void Dispose(bool disposing)
        {
            if (m_Output != null)
            {
                while (m_RemainingLength > 0)
                {
                    if (m_RemainingLength > 10240)
                    {
                        Write(FillBytes, 0, 10240);
                    }
                    else
                    {
                        Write(FillBytes, 0, (int)m_RemainingLength);
                    }
                }
                m_Output.Flush();
                m_Output = null;
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            if (m_Output != null && m_RemainingLength > 0)
            {
                while (m_RemainingLength > 0)
                {
                    if (m_RemainingLength > FillBytes.Length)
                    {
                        Write(FillBytes, 0, FillBytes.Length);
                    }
                    else
                    {
                        Write(FillBytes, 0, (int)m_RemainingLength);
                    }
                }
                m_Output.Flush();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if(count > m_RemainingLength && m_HasLimitedLength)
            {
                count = (int)m_RemainingLength;
            }
            m_Output.Write(buffer, offset, count);
            if (m_RemainingLength >= count)
            {
                m_RemainingLength -= count;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
