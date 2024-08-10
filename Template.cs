using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Project1
{
    internal class Template
    {
        public enum Players
        {
            One = 1,
            Two = 2,
        }

        public enum Controllers
        {

        }

        public static async Task Handle(HttpListenerContext httpContext)
        {
            byte[]? cTemplate = GetControllerByPlayer(Template.Players.One);

            string? absolutePath = httpContext.Request.Url?.AbsolutePath;
            string? playerPath = null;


            if (absolutePath != null)
            {
                playerPath = absolutePath.Replace("/player-", "");
            }


            if (playerPath != null)
            {
                try
                {
                    int player = int.Parse(playerPath);
                }
                catch
                {
                    httpContext.Response.StatusCode = 404;
                    httpContext.Response.Close();
                }
            }
            else if (cTemplate?.Length > 1)
            {
                httpContext.Response.ContentType = "text/html";
                httpContext.Response.ContentLength64 = cTemplate.Length;
                await httpContext.Response.OutputStream.WriteAsync(cTemplate, 0, cTemplate.Length);
                httpContext.Response.StatusCode = 200;
            }
            else
            {
                httpContext.Response.StatusCode = 404;
            }

            httpContext.Response.Close();
        }

        public static bool CheckPlayerPath(HttpListenerContext httpContext)
        {
            string? absolutePath = null;

            if (httpContext.Request.Url != null)
            {
                absolutePath = httpContext.Request.Url.AbsolutePath;
            }

            return !String.IsNullOrEmpty(absolutePath) && absolutePath.StartsWith("/player");
        }


        public static byte[]? GetControllerByPlayer(Players player)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "controller.html");

            if (!File.Exists(filePath))
            {
                return default;
            }

            string fileContent = File.ReadAllText(filePath);

            if (string.IsNullOrEmpty(fileContent))
            {
                return default;
            }


            fileContent = fileContent.Replace("{{ player }}", ((int)player).ToString());


            return Encoding.UTF8.GetBytes(fileContent);
        }

    }
}
