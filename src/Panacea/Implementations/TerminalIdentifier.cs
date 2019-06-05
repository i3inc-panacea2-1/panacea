using Microsoft.Win32;
using Panacea.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    class TerminalIdentifier
    {
        private readonly ISerializer _serializer;

        public TerminalIdentifier(ISerializer serializer)
        {
            _serializer = serializer;
        }

        private Task<string> GetFilePathAsync()
        {
            return Task.Run(() =>
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"software\panacea"))
                {
                    var tisFilePath = Path.GetPathRoot(Assembly.GetExecutingAssembly().Location);
                    string path = null;
                    if ((path = key?.GetValue("TisFilePath", null)?.ToString()) != null)
                    {
                        tisFilePath = path;
                    }
                    return Path.Combine(tisFilePath, "tis.txt");
                }
            });
        }
        public async Task<string> GetIdentifierAsync()
        {
            var file = await GetFilePathAsync();
            if (File.Exists(file))
            {
                using (var reader = new StreamReader(file))
                {
                    var json = await reader.ReadToEndAsync();
                    var identificationInfo = _serializer.Deserialize<IdentificationInfo>(json);
                    return identificationInfo.Putik;
                }
            }
            return null;
        }
    }

    public class IdentificationInfo
    {
        public string Putik { get; set; }

        public string PrivateKey { get; set; }

        public string PublicKey { get; set; }
    }
}
