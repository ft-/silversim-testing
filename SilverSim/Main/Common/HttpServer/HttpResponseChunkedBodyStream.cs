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
using System.IO;
using System.Text;

namespace SilverSim.Main.Common.HttpServer
{
    public class HttpResponseChunkedBodyStream : Stream
    {
        private Stream m_Output;
        private long m_WrittenLength = 0;
        private byte[] StreamBuffer = new byte[10240];
        private int BufferFill = 0;

        public HttpResponseChunkedBodyStream(Stream output)
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
