using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net.Mail;

namespace HttpDecoy.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var artifactUrl = Environment.GetEnvironmentVariable("ARTIFACT_URL");
            var notifyUrl = Environment.GetEnvironmentVariable("NOTIFY_URL");
            var emailServer = Environment.GetEnvironmentVariable("EMAIL_SERVER");
            var emailFrom = Environment.GetEnvironmentVariable("EMAIL_FROM");
            var emailPassword = Environment.GetEnvironmentVariable("EMAIL_FROM_PASSWORD");
            var emailTo = Environment.GetEnvironmentVariable("EMAIL_TO");

            bool.TryParse(Environment.GetEnvironmentVariable("ENABLE_SSL"), out bool enableSsl);

            if (string.IsNullOrEmpty(notifyUrl) && (string.IsNullOrEmpty(emailServer) || string.IsNullOrEmpty(emailFrom) || string.IsNullOrEmpty(emailTo)))
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

            Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));

            if (!string.IsNullOrEmpty(notifyUrl))
            {
                try
                {
                    using (var c = new HttpClient())
                    {
                        c.Timeout = new TimeSpan(0, 0, 15);
                        await c.PostAsJsonAsync(notifyUrl, data);
                    }
                }
                catch (Exception ex) { Console.Error.Write(ex.ToString()); }
            }

            if (!(string.IsNullOrEmpty(emailServer) || string.IsNullOrEmpty(emailFrom)))
            {
                try
                {
                    MailMessage msg = new MailMessage
                    {
                        From = new MailAddress(emailFrom),
                        Subject = "HttpDecoy Notification",
                        Body = JsonConvert.SerializeObject(data, Formatting.Indented)
                    };
                    msg.To.Add(emailTo ?? emailFrom);
                    SmtpClient smtp = new SmtpClient
                    {
                        Host = emailServer.Split(':').First(),
                        Port = emailServer.Split(':').Count() == 2 ? int.Parse(emailServer.Split(':').Last()) : 25,
                        EnableSsl = false
                    };
                    if (!string.IsNullOrEmpty(emailPassword))
                        smtp.Credentials = new System.Net.NetworkCredential(emailFrom, emailPassword);

                    smtp.Send(msg);
                }
                catch (Exception ex) { Console.Error.Write(ex.ToString()); }
            }

            if (!string.IsNullOrEmpty(artifactUrl))
            {
                try
                {
                    using (var c = new HttpClient())
                    {
                        var filename = System.Web.MimeMapping.GetFileName(artifactUrl).Trim(new[] { '\\', '/' });
                        var mime = System.Web.MimeMapping.GetMimeMapping(artifactUrl);
                        var artifact = new FileStreamResult(await c.GetStreamAsync(artifactUrl), mime) { FileDownloadName = filename };
                        return artifact;
                    }
                }
                catch (Exception ex) { Console.Error.Write(ex.ToString()); }
            }

            return new NotFoundResult();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return new StatusCodeResult(500);
        }
    }
}
