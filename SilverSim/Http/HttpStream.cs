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
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;

namespace SilverSim.Http
{
    public sealed class HttpStream : AbstractHttpStream
    {
        private readonly Socket m_Socket;
        private readonly byte[] m_Buffer;
        private int m_BufferPos;
        private int m_BufferFill;

        [Serializable]
        public class TimeoutException : Exception
        {
            public TimeoutException()
            {
            }

            public TimeoutException(string message) : base(message)
            {
            }

            public TimeoutException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected TimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

        public HttpStream(Socket sock)
        {
            ReadTimeout = 5000;
            m_Buffer = new byte[4096];
            m_Socket = sock;
            m_Socket.NoDelay = true;
        }

        protected override void Dispose(bool flag)
        {
            m_Socket.Dispose();
        }

        public override void Close()
        {
            m_Socket.Dispose();
            base.Close();
        }

        public int ReadBytesInternal(byte[] buffer, int maxbytes, int timeoutms)
        {
            m_Socket.ReceiveTimeout = timeoutms;
            try
            {
                return m_Socket.Receive(buffer, maxbytes, SocketFlags.Partial);
            }
            catch(SocketException e)
            {
                if(e.SocketErrorCode == SocketError.TimedOut)
                {
                    throw new TimeoutException();
                }
                else
                {
                    throw;
                }
            }
        }

        public override int ReadByte()
        {
            if(m_BufferFill == m_BufferPos)
            {
                m_BufferPos = 0;
                m_BufferFill = 0; /* reset buffer fill first, we may leave by exception in ReadBytesInternal */
                m_BufferFill = ReadBytesInternal(m_Buffer, m_Buffer.Length, ReadTimeout);
            }
            return (m_BufferPos < m_BufferFill) ? (int)m_Buffer[m_BufferPos++] : -1;
        }

        public override string ReadHeaderLine()
        {
            var s = new StringBuilder();
            for (; ;)
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
            while(count > 0)
            {
                if(m_BufferFill == m_BufferPos)
                {
                    m_BufferPos = 0;
                    m_BufferFill = 0; /* reset buffer fill first, we may leave by exception in ReadBytesInternal */
                    m_BufferFill = ReadBytesInternal(m_Buffer, m_Buffer.Length, ReadTimeout);
                    if(m_BufferFill == 0)
                    {
                        return rescount;
                    }
                }

                int bufferAvail = m_BufferFill - m_BufferPos;
                if(count > bufferAvail)
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
            get { return m_Socket.SendTimeout; }

            set { m_Socket.SendTimeout = value; }
        }

        public override bool CanTimeout => true;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override void Flush()
        {
            /* intentionally left empty */
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }

            set { throw new NotSupportedException(); }
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
            m_Socket.Send(buffer, offset, count, SocketFlags.None);
        }

        public override void WriteByte(byte value)
        {
            var b = new byte[] { value };
            Write(b, 0, 1);
        }
        #endregion
    }
}
