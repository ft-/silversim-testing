// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;
using System.Text;

namespace SilverSim.Main.Common.HttpServer
{
    public sealed class HttpRequestBodyStream : Stream
    {
        private Stream m_Input;
        private long m_RemainingLength;
        private long m_ContentLength;
        private bool m_Expect100Continue;
        public HttpRequestBodyStream(Stream input, long contentLength, bool expect100Continue)
        {
            m_RemainingLength = contentLength;
            m_Input = input;
            m_ContentLength = contentLength;
            m_Expect100Continue = expect100Continue;
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
                return false;
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

        public override int ReadTimeout 
        { 
            get
            {
                return m_Input.ReadTimeout;
            }
            set
            {
                m_Input.ReadTimeout = value;
            }
        }

        void CheckExpect100()
        {
            if(m_Expect100Continue)
            {
                byte[] b = Encoding.ASCII.GetBytes("HTTP/1.0 100 Continue\r\n\r\n");
                m_Input.Write(b, 0, b.Length);
                m_Expect100Continue = false;
            }
        }
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            if(m_Input != null)
            {
                CheckExpect100();

                byte[] b = new byte[10240];
                while(m_RemainingLength > 0)
                {
                    if(m_RemainingLength > 10240)
                    {
                        Read(b, 0, 10240);
                    }
                    else
                    {
                        Read(b, 0, (int)m_RemainingLength);
                    }
                }
                m_Input = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (m_Input != null)
            {
                CheckExpect100();

                byte[] b = new byte[10240];
                while (m_RemainingLength > 0)
                {
                    if (m_RemainingLength > 10240)
                    {
                        Read(b, 0, 10240);
                    }
                    else
                    {
                        Read(b, 0, (int)m_RemainingLength);
                    }
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
                CheckExpect100();

                byte[] b = new byte[10240];
                while (m_RemainingLength > 0)
                {
                    if (m_RemainingLength > 10240)
                    {
                        Read(b, 0, 10240);
                    }
                    else
                    {
                        Read(b, 0, (int)m_RemainingLength);
                    }
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckExpect100();

            int rescount = 0;
            while (count > 0)
            {
                if (count > m_RemainingLength)
                {
                    count = (int)m_RemainingLength;
                }
                int result;
                if (count > 10240)
                {
                    result = m_Input.Read(buffer, offset, 10240);
                }
                else
                {
                    result = m_Input.Read(buffer, offset, count);
                }
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
            byte[] b = new byte[1];
            if (0 == Read(b, 0, 1))
            {
                return -1;
            }
            return (int)b[0];
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
