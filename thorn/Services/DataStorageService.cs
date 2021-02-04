using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using thorn.UserAccounts;

namespace thorn.Services
{
    public class DataStorageService
    {
        private readonly JsonSerializerSettings _settings;

        public DataStorageService()
        {
            _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include
            };
        }

        public Task SaveAllUserAccounts(IEnumerable<UserAccount> accounts, string filePath)
        {
            var json = JsonConvert.SerializeObject(accounts, _settings);

            File.WriteAllText(filePath, json);
            return Task.CompletedTask;
        }

        public Dictionary<string, T> GetDictionary<T>(string path) =>
            JsonConvert.DeserializeObject<Dictionary<string, T>>(File.ReadAllText(path));

        public static List<UserAccount> LoadUserAccounts(string filePath)
        {
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
                throw new Exception("Save file does not exist");
            }

            var json = File.ReadAllText(filePath);
            var accounts = JsonConvert.DeserializeObject<List<UserAccount>>(json);
            return accounts ?? new List<UserAccount>();
        }
    }
}