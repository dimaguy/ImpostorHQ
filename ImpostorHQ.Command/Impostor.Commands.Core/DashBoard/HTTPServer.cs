using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using Impostor.Commands.Core.SELF;

namespace Impostor.Commands.Core.DashBoard
{
    public class HttpServer
    {
        //  our main listener.
        private TcpListener Listener { get; set; }
        private Impostor.Commands.Core.Class Interface { get; set; }
        /// <summary>
        /// The constant webpages. They are read from the disk and always remain constant.
        /// </summary>
        public readonly byte[] Document, Document404Error, DocumentTypeNotSupported;
        //  indicates whether or not we should continue accepting clients.
        public bool Running { get; private set; }
        private WebApiServer ApiServer { get; set; }
        private CsvComposer LogComposer { get; set; }
        /// <summary>
        /// Creates a new instance of our HTTP server. This is used to inject the HTML client into browsers.
        /// </summary>
        /// <param name="listenInterface">The interface to bind the socket to.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="document">The webpage.</param>
        /// <param name="document404Error">An HTML document used to indicate 404 errors.</param>
        /// <param name="documentMimeError">An HTML document used to indicate that a type of data is unsupported. This should never happen under normal circumstances, as the dashboard only uses supported file types.</param>
        public HttpServer(string listenInterface, ushort port,string document,string document404Error,string documentMimeError, Impostor.Commands.Core.Class Interface,WebApiServer apiServer)
        {
            //utf8 is standard.
            this.Document = Encoding.UTF8.GetBytes(document);
            this.Document404Error = Encoding.UTF8.GetBytes(document404Error);
            this.DocumentTypeNotSupported = Encoding.UTF8.GetBytes(documentMimeError);
            this.Running = true;
            this.Interface = Interface;
            this.Listener = new TcpListener(IPAddress.Parse(listenInterface),port);
            this.ApiServer = apiServer;
            this.LogComposer = new CsvComposer();
            Listener.Start();
            //we begin listening and accepting clients.
            Listener.BeginAcceptTcpClient(EndAccept, Listener);
        }

