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
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace OpenJp2.Net
{
    public static class J2cDecoder
    {
        public class J2cDecodingFailedException : Exception
        {
            public J2cDecodingFailedException()
            {
            }

            public J2cDecodingFailedException(string message) : base(message)
            {
            }

            public J2cDecodingFailedException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected J2cDecodingFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }


        [DllImport("openjp2", EntryPoint = "j2c_decoded_get_channels")]
        private static extern int J2cDecodedGetChannels(IntPtr dataref);

        [DllImport("openjp2", EntryPoint = "j2c_decoded_get_width")]
        private static extern int J2cDecodedGetWidth(IntPtr dataref);

        [DllImport("openjp2", EntryPoint = "j2c_decoded_get_height")]
        private static extern int J2cDecodedGetHeight(IntPtr dataref);

        [DllImport("openjp2", EntryPoint = "j2c_decode")]
        private static extern IntPtr J2cDecode(byte[] buffer, int length);

        [DllImport("openjp2", EntryPoint = "j2c_decoded_free")]
        private static extern void J2cDecodedFree(IntPtr dataref);

        [DllImport("openjp2", EntryPoint = "j2c_decoded_get_scan0")]
        private static extern IntPtr J2cDecodedGetScan0(IntPtr dataref, int channel);

        public static Image Decode(byte[] buffer)
        {
            byte[] dummy;
            return DecodeWithDump(buffer, out dummy, false);
        }

        public static Image DecodeWithDump(byte[] buffer, out byte[] bump, bool decodebump = true)
        {
            if (!J2cInit.m_Inited)
            {
                J2cInit.InitOpenJP2();
            }

            bump = null;

            int width;
            int height;
            int channels;
            int channelwidth;
            int[] red;
            int[] green;
            int[] blue;
            int[] alpha = null;

            {
                /* shorten the j2c decode context variable visibility here */
                IntPtr ptr = J2cDecode(buffer, buffer.Length);
                if (ptr == IntPtr.Zero)
                {
                    throw new J2cDecodingFailedException();
                }

                try
                {
                    width = J2cDecodedGetWidth(ptr);
                    height = J2cDecodedGetHeight(ptr);
                    channels = J2cDecodedGetChannels(ptr);

                    if (channels == 0)
                    {
                        throw new J2cDecodingFailedException();
                    }

                    channelwidth = width * height;

                    red = new int[channelwidth];
                    alpha = null;

                    switch (channels)
                    {
                        case 1:
                            Marshal.Copy(J2cDecodedGetScan0(ptr, 0), red, 0, channelwidth);
                            green = red;
                            blue = red;
                            break;

                        case 2:
                            Marshal.Copy(J2cDecodedGetScan0(ptr, 0), red, 0, channelwidth);
                            green = red;
                            blue = red;
                            alpha = new int[channelwidth];
                            Marshal.Copy(J2cDecodedGetScan0(ptr, 1), alpha, 0, channelwidth);
                            break;

                        case 3:
                            green = new int[channelwidth];
                            blue = new int[channelwidth];
                            Marshal.Copy(J2cDecodedGetScan0(ptr, 0), red, 0, channelwidth);
                            Marshal.Copy(J2cDecodedGetScan0(ptr, 1), green, 0, channelwidth);
                            Marshal.Copy(J2cDecodedGetScan0(ptr, 2), blue, 0, channelwidth);
                            break;

                        default:
                            alpha = new int[channelwidth];
                            Marshal.Copy(J2cDecodedGetScan0(ptr, 3), alpha, 0, channelwidth);
                            goto case 3;
                    }

                    if (channels > 4)
                    {
                        bump = new byte[channelwidth];
                        int[] ibump = new int[channelwidth];
                        Marshal.Copy(J2cDecodedGetScan0(ptr, 4), ibump, 0, channelwidth);
                        for(int i = 0; i < channelwidth; ++i)
                        {
                            bump[i] = (byte)ibump[i];
                        }
                    }
                }
                finally
                {
                    J2cDecodedFree(ptr);
                }
            }

            /* transform input channels to actual Image data */
            byte[] imagedata;
            int destpos = 0;
            PixelFormat destformat;

            if (alpha != null)
            {
                destformat = PixelFormat.Format32bppArgb;
                imagedata = new byte[channelwidth * 4];
                for(int i = 0; i < channelwidth; ++i)
                {
                    imagedata[destpos++] = (byte)blue[i];
                    imagedata[destpos++] = (byte)green[i];
                    imagedata[destpos++] = (byte)red[i];
                    imagedata[destpos++] = (byte)alpha[i];
                }
            }
            else
            {
                destformat = PixelFormat.Format24bppRgb;
                imagedata = new byte[channelwidth * 3];
                for (int i = 0; i < channelwidth; ++i)
                {
                    imagedata[destpos++] = (byte)blue[i];
                    imagedata[destpos++] = (byte)green[i];
                    imagedata[destpos++] = (byte)red[i];
                }
            }

            /* no using here, we are returning it */
            Bitmap bmp = new Bitmap(width, height, destformat);

            /* put the decoded image into it */
            BitmapData lockBits = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, destformat);
            Marshal.Copy(imagedata, 0, lockBits.Scan0, imagedata.Length);
            bmp.UnlockBits(lockBits);

            return bmp;
        }
    }
}
