// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
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
        static void DeserializeStructMember(XmlTextReader reader, Map map)
        {
            string fieldname = string.Empty;
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                bool isEmptyElement = reader.IsEmptyElement;
                string nodeName = reader.Name;

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
                            if(string.IsNullOrEmpty(fieldname))
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

        static Map DeserializeStruct(XmlTextReader reader)
        {
            Map iv = new Map();
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

        static void DeserializeArrayData(XmlTextReader reader, AnArray ar)
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

        static AnArray DeserializeArray(XmlTextReader reader)
        {
            AnArray iv = new AnArray();
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

        static IValue DeserializeValue(XmlTextReader reader)
        {
            IValue iv = null;
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidXmlRpcSerializationException();
                }

                bool isEmptyElement = reader.IsEmptyElement;
                string nodeName = reader.Name;

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
                                iv = new Integer(reader.ReadElementValueAsInt());
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
                                throw new InvalidXmlRpcSerializationException();
                                //break;

                            case "base64":
                                if (isEmptyElement)
                                {
                                    iv = new BinaryData();
                                }
                                else
                                {
                                    iv = new BinaryData(Convert.FromBase64String(reader.ReadElementValueAsString()));
                                }
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
        static AnArray DeserializeRequestParams(XmlTextReader reader)
        {
            AnArray array = new AnArray();
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

        static XmlRpcRequest DeserializeRequestInner(XmlTextReader reader)
        {
            XmlRpcRequest req = new XmlRpcRequest();
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
            using (XmlTextReader reader = new XmlTextReader(o))
            {
                return DeserializeRequest(reader);
            }
        }

        public static XmlRpcRequest DeserializeRequest(XmlTextReader reader)
        {
            if(null == reader)
            {
                throw new ArgumentNullException("reader");
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
            if (null == o)
            {
                throw new ArgumentNullException("o");
            }
            using (XmlTextReader reader = new XmlTextReader(o))
            {
                return DeserializeResponse(reader);
            }
        }

        public static XmlRpcResponse DeserializeResponse(XmlTextReader reader)
        {
            if(null == reader)
            {
                throw new ArgumentNullException("reader");
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

        static IValue DeserializeResponseParam(XmlTextReader reader)
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

        static IValue DeserializeFault(XmlTextReader reader)
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

        static IValue DeserializeResponseParams(XmlTextReader reader)
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

        static XmlRpcResponse DeserializeResponseInner(XmlTextReader reader)
        {
            XmlRpcResponse res = new XmlRpcResponse();
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
                                    Map f = DeserializeFault(reader) as Map;
                                    if(null != f)
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
        static void Serialize(IValue iv, XmlTextWriter w)
        {
            Map iv_m;
            AnArray iv_a;
            BinaryData iv_bin;

            if(iv is UUID)
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("string", iv.ToString());
                w.WriteEndElement();
            }
            else if(iv is Date)
            {
                /*
                w.WriteStartElement("value");
                w.WriteNamedValue("dateTime.iso8601", iv.);
                w.WriteEndElement();
                 * */
                throw new InvalidXmlRpcSerializationException();
            }
            else if(iv is AString)
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("string", iv.ToString());
                w.WriteEndElement();
            }
            else if(null != (iv_m = iv as Map))
            {
                w.WriteStartElement("value");
                {
                    w.WriteStartElement("struct");
                    foreach (KeyValuePair<string, IValue> kvp in iv_m)
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
            else if(null != (iv_a = iv as AnArray))
            {
                w.WriteStartElement("value");
                {
                    w.WriteStartElement("array");
                    w.WriteStartElement("data");
                    foreach (IValue elem in iv_a)
                    {
                        Serialize(elem, w);
                    }
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                w.WriteEndElement();
            }
            else if(iv is Integer)
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("int", iv.AsInt);
                w.WriteEndElement();
            }
            else if(iv is Real)
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("double", iv.AsReal);
                w.WriteEndElement();
            }
            else if(iv is ABoolean)
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("boolean", iv.AsInt);
                w.WriteEndElement();
            }
            else if(null != (iv_bin = iv as BinaryData))
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("base64", iv_bin);
                w.WriteEndElement();
            }
            else if(iv is URI)
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

            public XmlRpcRequest()
            {

            }

            public XmlRpcRequest(string method)
            {
                MethodName = method;
            }

            public void Serialize(Stream o)
            {
                using (XmlTextWriter writer = new XmlTextWriter(o, UTF8NoBOM))
                {
                    writer.WriteStartElement("methodCall");
                    writer.WriteNamedValue("methodName", MethodName);
                    writer.WriteStartElement("params");
                    foreach (IValue iv in Params)
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
                using (MemoryStream ms = new MemoryStream())
                {
                    Serialize(ms);
                    return ms.GetBuffer();
                }
            }
        }

        public class XmlRpcResponse
        {
            public IValue ReturnValue;

            public XmlRpcResponse()
            {

            }

            public void Serialize(Stream o)
            {
                using(XmlTextWriter writer = new XmlTextWriter(o, UTF8NoBOM))
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
                using(MemoryStream ms = new MemoryStream())
                {
                    Serialize(ms);
                    return ms.GetBuffer();
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

            public void Serialize(Stream o)
            {
                using(XmlTextWriter writer = new XmlTextWriter(o, UTF8NoBOM))
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
                using (MemoryStream ms = new MemoryStream())
                {
                    Serialize(ms);
                    return ms.GetBuffer();
                }
            }
        }

        static readonly UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
