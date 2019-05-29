using Panacea.Core;
using Panacea.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    public class HttpClient : IHttpClient
    {
        private static object Lock = new object();
        private static Dictionary<string, Task<byte[]>> tasks = new Dictionary<string, Task<byte[]>>();
        public static int ActiveTasks { get; private set; }
        public static readonly string Header = "Panacea/" + Assembly.GetEntryAssembly().GetName().Version;
        private const string HeaderTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
        private readonly IHttpCache _cache;
        private readonly ILogger _logger;
        private readonly ISerializer _serializer;


        public Uri Uri { get; private set; }
        public IHttpCache Cache { get => _cache; }

        public HttpClient(Uri uri, int portsToAddForApiCalls, IHttpCache cache, ISerializer serializer, ILogger logger)
        {
            var builder = new UriBuilder(uri);
            builder.Port += portsToAddForApiCalls;
            Uri = builder.Uri;
            _serializer = serializer;
            _logger = logger;
            _cache = cache;
        }

        List<IHttpMiddleware> _middlewares = new List<IHttpMiddleware>();
        public void AddMiddleware(IHttpMiddleware middleware)
        {
            _middlewares.Add(middleware);
        }

        public async Task<ServerResponse<T>> GetObjectAsync<T>(
            string url,
            List<KeyValuePair<string, string>> postData = null,
            Dictionary<string, byte[]> files = null,
            bool allowCache = true,
            CancellationTokenSource cts = null)
        {
            if (cts == null) cts = new CancellationTokenSource();
            ServerResponse<T> resp = null;
            var result = await GetStringAsync(url, postData, files, allowCache, cts);
            resp = _serializer.Deserialize<ServerResponse<T>>(result);
            return resp;
        }

        public virtual async Task<string> GetStringAsync(
            string url,
            List<KeyValuePair<string, string>> postData = null,
            Dictionary<string, byte[]> files = null,
            bool allowCache = true,
            CancellationTokenSource cts = null)
        {
            url = await BuildUrl(url);
            return await DownloadString(url, postData, files, allowCache, cts);
        }

        protected virtual async Task<string> BuildUrl(string url)
        {
            var uri = new Uri(RelativeToAbsoluteUri(url));
            foreach (var mid in _middlewares)
            {
                uri = await mid.OnBeforeRequest(uri);
            }
            var transformed = uri;
            return transformed.ToString();
        }

        public virtual async Task<ServerResponse<T>> SetCookieAsync<T>(string name, T data)
        {
            var req = BuildWebRequest(await BuildUrl("set_cookie/" + name + "/"));
            req.Method = "POST";
            string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
            req.ContentType = "multipart/form-data; boundary=" + boundary;
            WritePostDictionaryToStream(req.GetRequestStream(),
                new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("data", _serializer.Serialize(data)) },
                boundary);
            using (var response = await req.GetHttpResponseAsync(10000))
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return _serializer.Deserialize<ServerResponse<T>>(await reader.ReadToEndAsync());
            }
        }

        public virtual async Task<ServerResponse<T>> GetCookieAsync<T>(string name)
        {
            var req = BuildWebRequest(await BuildUrl("get_cookie/" + name + "/"));
            using (var response = await req.GetHttpResponseAsync(10000))
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return _serializer.Deserialize<ServerResponse<T>>(await reader.ReadToEndAsync());
            }
        }

        private static void WritePostDictionaryToStream(
            Stream stream,
            List<KeyValuePair<string, string>> postData,
            string boundary)
        {
            if (postData == null) return;
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            string formdataTemplate = "\r\n--" + boundary +
                                                          "\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";

            foreach (var pair in postData)
            {

                string formitem = string.Format(formdataTemplate, pair.Key, pair.Value);
                byte[] formitembytes = Encoding.UTF8.GetBytes(formitem);
                stream.Write(formitembytes, 0, formitembytes.Length);
            }
            stream.Write(boundarybytes, 0, boundarybytes.Length);
        }

        private static void WriteFilesDictionaryToStream(Stream stream,
            Dictionary<string, byte[]> files,
            string boundary)
        {
            if (files == null) return;
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            foreach (var key in files.Keys)
            {

                //string header = string.Format(headerTemplate, "file" + i, files[i]);
                var fileName = key;
                if (fileName.Contains("."))
                {
                    fileName = key.Split('.')[0];
                }
                string header = string.Format(HeaderTemplate, fileName, key);

                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);

                stream.WriteAsync(headerbytes, 0, headerbytes.Length);
                stream.WriteAsync(files[key], 0, files[key].Length);
                stream.WriteAsync(boundarybytes, 0, boundarybytes.Length);

            }
        }



        private static HttpWebRequest BuildWebRequest(string url, bool addTimeout = false)
        {

            var req = (HttpWebRequest)WebRequest.Create(url);
            if (addTimeout)
                req.Timeout = 60000;
            req.KeepAlive = true;
            req.ProtocolVersion = HttpVersion.Version11;
            req.Accept = "application/json";
            req.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, sdch");//
            req.UserAgent = Header;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            return req;
        }

        private async Task<string> DownloadString(string url,
            List<KeyValuePair<string, string>> postData = null,
            Dictionary<string, byte[]> files = null,
            bool allowCache = false,
            CancellationTokenSource cts = null)
        {

            var entry = await _cache.GetStringAsync(url);
            int tries = 0;
            Exception innerException = null;
            while (tries < 2)
            {
                tries++;

                if (tries == 3)
                {
                    await Task.Delay(5000);
                }
                try
                {
                    //throw new WebException("custom");
                    if (cts != null && cts.IsCancellationRequested) cts.Token.ThrowIfCancellationRequested();

                    var req = BuildWebRequest(url, files == null);
                    if (files != null || postData != null)
                    {
                        string boundary = "----------------------------" +
                                      DateTime.Now.Ticks.ToString("x");
                        using (Stream memStream = new System.IO.MemoryStream())
                        {
                            req.Method = "POST";
                            req.ContentType = "multipart/form-data; boundary=" + boundary;

                            WritePostDictionaryToStream(memStream, postData, boundary);
                            WriteFilesDictionaryToStream(memStream, files, boundary);

                            using (var requestStream = req.GetRequestStream())
                            {
                                memStream.Position = 0;
                                byte[] tempBuffer = new byte[memStream.Length];
                                memStream.Read(tempBuffer, 0, tempBuffer.Length);
                                memStream.Close();
                                requestStream.Write(tempBuffer, 0, tempBuffer.Length);
                            }
                        }
                    }
                    using (var resp = (HttpWebResponse)await req.GetHttpResponseAsync(10000))
                    using (var sr = new StreamReader(resp.GetResponseStream()))
                    {
                        if (cts != null && cts.IsCancellationRequested) cts.Token.ThrowIfCancellationRequested();
                        var result = await sr.ReadToEndAsync();
                        if (allowCache)
                        {
                            entry.String = result;
                            await _cache.SetStringAsync(entry);
                        }

                        return result;
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError) tries = 5;
                    _logger?.Error(this, ex.Message + " / " + url);
                    if (tries == 2 && !allowCache)
                    {
                        throw;
                    }
                    innerException = ex;
                }
                catch (Exception ex)
                {
                    _logger?.Error(this, ex.Message + " / " + url);
                    throw;
                }
                finally
                {

                }
            }
            if (allowCache && entry.Exists)
            {
                return entry.String;
            }
            throw new Exception("A request failed (" + innerException.Message + ") and no resource found in cache.",
                innerException);
        }

        public virtual string RelativeToAbsoluteUri(string path)
        {
            bool isFile = false;
            if (path != null)
            {
                try
                {
                    if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                        isFile = new Uri(path).IsFile;
                }
                catch (UriFormatException)
                {
                }

                if (!isFile)
                {
                    if (!path.StartsWith("http://") && !path.StartsWith("https://"))
                    {
                        if (path.StartsWith("/"))
                            return Uri + path.Substring(1, path.Length - 1);
                        return Uri + path;
                    }
                }
            }

            return path;
        }

        public Task<byte[]> DownloadDataAsync(string url, CancellationTokenSource cts = null)
        {
            var taskToAwait = DownloadFileTaskAsync(url, cts);
            taskToAwait = SafeAccessTaskList(url, taskToAwait, false);
            taskToAwait.ContinueWith((task) =>
            {
                SafeAccessTaskList(url, task, true);
            });
            return taskToAwait;
        }

        async Task<byte[]> DownloadFileTaskAsync(string url, CancellationTokenSource cts = null)
        {

            url = RelativeToAbsoluteUri(url);
            var entry = await _cache.GetDataAsync(url);

            if (DateTime.Now.Subtract(entry.LastChecked).TotalHours < 2)
                return entry.Data;

            try
            {
                var request =
                    (HttpWebRequest)WebRequest.Create(new Uri(url, UriKind.Absolute));
                request.KeepAlive = false;
                request.ProtocolVersion = HttpVersion.Version11;
                request.Timeout = 100000;
                request.Accept = "text/html, application/xhtml+xml, */*";
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");//
                request.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.7,el;q=0.3");
                request.UserAgent = Header;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Revalidate);
                if (entry.Exists)
                {
                    request.IfModifiedSince = entry.LastModified;
                }
                if (cts != null && cts.IsCancellationRequested) cts.Token.ThrowIfCancellationRequested();
                using (var response = await request.GetHttpResponseAsync(100000))
                {

                    if (cts != null && cts.IsCancellationRequested) cts.Token.ThrowIfCancellationRequested();
                    if ((int)response.StatusCode == 304)
                    {
                        response.Close();
                        entry.LastChecked = DateTime.Now;
                        await _cache.SetDataAsync(entry);
                        return entry.Data;
                    }
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            if (cts != null && cts.IsCancellationRequested) cts.Token.ThrowIfCancellationRequested();
                            var dtlm = response.LastModified;
                            using (var memoryStream = new MemoryStream())
                            {
                                await responseStream.CopyToAsync(memoryStream);
                                entry.LastModified = response.LastModified;
                                entry.LastChecked = DateTime.Now;
                                entry.Data = memoryStream.ToArray();
                                await _cache.SetDataAsync(entry);
                                return entry.Data;
                            }
                        }
                    }

                }
            }
            catch (WebException ex)
            {

            }
            return null;
        }

        static Task<byte[]> SafeAccessTaskList(string url, Task<byte[]> task, bool remove)
        {
            lock (Lock)
            {
                if (tasks.ContainsKey(url))
                {
                    if (remove) tasks.Remove(url);
                    else
                    {
                        return tasks[url];
                    }
                }
                else if (!remove && task != null)
                {
                    tasks.Add(url, task);
                    if (task.Status == TaskStatus.Created)
                        task.Start();
                }
                return task;
            }
        }

        public Task<string> GetApiEndpoint(string path)
        {
            return BuildUrl(path);
        }
    }

}
