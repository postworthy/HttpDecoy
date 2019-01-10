using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.HttpOverrides;

namespace HttpDecoy.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var notifyUrl = Environment.GetEnvironmentVariable("NOTIFY_URL");
            if (string.IsNullOrEmpty(notifyUrl))
                return Error();

            var data = new
            {
                Headers = Request.Headers.Select(x => new { x.Key, x.Value }),
                Cookies = Request.Cookies.Select(x => new { x.Key, x.Value }),
                Form = Request.HasFormContentType ? Request.Form.Select(x => new { x.Key, x.Value }) : null,
                Query = Request.Query.Select(x => new { x.Key, x.Value }),
                Host = Request.Host.ToString(),
                Path = Request.Path.ToString(),
                RemoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString()
            };

            try
            {
                using (var c = new HttpClient())
                {
                    await c.PostAsJsonAsync(notifyUrl, data);
                }
            }
            catch { }

            return new NotFoundResult();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return new StatusCodeResult(500);
        }
    }
}
