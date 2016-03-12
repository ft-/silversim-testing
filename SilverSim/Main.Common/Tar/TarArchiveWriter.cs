// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace SilverSim.Main.Common.Tar
{
    [SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule")]
    public class TarArchiveWriter
    {
        readonly Stream m_Stream;
        int m_Position;

        public TarArchiveWriter(Stream s)
        {
            m_Stream = s;
        }

        static readonly byte[] m_FileModeBytes = Encoding.ASCII.GetBytes("0000777");
        static readonly byte[] m_OwnerIdBytes = Encoding.ASCII.GetBytes("0000764");
        static readonly byte[] m_GroupIdBytes = Encoding.ASCII.GetBytes("0000764");
        readonly List<string> m_Directories = new List<string>();

        void WriteDirectory(string dirname)
        {
            if(dirname.IndexOf('/') < 0)
            {
                return;
            }
            string[] dirparts = dirname.Split('/');
            StringBuilder dirpath = new StringBuilder();

            foreach(string dirpart in dirparts)
            {
                if(dirpath.Length != 0)
                {
                    dirpath.Append("/");
                }
                dirpath.Append(dirpart);
                if(!m_Directories.Contains(dirpath.ToString()))
                {
                    WriteHeader(dirpath + "/", TarFileType.Directory, 0);
                }
            }
        }

        public void WriteEndOfTar()
        {
            byte[] header = new byte[512];
            if (m_Position % 512 != 0)
            {
                m_Stream.Write(header, 0, 512 - (m_Position % 512));
                m_Position += (512 - (m_Position % 512));
            }
            m_Stream.Write(header, 0, 512);
            m_Stream.Flush();
        }

        void WriteHeader(string filename, TarFileType fileType, int fileSize)
        {
            Encoding ascii = Encoding.ASCII;
            byte[] bName = ascii.GetBytes(filename);
            if(bName.Length > 100)
            {
                WriteHeader("././@LongLink", TarFileType.LongLink, bName.Length);
                WriteBytes(bName);
            }

            byte[] header = new byte[512];
            if(m_Position % 512 != 0)
            {
                m_Stream.Write(header, 0, 512 - (m_Position % 512));
                m_Position += (512 - (m_Position % 512));
            }

            Buffer.BlockCopy(bName, 0, header, 0, bName.Length > 100 ? 100 : bName.Length);
            Buffer.BlockCopy(m_FileModeBytes, 0, header, 100, 7);
            Buffer.BlockCopy(m_OwnerIdBytes, 0, header, 108, 7);
            Buffer.BlockCopy(m_GroupIdBytes, 0, header, 116, 7);
            string fileSizeOctal = Convert.ToString(fileSize, 8);
            while(fileSizeOctal.Length < 11)
            {
                fileSizeOctal = "0" + fileSizeOctal;
            }
            Buffer.BlockCopy(ascii.GetBytes(fileSizeOctal), 0, header, 124, 11);

            string lastModTime = Convert.ToString((uint)Date.GetUnixTime(), 8);
            while(lastModTime.Length < 11)
            {
                lastModTime = " " + lastModTime;
            }
            Buffer.BlockCopy(ascii.GetBytes(lastModTime), 0, header, 136, 11);

            header[156] = (byte)fileType;

            Buffer.BlockCopy(ascii.GetBytes("0000000"), 0, header, 329, 7);
            Buffer.BlockCopy(ascii.GetBytes("0000000"), 0, header, 337, 7);

            /* TAR checksum calculation */
            Buffer.BlockCopy(ascii.GetBytes("        "), 0, header, 148, 8);

            int checksum = 0;
            foreach (byte b in header)
            {
                checksum += b;
            }

            string checkSumStr = Convert.ToString(checksum, 8);
            while(checkSumStr.Length < 6)
            {
                checkSumStr = "0" + checkSumStr;
            }
            Array.Copy(ascii.GetBytes(checkSumStr), 0, header, 148, 6);
            header[154] = 0;
            WriteBytes(header);
        }

        void WriteBytes(byte[] b)
        {
            m_Stream.Write(b, 0, b.Length);
            m_Position += b.Length;
        }

        public void WriteAsset(AssetData ad)
        {
            string fname = ad.FileName;
            WriteDirectory(fname.Substring(0, fname.LastIndexOf('/')));
            WriteHeader(fname, TarFileType.File, ad.Data.Length);
            WriteBytes(ad.Data);
        }

        public void WriteFile(string fname, byte[] data)
        {
            string dirname = fname;
            int rslash = dirname.LastIndexOf('/');
            if(rslash >= 0)
            {
                dirname = dirname.Substring(0, rslash);
            }
            WriteDirectory(dirname);
            WriteHeader(fname, TarFileType.File, data.Length);
            WriteBytes(data);
        }
    }
}
