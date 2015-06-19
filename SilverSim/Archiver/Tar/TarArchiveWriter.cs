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

using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilverSim.Archiver.Tar
{
    public class TarArchiveWriter
    {
        Stream m_Stream;
        int m_Position;

        public TarArchiveWriter(Stream s)
        {
            m_Stream = s;
        }

        byte[] m_FileModeBytes = Encoding.ASCII.GetBytes("0000777");
        byte[] m_OwnerIdBytes = Encoding.ASCII.GetBytes("0000764");
        byte[] m_GroupIdBytes = Encoding.ASCII.GetBytes("0000764");
        List<string> m_Directories = new List<string>();

        void WriteDirectory(string dirname)
        {
            string[] dirparts = dirname.Split('/');
            string dirpath = "";

            foreach(string dirpart in dirparts)
            {
                if(dirpath != "")
                {
                    dirpath += "/";
                }
                dirpath += dirpart;
                if(!m_Directories.Contains(dirpath))
                {
                    WriteHeader(dirpath + "/", TarFileType.Directory, 0);
                }
            }
        }

        void WriteHeader(string filename, TarFileType fileType, int fileSize)
        {
            byte[] bName = Encoding.ASCII.GetBytes(filename);
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
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(fileSizeOctal), 0, header, 124, 11);

            string lastModTime = Date.GetUnixTime().ToString();
            while(lastModTime.Length < 11)
            {
                lastModTime = "0" + lastModTime;
            }
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(lastModTime), 0, header, 136, 11);

            header[156] = (byte)fileType;

            Buffer.BlockCopy(Encoding.ASCII.GetBytes("0000000"), 0, header, 329, 7);
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("0000000"), 0, header, 337, 7);

            /* TAR checksum calculation */
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("        "), 0, header, 148, 8);

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
            Array.Copy(Encoding.ASCII.GetBytes(checkSumStr), 0, header, 148, 6);
            header[154] = 0;
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
