using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    public class SqLiteNetworkCache : IHttpCache
    {
        private readonly string _fileName;
        private readonly string _path;
        private readonly string _fileFullName;
        int _retryAttempts = 0;

        public SqLiteNetworkCache(string path, string fileName)
        {
            _fileName = fileName;
            _path = path;
            _fileFullName = Path.Combine(_path, _fileName);
            OpenDatabase();
        }

        private SQLiteConnection con;
        static object _lock = new object();

        private void OpenDatabase(bool retry = true)
        {
            if (!Directory.Exists(_path)) Directory.CreateDirectory(_path);
            var opened = false;
            while (!opened)
            {
                try
                {
                    con = new SQLiteConnection($@"Data Source={_fileFullName};Compress=False;");
                    con.Open();
                    var command =
                        new SQLiteCommand(
                            "CREATE TABLE IF NOT EXISTS data_cache (url text primary key, last_modified text, last_checked text, data blob);",
                            con);
                    command.ExecuteNonQuery();
                    command =
                        new SQLiteCommand(
                            "CREATE TABLE IF NOT EXISTS string_cache (url text primary key, last_modified text, last_checked text, string text);",
                            con);
                    command.ExecuteNonQuery();
                    opened = true;
                    _retryAttempts = 0;
                }
                catch
                {
                    if (!retry) throw;
                    ResetFile();
                }
            }

        }

        void ResetFile()
        {
            _retryAttempts++;
            try
            {
                con?.Close();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            }
            catch
            {
            }
            try
            {
                if (File.Exists(_fileFullName))
                {
                    File.Delete(_fileFullName);
                }
            }
            catch
            {
            }
            OpenDatabase(_retryAttempts < 10);
        }

        public async Task<DataCacheEntry> GetDataAsync(string url)
        {
            using (var command =
                new SQLiteCommand("SELECT url, last_modified, last_checked, data FROM data_cache WHERE url = @url", con))
            {
                command.Parameters.AddWithValue("@url", url);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (!reader.Read())
                        return new DataCacheEntry() { Exists = false, Url = url };

                    return new DataCacheEntry()
                    {
                        Exists = true,
                        Url = url,
                        LastChecked = reader.GetDateTime(2),
                        LastModified = reader.GetDateTime(1),
                        Data = await GetBytesAsync(reader, 3)
                    };
                }
            }
        }

        public async Task<StringCacheEntry> GetStringAsync(string url)
        {
            using (var command =
                new SQLiteCommand("SELECT url, last_modified, last_checked, string FROM string_cache WHERE url = @url", con))
            {
                command.Parameters.AddWithValue("@url", url);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (!reader.Read())
                    {
                        return new StringCacheEntry() { Exists = false, Url = url };
                    }

                    return new StringCacheEntry()
                    {
                        Exists = true,
                        Url = url,
                        LastChecked = reader.GetDateTime(2),
                        LastModified = reader.GetDateTime(1),
                        String = reader.GetString(3)
                    };
                }
            }
        }

        public Task SetStringAsync(StringCacheEntry entry)
        {
            return SetEntryAsync(entry);
        }

        public Task SetDataAsync(DataCacheEntry entry)
        {
            return SetEntryAsync(entry);
        }

        protected async Task SetEntryAsync<T>(T entry) where T : CacheEntry
        {
            string what = entry is DataCacheEntry ? "data" : "string";

            using (var command =
                new SQLiteCommand("SELECT last_modified FROM " + what + "_cache WHERE url = @url", con))
            {
                command.Parameters.AddWithValue("@url", entry.Url);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read()) entry.Exists = true;
                }
            }
            using (var command = entry.Exists
                ? new SQLiteCommand(
                    "UPDATE " + what + "_cache SET " + what + "=@data, last_modified = @lm, last_checked = @cr where url = @url", con)
                : new SQLiteCommand(
                    "INSERT INTO " + what + "_cache (" + what + ", last_modified, last_checked, url) VALUES (@data, @lm, @cr, @url)",
                    con))
            {
                entry.LastChecked = DateTime.Now;
                if (entry is DataCacheEntry)
                    command.Parameters.AddWithValue("@data", (entry as DataCacheEntry).Data);
                else
                    command.Parameters.AddWithValue("@data", (entry as StringCacheEntry).String);
                command.Parameters.AddWithValue("@lm", entry.LastModified);
                command.Parameters.AddWithValue("@cr", entry.LastChecked);
                command.Parameters.AddWithValue("@url", entry.Url);
                await command.ExecuteNonQueryAsync();
            }
            return;
        }

        private async Task<byte[]> GetBytesAsync(DbDataReader reader, int index)
        {
            using (var stream = new MemoryStream())
            {
                await reader.GetStream(index).CopyToAsync(stream);
                return stream.ToArray();
            }
        }

        public string GetFileNameFromUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) return url;
            if (uri.Scheme != "http" && uri.Scheme != "https") return url;

            string path = Path.Combine(_path, uri.Scheme, uri.Host) + uri.LocalPath;

            if (Path.GetFileName(path) == "")
            {
                if (path.EndsWith("/") || path.EndsWith("\\"))
                    path = path.Substring(0, path.Length - 1) + ".txt";
                return path;
            }
            if (!String.IsNullOrEmpty(uri.Query))
                path += "#" + Uri.EscapeDataString(uri.Query);
            return path;
        }
    }
}
