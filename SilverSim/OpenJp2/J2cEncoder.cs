// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        static extern uint J2cEncodedRead(IntPtr dataref, byte[] buffer, int length);

        [DllImport("openjp2", EntryPoint = "j2c_encoded_free")]
        static extern uint J2cEncodedFree(IntPtr dataref);

        static J2cEncoder()
        {
            /* preload necessary windows dll */
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (Environment.Is64BitProcess)
                {
                    LoadLibrary(Path.GetFullPath("platform-libs/windows/64/openjp2.dll"));
                }
                else
                {
                    LoadLibrary(Path.GetFullPath("platform-libs/windows/32/openjp2.dll"));
                }
            }
        }

        public static byte[] Encode(Bitmap img, bool lossless)
        {
            int imgChannelWidth = img.Width * img.Height;
            int channels = 3;
            byte[] datastream;
            if(img.PixelFormat == PixelFormat.Format32bppArgb)
            {
                channels = 4;
                datastream = new byte[imgChannelWidth * channels];
                for(int y = 0; y < img.Height; ++y)
                {
                    for(int x = 0; x < img.Width; ++x)
                    {
                        Color c = img.GetPixel(x, y);
                        int pixpos = y * img.Height + x;
                        datastream[pixpos] = c.R;
                        datastream[pixpos + imgChannelWidth] = c.G;
                        datastream[pixpos + imgChannelWidth * 2] = c.B;
                        datastream[pixpos + imgChannelWidth * 3] = c.A;
                    }
                }
            }
            else if(img.PixelFormat == PixelFormat.Format24bppRgb)
            {
                channels = 3;
                datastream = new byte[imgChannelWidth * channels];
                for (int y = 0; y < img.Height; ++y)
                {
                    for (int x = 0; x < img.Width; ++x)
                    {
                        Color c = img.GetPixel(x, y);
                        int pixpos = y * img.Height + x;
                        datastream[pixpos] = c.R;
                        datastream[pixpos + imgChannelWidth] = c.G;
                        datastream[pixpos + imgChannelWidth * 2] = c.B;
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
                J2cEncodedRead(nativePtr, j2cstream, j2cstream.Length);
            }
            finally
            {
                J2cEncodedFree(nativePtr);
            }

            return j2cstream;
        }
    }
}
