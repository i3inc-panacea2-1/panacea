using Panacea.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Interfaces
{
    public interface IPanaceaSettingsManager
    {
        Task<ServerInformation> GetRegistrationInfo();

        Task<string> GetTerminalServer();

        Task UpdateRegistrationInformation(ServerInformation info);

        Task SetManagementServer(string url);

    }


    public class ServerInformation
    {
        public string HospitalServer { get => ManagementServerResponse?.Result?.HospitalServers[0]; }

        public ServerResponse<GetHospitalServersResponse> ManagementServerResponse { get; set; }
    }

    [DataContract]
    public class GetHospitalServersResponse
    {
        [DataMember(Name = "teamviewer_id")]
        public string TeamviewerId { get; set; }

        [DataMember(Name = "hospital_servers")]
        public List<string> HospitalServers { get; set; }

        [DataMember(Name = "crutch")]
        public string Crutch { get; set; }

        [DataMember(Name = "terminal_type")]
        public TerminalType TerminalType { get; set; }
    }


    [DataContract]
    public class TerminalType
    {
        [DataMember(Name = "pairs")]
        public string Pairs { get; set; }

    }

}
