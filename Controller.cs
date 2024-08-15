using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Project1
{
    internal class Controller
    {
        public enum Controllers
        {
            One = 1,
            Two = 2,
        }

        public enum Commands
        {
            Select,
            Start,
            Up,
            Down,
            Left,
            Right,
            A,
            B,
            Y,
            X
        }
        public enum Actions
        {
            Up,
            Down
        }

        private class ControllerSettings
        {
            public Controllers controller { get; set; }
            public string? token { get; set; }
            public object? commands { get; set; }
            public object? actions { get; set; }
        }


        private static Dictionary<Controllers, string> controllerTokens = [];

        internal const string PATH = "/controller";

        public static async Task Handle(HttpListenerContext httpContext)
        {
            Controllers? controller = Resolve(httpContext);
            HttpListenerResponse resp = httpContext.Response;

            if (controller.HasValue)
            {
                string? storageToken = GetToken(controller.Value);

                Console.WriteLine($"Token já tem => {storageToken}");


                var controllerToken = Guid.NewGuid().ToString();
                SetToken(controller.Value, controllerToken);

                var controllerSettings = JsonSerializer.Serialize(new ControllerSettings
                {
                    controller = controller.Value,
                    token = Guid.NewGuid().ToString(),
                    commands = Enum.GetValues(typeof(Commands))
                               .Cast<Commands>()
                               .ToDictionary(
                                   key => key.ToString().ToLower(),
                                   value => (int)value
                               ),
                    actions = Enum.GetValues(typeof(Actions))
                              .Cast<Actions>()
                              .ToDictionary(
                                  key => key.ToString().ToLower(),
                                  value => (int)value
                              )
                });

                byte[]? template = Template.CreateController(controllerSettings);

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

        private static Controllers? Resolve(HttpListenerContext httpContext)
        {
            string? absolutePath = httpContext.Request.Url?.AbsolutePath;

            if (absolutePath is null || !absolutePath.StartsWith($"{PATH}-"))
            {
                return default;
            }

            string controllerPath = absolutePath[$"{PATH}-".Length..];

            if (int.TryParse(controllerPath, out int controller) && IsValidController(controller))
            {
                return (Controllers)controller;
            }

            return default;
        }

        public static string[] GetPaths()
        {
            return Enum.GetValues(typeof(Controllers))
                   .Cast<Controllers>()
                   .Select(e => $"{PATH}-{(int)e}")
                   .ToArray();
        }

        public static bool MatchPath(HttpListenerContext httpContext)
        {
            string? absolutePath = null;

            if (httpContext.Request.Url != null)
            {
                absolutePath = httpContext.Request.Url.AbsolutePath;
            }

            return !String.IsNullOrEmpty(absolutePath) && absolutePath.StartsWith(PATH);
        }

        private static void SetToken(Controllers controller, string token)
        {
            controllerTokens[controller] = token;
        }

        private static string? GetToken(Controllers controller)
        {

            try
            {
                if (controllerTokens.TryGetValue(controller, out string? token))
                {
                    return token;
                }
                else
                {
                    return default;
                }
            }
            catch
            {
                return default;
            }
        }

        public static bool IsValidController(int controller)
        {
            return Enum.IsDefined(typeof(Controllers), controller);
        }

        public static bool IsValidCommand(int command)
        {
            return Enum.IsDefined(typeof(Commands), command);
        }

        public static bool IsValidAction(int action)
        {
            return Enum.IsDefined(typeof(Actions), action);
        }

    }
}
