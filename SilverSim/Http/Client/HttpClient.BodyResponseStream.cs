// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;

namespace SilverSim.Http.Client
{
    public static partial class HttpClient
    {
        public class ResponseBodyStream : Stream
        {
            private AbstractHttpStream m_Input;
            private long m_RemainingLength;
            readonly long m_ContentLength;
            readonly bool m_KeepAlive;
            readonly string m_Scheme;
            readonly string m_Host;
            readonly int m_Port;

            internal ResponseBodyStream(AbstractHttpStream input, long contentLength, bool keepAlive, string scheme, string host, int port)
            {
                m_RemainingLength = contentLength;
                m_Input = input;
                m_ContentLength = contentLength;
                m_KeepAlive = keepAlive;
                m_Scheme = scheme;
                m_Host = host;
                m_Port = port;
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

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                throw new NotSupportedException();
            }

            public override void Close()
            {
                if(m_Input != null)
                {
                    byte[] b = new byte[10240];
                    while(m_RemainingLength > 0)
                    {
                        Read(b, 0, m_RemainingLength > b.Length ?
                            b.Length :
                            (int)m_RemainingLength);
                    }

                    if(m_Input != null)
                    {
                        m_Input.Close();
                        m_Input = null;
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (m_Input != null)
                {
                    byte[] b = new byte[10240];
                    while (m_RemainingLength > 0)
                    {
                        Read(b, 0, m_RemainingLength > b.Length ?
                            b.Length :
                            (int)m_RemainingLength);
                    }

                    if (m_Input != null)
                    {
                        m_Input.Close();
                        m_Input = null;
                    }
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
                    byte[] b = new byte[10240];
                    while (m_RemainingLength > 0)
                    {
                        Read(b, 0, m_RemainingLength > b.Length ?
                            b.Length :
                            (int)m_RemainingLength);
                    }

                    if (m_Input != null)
                    {
                        m_Input.Close();
                        m_Input = null;
                    }
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int rescount = 0;
                int result;
                if (m_RemainingLength == 0 || m_Input == null)
                {
                    return 0;
                }

                while (count > 0 && m_RemainingLength != 0)
                {
                    int remcount = count;
                    if (remcount > m_RemainingLength)
                    {
                        remcount = (int)m_RemainingLength;
                    }

                    result = m_Input.Read(buffer, offset, remcount > 10240 ? 10240 : remcount);

                    if (result > 0)
                    {
                        m_RemainingLength -= result;
                        offset += result;
                        count -= result;
                        rescount += result;
                    }
                }

                if(m_RemainingLength == 0)
                {
                    if (m_KeepAlive)
                    {
                        AddStreamForNextRequest(m_Input, m_Scheme, m_Host, m_Port);
                    }
                    else
                    {
                        m_Input.Close();
                    }
                    m_Input = null;
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
}
