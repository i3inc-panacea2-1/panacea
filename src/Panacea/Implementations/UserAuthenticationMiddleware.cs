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

        public Task<Uri> OnBeforeRequest(Uri uri)
        {
            var ret = new Uri(uri.GetLeftPart(UriPartial.Authority) + $"/{_userService.User.Id ?? "0"}" + uri.PathAndQuery);
            return Task.FromResult(ret);
        }

        public Task OnAfterRequest()
        {
            return Task.CompletedTask;
        }
    }
}
