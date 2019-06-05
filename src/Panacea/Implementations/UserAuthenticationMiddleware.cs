using Panacea.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    public class UserAuthenticationMiddleware:IHttpMiddleware
    {

        private readonly IUserService _userService;

        public UserAuthenticationMiddleware(IUserService userService)
        {
            _userService = userService;
        }

        public Uri OnBeforeRequest(Uri uri)
        {
            return new Uri(uri.GetLeftPart(UriPartial.Authority) + $"/{_userService.User.Id ?? "0"}" + uri.PathAndQuery);
        }

        public Task OnAfterRequest()
        {
            return Task.CompletedTask;
        }
    }
}
