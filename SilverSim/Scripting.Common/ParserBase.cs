// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

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

            public StackEmptyException(string message)
                : base(message)
            {

            }

            public StackEmptyException(string message, Exception innerException)
                : base(message, innerException)
            {

            }

            protected StackEmptyException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            { 
            }
        }

        [Serializable]
        public class EndOfStringException : Exception
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

            protected EndOfStringException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            { 
            }
        }

        [Serializable]
        public class EndOfFileException : Exception
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

            protected EndOfFileException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            { 
            }
        }

        [Serializable]
        public class PreprocessorLineErrorException : Exception
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

            protected PreprocessorLineErrorException(SerializationInfo info, StreamingContext context)
                : base(info, context)
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

            public ParenthesisMismatchErrorException(string message)
                : base(message)
            {

            }

            public ParenthesisMismatchErrorException(string message, Exception innerException)
                : base(message, innerException)
            {

            }

            protected ParenthesisMismatchErrorException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            { 
            }
        }

        [Serializable]
        public class FileIoErrorException : Exception
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

            protected FileIoErrorException(SerializationInfo info, StreamingContext context)
                : base(info, context)
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

            public CircularIncludeException()
            {

            }

            public CircularIncludeException(string message, Exception innerException)
                : base(message, innerException)
            {

            }

            protected CircularIncludeException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            { 
            }
        }

        private struct ParserInput
        {
            public string FileName;
            public TextReader Reader;
            public int LineNumberCounter;
        };
        readonly List<ParserInput> m_ParserInputs = new List<ParserInput>();

        protected ParserBase()
        {
        }

        public abstract void Read(List<string> arguments);
        protected int cur_linenumber;
        protected string cur_filename;

        protected int begin_linenumber;
        protected string begin_filename;

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
                    if (0 == m_ParserInputs.Count)
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
