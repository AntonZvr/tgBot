using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1
{
    public class Configuration
    {
        public string BotToken { get; set; }

        public static Configuration LoadConfiguration()
        {
            string configPath = "config.json";
            string configJson = File.ReadAllText(configPath);
            return JObject.Parse(configJson).ToObject<Configuration>();
        }
    }
}
