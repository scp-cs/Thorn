using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace thorn.Services
{
    public class ConstantsService
    {
        private const string ConstantsFile = "Config/constants.json";
        private const string ConstantsFolder = "Config";
        
        public Dictionary<string, ulong> Channels { get; private set; }
        public Dictionary<string, ulong> Roles { get; private set; }
        public List<string> Help { get; set; }
        public dynamic Strings { get; private set; }

        public ConstantsService()
        {
            if (!File.Exists(ConstantsFile) || !Directory.Exists(ConstantsFolder))
                throw new Exception("Constants file does not exist!");
            ReloadConstants();
        }

        public void ReloadConstants()
        {
            var constants = JsonConvert.DeserializeObject<Constants>(File.ReadAllText(ConstantsFile));
            
            Channels = constants.Channels;
            Roles = constants.Roles;
            Help = constants.Help;
            Strings = constants.Strings;
        }

        private struct Constants
        {
            public Dictionary<string, ulong> Channels { get; set; }
            public Dictionary<string, ulong> Roles { get; set; }
            public List<string> Help { get; set; }
            public dynamic Strings { get; set; }
        }
    }
}