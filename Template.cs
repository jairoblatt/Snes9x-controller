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

        internal const string PATH_MATCH = "/controller";
        internal const string TEMPLATE_FOLDER_NAME = "templates";
        internal const string TEMPLATE_CONTROLLER_FILE = "controller.html";
        internal const string TEMPLATE_CONTROLLER_DELIMITER_PLAYER = "<%player%>";

        public static async Task Handle(HttpListenerContext httpContext)
        {
            Players? player = ResolvePlayer(httpContext);
            HttpListenerResponse resp = httpContext.Response;

            if (player.HasValue)
            {
                byte[]? template = GetControllerByPlayer(player.Value);

                if (template != null)
                {
                    resp.StatusCode = 200;
                    resp.ContentType = "text/html";
                    resp.ContentLength64 = template.Length;
                    await resp.OutputStream.WriteAsync(template);
                }
                else
                {
                    resp.StatusCode = 404;
                }
            }
            else
            {
                resp.StatusCode = 404;
            }

            resp.Close();
        }

        public static List<string> GetControllersPath()
        {
            List<string> paths = [];

            foreach (Players player in Enum.GetValues(typeof(Players)))
            {
                paths.Add($"{PATH_MATCH}-{(int)player}");
            }

            return paths;
        }

        private static Players? ResolvePlayer(HttpListenerContext httpContext)
        {
            string? absolutePath = httpContext.Request.Url?.AbsolutePath;

            if (absolutePath is null || !absolutePath.StartsWith($"{PATH_MATCH}-"))
            {
                return default;
            }

            string playerPath = absolutePath[$"{PATH_MATCH}-".Length..];

            if (int.TryParse(playerPath, out int player) && Enum.IsDefined(typeof(Players), player))
            {
                return (Players)player;
            }

            return default;
        }

        public static bool CheckPlayerPath(HttpListenerContext httpContext)
        {
            string? absolutePath = null;

            if (httpContext.Request.Url != null)
            {
                absolutePath = httpContext.Request.Url.AbsolutePath;
            }

            return !String.IsNullOrEmpty(absolutePath) && absolutePath.StartsWith(PATH_MATCH);
        }


        public static byte[]? GetControllerByPlayer(Players player)
        {
            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                TEMPLATE_FOLDER_NAME,
                TEMPLATE_CONTROLLER_FILE
             );

            if (!File.Exists(filePath))
            {
                return default;
            }

            string fileContent = File.ReadAllText(filePath);

            if (string.IsNullOrEmpty(fileContent))
            {
                return default;
            }


            fileContent = fileContent.Replace(TEMPLATE_CONTROLLER_DELIMITER_PLAYER, ((int)player).ToString());


            return Encoding.UTF8.GetBytes(fileContent);
        }

    }
}
