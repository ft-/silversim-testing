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

using System.Collections.Generic;
using System.Text;

namespace SilverSim.Scripting.Common
{
    public sealed class TokenInfo
    {
        public string Token;
        public readonly int LineNumber;

        public TokenInfo(char token, int linenumber)
        {
            Token = token.ToString();
            LineNumber = linenumber;
        }

        public TokenInfo(string token, int linenumber)
        {
            Token = token;
            LineNumber = linenumber;
        }

        public TokenInfo Substring(int start) => new TokenInfo(
            Token.Substring(start),
            LineNumber);

        public TokenInfo Substring(int start, int length) => new TokenInfo(
            Token.Substring(start, length),
            LineNumber);

        public override string ToString() => Token;

        public static implicit operator string(TokenInfo ti) => ti.Token;

        public char this[int key]
        {
            get
            {
                return Token[key];
            }
        }

        public int Length => Token.Length;

        public bool StartsWith(string pattern) => Token.StartsWith(pattern);

        public bool EndsWith(string pattern) => Token.EndsWith(pattern);

        public string Remove(int start, int length) => Token.Remove(start, length);
    }

    public sealed class TokenInfoBuilder
    {
        private StringBuilder m_Token = new StringBuilder();
        public int LineNumber;

        public string Token => m_Token.ToString();

        public int Length => m_Token.Length;

        public void Clear() => m_Token.Clear();

        public void Append(char c, int linenumber)
        {
            if(m_Token.Length == 0)
            {
                LineNumber = linenumber;
            }
            m_Token.Append(c);
        }

        public void Append(string s, int linenumber)
        {
            if (m_Token.Length == 0)
            {
                LineNumber = linenumber;
            }
            m_Token.Append(s);
        }

        public void Remove(int start, int len) => m_Token.Remove(start, len);

        public bool StartsWith(string pattern) => m_Token.ToString().StartsWith(pattern);

        public bool EndsWith(string pattern) => m_Token.ToString().EndsWith(pattern);

        public char this[int key]
        {
            get
            {
                return m_Token[key];
            }
            set
            {
                m_Token[key] = value;
            }
        }

        public static implicit operator TokenInfo(TokenInfoBuilder tib) => new TokenInfo(tib.Token, tib.LineNumber);
    }

    public static class TokenInfoExtensionMethods
    {
        public static int IndexOf(this List<TokenInfo> til, string tok)
        {
            for(int i = 0; i < til.Count; ++i)
            {
                if(til[i].Token == tok)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int IndexOf(this List<TokenInfo> til, string tok, int index)
        {
            for (int i = index; i < til.Count; ++i)
            {
                if (til[i].Token == tok)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
