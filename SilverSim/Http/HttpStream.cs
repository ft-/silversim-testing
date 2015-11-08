// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;

namespace SilverSim.Http
{
    public class HttpStream : AbstractHttpStream
    {
        readonly Socket m_Socket;
        readonly byte[] m_Buffer;
        int m_BufferPos;
        int m_BufferFill;

        [Serializable]
        public class TimeoutException : Exception
        {
            public TimeoutException()
            {

            }

            public TimeoutException(string message)
                : base(message)
            {

            }

            protected TimeoutException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public TimeoutException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        public HttpStream(Socket sock)
        {
            ReadTimeout = 5000;
            m_Buffer = new byte[4096];
            m_Socket = sock;
        }

        public int ReadBytesInternal(byte[] buffer, int maxbytes, int timeoutms)
        {
            ArrayList readList = new ArrayList();
            readList.Add(m_Socket);
            ArrayList errorList = new ArrayList();
            errorList.Add(m_Socket);
            Socket.Select(readList, null, errorList, timeoutms * 1000);
            if(errorList.Count > 0 && readList.Count == 0)
            {
                throw new EndOfStreamException();
            }
            if(readList.Count == 0)
            {
                throw new TimeoutException();
            }

            int availableBytes = m_Socket.Available;
            if(availableBytes < maxbytes)
            {
                maxbytes = availableBytes;
            }
            return m_Socket.Receive(buffer, maxbytes, SocketFlags.Partial);
        }

        public override int ReadByte()
        {
            if(m_BufferFill == m_BufferPos)
            {
                m_BufferPos = 0;
                m_BufferFill = ReadBytesInternal(m_Buffer, m_Buffer.Length, ReadTimeout);
            }
            return (m_BufferPos < m_BufferFill) ? (int)m_Buffer[m_BufferPos++] : -1;
        }

        public override string ReadHeaderLine()
        {
            string s = string.Empty;
            for (; ;)
            {
                if (m_BufferFill == m_BufferPos)
                {
                    m_BufferPos = 0;
                    m_BufferFill = ReadBytesInternal(m_Buffer, m_Buffer.Length, ReadTimeout);
                    if (m_BufferFill == 0)
                    {
                        return s;
                    }
                }

                for (int i = m_BufferPos; i < m_BufferFill; ++i)
                {
                    if (m_Buffer[i] == (byte)'\r')
                    {
                        s += Encoding.ASCII.GetString(m_Buffer, m_BufferPos, i - m_BufferPos);
                        m_BufferPos = ++i;
                        if (ReadByte() != '\n')
                        {
                            throw new HttpHeaderFormatException();
                        }
                        return s;
                    }
                }
                s += Encoding.ASCII.GetString(m_Buffer, m_BufferPos, m_BufferFill - m_BufferPos);
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
            get
            {
                return m_Socket.SendTimeout;
            }
            set
            {
                m_Socket.SendTimeout = value;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return true;
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
            m_Socket.Send(buffer, offset, count, SocketFlags.None);
        }

        public override void WriteByte(byte value)
        {
            byte[] b = new byte[] { value };
            Write(b, 0, 1);
        }
        #endregion
    }
}
