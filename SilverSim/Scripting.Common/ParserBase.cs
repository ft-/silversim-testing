// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Scripting.Common
{
    public abstract class ParserBase
    {
        [Serializable]
        public class StackEmptyException : Exception
        {
            public StackEmptyException()
            {

            }
        }

        [Serializable]
        public class EndOfStringException : Exception
        {
            public EndOfStringException()
            {

            }
        }

        [Serializable]
        public class EndOfFileException : Exception
        {
            public EndOfFileException()
            {

            }
        }

        [Serializable]
        public class PreprocessorLineErrorException : Exception
        {
            public PreprocessorLineErrorException()
                : base()
            {

            }
        }

        [Serializable]
        public class ParenthesisMismatchErrorException : Exception
        {
            public ParenthesisMismatchErrorException()
                : base("')' has no matching '('")
            {

            }
        }

        [Serializable]
        public class FileIoErrorException : Exception
        {
            public FileIoErrorException()
            {

            }
        }

        [Serializable]
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

        public abstract void Read(List<string> arguments);
        protected int cur_linenumber;
        protected string cur_filename;

        public int CurrentLineNumber; /* set to start of line */

        public void Push(TextReader stream, string filename, int lineNumber = 1)
        {
            ParserInput pi = new ParserInput();
            pi.FileName = filename;
            pi.Reader = stream;
            pi.LineNumberCounter = lineNumber;
            m_ParserInputs.Add(pi);
            cur_linenumber = pi.LineNumberCounter;
            cur_filename = pi.FileName;
        }

        public void Pop()
        {
            m_ParserInputs.RemoveAt(m_ParserInputs.Count - 1);

            if(m_ParserInputs.Count != 0)
            {
                ParserInput pi = m_ParserInputs[m_ParserInputs.Count - 1];
                cur_linenumber = pi.LineNumberCounter;
                cur_filename = pi.FileName;
            }
        }

        public void Begin()
        {
            if(m_ParserInputs.Count == 0)
            {
                throw new StackEmptyException();
            }
        }

        public void GetFileInfo(out string filename, out int linenumber)
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

        public FileInfo GetFileInfo()
        {
            if (m_ParserInputs.Count == 0)
            {
                throw new StackEmptyException();
            }

            return new FileInfo(cur_filename, cur_linenumber);
        }

        public char ReadC()
        {
            if(m_ParserInputs.Count == 0)
            {
                throw new StackEmptyException();
            }
            ParserInput pi = m_ParserInputs[m_ParserInputs.Count - 1];
            if(pi.Reader == null)
            {
                throw new FileIoErrorException();
            }

            for(;;)
            {
                int c;
                c = pi.Reader.Read();
                if(c == -1)
                {
                    Pop();
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
