// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;

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
            string headerLine = string.Empty;
            while ((c = m_Input.ReadByte()) != '\r')
            {
                if (c == -1)
                {
                    throw new EndOfStreamException();
                }
                headerLine += ((char)c).ToString();
            }

            if (m_Input.ReadByte() != '\n')
            {
                throw new InvalidDataException();
            }

            return headerLine;
        }

        public HttpReadChunkedBodyStream(Stream input)
        {
            m_Input = input;
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

        public void ReadToEnd()
        {
            byte[] b = new byte[10240];
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

        public new void Close()
        {
            if(m_Input != null)
            {
                ReadToEnd();
                m_Input = null;
            }
        }

        protected virtual new void Dispose(bool disposing)
        {
            if (m_Input != null)
            {
                ReadToEnd();
                m_Input = null;
            }
            base.Dispose(disposing);
        }

        public new void EndWrite(IAsyncResult asyncResult)
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
                        m_RemainingChunkLength = int.Parse(chunkFields[0], System.Globalization.NumberStyles.HexNumber);
                        if (0 == m_RemainingChunkLength)
                        {
                            m_EndOfChunked = true;
                            while ((chunkHeader = ReadHeaderLine()).Length != 0)
                            {
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
        public new void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }
    }
}
