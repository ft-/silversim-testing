// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace SilverSim.Main.Common.Tar
{
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public class TarArchiveReader : Stream
    {
        /* TarArchiveReader has Stream support, so that we can directly apply XmlTextReader and so on */
        [Serializable]
        public class EndOfTarException : Exception
        {
            public EndOfTarException()
            {

            }

            public EndOfTarException(string message)
                : base(message)
            {

            }

            protected EndOfTarException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public EndOfTarException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        public struct Header
        {
            public string FileName;
            public TarFileType FileType;
            public int Length;
        }

        int m_Position;
        readonly Stream m_Stream;
        int m_LengthOfData;

        public TarArchiveReader(Stream s)
        {
            m_Stream = s;
        }

        byte[] ReadHeaderBytes()
        {
            byte[] buf = new byte[512];
            while (m_LengthOfData > 0)
            {
                int readBytes = m_Stream.Read(buf, 0, m_LengthOfData > 512 ? 512 : m_LengthOfData);
                m_LengthOfData -= readBytes;
                m_Position += readBytes;
            }
            while(m_Position % 512 != 0)
            {
                m_Position += m_Stream.Read(buf, 0, 512 - (m_Position % 512));
            }

            if(m_Stream.Read(buf, 0, 512) != 512)
            {
                throw new IOException();
            }

            return buf;
        }

        public Header ReadHeader()
        {
            Encoding ascii = Encoding.ASCII;
            bool haveLongLink = false;
            byte[] buf;
            Header hdr = new Header();
            do
            {
                buf = ReadHeaderBytes();
                if(buf[0] == 0)
                {
                    throw new EndOfTarException();
                }
                hdr.FileType = (TarFileType)buf[156];
                hdr.Length = Convert.ToInt32(ascii.GetString(buf, 124, 11), 8);
                
                if(buf[156] == (byte)TarFileType.LongLink)
                {
                    haveLongLink = true;
                    byte[] fnameBytes = new byte[hdr.Length];
                    if(Read(fnameBytes, 0, hdr.Length) != hdr.Length)
                    {
                        throw new IOException();
                    }
                    hdr.FileName = ascii.GetString(fnameBytes);
                }
                else if(!haveLongLink)
                {
                    int fnameLen;
                    for(fnameLen = 0; fnameLen < 100 && buf[fnameLen] != 0; ++fnameLen)
                    {

                    }
                    hdr.FileName = ascii.GetString(buf, 0, fnameLen);
                }

            } while (buf[156] == (byte)TarFileType.LongLink);
            m_LengthOfData = hdr.Length;
            return hdr;
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
                return false;
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
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

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(m_LengthOfData < count)
            {
                count = m_LengthOfData;
            }
            if(count > 0)
            {
                int readBytes = m_Stream.Read(buffer, offset, count);
                m_Position += readBytes;
                m_LengthOfData -= readBytes;
                count = readBytes;
            }

            return count;
        }

        public override int ReadByte()
        {
            if(m_LengthOfData == 0)
            {
                return -1;
            }
            int b = m_Stream.ReadByte();
            if(b >= 0)
            {
                --m_LengthOfData;
                ++m_Position;
            }
            return b;
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
    }
}
