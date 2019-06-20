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
    public class UserService : HttpServiceBase, IUserService
    {
        private readonly ILogger _logger;

        public event UserEvent UserLoggedIn;
        public event UserEvent UserLoggedOut;

        public IUser User { get; private set; }

        public UserService(IHttpClient client, ILogger logger) : base(client)
        {
            _logger = logger;
            User = new User()
            {
                Id = null
            };
        }

        protected Task OnUserLoggedIn(IUser user)
        {
            return UserLoggedIn?.Invoke(user);
        }

        protected Task OnUserLoggedOut(IUser user)
        {
            return UserLoggedOut?.Invoke(user);
        }

        public async Task SetUser(User value)
        {
            var previousUser = User;
            if (User == value && value != null) return;
            if (value == null)
            {
                //_events.TriggerEvent(EventType.UserLoggingOut);
                if (User?.Id != null)
                {
                    if (File.Exists(GetPath("user.txt")))
                    {
                        try
                        {
                            File.Delete(GetPath("user.txt"));
                        }
                        catch
                        {
                        }
                    }
                    try
                    {
                        await _client.GetObjectAsync<object>("logout_user/", new List<KeyValuePair<string, string>>());
                    }
                    catch
                    {
                    }

                }
                User = new User() { LastName = "Guest", FirstName = "User" };
                if (previousUser != null) await OnUserLoggedOut(previousUser);
                await OnUserLoggedOut(User);
            }
            else
            {
                User = value;
                SaveUserFile();
                await OnUserLoggedIn(value);
            }
        }

        private string GetPath(params string[] args)
        {
            return Path.Combine(new string[] { Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) }.Concat(args).ToArray());
        }

        private Task<bool> SignUserIn(string dob, string email, string pass)
        {
            var data = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("e_mail", email),
                    new KeyValuePair<string, string>("password", pass),
                    new KeyValuePair<string, string>("date_of_birth", dob)
                };
            return DoSignInRequest("login_user/", data);

        }

        public Task<bool> LoginAsync(DateTime dateOfBirth, string password)
        {
            return SignUserIn(dateOfBirth.ToString("yyyy-MM-dd"), null, password);
        }

        public Task<bool> LoginAsync(string email, string password)
        {
            return SignUserIn(null, email, password);
        }

        public Task<bool> LoginAsync(string cardId)
        {
            var data = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("code", cardId)
            };
            return DoSignInRequest("login_user_with_code/", data);
        }

        private async Task<bool> DoSignInRequest(string url, List<KeyValuePair<string, string>> data)
        {

            var response = await _client.GetObjectAsync<UserModel>(
                url,
                allowCache: false,
                postData: data
                );
            if (response.Success)
            {
                await SetUser(new User(response.Result));
                return true;
            }
            else
            {
                return false;
            }
        }

        internal async Task LoginFromFileAsync()
        {
            if (File.Exists(GetPath("user.txt")))
            {
                try
                {
                    DateTime dob;
                    string pass = null;
                    using (var sr = new StreamReader(GetPath("user.txt")))
                    {
                        dob = DateTime.Parse(sr.ReadLine());
                        pass = sr.ReadLine();
                    }
                    await LoginAsync(dob, pass);
                    if (User?.Id == null)
                    {
                        await SetUser(null);
                    }
                }
                catch
                {
                }
            }
        }

        public Task LogoutAsync()
        {
            return SetUser(null);
        }

        public async Task<bool> UpdateUserInfoAsync(string firstName, string lastName, string phoneNumber)
        {
            var response = await _client.GetObjectAsync<object>(
                "update_user/",
                postData: new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("first_name", firstName),
                    new KeyValuePair<string, string>("last_name", lastName),
                    new KeyValuePair<string, string>("phonenumber", phoneNumber)
                });
            if (response.Success)
            {
                User u = new User();
                u.DateOfBirth = User.DateOfBirth;
                u.Email = User.Email;
                u.Id = User.Id;
                u.IsAnonymous = User.IsAnonymous;
                u.Telephone = phoneNumber;
                u.LastName = lastName;
                u.FirstName = firstName;
                await SetUser(u);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal void SaveUserFile()
        {
            try
            {
                if (User?.Id == null) return;

                using (var sw = new StreamWriter(GetPath("user.txt")))
                {
                    sw.WriteLine(User.DateOfBirth.ToUniversalTime().ToString("O"));
                    sw.WriteLine(User.Password);
                }
            }
            catch
            {
                try
                {
                    File.Delete(GetPath("user.txt"));
                }
                catch
                {
                    //ignore
                }
            }
        }
    }
}
