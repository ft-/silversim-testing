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
using System.Text;

namespace SilverSim.Types
{
    public class FormattedListBuilder
    {
        private readonly Dictionary<string, int> Fields;
        private readonly StringBuilder m_StringBuilder = new StringBuilder();

        public FormattedListBuilder()
        {
            Fields = new Dictionary<string, int>();
        }

        public FormattedListBuilder(IDictionary<string, int> fields)
        {
            Fields = new Dictionary<string, int>(fields);
        }

        public override string ToString() => m_StringBuilder.ToString();

        public FormattedListBuilder AddColumn(string column, int columnSize)
        {
            if(columnSize == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columnSize));
            }
            Fields.Add(column, columnSize);
            return this;
        }

        public void AddLine(string fmt, params object[] p)
        {
            m_StringBuilder.AppendFormat(fmt + "\n", p);
        }

        public void AddLine()
        {
            m_StringBuilder.AppendLine();
        }

        public FormattedListBuilder AddHeader()
        {
            foreach (var kvp in Fields)
            {
                int fieldSize = kvp.Value;
                m_StringBuilder.Append(kvp.Key.TrimToMaxLength(fieldSize - 1).PadRight(fieldSize));
            }
            m_StringBuilder.AppendLine();
            return this;
        }

        public FormattedListBuilder AddSeparator()
        {
            int countChars = 0;
            foreach(int fieldsize in Fields.Values)
            {
                countChars += fieldsize;
            }
            m_StringBuilder.AppendLine("-".PadRight(countChars, '-'));
            return this;
        }

        public void AddData(params object[] para)
        {
            if(para.Length > Fields.Count)
            {
                throw new ArgumentException("para too long");
            }

            int fieldIndex = 0;
            foreach(int fieldSize in Fields.Values)
            {
                var fieldValue = (fieldIndex < para.Length) ? para[fieldIndex++].ToString() : string.Empty;
                fieldValue = fieldValue.TrimToMaxLength(Math.Abs(fieldSize));
                fieldValue = (fieldSize < 0) ? fieldValue.PadLeft(fieldSize) : fieldValue.PadRight(fieldSize);
                m_StringBuilder.Append(fieldValue);
            }
            m_StringBuilder.AppendLine();
        }

        public void AddData(IDictionary<string, object> values)
        {
            foreach (var kvp in Fields)
            {
                var fieldValue = string.Empty;
                int fieldSize = kvp.Value;
                object fieldObj;
                if (values.TryGetValue(kvp.Key, out fieldObj))
                {
                    fieldValue = fieldObj.ToString();
                }

                fieldValue = fieldValue.TrimToMaxLength(Math.Abs(fieldSize));
                fieldValue = (fieldSize < 0) ? fieldValue.PadLeft(fieldSize) : fieldValue.PadRight(fieldSize);
                m_StringBuilder.Append(fieldValue);
            }
            m_StringBuilder.AppendLine();
        }
    }
}
