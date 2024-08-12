using System.Text;
using System.Text.Json;

namespace Project1
{
    internal class Template
    {
        internal const string TEMPLATE_FOLDER_NAME = "templates";
        internal const string TEMPLATE_CONTROLLER_FILE = "controller.html";
        internal const string TEMPLATE_CONTROLLER_SETTINGS_DELIMITER = "<%settings%>";

        public static byte[]? CreateController(string settings)
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

            fileContent = fileContent.Replace(
                TEMPLATE_CONTROLLER_SETTINGS_DELIMITER,
                settings
             );

            return Encoding.UTF8.GetBytes(fileContent);
        }

    }
}
