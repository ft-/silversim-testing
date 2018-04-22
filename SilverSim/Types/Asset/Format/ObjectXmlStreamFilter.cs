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
using System.IO;

namespace SilverSim.Types.Asset.Format
{
    public class ObjectXmlStreamFilter : Stream
    {
        private readonly byte[] m_Buffer = new byte[10240];
        private int m_BufFill;
        private int m_BufUsed;
        private readonly Stream m_BufInput;

        public ObjectXmlStreamFilter(Stream input)
        {
            m_BufInput = input;
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
            int rescount = 0;
            while(count > 0)
            {
                if(m_BufFill == m_BufUsed)
                {
                    m_BufUsed = 0;
                    m_BufFill = m_BufInput.Read(m_Buffer, 0, m_Buffer.Length);
                    if(m_BufFill < 0)
                    {
                        return -1;
                    }
                    if(0 == m_BufFill)
                    {
                        return rescount;
                    }
                }

                if(m_Buffer[m_BufUsed] == (byte)'<')
                {
                    if (m_BufUsed != 0)
                    {
                        Buffer.BlockCopy(m_Buffer, m_BufUsed, m_Buffer, 0, m_BufFill - m_BufUsed);
                    }
                    m_BufFill -= m_BufUsed;
                    m_BufUsed = 0;
                    int addbytes = m_BufInput.Read(m_Buffer, m_BufFill, m_Buffer.Length - m_BufFill);
                    if(addbytes < 0)
                    {
                        return -1;
                    }
                    m_BufFill += addbytes;

                    int tagend;
                    for (tagend = 0; tagend < m_BufFill; ++tagend)
                    {
                        if(m_Buffer[tagend] == '>')
                        {
                            break;
                        }
                    }
                    var test = m_Buffer.FromUTF8Bytes(0, tagend + 1);
                    if(test.StartsWith("<SceneObjectPart"))
                    {
                        if(test.Contains("xmlns:xmlns:"))
                        {
                            /* stupid mono XmlTextReader and XmlTextWriter when using as specified it adds more and more xmlns after each iteration 
                             * when used as filter parser and writer. 
                             * 
                             * And this was an OpenSim misuse since it mistook LocalName and Name.
                             * Pushing Name into LocalName is just a misuse of the API.
                             */
                            int pos;
                            while((pos = test.IndexOf("xmlns:xmlns:")) >= 0)
                            {
                                int cpos = pos;
                                for(; cpos < test.Length && test[cpos] != '='; ++cpos)
                                {
                                    /* nothing to do besides cpos counting */
                                }
                                if(cpos == test.Length)
                                {
                                    /* no equal sign is weird */
                                    break;
                                }
                                var repl = test.Substring(pos, cpos - pos);
                                var replnew = repl.Substring(repl.LastIndexOf("xmlns:"));
                                test = test.Replace(repl, replnew);
                            }
                            var newbuf = new byte[m_BufFill - tagend - 1];
                            Buffer.BlockCopy(m_Buffer, tagend + 1, newbuf, 0, m_BufFill - tagend - 1);
                            var newstr = test.ToUTF8Bytes();
                            Buffer.BlockCopy(newstr, 0, m_Buffer, 0, newstr.Length);
                            Buffer.BlockCopy(newbuf, 0, m_Buffer, newstr.Length, newbuf.Length);
                            m_BufFill = newstr.Length + newbuf.Length;
                        }
                    }
                    else if(test.StartsWith("<?xml"))
                    {
                        /* OpenSim guys messed up xml declarations, so we have to ignore it */
                        /* filter every other tag opensim does not use anything else than UTF-8
                         * but falsely declared some as UTF-16 
                         */
                        test = "<?xml version=\"1.0\"?>";
                        var newbuf = new byte[m_BufFill - tagend - 1];
                        Buffer.BlockCopy(m_Buffer, tagend + 1, newbuf, 0, m_BufFill - tagend - 1);
                        var newstr = test.ToUTF8Bytes();
                        Buffer.BlockCopy(newstr, 0, m_Buffer, 0, newstr.Length);
                        Buffer.BlockCopy(newbuf, 0, m_Buffer, newstr.Length, newbuf.Length);
                        m_BufFill = newstr.Length + newbuf.Length;
                    }
                }

                buffer[offset++] = m_Buffer[m_BufUsed++];
                --count;
                ++rescount;
            }
            return rescount;
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

        protected override void Dispose(bool disposing)
        {
            m_BufInput.Dispose();
        }
    }
}
