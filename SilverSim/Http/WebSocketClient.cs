// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Http.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace SilverSim.Http
{
    [Serializable]
    public class NotAWebSocketConnectionException : Exception
    {
        public NotAWebSocketConnectionException()
        {
        }
    }

    public class WebSocketClient : HttpWebSocket
    {
        public string SelectedProtocol { get; private set; }

        public WebSocketClient(Stream o)
            : base(o)
        {

        }

        public static WebSocketClient Connect(string url, string[] protocols = null)
        {
            string selectedProtocol;
            Stream s = ConnectStream(url, protocols, out selectedProtocol);
            return new WebSocketClient(s) { SelectedProtocol = selectedProtocol };
        }

        static Stream ConnectStream(string url, string[] protocols, out string selectedProtocol)
        {
            Stream stream;
            Uri uri = new Uri(url, UriKind.Absolute);
            string host = uri.Host;
            int port = uri.Port;

            switch(uri.Scheme)
            {
                case "ws":
                case "http":
                    stream = OpenPlainStream(host, port);
                    break;

                case "wss":
                case "https":
                    stream = OpenSslStream(host, port);
                    break;

                default:
                    throw new ArgumentException("Unsupported scheme");
            }

            Guid m_Guid = Guid.NewGuid();
            string webSocketKey = Convert.ToBase64String(m_Guid.ToByteArray());
            string webSocketRequest = string.Format("GET {0} HTTP/1.1\r\n" +
                                        "Host: {1}\r\n" +
                                        "Upgrade: websocket\r\n" +
                                        "Connection: Upgrade\r\n" +
                                        "Sec-WebSocket-Key: {2}\r\n" +
                                        "Sec-WebSocket-Version: 13\r\n",
                                        uri.PathAndQuery,
                                        host,
                                        webSocketKey);
            if (protocols != null)
            {
                webSocketRequest += "Sec-WebSocket-Protocol: " + string.Join(",", protocols) + "\r\n";
            }
            webSocketRequest += "\r\n";

            string websocketkeyuuid;
            websocketkeyuuid = webSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] websocketacceptdata = Encoding.ASCII.GetBytes(websocketkeyuuid);
            string websocketaccept;
            using (SHA1 sha1 = SHA1.Create())
            {
                websocketaccept = Convert.ToBase64String(sha1.ComputeHash(websocketacceptdata));
            }

            byte[] reqdata = Encoding.ASCII.GetBytes(webSocketRequest);
            stream.Write(reqdata, 0, reqdata.Length);

            string resline;

            resline = ReadHeaderLine(stream);
            string[] splits = resline.Split(new char[] { ' ' }, 3);
            if (splits.Length < 3)
            {
                stream.Close();
                throw new HttpClient.BadHttpResponseException();
            }

            if (!splits[0].StartsWith("HTTP/"))
            {
                stream.Close();
                throw new HttpClient.BadHttpResponseException();
            }

            if (splits[1] != "101")
            {
                int statusCode;
                if (!int.TryParse(splits[1], out statusCode))
                {
                    statusCode = 500;
                }
                if (statusCode == 404)
                {
                    throw new HttpException(statusCode, splits[2] + " (" + url + ")");
                }
                else if(splits[1] == "200")
                {
                    stream.Close();
                    throw new NotAWebSocketConnectionException();
                }
                else
                {
                    stream.Close();
                    throw new HttpException(statusCode, splits[2]);
                }
            }

            /* parse Headers */
            string lastHeader = string.Empty;
            Dictionary<string, string> headers = new Dictionary<string, string>();
            string headerLine;
            while ((headerLine = ReadHeaderLine(stream)).Length != 0)
            {
                if (headers.Count == 0)
                {
                    /* we have to trim first header line as per RFC7230 when it starts with whitespace */
                    headerLine = headerLine.TrimStart(new char[] { ' ', '\t' });
                }
                /* a white space designates a continuation , RFC7230 deprecates is use for anything else than Content-Type but we stay more permissive here */
                else if (char.IsWhiteSpace(headerLine[0]))
                {
                    headers[lastHeader] += headerLine.Trim();
                    continue;
                }

                string[] headerData = headerLine.Split(new char[] { ':' }, 2);
                if (headerData.Length != 2 || headerData[0].Trim() != headerData[0])
                {
                    stream.Close();
                    throw new HttpClient.BadHttpResponseException();
                }
                lastHeader = headerData[0];
                headers[lastHeader] = headerData[1].Trim();
            }

            if(!headers.TryGetValue("Connection", out headerLine) || headerLine.Trim() != "upgrade")
            {
                stream.Close();
                throw new NotAWebSocketConnectionException();
            }
            if(!headers.TryGetValue("Upgrade", out headerLine) || headerLine.Trim() != "websocket")
            {
                stream.Close();
                throw new NotAWebSocketConnectionException();
            }
            if(!headers.TryGetValue("Sec-WebSocket-Accept", out headerLine) || headerLine.Trim() != websocketaccept)
            {
                stream.Close();
                throw new NotAWebSocketConnectionException();
            }

            if(headers.TryGetValue("Sec-WebSocket-Protocol", out headerLine))
            {
                selectedProtocol = headerLine;
            }
            else
            {
                selectedProtocol = string.Empty;
            }

            return stream;
        }

        static string ReadHeaderLine(Stream stream)
        {
            StringBuilder s = new StringBuilder();
            for (;;)
            {
                int c = stream.ReadByte();
                if(c == -1)
                {
                    throw new EndOfStreamException();
                }

                if (c == (byte)'\r')
                {
                    if (stream.ReadByte() != '\n')
                    {
                        throw new HttpHeaderFormatException();
                    }
                    return s.ToString();
                }
                else if(c == (byte)'\n')
                {
                    throw new HttpHeaderFormatException();
                }
                else
                {
                    s.Append((char)c);
                }
            }
        }

        static Stream OpenPlainStream(string host, int port)
        {
            Socket s = new Socket(SocketType.Stream, ProtocolType.Tcp);
            s.Connect(host, port);
            return new NetworkStream(s);
        }

        static Stream OpenSslStream(string host, int port)
        {
            SslStream ssl = new SslStream(OpenPlainStream(host, port));
            ssl.AuthenticateAsClient(host);
            return ssl;
        }
    }
}
