using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    [DataContract]
    public class UserModel
    {
        [DataMember(Name = "first_name")]
        public string FirstName { get; set; }

        [DataMember(Name = "last_name")]
        public string LastName { get; set; }

        [DataMember(Name = "e_mail")]
        public string Email { get; set; }

        [DataMember(Name = "_id")]
        public string ID { get; set; }

        [DataMember(Name = "password")]
        public string Password { get; set; }

        [DataMember(Name = "phonenumber")]
        public string Telephone { get; set; }

        [DataMember(Name = "date_of_birth")]
        public DateTime DateOfBirth { get; set; }

    }
}
