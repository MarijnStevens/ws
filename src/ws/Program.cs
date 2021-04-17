using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ws
{
    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;

        public WebServer(IReadOnlyCollection<string> prefixes, Func<HttpListenerRequest, string> method)
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
            }

            if (prefixes == null || prefixes.Count == 0)
            {
                throw new ArgumentException("URI prefixes are required");
            }

            if (method == null)
            {
                throw new ArgumentException("responder method required");
            }

            foreach (var s in prefixes)
            {
                _listener.Prefixes.Add(s);
            }

            _responderMethod = method;
            _listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
            : this(prefixes, method)
        {
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem(c =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                if (ctx == null)
                                {
                                    return;
                                }

                                var rstr = _responderMethod(ctx.Request);
                                var buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch (Exception ex)
                            {
                                var exception = ex.ToString();
                                var buf = Encoding.UTF8.GetBytes(exception);
                                ctx.Response.StatusCode = 500;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            finally
                            {
                                // always close the stream
                                if (ctx != null)
                                {
                                    ctx.Response.OutputStream.Close();
                                }
                            }
                        }, _listener.GetContext());
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            });
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }

    internal class Program
    {
        static bool _verbose = false;
        static string _workingDirectory = string.Empty;

        public static string SendDirectory(string path)
        {
            var directoryContent = string.Empty;
            DirectoryInfo dir = new DirectoryInfo(path);

            path = path.Replace(_workingDirectory, string.Empty);

            directoryContent = "<table>";

            foreach(var d in dir.GetDirectories())
            {
                directoryContent += "<tr>";
                directoryContent += $"<td><a href=\"{path + d.Name}/\">{d.Name}/</a></td>";
                directoryContent += $"<td>{d.CreationTime.ToShortDateString()} {d.CreationTime.ToShortTimeString()}</td>";
                directoryContent += "</tr>" + Environment.NewLine;
            }

            foreach(var f in dir.GetFiles())
            {
                directoryContent += "<tr>";
                directoryContent += $"<td><a href=\"{path + f.Name}\">{f.Name}</a></td>";
                directoryContent += $"<td>{f.CreationTime.ToShortDateString()} {f.CreationTime.ToShortTimeString()}</td>";
                directoryContent += "</tr>" + Environment.NewLine;
            }

            directoryContent += "</table>";

            var body =
$@"<html>
<head><title>Index of /{path}</title></head>
<body bgcolor='white'>
<h1>Index of /</h1><hr>
<pre><a href='..'>../</a>
{directoryContent}
</pre><hr></body>
</html>
";

            return body;

        }

        public static string SendFile(string path)
        {
            return Encoding.UTF8.GetString(File.ReadAllBytes(path));
        }

        public static string SendResponse(HttpListenerRequest request)
        {
            string response = string.Empty;
            Stopwatch stopwatch = Stopwatch.StartNew();

            var localPath = request.Url.LocalPath.TrimStart('/');

            if (localPath == "")
            {
                var haccess = new[] { "index.html", "index.xml", "index.htm", "default.htm" };

                var uri = Path.Combine(_workingDirectory, localPath);

                if (!string.IsNullOrEmpty(uri))
                {
                    foreach (var f in haccess)
                    {
                        var combinedUri = Path.Combine(_workingDirectory, f);
                        if (File.Exists(combinedUri)) { response = SendFile(combinedUri); break; }
                    }
                }

            }

            if (string.IsNullOrWhiteSpace(response))
            {
                localPath = Path.Combine(_workingDirectory, localPath);

                if (File.Exists(localPath))
                {
                    response = SendFile(localPath);
                }
            }

            if(string.IsNullOrWhiteSpace(response))
            {
                var uri = Path.Combine(_workingDirectory, localPath);
                response = SendDirectory(uri);
            }

            if (_verbose)
            {
                stopwatch.Stop();

                if (_verbose)
                    Console.WriteLine($"[{request.HttpMethod}] {request.Url.LocalPath} - {stopwatch.ElapsedMilliseconds}ms");
            }

            if (!string.IsNullOrWhiteSpace(response))
            {
                return response;
            }

            var V = Assembly.GetExecutingAssembly().GetName().Version;
            var version = $"{V.Major}.{V.Minor}.{V.Build}";

            return @"<html>
<head><title>404 Not Found</title></head>
<body bgcolor=""white"">
<center><h1>404 Not Found</h1></center>
<hr><center>ws/" + version + @"</center>
</body>
</html>
<!-- a padding to disable MSIE and Chrome friendly error page -->
<!-- a padding to disable MSIE and Chrome friendly error page -->
<!-- a padding to disable MSIE and Chrome friendly error page -->
<!-- a padding to disable MSIE and Chrome friendly error page -->
<!-- a padding to disable MSIE and Chrome friendly error page -->
<!-- a padding to disable MSIE and Chrome friendly error page -->
";
        }

        private static string _prefix = "localhost";

        ///
        /// Piss poor argument grabber, but hey; kiss.
        ///
        public static object? GetValue(string[] arguments, string shortForm, string longForm, bool required, string variableName = "")
        {
            List<string> values = new List<string>();

            for (int index = 0; index < arguments.Length; index++)
            {
                if (arguments[index] == shortForm || arguments[index] == longForm)
                {
                    if (required)
                    {
                        // Grab the next argument as variable
                        if (index + 1 != arguments.Length)
                        {
                            return arguments[index+1];
                        }
                    }
                    else
                    {
                        if (_verbose)
                        {
                            Console.WriteLine($"{longForm} flag is set.");
                        }
                        // Set the flag.
                        return true;
                    }
                }
            }

            var str = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            return null;
        }

        private static int Main(string[] args)
        {
            var stopwatch = Stopwatch.StartNew();

            bool verbose = false;

            object value = (object?)GetValue(args, "-v", "--verbose", false);
            if (value != null) bool.TryParse(verbose.ToString(), out _verbose);


            var directory = (string?)GetValue(args, null, null, false, "WS_DIRECTORY");
            string strPort = (string)GetValue(args, "-p", "--port", true, "WS_PORT");

            int.TryParse(strPort, out int port);


            // Default the current working directory.
            if (string.IsNullOrWhiteSpace(directory))
            {
                _workingDirectory = Directory.GetCurrentDirectory() ?? "";
                directory = _workingDirectory;
            }
            else
            {
                _workingDirectory = directory;
            }


            if (port == 0)
            {
                port = new Random().Next(8000, 65535);
            }
            var prefix = $"http://localhost:{port}/";
            var ws = new WebServer(SendResponse, prefix);
            ws.Run();

            Console.WriteLine($"Listening on {port}. Directory: " + _workingDirectory);

            if (verbose) Console.WriteLine(Environment.NewLine + "Press any key to quit.");

            try
            {
                if (verbose) Console.WriteLine();

                Process.Start(new ProcessStartInfo(prefix) { UseShellExecute = true });

                Console.ReadLine();

                // Stop the server.
                ws.Stop();

            }
            catch
            {
                // If an exception occured.
                return 1;
            }

            stopwatch.Stop();
            if (verbose) Console.WriteLine("Shutdown. Uptime: " + stopwatch.Elapsed);

            return 0;
        }

    }
}

