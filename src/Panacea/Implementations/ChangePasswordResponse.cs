using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    [DataContract]
    public class ChangePasswordResponse
    {
        [DataMember(Name = "password")]
        public string Password { get; set; }
    }
}
