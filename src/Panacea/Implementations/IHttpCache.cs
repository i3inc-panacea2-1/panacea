using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    public interface IHttpCache
    {
        Task<StringCacheEntry> GetStringAsync(string url);
        Task<DataCacheEntry> GetDataAsync(string url);

        Task SetDataAsync(DataCacheEntry entry);
        Task SetStringAsync(StringCacheEntry entry);

        string GetFileNameFromUrl(string url);
    }

    public abstract class CacheEntry
    {
        public DateTime LastModified { get; set; }

        public DateTime LastChecked { get; set; }

        public string Url { get; set; }

        public bool Exists { get; set; }
    }

    public class DataCacheEntry : CacheEntry
    {
        public byte[] Data { get; set; }
    }

    public class StringCacheEntry : CacheEntry
    {
        public string String { get; set; }
    }
}
