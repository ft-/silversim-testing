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
using System.Globalization;
using System.IO;
using System.Text;

namespace SilverSim.Http
{
    public class HttpReadChunkedBodyStream : Stream
    {
        private Stream m_Input;
        private int m_RemainingChunkLength;
        private bool m_EndOfChunked;

        private string ReadHeaderLine()
        {
            int c;
            var headerLine = new StringBuilder();
            while ((c = m_Input.ReadByte()) != '\r')
            {
                if (c == -1)
                {
                    throw new EndOfStreamException();
                }
                headerLine.Append((char)c);
            }

            if (m_Input.ReadByte() != '\n')
            {
                throw new InvalidDataException();
            }

            return headerLine.ToString();
        }

        public HttpReadChunkedBodyStream(Stream input)
        {
            m_Input = input;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanTimeout => true;

        public override bool CanWrite => false;

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }

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

        public void ReadToEnd()
        {
            var b = new byte[10240];
            while (!m_EndOfChunked)
            {
                if (m_RemainingChunkLength == 0)
                {
                    Read(b, 0, 1);
                }
                else if (m_RemainingChunkLength > 10240)
                {
                    Read(b, 0, 10240);
                }
                else
                {
                    Read(b, 0, m_RemainingChunkLength);
                }
            }
        }

        public override void Close()
        {
            if(m_Input != null)
            {
                ReadToEnd();
                m_Input = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (m_Input != null)
            {
                ReadToEnd();
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
            if (m_Input != null)
            {
                ReadToEnd();
                m_Input = null;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int sumResult = 0;
            while(!m_EndOfChunked && count > 0)
            {
                if(m_RemainingChunkLength == 0)
                {
                    string chunkHeader = ReadHeaderLine();
                    string[] chunkFields = chunkHeader.Split(';');
                    if (chunkFields[0].Length == 0)
                    {
                        m_EndOfChunked = true;
                    }
                    else
                    {
                        if(!int.TryParse(chunkFields[0], System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out m_RemainingChunkLength))
                        {
                            throw new InvalidDataException();
                        }
                        if (0 == m_RemainingChunkLength)
                        {
                            m_EndOfChunked = true;
                            while ((chunkHeader = ReadHeaderLine()).Length != 0)
                            {
                                /* ReadHeaderLine() is all we have to do */
                            }
                        }
                    }
                    /* start to read a new chunk */
                }
                else if(m_RemainingChunkLength >= count)
                {
                    int result = m_Input.Read(buffer, offset, count);
                    if(result > 0)
                    {
                        m_RemainingChunkLength -= result;
                        if (0 == m_RemainingChunkLength)
                        {
                            ReadHeaderLine();
                        }
                        return sumResult + result;
                    }
                    else
                    {
                        throw new InvalidDataException();
                    }
                }
                else
                {
                    int result = m_Input.Read(buffer, offset, m_RemainingChunkLength);
                    if (result > 0)
                    {
                        m_RemainingChunkLength -= result;
                        sumResult += result;
                        offset += result;
                        count -= result;
                        if (0 == m_RemainingChunkLength)
                        {
                            ReadHeaderLine();
                        }
                    }
                    else
                    {
                        throw new InvalidDataException();
                    }
                }
            }
            return sumResult;
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
