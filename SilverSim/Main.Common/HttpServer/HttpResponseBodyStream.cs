// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;

namespace SilverSim.Main.Common.HttpServer
{
    public sealed class HttpResponseBodyStream : Stream
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

        public override int WriteTimeout 
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

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        public override void Close()
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

        protected override void Dispose(bool disposing)
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
