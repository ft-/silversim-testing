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
using System.Text;

namespace SilverSim.Main.Common.HttpServer
{
    public sealed class HttpRequestBodyStream : Stream
    {
        private Stream m_Input;
        private long m_RemainingLength;
        public HttpRequestBodyStream(Stream input, long contentLength)
        {
            m_RemainingLength = contentLength;
            m_Input = input;
            Length = contentLength;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanTimeout => true;

        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position
        {
            get { return Length - m_RemainingLength; }

            set { throw new NotSupportedException(); }
        }

        public override int ReadTimeout
        {
            get { return m_Input.ReadTimeout; }

            set { m_Input.ReadTimeout = value; }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            if(m_Input != null)
            {
                var b = new byte[10240];
                while(m_RemainingLength > 0)
                {
                    Read(b, 0, m_RemainingLength > b.Length ? b.Length : (int)m_RemainingLength);
                }
                m_Input = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (m_Input != null)
            {
                var b = new byte[10240];
                while (m_RemainingLength > 0)
                {
                    Read(b, 0, m_RemainingLength > b.Length ? b.Length : (int)m_RemainingLength);
                }
                m_Input = null;
            }
            base.Dispose(disposing);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            if (m_Input != null && m_RemainingLength > 0)
            {
                var b = new byte[10240];
                while (m_RemainingLength > 0)
                {
                    Read(b, 0, m_RemainingLength > b.Length ? b.Length : (int)m_RemainingLength);
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int rescount = 0;
            while (count > 0)
            {
                if (count > m_RemainingLength)
                {
                    count = (int)m_RemainingLength;
                }
                int result = m_Input.Read(buffer, offset, count > 10240 ? 10240 : count);

                if (result > 0)
                {
                    m_RemainingLength -= result;
                    offset += result;
                    count -= result;
                    rescount += result;
                }
            }
            return rescount;
        }

        public override int ReadByte()
        {
            var b = new byte[1];
            if (0 == Read(b, 0, 1))
            {
                return -1;
            }
            return b[0];
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }
    }
}
