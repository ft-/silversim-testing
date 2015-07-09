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
using System.Linq;
using System.Text;
using System.Xml;
using SilverSim.Types;

namespace SilverSim.Types.StructuredData.XMLRPC
{
    public static class XMLRPC
    {
        public class InvalidXmlRpcSerialization : Exception { }

        public class XmlRpcFaultException : Exception
        {
            public int FaultCode;
            public XmlRpcFaultException(int faultCode, string faultString)
                : base(faultString)
            {
                FaultCode = faultCode;
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
                    throw new InvalidXmlRpcSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name == "name")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerialization();
                            }
                            fieldname = reader.ReadElementValueAsString();
                        }
                        else if (reader.Name == "value")
                        {
                            if(fieldname == "")
                            {
                                throw new InvalidXmlRpcSerialization();
                            }
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerialization();
                            }
                            map.Add(fieldname, DeserializeValue(reader));
                        }
                        else
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "member")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return;
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
                    throw new InvalidXmlRpcSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "member")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerialization();
                            }
                            DeserializeStructMember(reader, iv);
                        }
                        else
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "struct")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return iv;
                }
            }

        }

        static void DeserializeArrayData(XmlTextReader reader, AnArray ar)
        {
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name == "value")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerialization();
                            }
                            ar.Add(DeserializeValue(reader));
                        }
                        else
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "data")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return;
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
                    throw new InvalidXmlRpcSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "data")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerialization();
                            }
                            DeserializeArrayData(reader, iv);
                        }
                        else
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "array")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return iv;
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
                    throw new InvalidXmlRpcSerialization();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch(reader.Name)
                        {
                            case "i4":
                            case "int":
                                if(reader.IsEmptyElement)
                                {
                                    throw new InvalidXmlRpcSerialization();
                                }
                                iv = new Integer(reader.ReadElementValueAsInt());
                                break;

                            case "boolean":
                                if(reader.IsEmptyElement)
                                {
                                    throw new InvalidXmlRpcSerialization();
                                }
                                iv = new ABoolean(reader.ReadElementValueAsInt() != 0);
                                break;

                            case "string":
                                if(reader.IsEmptyElement)
                                {
                                    throw new InvalidXmlRpcSerialization();
                                }
                                iv = new AString(reader.ReadElementValueAsString());
                                break;

                            case "double":
                                if(reader.IsEmptyElement)
                                {
                                    throw new InvalidXmlRpcSerialization();
                                }
                                iv = new Real(reader.ReadElementValueAsDouble());
                                break;

                            case "dateTime.iso8601":
                                throw new InvalidXmlRpcSerialization();
                                //break;

                            case "base64":
                                if (reader.IsEmptyElement)
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
                                if(!reader.IsEmptyElement)
                                {
                                    reader.Skip();
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "value")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        if(iv == null)
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return iv;
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
                    throw new InvalidXmlRpcSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "param")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerialization();
                            }
                            array.Add(DeserializeResponseParam(reader));
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "params")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return array;
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
                    throw new InvalidXmlRpcSerialization();
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
                                throw new InvalidXmlRpcSerialization();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "methodCall")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return req;
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
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidXmlRpcSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name != "methodCall")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        if (reader.IsEmptyElement)
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return DeserializeRequestInner(reader);
                }
            }
        }

        #endregion

        #region Deserialization (Response)
        public static XmlRpcResponse DeserializeResponse(Stream o)
        {
            using(XmlTextReader reader = new XmlTextReader(o))
            {
                return DeserializeResponse(reader);
            }
        }

        public static XmlRpcResponse DeserializeResponse(XmlTextReader reader)
        {
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidXmlRpcSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name != "methodResponse")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        if(reader.IsEmptyElement)
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return DeserializeResponseInner(reader);
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
                    throw new InvalidXmlRpcSerialization();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name == "value")
                        {
                            if(reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerialization();
                            }
                            iv = DeserializeValue(reader);
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "param")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        if (iv == null)
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return iv;
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
                    throw new InvalidXmlRpcSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "value")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerialization();
                            }
                            iv = DeserializeValue(reader);
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "fault")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        if (iv == null)
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return iv;
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
                    throw new InvalidXmlRpcSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "param")
                        {
                            if (reader.IsEmptyElement)
                            {
                                throw new InvalidXmlRpcSerialization();
                            }
                            iv = DeserializeResponseParam(reader);
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "params")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        if(iv == null)
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return iv;
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
                    throw new InvalidXmlRpcSerialization();
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
                                    throw new InvalidXmlRpcSerialization();
                                }
                                else
                                {
                                    IValue fault = DeserializeFault(reader);
                                    if(fault is Map)
                                    {
                                        Map f = (Map)fault;
                                        if(f.ContainsKey("faultCode") && f.ContainsKey("faultString"))
                                        {
                                            throw new XmlRpcFaultException(f["faultCode"].AsInt, f["faultString"].ToString());
                                        }
                                        else
                                        {
                                            throw new InvalidXmlRpcSerialization();
                                        }
                                    }
                                    else
                                    {
                                        throw new InvalidXmlRpcSerialization();
                                    }
                                }

                            default:
                                throw new InvalidXmlRpcSerialization();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "methodResponse")
                        {
                            throw new InvalidXmlRpcSerialization();
                        }
                        return res;
                }
            }
        }

        #endregion

        #region Serialization
        static void Serialize(IValue iv, XmlTextWriter w)
        {
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
                throw new InvalidXmlRpcSerialization();
            }
            else if(iv is AString)
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("string", iv.ToString());
                w.WriteEndElement();
            }
            else if(iv is Map)
            {
                w.WriteStartElement("value");
                {
                    w.WriteStartElement("struct");
                    foreach (KeyValuePair<string, IValue> kvp in (Map)iv)
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
            else if(iv is AnArray)
            {
                w.WriteStartElement("value");
                {
                    w.WriteStartElement("array");
                    w.WriteStartElement("data");
                    foreach (IValue elem in (AnArray)iv)
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
                w.WriteNamedValue("double", (Real)iv);
                w.WriteEndElement();
            }
            else if(iv is ABoolean)
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("boolean", ((ABoolean)iv).AsInt);
                w.WriteEndElement();
            }
            else if(iv is BinaryData)
            {
                w.WriteStartElement("value");
                w.WriteNamedValue("base64", (BinaryData)iv);
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
                        XMLRPC.Serialize(iv, writer);
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
                    XMLRPC.Serialize(ReturnValue, writer);
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
            public int FaultCode = 0;
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

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