        /// <summary>
        /// This is our async callback, used to accept TCP clients. The code is quite a ride!
        /// </summary>
        /// <param name="ar"></param>
        private void EndAccept(IAsyncResult ar)
        {
            var listener = (TcpListener) ar.AsyncState;
            if (listener == null) return;
            string address = "[BEFORE ACCEPT]";
            if(Running) listener.BeginAcceptTcpClient(EndAccept, listener);
            try
            {
                var client = listener.EndAcceptTcpClient(ar);
                var ns = client.GetStream();
                address = (((IPEndPoint) client.Client.RemoteEndPoint).Address).ToString();
                var startPos = 0;
                var receiveBuffer = new byte[1024]; //should not give us trouble.

                //maybe i will use 'read' at some point...
                var read = ns.Read(receiveBuffer, 0, receiveBuffer.Length);
                var strData = Encoding.ASCII.GetString(receiveBuffer);
                if (strData.Substring(0, 3) != "GET")
                {
                    //only GET is allowed.
                    WriteHeader("HTTP/1.1", "text/html", DocumentTypeNotSupported.Length, " 501 Not Implemented", ref ns);
                    ns.Write(DocumentTypeNotSupported, 0, DocumentTypeNotSupported.Length);
                    ns.Dispose();
                    client.Dispose();
                    return;
                }
                // Extract the request.
                startPos = strData.IndexOf("HTTP", 1,StringComparison.InvariantCultureIgnoreCase);
                var version = strData.Substring(startPos, 8);
                var request = strData.Substring(0, startPos - 1);
                request = request.Replace("\\", "/");
                if ((request.IndexOf(".",StringComparison.InvariantCultureIgnoreCase) < 1) && (!request.EndsWith("/")))
                {
                    request += "/";
                }
                startPos = request.LastIndexOf("/",StringComparison.CurrentCultureIgnoreCase) + 1;
                var file = request.Substring(startPos);
                var directory = request.Substring(request.IndexOf("/",StringComparison.InvariantCultureIgnoreCase), request.LastIndexOf("/",StringComparison.InvariantCultureIgnoreCase) - 3);
                //COMPARISION : If the path is clean, move on to the next comparision. If not, send the error.
                if (!directory.Contains("..")) //   Only root ('/') is allowed.
                {
                    var cleanedPath = GetLocalPath(file, directory);
                    if (!(cleanedPath.Contains("players.csv")||cleanedPath.Contains("logs")) && cleanedPath.Contains("?")) //you are angering me dima
                    {
                        cleanedPath = cleanedPath.Remove(cleanedPath.IndexOf("?",StringComparison.InvariantCultureIgnoreCase),
                            cleanedPath.Length - cleanedPath.IndexOf("?",StringComparison.InvariantCultureIgnoreCase));
                    }
                    //COMPARISION : If file exists, move on to the next comparision. If not, send the error.
                    if (File.Exists(cleanedPath))
                    {
                        var mimeType = ParseMime(cleanedPath);
                        //COMPARISION : If the file type is supported, move on to the next comparision. If not, send the error.
                        if (mimeType != null)
                        {
                            //COMPARISION : Send client from memory or an unloaded file from the disk.
                            if (cleanedPath.Contains("client.html"))
                            {
                                WriteHeader(version,"text/html",Document.Length," 200 OK",ref ns);
                                ns.Write(Document,0,Document.Length);
                            }
                            else
                            {
                                var document = File.ReadAllBytes(cleanedPath);
                                WriteHeader(version,mimeType,document.Length," 200 OK",ref ns);
                                ns.Write(document,0,document.Length);
                            }
                        }
                        else
                        {
                            WriteHeader(version, "text/html", DocumentTypeNotSupported.Length, " 501 Not Implemented", ref ns);
                            ns.Write(DocumentTypeNotSupported, 0, DocumentTypeNotSupported.Length);
                        }
                    }
                    else
                    {
                        if (cleanedPath.Contains("players.csv")&&cleanedPath.Contains('?'))
                        {
                            var key = cleanedPath.Substring(cleanedPath.IndexOf('?') + 1);
                            if(ApiServer.CheckKey(key))
                            {
                                var response = Encoding.UTF8.GetBytes(Interface.CompilePlayers());
                                WriteHeader(version, "text/csv", (int)response.Length, " 200 OK", ref ns);
                                ns.Write(response, 0, response.Length);
                                ns.Flush();
                                //ApiServer.Push($"Players listed by: {address}, with key : {key}.",Structures.ServerSources.CommandSystem,Structures.MessageFlag.ConsoleLogMessage);
                            }
                            else
                            {
                                var data = Encoding.UTF8.GetBytes(
                                    "<h1>You have entered an invalid key. Please stop trying to break into our system!</h1>");
                                WriteHeader(version, "text/html", data.Length, " 200 OK", ref ns);
                                ns.Write(data,0,data.Length);
                            }

                        }
                        else if (cleanedPath.Contains("logs.csv") && cleanedPath.Contains('?'))
                        {
                            var requestData = cleanedPath.Substring(cleanedPath.IndexOf('?') + 1).Split('&');
                            if (requestData.Length == 2 && !string.IsNullOrEmpty(requestData[0])&& !string.IsNullOrEmpty(requestData[1]))
                            {
                                var key = requestData[0];
                                if (!ApiServer.CheckKey(key))
                                {
                                    var data = Encoding.UTF8.GetBytes(
                                        "<h1>You have entered an invalid key. Please stop trying to break into our system!</h1>");
                                    WriteHeader(version, "text/html", data.Length, " 200 OK", ref ns);
                                    ns.Write(data, 0, data.Length);
                                }
                                else
                                {
                                    var requestedLog = Path.Combine("hqlogs", Path.GetFileNameWithoutExtension(requestData[1]) + ".self");
                                    var logs = Interface.LogManager.GetLogNames();
                                    if (!logs.Contains(requestedLog))
                                    {
                                        var data = Encoding.UTF8.GetBytes(
                                            "<h1>Could not find the logs you requested.</h1>");
                                        WriteHeader(version, "text/html", data.Length, " 404 Not Found", ref ns);
                                        ns.Write(data, 0, data.Length);
                                    }
                                    else
                                    {
                                        byte[] data;
                                        using (FileStream fs = new FileStream(requestedLog, FileMode.Open,
                                            FileAccess.Read, FileShare.ReadWrite))
                                        {
                                            //we don't want to read while it is writing.
                                            lock (Interface.LogManager.FileLock)
                                            {
                                                data = new byte[fs.Length];
                                                fs.Read(data, 0, (int)fs.Length);
                                            }
                                        }
                                        //i am not doing it directly off the stream so i don't lock the manager for too long...
                                        //should not be a problem, logs will be quite small.
                                        var logData = GetCsv(data);
                                        WriteHeader(version, "text/csv", logData.Length, " 200 OK", ref ns);
                                        ns.Write(logData, 0, logData.Length);
                                    }
                                }
                            }

                        }
                        else
                        {
                            WriteHeader(version, "text/html", Document404Error.Length, " 404 Not Found", ref ns);
                            ns.Write(Document404Error, 0, Document404Error.Length);
                        }
                    }
                }
                else
                {
                    WriteHeader(version, "text/html", Document404Error.Length, " 404 Not Found", ref ns);
                    ns.Write(Document404Error, 0, Document404Error.Length);
                }
                ns.Dispose();
                client.Dispose();
            }
            catch(Exception ex)
            {
                //                  *shutting down
                if(!address.Equals("[BEFORE ACCEPT]")) Interface.LogManager.LogError($"SRC: {address}: {ex.ToString()}", Shared.ErrorLocation.HttpServer);
            }
            
        }

