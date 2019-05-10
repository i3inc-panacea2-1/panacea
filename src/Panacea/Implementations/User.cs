using Panacea.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    public class User : PropertyChangedBase, IUser
    {
        public User()
        {
            
        }

        public User(UserModel userModel) : this()
        {
            FirstName = userModel.FirstName;
            LastName = userModel.LastName;
            DateOfBirth = userModel.DateOfBirth;
            Telephone = userModel.Telephone;
            Password = userModel.Password;
            Id = userModel.ID;
            Email = userModel.Email;
        }

        private string _firstName;
        public string FirstName
        {
            get { return _firstName; }
            set
            {
                _firstName = value;
                OnPropertyChanged("FirstName");
            }
        }

        private string _lastName;
        public string LastName
        {
            get { return _lastName; }
            set
            {
                _lastName = value;
                OnPropertyChanged("LastName");
            }
        }

        private string _email;
        public string Email
        {
            get { return _email; }
            set
            {
                _email = value;
                OnPropertyChanged("Email");
            }
        }
        public string Id { get; set; }

        public string Password { get; set; }

        private string _telephone;
        public string Telephone
        {
            get { return _telephone; }
            set
            {
                _telephone = value;
                OnPropertyChanged("Telephone");
            }
        }
        public DateTime DateOfBirth { get; set; }

        public bool IsAnonymous { get; set; }

    }
}
