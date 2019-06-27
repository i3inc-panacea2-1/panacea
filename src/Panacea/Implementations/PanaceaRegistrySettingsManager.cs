using Microsoft.Win32;
using Panacea.Core;
using Panacea.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    public class PanaceaRegistrySettingsManager : IPanaceaSettingsManager
    {
        private readonly ISerializer _serializer;

        public PanaceaRegistrySettingsManager(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public async Task<ServerInformation> GetRegistrationInfo()
        {
            return await Task.Run(() =>
            {
                using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("Software", false))
                using (var panacea = key?.OpenSubKey("Panacea"))
                {
                    if (panacea == null)
                    {
                        //if (throwException) throw new Exception("Terminal reg key is missing...");
                        return new ServerInformation();
                    }
                    var ts = panacea.GetValue("TerminalServer", null);
                    var hs = panacea.GetValue("HospitalServer", null);
                    var responseText = panacea.GetValue("TerminalServerResponse", null);
                    var noupdate = (int)panacea.GetValue("NoUpdate", 0);
                    var runtimepath = (string)panacea.GetValue("RuntimePath", "");
                    if (ts == null)
                        ts = "http://management.i3panacea.com:1337/";

                    return new ServerInformation()
                    {
                        ManagementServerResponse =
                            responseText != null
                                ? _serializer.Deserialize<ServerResponse<GetHospitalServersResponse>>(responseText.ToString())
                                : null
                    };
                }
            });
        }


        public async Task<string> GetTerminalServer()
        {
            return await Task.Run(() =>
            {

                using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("Software", false))
                using (var panacea = key?.OpenSubKey("Panacea"))
                {
                    if (panacea == null)
                    {
                        return "http://management.i3panacea.com:1337/";
                    }
                    var ts = panacea.GetValue("TerminalServer", null);
                    if (ts == null)
                        ts = "http://management.i3panacea.com:1337/";

                    return ts.ToString();
                }
            });
        }


        public async Task UpdateRegistrationInformation(ServerInformation info)
        {
            await Task.Run(() =>
            {

                using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("Software", true))
                using (var panacea = key?.CreateSubKey("Panacea"))
                {
                    panacea.SetValue("TerminalServerResponse", _serializer.Serialize(info.ManagementServerResponse));
                }
            });
        }

        public async Task SetManagementServer(string url)
        {
            await Task.Run(() =>
            {
                using (var key = Registry.LocalMachine.OpenSubKey("Software", true))
                using (var panacea = key?.CreateSubKey("Panacea"))
                {
                    panacea.SetValue("TerminalServer", url);
                }
            });
        }
    }


}
