using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Models
{
    [DataContract]
    public class GetVersionsResponse
    {
        [DataMember(Name = "plugins")]
        public List<Plugin> Plugins { get; set; }

        [DataMember(Name = "custom_settings")]
        public string CustomSettings { get; set; }
    }
}
