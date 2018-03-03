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
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace SilverSim.Scripting.Common
{
    public abstract class ParserBase
    {
        [Serializable]
        public sealed class StackEmptyException : Exception
        {
            public StackEmptyException()
            {
            }

            public StackEmptyException(string message)
                : base(message)
            {
            }

            public StackEmptyException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            private StackEmptyException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        [Serializable]
        public sealed class EndOfStringException : Exception
        {
            public EndOfStringException()
            {
            }

            public EndOfStringException(string message)
                : base(message)
            {
            }

            public EndOfStringException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            private EndOfStringException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        [Serializable]
        public sealed class EndOfFileException : Exception
        {
            public EndOfFileException()
            {
            }

            public EndOfFileException(string message)
                : base(message)
            {
            }

            public EndOfFileException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            private EndOfFileException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        [Serializable]
        public sealed class PreprocessorLineErrorException : Exception
        {
            public PreprocessorLineErrorException()
            {
            }

            public PreprocessorLineErrorException(string message)
                : base(message)
            {
            }

            public PreprocessorLineErrorException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            private PreprocessorLineErrorException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        [Serializable]
        public sealed class ParenthesisMismatchErrorException : Exception
        {
            public ParenthesisMismatchErrorException()
                : base("')' has no matching '('")
            {
            }

            public ParenthesisMismatchErrorException(string message)
                : base(message)
            {
            }

            public ParenthesisMismatchErrorException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            private ParenthesisMismatchErrorException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        [Serializable]
        public sealed class FileIoErrorException : Exception
        {
            public FileIoErrorException()
            {
            }

            public FileIoErrorException(string message)
                : base(message)
            {
            }

            public FileIoErrorException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            private FileIoErrorException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        [Serializable]
        public sealed class CircularIncludeException : Exception
        {
            public CircularIncludeException(string msg)
                : base(msg)
            {
            }

            public CircularIncludeException()
            {
            }

            public CircularIncludeException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            private CircularIncludeException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        private struct ParserInput
        {
            public string FileName;
            public TextReader Reader;
            public int LineNumberCounter;
            public TextWriter Writer;
        }

        private readonly List<ParserInput> m_ParserInputs = new List<ParserInput>();

        protected ParserBase()
        {
        }

        ~ParserBase()
        {
            foreach (ParserInput pi in m_ParserInputs)
            {
                pi.Reader?.Dispose();
                pi.Writer?.Dispose();
            }
        }

        public abstract void Read(List<TokenInfo> arguments);
        protected int cur_linenumber;
        protected string cur_filename;

        protected int begin_linenumber;
        protected string begin_filename;

        public int CurrentLineNumber; /* set to start of line */

        public bool IsIncluded => m_ParserInputs.Count > 1;

        public void Push(TextReader stream, string filename, int lineNumber = 1, TextWriter sourceWriter = null)
        {
            var pi = new ParserInput
            {
                FileName = filename,
                Reader = stream,
                LineNumberCounter = lineNumber,
                Writer = sourceWriter
            };
            m_ParserInputs.Add(pi);
            cur_linenumber = pi.LineNumberCounter;
            cur_filename = pi.FileName;
        }

        public void Push(TextReader stream, string filename, TextWriter sourceWriter)
        {
            Push(stream, filename, 1, sourceWriter);
        }

        public void Pop()
        {
            if (m_ParserInputs.Count > 0)
            {
                ParserInput pi = m_ParserInputs[m_ParserInputs.Count - 1];
                pi.Reader?.Dispose();
                pi.Writer?.Dispose();
                m_ParserInputs.RemoveAt(m_ParserInputs.Count - 1);
            }

            if(m_ParserInputs.Count != 0)
            {
                var pi = m_ParserInputs[m_ParserInputs.Count - 1];
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

        protected void MarkBeginOfLine()
        {
            begin_linenumber = cur_linenumber;
            begin_filename = cur_filename;
        }

        public void GetFileInfo(out string filename, out int linenumber)
        {
            linenumber = cur_linenumber;
            filename = cur_filename;
        }

        public void GetBeginFileInfo(out string filename, out int linenumber)
        {
            linenumber = begin_linenumber;
            filename = begin_filename;
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
            var pi = m_ParserInputs[m_ParserInputs.Count - 1];
            if(pi.Reader == null)
            {
                throw new FileIoErrorException();
            }

            for(;;)
            {
                CurrentLineNumber = cur_linenumber;
                int c = pi.Reader.Read();
                if(c == -1)
                {
                    Pop();
                    if (0 == m_ParserInputs.Count)
                    {
                        throw new EndOfFileException();
                    }
                    pi = m_ParserInputs[m_ParserInputs.Count - 1];
                    continue;
                }
                else
                {
                    pi.Writer?.Write((char)c);
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
