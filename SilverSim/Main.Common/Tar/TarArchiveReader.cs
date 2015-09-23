// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SilverSim.Main.Common.Tar
{
    public class TarArchiveReader : Stream
    {
        /* TarArchiveReader has Stream support, so that we can directly apply XmlTextReader and so on */
        public class EndOfTarException : Exception
        {
            public EndOfTarException()
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
        Stream m_Stream;
        int m_LengthOfData = 0;

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
                hdr.Length = Convert.ToInt32(Encoding.ASCII.GetString(buf, 124, 11), 8);
                
                if(buf[156] == (byte)TarFileType.LongLink)
                {
                    haveLongLink = true;
                    byte[] fnameBytes = new byte[hdr.Length];
                    if(Read(fnameBytes, 0, hdr.Length) != hdr.Length)
                    {
                        throw new IOException();
                    }
                    hdr.FileName = Encoding.ASCII.GetString(fnameBytes);
                }
                else if(!haveLongLink)
                {
                    int fnameLen = 0;
                    for(fnameLen = 0; fnameLen < 100 && buf[fnameLen] != 0; ++fnameLen)
                    {

                    }
                    hdr.FileName = Encoding.ASCII.GetString(buf, 0, fnameLen);
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
        }

        public override long Length
        {
            get 
            { 
                throw new NotImplementedException(); 
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
