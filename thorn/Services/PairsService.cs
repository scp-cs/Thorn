using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace thorn.Services
{
    public class PairsService
    {
        private const string StringsFile = "Config/pairs.json";
        private const string StringsFolder = "Config";
        private Dictionary<string, string> _pairs;

        public PairsService()
        {
            if (!File.Exists(StringsFile) || !Directory.Exists(StringsFolder))
                throw new Exception("Pair file does not exist!");
            ReloadStrings();
        }

        public string GetString(string key)
        {
            if (_pairs.ContainsKey(key)) return _pairs[key];
            Console.WriteLine($"INVALID KEY: {key}");
            return string.Empty;
        }

        public Task ReloadStrings()
        {
            _pairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(StringsFile));
            return Task.CompletedTask;
        }
    }
}