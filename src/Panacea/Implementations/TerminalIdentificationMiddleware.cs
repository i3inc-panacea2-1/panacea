﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    public class TerminalIdentificationMiddleware : IHttpMiddleware
    {
        private readonly string _putik;

        public TerminalIdentificationMiddleware(string putik)
        {
            _putik = putik;
        }
        public Task OnAfterRequest()
        {
            return Task.CompletedTask;
        }

        public Uri OnBeforeRequest(Uri uri)
        {
            return new Uri(uri.GetLeftPart(UriPartial.Authority) + $"/api/{_putik}/test" + uri.PathAndQuery);
        }
    }
}
