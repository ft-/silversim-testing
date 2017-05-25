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
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

namespace SilverSim.Types.StructuredData.XmlRpc
{
    public static class XmlRpc
    {
        [Serializable]
        public class InvalidXmlRpcSerializationException : Exception
        {
            public InvalidXmlRpcSerializationException()
            {
            }

            public InvalidXmlRpcSerializationException(string message)
                : base(message)
            {
            }

            protected InvalidXmlRpcSerializationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public InvalidXmlRpcSerializationException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        [Serializable]
        public class XmlRpcFaultException : Exception
        {
            public int FaultCode;

            public XmlRpcFaultException()
            {
            }

            public XmlRpcFaultException(int faultCode, string faultString)
                : base(faultString)
            {
                FaultCode = faultCode;
            }

            public XmlRpcFaultException(string message)
                : base(message)
            {
            }

            protected XmlRpcFaultException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
                FaultCode = info.GetInt32("FaultCode");
            }

            public XmlRpcFaultException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        #region Deserialization Common
        private static void DeserializeStructMember(XmlTextReader reader, Map map)
        {
            var fieldname = string.Empty;
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                var isEmptyElement = reader.IsEmptyElement;
                var nodeName = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (nodeName == "name")
                        {
                            if (isEmptyElement)
                            {
                                throw new InvalidXmlRpcSerializationException();
                            }
                            fieldname = reader.ReadElementValueAsString();
                        }
                        else if (reader.Name == "value")
                        {
                            if(fieldname?.Length == 0)
                            {
                                throw new InvalidXmlRpcSerializationException();
                            }
                            if (isEmptyElement)
                            {
                                throw new InvalidXmlRpcSerializationException();
                            }
                            map.Add(fieldname, DeserializeValue(reader));
                        }
                        else
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != "member")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        private static Map DeserializeStruct(XmlTextReader reader)
        {
            var iv = new Map();
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "member")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerializationException();
                            }
                            DeserializeStructMember(reader, iv);
                        }
                        else
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "struct")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return iv;

                    default:
                        break;
                }
            }
        }

        private static void DeserializeArrayData(XmlTextReader reader, AnArray ar)
        {
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name == "value")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerializationException();
                            }
                            ar.Add(DeserializeValue(reader));
                        }
                        else
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "data")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        private static AnArray DeserializeArray(XmlTextReader reader)
        {
            var iv = new AnArray();
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "data")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerializationException();
                            }
                            DeserializeArrayData(reader, iv);
                        }
                        else
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "array")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return iv;

                    default:
                        break;
                }
            }
        }

        private static readonly string[] Iso8601DateFormats = new string[]
        {
            "yyyyMMdd'T'HHmmss",
            "yyyyMMdd'T'HHmmss'Z'",
            "yyyyMMdd'T'HHmmsszzz",
            "yyyyMMdd'T'HHmmsszz"
        };

        private static readonly Regex Iso8601DateRegex = new Regex(
            @"(((?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}))|((?<year>\d{4})(?<month>\d{2})(?<day>\d{2})))" +
            "T" +
            @"(((?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2}))|((?<hour>\d{2})(?<minute>\d{2})(?<second>\d{2})))" +
            @"(?<tz>$|Z|([+-]\d{2}:?(\d{2})?))");

        private static IValue DeserializeValue(XmlTextReader reader)
        {
            IValue iv = null;
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                var isEmptyElement = reader.IsEmptyElement;
                var nodeName = reader.Name;

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "i4":
                            case "int":
                                if (isEmptyElement)
                                {
                                    throw new InvalidXmlRpcSerializationException();
                                }
                                iv = new SilverSim.Types.Integer(reader.ReadElementValueAsInt());
                                break;

                            case "boolean":
                                if (isEmptyElement)
                                {
                                    throw new InvalidXmlRpcSerializationException();
                                }
                                iv = new ABoolean(reader.ReadElementValueAsInt() != 0);
                                break;

                            case "string":
                                iv = new AString(reader.ReadElementValueAsString());
                                break;

                            case "double":
                                if (isEmptyElement)
                                {
                                    throw new InvalidXmlRpcSerializationException();
                                }
                                iv = new Real(reader.ReadElementValueAsDouble());
                                break;

                            case "dateTime.iso8601":
                                if (isEmptyElement)
                                {
                                    throw new InvalidXmlRpcSerializationException();
                                }
                                else
                                {
                                    var m = Iso8601DateRegex.Match(reader.ReadElementValueAsString());
                                    var parseInput = m.Groups["year"].Value + m.Groups["month"].Value + m.Groups["day"].Value +
                                            "T" + m.Groups["hour"].Value + m.Groups["minute"].Value + m.Groups["second"].Value + m.Groups["tz"].Value;
                                    DateTime dt;
                                    if(!DateTime.TryParseExact(parseInput, Iso8601DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dt))
                                    {
                                        throw new InvalidXmlRpcSerializationException();
                                    }
                                    iv = new Date(dt);
                                }
                                break;

                            case "base64":
                                iv = (isEmptyElement) ?
                                    new BinaryData() :
                                    new BinaryData(Convert.FromBase64String(reader.ReadElementValueAsString()));
                                break;

                            case "struct":
                                iv = DeserializeStruct(reader);
                                break;

                            case "array":
                                iv = DeserializeArray(reader);
                                break;

                            default:
                                if (!isEmptyElement)
                                {
                                    reader.Skip();
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(nodeName != "value")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        if(iv == null)
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return iv;

                    default:
                        break;
                }
            }
        }

        #endregion

        #region Deserialization (Request)
        private static AnArray DeserializeRequestParams(XmlTextReader reader)
        {
            var array = new AnArray();
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "param")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerializationException();
                            }
                            array.Add(DeserializeResponseParam(reader));
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "params")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return array;

                    default:
                        break;
                }
            }
        }

        private static XmlRpcRequest DeserializeRequestInner(XmlTextReader reader)
        {
            var req = new XmlRpcRequest();
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "methodName":
                                req.MethodName = reader.ReadElementValueAsString();
                                break;

                            case "params":
                                if (!reader.IsEmptyElement)
                                {
                                    req.Params = DeserializeRequestParams(reader);
                                }
                                break;

                            default:
                                throw new InvalidXmlRpcSerializationException();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "methodCall")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return req;

                    default:
                        break;
                }
            }
        }

        public static XmlRpcRequest DeserializeRequest(Stream o)
        {
            using (var reader = new XmlTextReader(o))
            {
                return DeserializeRequest(reader);
            }
        }

        public static XmlRpcRequest DeserializeRequest(XmlTextReader reader)
        {
            if(reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name != "methodCall")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        if (reader.IsEmptyElement)
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return DeserializeRequestInner(reader);

                    default:
                        break;
                }
            }
        }

        #endregion

        #region Deserialization (Response)
        public static XmlRpcResponse DeserializeResponse(Stream o)
        {
            if (o == null)
            {
                throw new ArgumentNullException(nameof(o));
            }
            using (var reader = new XmlTextReader(o))
            {
                return DeserializeResponse(reader);
            }
        }

        public static XmlRpcResponse DeserializeResponse(XmlTextReader reader)
        {
            if(reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name != "methodResponse")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        if(reader.IsEmptyElement)
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return DeserializeResponseInner(reader);

                    default:
                        break;
                }
            }
        }

        private static IValue DeserializeResponseParam(XmlTextReader reader)
        {
            IValue iv = null;
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name == "value")
                        {
                            if(reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerializationException();
                            }
                            iv = DeserializeValue(reader);
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "param")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        if (iv == null)
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return iv;

                    default:
                        break;
                }
            }
        }

        private static IValue DeserializeFault(XmlTextReader reader)
        {
            IValue iv = null;
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "value")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerializationException();
                            }
                            iv = DeserializeValue(reader);
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "fault")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        if (iv == null)
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return iv;

                    default:
                        break;
                }
            }
        }

        private static IValue DeserializeResponseParams(XmlTextReader reader)
        {
            IValue iv = null;
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "param")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerializationException();
                            }
                            iv = DeserializeResponseParam(reader);
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "params")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        if(iv == null)
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return iv;

                    default:
                        break;
                }
            }
        }

        private static XmlRpcResponse DeserializeResponseInner(XmlTextReader reader)
        {
            var res = new XmlRpcResponse();
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch(reader.Name)
                        {
                            case "params":
                                if(!reader.IsEmptyElement)
                                {
                                    res.ReturnValue = DeserializeResponseParams(reader);
                                }
                                break;

                            case "fault":
                                if (reader.IsEmptyElement)
                                {
                                    throw new InvalidXmlRpcSerializationException();
                                }
                                else
                                {
                                    var f = DeserializeFault(reader) as Map;
                                    if(f != null)
                                    {
                                        if(f.ContainsKey("faultCode") && f.ContainsKey("faultString"))
                                        {
                                            throw new XmlRpcFaultException(f["faultCode"].AsInt, f["faultString"].ToString());
                                        }
                                        else
                                        {
                                            throw new InvalidXmlRpcSerializationException();
                                        }
                                    }
                                    else
                                    {
                                        throw new InvalidXmlRpcSerializationException();
                                    }
                                }

                            default:
                                throw new InvalidXmlRpcSerializationException();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "methodResponse")
                        {
                            throw new InvalidXmlRpcSerializationException();
                        }
                        return res;

                    default:
                        break;
                }
            }
        }

        #endregion

        #region Serialization
        private static void Serialize(IValue iv, XmlTextWriter w)
        {
            var t = iv.GetType();

            if(t == typeof(UUID) || t == typeof(AString))
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("string", iv.ToString());
                w.WriteEndElement();
            }
            else if(t == typeof(Date))
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("dateTime.iso8601", ((Date)iv).Iso8601);
                w.WriteEndElement();
            }
            else if(t == typeof(Map))
            {
                var iv_m = (Map)iv;
                w.WriteStartElement("value");
                {
                    w.WriteStartElement("struct");
                    foreach (var kvp in iv_m)
                    {
                        w.WriteStartElement("member");
                        w.WriteNamedValue("name", kvp.Key);
                        Serialize(kvp.Value, w);
                        w.WriteEndElement();
                    }
                    w.WriteEndElement();
                }
                w.WriteEndElement();
            }
            else if(t == typeof(AnArray))
            {
                var iv_a = (AnArray)iv;
                w.WriteStartElement("value");
                {
                    w.WriteStartElement("array");
                    w.WriteStartElement("data");
                    foreach (var elem in iv_a)
                    {
                        Serialize(elem, w);
                    }
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                w.WriteEndElement();
            }
            else if (t == typeof(Integer))
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("int", iv.AsInt);
                w.WriteEndElement();
            }
            else if(t == typeof(Real))
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("double", iv.AsReal);
                w.WriteEndElement();
            }
            else if(t == typeof(ABoolean))
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("boolean", iv.AsInt);
                w.WriteEndElement();
            }
            else if(t == typeof(BinaryData))
            {
                var iv_bin = (BinaryData)iv;
                w.WriteStartElement("value");
                w.WriteNamedValue("base64", iv_bin);
                w.WriteEndElement();
            }
            else if(t == typeof(URI))
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("string", iv.ToString());
                w.WriteEndElement();
            }
            else
            {
                throw new ArgumentException(iv.GetType().FullName);
            }
        }
        #endregion

        public class XmlRpcRequest
        {
            public string MethodName;
            public AnArray Params = new AnArray();

            /* Informative properties */
            public string CallerIP { get; set; }
            public bool IsSsl { get; set; }

            public XmlRpcRequest()
            {
            }

            public XmlRpcRequest(string method)
            {
                MethodName = method;
            }

            public void Serialize(Stream o)
            {
                using (var writer = o.UTF8XmlTextWriter())
                {
                    writer.WriteStartElement("methodCall");
                    writer.WriteNamedValue("methodName", MethodName);
                    writer.WriteStartElement("params");
                    foreach (var iv in Params)
                    {
                        writer.WriteStartElement("param");
                        XmlRpc.Serialize(iv, writer);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
            }

            public byte[] Serialize()
            {
                using (var ms = new MemoryStream())
                {
                    Serialize(ms);
                    return ms.ToArray();
                }
            }
        }

        public class XmlRpcResponse
        {
            public IValue ReturnValue;

            public void Serialize(Stream o)
            {
                using(XmlTextWriter writer = o.UTF8XmlTextWriter())
                {
                    writer.WriteStartElement("methodResponse");
                    writer.WriteStartElement("params");
                    writer.WriteStartElement("param");
                    XmlRpc.Serialize(ReturnValue, writer);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
            }

            public byte[] Serialize()
            {
                using(var ms = new MemoryStream())
                {
                    Serialize(ms);
                    return ms.ToArray();
                }
            }
        }

        public class XmlRpcFaultResponse
        {
            public int FaultCode;
            public string FaultString = string.Empty;

            public XmlRpcFaultResponse()
            {
            }

            public XmlRpcFaultResponse(int faultCode, string faultString)
            {
                FaultCode = faultCode;
                FaultString = faultString;
            }

            public void Serialize(Stream o)
            {
                using(var writer = o.UTF8XmlTextWriter())
                {
                    writer.WriteStartElement("methodResponse");
                    {
                        writer.WriteStartElement("fault");
                        {
                            writer.WriteStartElement("struct");
                            {
                                writer.WriteStartElement("member");
                                {
                                    writer.WriteNamedValue("name", "faultCode");
                                    writer.WriteStartElement("value");
                                    writer.WriteNamedValue("int", FaultCode);
                                    writer.WriteEndElement();
                                }
                                writer.WriteEndElement();
                                writer.WriteStartElement("member");
                                {
                                    writer.WriteNamedValue("name", "faultString");
                                    writer.WriteStartElement("value");
                                    writer.WriteNamedValue("string", FaultString);
                                    writer.WriteEndElement();
                                }
                                writer.WriteEndElement();
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
            }

            public byte[] Serialize()
            {
                using (var ms = new MemoryStream())
                {
                    Serialize(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