        /// <summary>
        /// This will get the MIME type from the extension.
        /// </summary>
        /// <param name="file">The path to the requested file.</param>
        /// <returns>Returns the MIME type, to be used with HTTP. If it is unknown, it will return null.</returns>
        private string ParseMime(string file)
        {
            file = file.ToLower();
            var extension = file.Substring(file.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase));
            switch (extension.ToLower())
            {
                case ".html":
                    return "text/html";
                case ".htm":
                    return "text/html";
                case ".js":
                    return "text/javascript";
                case ".css":
                    return "text/css";
                case ".ico":
                    return "image/vnd.microsoft.icon";
                case ".jpeg":
                    return "image/jpeg";
                case ".jpg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".ttf":
                    return "application/x-font-ttf";
                default:
                    //unsupported type...
                    return null;
            }
        }

        /// <summary>
        /// This will sanitize a path. My thinking is that somebody could add ('..') to their path, in order to probe the filesystem.
        /// </summary>
        /// <param name="target">The requested path.</param>
        /// <returns>The path is sanitized and localized to the required data folder.</returns>
        public string GetLocalPath(string target,string dir)
        {
            string rv;
            target = target.Replace("..",""); //don't you dare probe my filesystem!!
            dir = dir.Replace("..", "");
            rv = Path.Combine("dashboard", dir.Replace("/",""));
            rv = Path.Combine(rv, target);
            return rv;
        }

        /// <summary>
        /// Used to shut down the HTTP server.
        /// </summary>
        private byte[] GetCsv(byte[] data)
        {
            return Encoding.UTF8.GetBytes(LogComposer.Compose(data));
        }
        public void Shutdown()
        {
            this.Running = false;
            Listener.Stop();
        }
        /// <summary>
        /// This will write a correct header to our HTTP transport.
        /// </summary>
        /// <param name="httpVer">The version, as requested by the client.</param>
        /// <param name="documentLen">The length of the following data.</param>
        /// <param name="sStatusCode">Status code.</param>
        /// <param name="stream">The transport to write to.</param>
        private void WriteHeader(string httpVer, string mimeType,int documentLen, string sStatusCode, ref NetworkStream stream)
        {
            var sb = new StringBuilder();
            sb.Append(httpVer + sStatusCode + "\r\n");
            sb.Append("Server: antiHTTP\r\n");
            sb.Append("Content-Type: " + mimeType + "\r\n");
            sb.Append("Accept-Ranges: bytes\r\n");
            sb.Append("Content-Length: " + documentLen + "\r\n\r\n");
            var data = Encoding.ASCII.GetBytes(sb.ToString());
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

    }
}
