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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SilverSim.Main.Common.HttpClient
{
    public static partial class HttpRequestHandler
    {
        class ResponseBodyStream : Stream
        {
            private Stream m_Input;
            private long m_RemainingLength;
            private long m_ContentLength;
            private static readonly byte[] FillBytes = new byte[10240];
            private bool m_KeepAlive;
            string m_Scheme;
            string m_Host;
            int m_Port;

            public ResponseBodyStream(Stream input, long contentLength, bool keepAlive, string scheme, string host, int port)
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

            public new int ReadTimeout 
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

            public new IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                throw new NotSupportedException();
            }

            public new void Close()
            {
                if(m_Input != null)
                {
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

                    if(m_Input != null)
                    {
                        m_Input.Close();
                        m_Input = null;
                    }
                }
            }

            protected new void Dispose(bool disposing)
            {
                if (m_Input != null)
                {
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

                    if (m_Input != null)
                    {
                        m_Input.Close();
                        m_Input = null;
                    }
                }
                base.Dispose(disposing);
            }

            public new void EndWrite(IAsyncResult asyncResult)
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
                        if (m_RemainingLength > 10240)
                        {
                            Read(b, 0, 10240);
                        }
                        else
                        {
                            Read(b, 0, (int)m_RemainingLength);
                        }
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
                if(m_RemainingLength == 0 || m_Input == null)
                {
                    return 0;
                }

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
            public new void WriteByte(byte value)
            {
                throw new NotSupportedException();
            }
        }
    }
}
