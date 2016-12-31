// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;

namespace SilverSim.Http.Client
{
    public static partial class HttpClient
    {
        public class RequestBodyStream : Stream
        {
            private Stream m_Output;
            private long m_RemainingLength;
            readonly long m_ContentLength;
            private static readonly byte[] FillBytes = new byte[10240];

            internal RequestBodyStream(Stream output, long contentLength)
            {
                m_RemainingLength = contentLength;
                m_Output = output;
                m_ContentLength = contentLength;
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
                if (m_Output != null)
                {
                    while (m_RemainingLength > 0)
                    {
                        Write(FillBytes, 0, 
                            m_RemainingLength > FillBytes.Length ? 
                            FillBytes.Length :
                            (int)m_RemainingLength);
                    }
                    m_Output = null;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (m_Output != null)
                {
                    while (m_RemainingLength > 0)
                    {
                        Write(FillBytes, 0,
                            m_RemainingLength > FillBytes.Length ?
                            FillBytes.Length :
                            (int)m_RemainingLength);
                    }
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
                        Write(FillBytes, 0,
                            m_RemainingLength > FillBytes.Length ?
                            FillBytes.Length :
                            (int)m_RemainingLength);
                    }
                }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (count > m_RemainingLength)
                {
                    count = (int)m_RemainingLength;
                }
                m_Output.Write(buffer, offset, count);
                m_RemainingLength -= count;
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
}
