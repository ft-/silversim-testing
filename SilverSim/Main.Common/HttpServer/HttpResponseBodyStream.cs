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

using System;
using System.IO;

namespace SilverSim.Main.Common.HttpServer
{
    public sealed class HttpResponseBodyStream : Stream
    {
        private Stream m_Output;
        private long m_RemainingLength;
        readonly bool m_HasLimitedLength;
        static readonly byte[] FillBytes = new byte[10240];

        public HttpResponseBodyStream(Stream output, long contentLength)
        {
            m_RemainingLength = contentLength;
            m_Output = output;
            Length = contentLength;
            m_HasLimitedLength = true;
        }

        public HttpResponseBodyStream(Stream output)
        {
            m_RemainingLength = 0;
            m_Output = output;
            Length = 0;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanTimeout => true;

        public override bool CanWrite => true;

        public override long Length { get; }

        public override long Position
        {
            get
            {
                return Length - m_RemainingLength;
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
                    Write(FillBytes, 0, m_RemainingLength > FillBytes.Length ? FillBytes.Length : (int)m_RemainingLength);
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
                    Write(FillBytes, 0, m_RemainingLength > FillBytes.Length ? FillBytes.Length : (int)m_RemainingLength);
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
                    Write(FillBytes, 0, m_RemainingLength > FillBytes.Length ? FillBytes.Length : (int)m_RemainingLength);
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
