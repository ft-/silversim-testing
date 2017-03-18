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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenJp2.Net
{
    public static class J2cEncoder
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("openjp2", EntryPoint = "j2c_encode")]
        static extern IntPtr J2cEncode(byte[] rawdata, int imagewidth, int imageheight, int imagecomponents, bool lossless);

        [DllImport("openjp2", EntryPoint = "j2c_encoded_get_length")]
        static extern uint J2cEncodedGetLength(IntPtr dataref);

        [DllImport("openjp2", EntryPoint = "j2c_encoded_read")]
        static extern int J2cEncodedRead(IntPtr dataref, byte[] buffer, int length);

        [DllImport("openjp2", EntryPoint = "j2c_encoded_free")]
        static extern void J2cEncodedFree(IntPtr dataref);

        static object m_InitLock = new object();
        static bool m_Inited;

        public static byte[] Encode(Bitmap img, bool lossless)
        {
            if(!m_Inited)
            {
                lock(m_InitLock)
                {
                    if(!m_Inited)
                    {
                        /* preload necessary windows dll */
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            if (Environment.Is64BitProcess)
                            {
                                if(IntPtr.Zero == LoadLibrary(Path.GetFullPath("platform-libs/windows/64/openjp2.dll")))
                                {
                                    throw new FileNotFoundException("missing platform-libs/windows/64/openjp2.dll");
                                }
                            }
                            else
                            {
                                if(IntPtr.Zero == LoadLibrary(Path.GetFullPath("platform-libs/windows/32/openjp2.dll")))
                                {
                                    throw new FileNotFoundException("missing platform-libs/windows/32/openjp2.dll");
                                }
                            }
                        }
                        m_Inited = true;
                    }
                }
            }
            int imgChannelWidth = img.Width * img.Height;
            int channels = 3;
            byte[] datastream;
            if(img.PixelFormat == PixelFormat.Format32bppArgb)
            {
                channels = 4;
                datastream = new byte[imgChannelWidth * channels];
                int pixpos = 0;
                for(int y = 0; y < img.Height; ++y)
                {
                    for(int x = 0; x < img.Width; ++x)
                    {
                        Color c = img.GetPixel(x, y);
                        datastream[pixpos++] = c.R;
                        datastream[pixpos++] = c.G;
                        datastream[pixpos++] = c.B;
                        datastream[pixpos++] = c.A;
                    }
                }
            }
            else if(img.PixelFormat == PixelFormat.Format24bppRgb)
            {
                channels = 3;
                datastream = new byte[imgChannelWidth * channels];
                int pixpos = 0;
                for (int y = 0; y < img.Height; ++y)
                {
                    for (int x = 0; x < img.Width; ++x)
                    {
                        Color c = img.GetPixel(x, y);
                        datastream[pixpos++] = c.R;
                        datastream[pixpos++] = c.G;
                        datastream[pixpos++] = c.B;
                    }
                }
            }
            else
            {
                throw new InvalidDataException();
            }

            IntPtr nativePtr = J2cEncode(datastream, img.Width, img.Height, channels, lossless);
            if(nativePtr == IntPtr.Zero)
            {
                throw new InvalidDataException();
            }

            byte[] j2cstream;
            try
            {
                j2cstream = new byte[J2cEncodedGetLength(nativePtr)];
                if(j2cstream.Length != J2cEncodedRead(nativePtr, j2cstream, j2cstream.Length))
                {
                    throw new InvalidDataException();
                }
            }
            finally
            {
                J2cEncodedFree(nativePtr);
            }

            return j2cstream;
        }
    }
}
