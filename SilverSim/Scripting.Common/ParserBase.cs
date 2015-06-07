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
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Scripting.Common
{
    public abstract class ParserBase
    {
        public class StackEmptyException : Exception
        {
            public StackEmptyException()
            {

            }
        }

        public class EndOfStringException : Exception
        {
            public EndOfStringException()
            {

            }
        }

        public class EndOfFileException : Exception
        {
            public EndOfFileException()
            {

            }
        }

        public class PreprocessorLineError : Exception
        {
            public PreprocessorLineError()
                : base()
            {

            }
        }

        public class ParenthesisMismatchError : Exception
        {
            public ParenthesisMismatchError()
                : base("')' has no matching '('")
            {

            }
        }

        public class FileIoError : Exception
        {
            public FileIoError()
            {

            }
        }

        public class CircularIncludeException : Exception
        {
            public CircularIncludeException(string msg)
                : base(msg)
            {

            }
        }

        private struct ParserInput
        {
            public string FileName;
            public TextReader Reader;
            public int LineNumberCounter;
        };
        private List<ParserInput> m_ParserInputs = new List<ParserInput>();

        public ParserBase()
        {

        }

        public abstract void read(List<string> arguments);
        protected int cur_linenumber;
        protected string cur_filename;

        public int CurrentLineNumber; /* set to start of line */

        public void push(TextReader stream, string filename, int lineNumber = 1)
        {
            ParserInput pi = new ParserInput();
            pi.FileName = filename;
            pi.Reader = stream;
            pi.LineNumberCounter = lineNumber;
            m_ParserInputs.Add(pi);
            cur_linenumber = pi.LineNumberCounter;
            cur_filename = pi.FileName;
        }

        public void pop()
        {
            m_ParserInputs.RemoveAt(m_ParserInputs.Count - 1);

            if(m_ParserInputs.Count != 0)
            {
                ParserInput pi = m_ParserInputs[m_ParserInputs.Count - 1];
                cur_linenumber = pi.LineNumberCounter;
                cur_filename = pi.FileName;
            }
        }

        public void begin()
        {
            if(m_ParserInputs.Count == 0)
            {
                throw new StackEmptyException();
            }
        }

        public void getfileinfo(out string filename, out int linenumber)
        {
            if(m_ParserInputs.Count == 0)
            {
                throw new StackEmptyException();
            }

            linenumber = cur_linenumber;
            filename = cur_filename;
        }

        public struct FileInfo
        {
            public string FileName;
            public int LineNumber;

            public FileInfo(string filename, int linenumber)
            {
                FileName = filename;
                LineNumber = linenumber;
            }
        }

        public FileInfo getfileinfo()
        {
            if (m_ParserInputs.Count == 0)
            {
                throw new StackEmptyException();
            }

            return new FileInfo(cur_filename, cur_linenumber);
        }

        public char readc()
        {
            if(m_ParserInputs.Count == 0)
            {
                throw new StackEmptyException();
            }
            ParserInput pi = m_ParserInputs[m_ParserInputs.Count - 1];
            if(pi.Reader == null)
            {
                throw new FileIoError();
            }

            for(;;)
            {
                int c;
                c = pi.Reader.Read();
                if(c == -1)
                {
                    pop();
                    if(0 == m_ParserInputs.Count)
                    {
                        throw new EndOfFileException();
                    }
                    pi = m_ParserInputs[m_ParserInputs.Count - 1];
                    continue;
                }
                else
                {
                    if(c == '\n')
                    {
                        ++cur_linenumber;
                    }
                    return (char)c;
                }
            }
        }
    }
}
