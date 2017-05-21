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

using log4net;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace SilverSim.Main.Common.Tar
{
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public class TarArchiveReader : Stream
    {
#if DEBUG
        private static readonly ILog m_Log = LogManager.GetLogger("TAR ARCHIVE READER");
#endif
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

        private int m_Position;
        private readonly Stream m_Stream;
        private int m_LengthOfData;

        public TarArchiveReader(Stream s)
        {
            m_Stream = s;
        }

        private byte[] ReadHeaderBytes()
        {
            var buf = new byte[512];
            if (m_LengthOfData != 0)
            {
                var exbuf = new byte[8192];
                while (m_LengthOfData > 0)
                {
                    int readBytes = m_Stream.Read(exbuf, 0, m_LengthOfData > exbuf.Length ? exbuf.Length : m_LengthOfData);
                    m_LengthOfData -= readBytes;
                    m_Position += readBytes;
                }
            }
            while(m_Position % 512 != 0)
            {
                m_Position += m_Stream.Read(buf, 0, 512 - (m_Position % 512));
            }

            if(m_Stream.Read(buf, 0, 512) != 512)
            {
                throw new IOException();
            }
            m_Position += 512;

            return buf;
        }

        public Header ReadHeader()
        {
            Encoding ascii = Encoding.ASCII;
            bool haveLongLink = false;
            byte[] buf;
            var hdr = new Header();
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
                    var fnameBytes = new byte[hdr.Length];
                    m_LengthOfData = hdr.Length;
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
                        /* nothing additional we can do besides what fnameLen counts */
                    }
                    hdr.FileName = ascii.GetString(buf, 0, fnameLen);
                }

            } while (buf[156] == (byte)TarFileType.LongLink);
            m_LengthOfData = hdr.Length;
#if DEBUG
            m_Log.DebugFormat("File {0}: {1}: {2}", hdr.FileName, hdr.FileType.ToString(), hdr.Length);
#endif
            return hdr;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }

            set { throw new NotSupportedException(); }
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
