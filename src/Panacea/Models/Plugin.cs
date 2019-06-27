using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Models
{
    [DataContract]
    public class Plugin
    {

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "className")]
        public string ClassName { get; set; }

        [DataMember(Name = "ns")]
        public string Namespace { get; set; }

        [DataMember(Name = "file")]
        public string FileName { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}
