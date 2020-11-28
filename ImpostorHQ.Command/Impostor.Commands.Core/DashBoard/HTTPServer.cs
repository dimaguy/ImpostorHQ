using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Buffers;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Authentication;
using System.Runtime.CompilerServices;
using Impostor.Commands.Core.SELF;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Impostor.Commands.Core.DashBoard
{
    public class HttpServer
    {
        #region Members
        //  our main listener.
        private TcpListener Listener { get; set; }
        private Class MainClass { get; set; }
        /// <summary>
        /// The constant webpages. They are read from the disk and always remain constant.
        /// </summary>
        public readonly byte[] Document, Document404Error, DocumentTypeNotSupported;
        //  indicates whether or not we should continue accepting clients.
        public bool Running { get; private set; }
        private WebApiServer ApiServer { get; set; }
        private CsvComposer LogComposer { get; set; }
        public List<string> InvalidPageHandlers { get; private set; }
        public QuiteEffectiveDetector QEDetector { get; private set; }
        public ConcurrentBag<string> AttackerAddresses { get; private set; }
        private bool EnableSecurity { get; set; }
        private X509Certificate2 Certificate { get; set; }
        #endregion

        /// <summary>
        /// Creates a new instance of our HTTP server. This is used to inject the HTML client into browsers.
        /// </summary>
        /// <param name="listenInterface">The interface to bind the socket to.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="document">The webpage.</param>
        /// <param name="document404Error">An HTML document used to indicate 404 errors.</param>
        /// <param name="documentMimeError">An HTML document used to indicate that a type of data is unsupported. This should never happen under normal circumstances, as the dashboard only uses supported file types.</param>
        ///<param name="secure">True if you wish to enable HTTPS.</param>
        /// <param name="dosDetector">The DoS detector.</param>
        /// <param name="mainClass">The plugin's main.</param>
        /// <param name="apiServer">The WebAPI server.</param>
        /// <param name="certGenerator">The SSL Certificate Synthesizer.</param>
        public HttpServer(string listenInterface, ushort port,string document,string document404Error,string documentMimeError, Impostor.Commands.Core.Class mainClass,WebApiServer apiServer, QuiteEffectiveDetector dosDetector,bool secure, X509Certificate2 certificate)
        {
            if(secure&&certificate==null) throw new Exception("What are you doing, anti?");
            //utf8 is standard.
            this.Document = Encoding.UTF8.GetBytes(document);
            this.Document404Error = Encoding.UTF8.GetBytes(document404Error);
            this.DocumentTypeNotSupported = Encoding.UTF8.GetBytes(documentMimeError);
            this.Running = true;
            this.MainClass = mainClass;
            this.Listener = new TcpListener(IPAddress.Parse(listenInterface),port);
            this.ApiServer = apiServer;
            this.LogComposer = new CsvComposer();
            this.InvalidPageHandlers = new List<string>();
            this.QEDetector = dosDetector;
            this.AttackerAddresses = new ConcurrentBag<string>();
            this.EnableSecurity = secure;
            this.Certificate = certificate;
            Listener.Start();
            Listener.BeginAcceptTcpClient(EndAccept, Listener);
        }

        /// <summary>
        /// This is used by plugins to hook webpages on their own handler.
        /// </summary>
        /// <param name="handler">The URL path to handle.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddInvalidPageHandler(string handler)
        {
            lock (InvalidPageHandlers)
            {
                InvalidPageHandlers.Add("dashboard/" + handler);
                InvalidPageHandlers.Add("dashboard\\" + handler);
            }
        }
        /// <summary>
        /// This is our async callback, used to accept TCP clients. The code is quite a ride!
        /// </summary>
        /// <param name="ar"></param>
        private async void EndAccept(IAsyncResult ar)
        {
            var listener = ar.AsyncState as TcpListener;
            if (listener == null) return;
            string address = "[BEFORE ACCEPT]";
            if (Running) listener.BeginAcceptTcpClient(EndAccept, listener);
            try
            {
                var client = listener.EndAcceptTcpClient(ar);
                address = (client.Client.RemoteEndPoint as IPEndPoint).Address.ToString();
                if(QEDetector.IsAttacking(((IPEndPoint)(client.Client.RemoteEndPoint)).Address))
                {
                    if (!AttackerAddresses.Contains(address))
                    {
                        ApiServer.Push(
                            $"Under denial of service attack from: {address}! The address has been booted from accessing the HTTP server, for 5 minutes. Please take action!",
                            "-HIGHEST PRIORITY/CRITICAL-", Structures.MessageFlag.ConsoleLogMessage);
                        AttackerAddresses.Add(address);
                    }
                    return;
                }
                var ns = SelectProtocol(client);
                using (var reader = new StreamReader(ns))
                {
                    var strData = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (strData == null) return;
                    var startPos = 0;
                    if (strData.Substring(0, 3) != "GET")
                    {
                        //only GET is allowed.
                        await WriteHeader("HTTP/1.1", "text/html", DocumentTypeNotSupported.Length, " 501 Not Implemented", ns).ConfigureAwait(false);
                        await ns.WriteAsync(DocumentTypeNotSupported, 0, DocumentTypeNotSupported.Length).ConfigureAwait(false);
                        await ns.DisposeAsync().ConfigureAwait(false);
                        client.Dispose();
                        return;
                    }

                    // Extract the request.
                    startPos = strData.IndexOf("HTTP", 1, StringComparison.InvariantCultureIgnoreCase);
                    var version = strData.Substring(startPos, 8);
                    var request = strData.Substring(0, startPos - 1);
                    request = request.Replace("\\", "/");
                    if ((request.IndexOf(".", StringComparison.InvariantCultureIgnoreCase) < 1) &&
                        (!request.EndsWith("/")))
                    {
                        request += "/";
                    }

                    startPos = request.LastIndexOf("/", StringComparison.CurrentCultureIgnoreCase) + 1;
                    var file = request.Substring(startPos);
                    var directory = request.Substring(request.IndexOf("/", StringComparison.InvariantCultureIgnoreCase), request.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase) - 3);
                    await Process(directory, file, version, address, ns).ConfigureAwait(false);
                }
                await ns.DisposeAsync().ConfigureAwait(false);
                client.Dispose();
            }
            catch (Exception ex)
            {
                //                  *shutting down
                if (!address.Equals("[BEFORE ACCEPT]"))
                    MainClass.LogManager.LogError($"SRC: {address}: {ex}", Shared.ErrorLocation.HttpServer);
            }
            finally
            {
            }
            
        }
        /// <summary>
        /// This is used to process a client.
        /// </summary>
        /// <param name="directory">The requested directory.</param>
        /// <param name="file">The requested file.</param>
        /// <param name="version">The HTTP version to play back.</param>
        /// <param name="address">The address of the client.</param>
        /// <param name="ns">The transport.</param>
        /// <returns></returns>
        private async Task Process(string directory, string file, string version, string address, Stream ns)
        {
            if (!directory.Contains("..")) //   Only root ('/') is allowed.
            {
                var cleanedPath = GetLocalPath(file, directory);
                if (!(cleanedPath.Contains("players.csv") || cleanedPath.Contains("logs")) &&
                    cleanedPath.Contains("?")) //you are angering me dima
                {
                    cleanedPath = cleanedPath.Remove(
                        cleanedPath.IndexOf("?", StringComparison.InvariantCultureIgnoreCase),
                        cleanedPath.Length -
                        cleanedPath.IndexOf("?", StringComparison.InvariantCultureIgnoreCase));
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
                            await WriteHeader(version, "text/html", Document.Length, " 200 OK", ns).ConfigureAwait(false);
                            await ns.WriteAsync(Document, 0, Document.Length).ConfigureAwait(false);
                        }
                        else
                        {
                            var document = File.ReadAllBytes(cleanedPath);
                            await WriteHeader(version, mimeType, document.Length, " 200 OK", ns).ConfigureAwait(false);
                            await ns.WriteAsync(document, 0, document.Length).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await WriteHeader(version, "text/html", DocumentTypeNotSupported.Length, " 501 Not Implemented", ns).ConfigureAwait(false);
                        await ns.WriteAsync(DocumentTypeNotSupported, 0, DocumentTypeNotSupported.Length).ConfigureAwait(false);
                    }
                }
                else
                {
                    List<string> handlers;
                    lock (InvalidPageHandlers) handlers = InvalidPageHandlers.ToList();
                    if (cleanedPath.Contains("players.csv") && cleanedPath.Contains('?'))
                    {
                        var key = cleanedPath.Substring(cleanedPath.IndexOf('?') + 1);
                        if (ApiServer.CheckKey(key))
                        {
                            var response = Encoding.UTF8.GetBytes(MainClass.CompilePlayers());
                            await WriteHeader(version, "text/csv", (int)response.Length, " 200 OK", ns).ConfigureAwait(false);
                            await ns.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
                            await ns.FlushAsync().ConfigureAwait(false);
                            //ApiServer.Push($"Players listed by: {address}, with key : {key}.",Structures.ServerSources.CommandSystem,Structures.MessageFlag.ConsoleLogMessage);
                        }
                        else
                        {
                            var data = Encoding.UTF8.GetBytes("<h1>You have entered an invalid key. Please stop trying to break into our system!</h1>");
                            await WriteHeader(version, "text/html", data.Length, " 200 OK", ns).ConfigureAwait(false);
                            await ns.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                        }

                    }
                    else if (cleanedPath.Contains("logs.csv") && cleanedPath.Contains('?'))
                    {
                        var requestData = cleanedPath.Substring(cleanedPath.IndexOf('?') + 1).Split('&');
                        if (requestData.Length == 2 && !string.IsNullOrEmpty(requestData[0]) &&
                            !string.IsNullOrEmpty(requestData[1]))
                        {
                            var key = requestData[0];
                            if (!ApiServer.CheckKey(key))
                            {
                                var data = Encoding.UTF8.GetBytes(
                                    "<h1>You have entered an invalid key. Please stop trying to break into our system!</h1>");
                                await WriteHeader(version, "text/html", data.Length, " 200 OK", ns).ConfigureAwait(false);
                                await ns.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                            }
                            else
                            {
                                var requestedLog = Path.Combine("hqlogs",
                                    Path.GetFileNameWithoutExtension(requestData[1]) + ".self");
                                var logs = MainClass.LogManager.GetLogNames();
                                if (!logs.Contains(requestedLog))
                                {
                                    var data = Encoding.UTF8.GetBytes(
                                        "<h1>Could not find the logs you requested.</h1>");
                                    await WriteHeader(version, "text/html", data.Length, " 404 Not Found", ns).ConfigureAwait(false);
                                    await ns.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                                }
                                else
                                {
                                    byte[] data;
                                    using (FileStream fs = new FileStream(requestedLog, FileMode.Open,
                                        FileAccess.Read, FileShare.ReadWrite))
                                    {
                                        //we don't want to read while it is writing.
                                        lock (MainClass.LogManager.FileLock)
                                        {
                                            data = new byte[fs.Length];
                                            fs.Read(data, 0, (int)fs.Length);
                                        }
                                    }

                                    //i am not doing it directly off the stream so i don't lock the manager for too long...
                                    //should not be a problem, logs will be quite small.
                                    var logData = GetCsv(data);
                                    await WriteHeader(version, "text/csv", logData.Length, " 200 OK", ns).ConfigureAwait(false);
                                    await ns.WriteAsync(logData, 0, logData.Length).ConfigureAwait(false);
                                }
                            }
                        }

                    }
                    else if (handlers.Contains(cleanedPath))
                    {
                        OnSpecialHandlerInvoked?.Invoke(
                            cleanedPath.Replace("dashboard\\", "").Replace("dashboard/", ""), ns, version,
                            address);
                    }
                    else
                    {
                        await WriteHeader(version, "text/html", Document404Error.Length, " 404 Not Found", ns).ConfigureAwait(false);
                        await ns.WriteAsync(Document404Error, 0, Document404Error.Length).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await WriteHeader(version, "text/html", Document404Error.Length, " 404 Not Found", ns).ConfigureAwait(false);
                await ns.WriteAsync(Document404Error, 0, Document404Error.Length).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// This will select the transport protocol.
        /// </summary>
        /// <param name="client">The client to process.</param>
        /// <returns>A secure/insecure transport.</returns>
        private Stream SelectProtocol(TcpClient client)
        {
            if (!EnableSecurity)
            {
                return (Stream)client.GetStream();
            }

            try
            {
                var stream = new SslStream(client.GetStream());
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                stream.AuthenticateAsServer(Certificate, false, SslProtocols.Tls12, true);
                return stream;
            }
            catch
            {
            }
            //fall to insecure protocol if SSL fails to authenticate.
            return client.GetStream();
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
        private string GetLocalPath(string target,string dir)
        {
            target = target.Replace("..",""); //don't you dare probe my filesystem!!
            dir = dir.Replace("..", "");
            var result = Path.Combine("dashboard", dir.Replace("/",""));
            result = Path.Combine(result, target);
            return result;
        }
        /// <summary>
        /// This is used to get CSV logs.
        /// </summary>
        /// <param name="data">The SELF file.</param>
        /// <returns>CSV logs formatted with UTF8 and CRLF></returns>
        private byte[] GetCsv(byte[] data)
        {
            return Encoding.UTF8.GetBytes(LogComposer.Compose(data));
        }
        /// <summary>
        /// This is used to shut down the server.
        /// </summary>
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
        public async Task WriteHeader(string httpVer, string mimeType,int documentLen, string sStatusCode, Stream stream)
        {
            var sb = new StringBuilder();
            sb.Append(httpVer + sStatusCode + "\r\n");
            sb.Append("Server: antiHTTP\r\n");
            sb.Append("Content-Type: " + mimeType + "\r\n");
            sb.Append("Accept-Ranges: bytes\r\n");
            sb.Append("Content-Length: " + documentLen + "\r\n\r\n");
            var data = Encoding.ASCII.GetBytes(sb.ToString());
            await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }
        /// <summary>
        /// This can be used by plugins to easily write documents / pages to a transport stream.
        /// </summary>
        /// <param name="document">The document to write.</param>
        /// <param name="mimeType">The type of document to write.</param>
        /// <param name="stream">The HTTP(s) transport stream to write to.</param>
        public void WriteDocument(byte[] document, string mimeType, Stream stream)
        {
            WriteHeader("HTTP/1.1",mimeType,document.Length," 200 OK",stream);
            stream.Write(document,0,document.Length);
        }
       
        public delegate void DelHandlerInvoked(string handler, Stream directTransport, string httpVer, string srcIpAddress);
        /// <summary>
        /// This is invoked when a registered special command handler is invoked.
        /// </summary>
        public event DelHandlerInvoked OnSpecialHandlerInvoked;
    }
}
