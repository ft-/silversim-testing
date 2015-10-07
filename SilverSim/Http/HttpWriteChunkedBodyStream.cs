// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;
using System.Text;

namespace SilverSim.Http
{
    public class HttpWriteChunkedBodyStream : Stream
    {
        private Stream m_Output;
        private long m_WrittenLength = 0;
        private byte[] StreamBuffer = new byte[10240];
        private int BufferFill = 0;
        private byte[] EOB = new byte[2] { (byte)'\r', (byte)'\n' };

        public HttpWriteChunkedBodyStream(Stream output)
        {
            m_Output = output;
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
                return m_WrittenLength;
            }
        }

        public override long Position
        {
            get
            {
                return m_WrittenLength;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public new int WriteTimeout 
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

        public new IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        private static readonly byte[] LastChunkData = new byte[] { (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
        public new void Close()
        {
            if (m_Output != null)
            {
                if (BufferFill != 0)
                {
                    FlushBuffer();
                }
                m_Output.Write(LastChunkData, 0, LastChunkData.Length);
                m_Output = null;
            }
        }

        protected new void Dispose(bool disposing)
        {
            if (m_Output != null)
            {
                if (BufferFill != 0)
                {
                    FlushBuffer();
                }
                m_Output.Write(LastChunkData, 0, LastChunkData.Length);
                m_Output = null;
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            if (m_Output != null)
            {
                if(BufferFill != 0)
                {
                    FlushBuffer();
                }
                m_Output.Write(LastChunkData, 0, LastChunkData.Length);
                m_Output = null;
            }
        }

        private void FlushBuffer()
        {
            string chunkHeader = string.Format("{0:x}\r\n", BufferFill);
            byte[] chunkHeaderData = Encoding.ASCII.GetBytes(chunkHeader);
            m_Output.Write(chunkHeaderData, 0, chunkHeaderData.Length);
            m_Output.Write(StreamBuffer, 0, BufferFill);
            m_Output.Write(EOB, 0, EOB.Length);
            BufferFill = 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while(count > 0)
            {
                if(count + BufferFill >= StreamBuffer.Length)
                {
                    Buffer.BlockCopy(buffer, offset, StreamBuffer, BufferFill, StreamBuffer.Length - BufferFill);
                    count -= (StreamBuffer.Length - BufferFill);
                    offset += (StreamBuffer.Length - BufferFill);
                    FlushBuffer();
                }
                else
                {
                    Buffer.BlockCopy(buffer, offset, StreamBuffer, BufferFill, count);
                    count = 0;
                }
            }
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
