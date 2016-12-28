// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;
using System.Net.Security;
using System.Text;

namespace SilverSim.Http
{
    public class HttpsStream : AbstractHttpStream
    {
        readonly SslStream m_Stream;
        readonly byte[] m_Buffer;
        int m_BufferPos;
        int m_BufferFill;

        public HttpsStream(SslStream stream)
        {
            ReadTimeout = 5000;
            m_Buffer = new byte[4096];
            m_Stream = stream;
        }

        protected override void Dispose(bool flag)
        {
            m_Stream.Dispose();
        }

        public int ReadBytesInternal(byte[] buffer, int maxbytes, int timeoutms)
        {
            m_Stream.ReadTimeout = timeoutms;
            return m_Stream.Read(buffer, 0, maxbytes);
        }

        public override int ReadByte()
        {
            if (m_BufferFill == m_BufferPos)
            {
                m_BufferPos = 0;
                m_BufferFill = 0; /* reset buffer fill first, we may leave by exception in ReadBytesInternal */
                m_BufferFill = ReadBytesInternal(m_Buffer, m_Buffer.Length, ReadTimeout);
            }
            return (m_BufferPos < m_BufferFill) ? (int)m_Buffer[m_BufferPos++] : -1;
        }

        public override string ReadHeaderLine()
        {
            StringBuilder s = new StringBuilder();
            for (;;)
            {
                if (m_BufferFill == m_BufferPos)
                {
                    m_BufferPos = 0;
                    m_BufferFill = 0; /* reset buffer fill first, we may leave by exception in ReadBytesInternal */
                    m_BufferFill = ReadBytesInternal(m_Buffer, m_Buffer.Length, ReadTimeout);
                    if (m_BufferFill == 0)
                    {
                        return s.ToString();
                    }
                }

                for (int i = m_BufferPos; i < m_BufferFill; ++i)
                {
                    if (m_Buffer[i] == (byte)'\r')
                    {
                        s.Append(Encoding.ASCII.GetString(m_Buffer, m_BufferPos, i - m_BufferPos));
                        m_BufferPos = i + 1;
                        if (ReadByte() != '\n')
                        {
                            throw new HttpHeaderFormatException();
                        }
                        return s.ToString();
                    }
                }
                s.Append(Encoding.ASCII.GetString(m_Buffer, m_BufferPos, m_BufferFill - m_BufferPos));
                m_BufferPos = m_BufferFill;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int rescount = 0;
            while (count > 0)
            {
                if (m_BufferFill == m_BufferPos)
                {
                    m_BufferPos = 0;
                    m_BufferFill = 0; /* reset buffer fill first, we may leave by exception in ReadBytesInternal */
                    m_BufferFill = ReadBytesInternal(m_Buffer, m_Buffer.Length, ReadTimeout);
                    if (m_BufferFill == 0)
                    {
                        return rescount;
                    }
                }

                int bufferAvail = m_BufferFill - m_BufferPos;
                if (count > bufferAvail)
                {
                    Buffer.BlockCopy(m_Buffer, m_BufferPos, buffer, offset, bufferAvail);
                    rescount += bufferAvail;
                    count -= bufferAvail;
                    offset += bufferAvail;
                    m_BufferPos += bufferAvail;
                }
                else
                {
                    Buffer.BlockCopy(m_Buffer, m_BufferPos, buffer, offset, count);
                    rescount += count;
                    m_BufferPos += count;
                    count = 0;
                }
            }

            return rescount;
        }

        #region Stream Functions
        public override int ReadTimeout { get; set; }

        public override int WriteTimeout
        {
            get
            {
                return m_Stream.WriteTimeout;
            }
            set
            {
                m_Stream.WriteTimeout = value;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return m_Stream.CanTimeout;
            }
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override void Flush()
        {
            /* intentionally left empty */
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
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
            m_Stream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            byte[] b = new byte[] { value };
            Write(b, 0, 1);
        }
        #endregion
    }
}
