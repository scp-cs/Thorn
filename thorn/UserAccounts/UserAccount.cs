using System;
using System.Collections.Generic;
using System.Text;

namespace thorn.UserAccounts
{
    public class UserAccount
    { 
        // Internal
        public ulong Id { get; set; }
        public Dictionary<PointType, float> Points { get; set; }
        public Dictionary<PointType, int> Ranks { get; set; }
        
        // User settable
        // ReSharper disable once MemberCanBePrivate.Global
        // This has to be public because of the JsonSerializer
        public Dictionary<AccountItem, string> Properties { get; set; }

        public UserAccount(ulong id)
        {
            Id = id;
            Points = new Dictionary<PointType, float>();
            Ranks = new Dictionary<PointType, int>();
            Properties = new Dictionary<AccountItem, string>();

            foreach (var type in (PointType[]) Enum.GetValues(typeof(PointType)))
            {
                Points.Add(type, 0);
                Ranks.Add(type, 0);
            }

            foreach (var type in (AccountItem[]) Enum.GetValues(typeof(AccountItem)))
                Properties.Add(type, null);
        }

        public override string ToString()
        {
            var description = new StringBuilder();

            if (this[AccountItem.Description] != null)
                description.Append(this[AccountItem.Description] + "\n\n");

            // This is for *our* based god, the one and only, ~Utylike~
            switch (Points[PointType.Translation] > 0)
            {
                case true when Id != 227114285074087938:
                    description.Append($"**Překladatelské body:** `{Points[PointType.Translation]}`\n" +
                                       $"**Pořadí v žebříčku:** `#{Ranks[PointType.Translation]}`\n\n");
                    break;
                case true when Id == 227114285074087938:
                    description.Append("**Překladatelské body:** `ano`\n" +
                                       "**Pořadí v žebříčku:** `#1`\n\n");
                    break;
            }

            if (Points[PointType.Writing] > 0)
                description.Append($"**Spisovatelské body:** `{Points[PointType.Writing]}`\n" +
                                   $"**Pořadí v žebříčku:** `#{Ranks[PointType.Writing]}`\n\n");
            
            if (Points[PointType.Correction] > 0)
                description.Append($"**Korektorské body:** `{Points[PointType.Correction]}`\n" +
                                   $"**Pořadí v žebříčku:** `#{Ranks[PointType.Correction]}`\n\n");
            
            if (this[AccountItem.TranslatorPage] != null && this[AccountItem.PrivatePage] == null)
                description.Append($"[Překladatelská složka]({this[AccountItem.TranslatorPage]})\n");
            
            if (this[AccountItem.AuthorPage] != null && this[AccountItem.PrivatePage] == null)
                description.Append($"[Spisovatelská složka]({this[AccountItem.AuthorPage]})\n");
            
            if (this[AccountItem.PrivatePage] != null)
                description.Append($"[Osobní složka]({this[AccountItem.PrivatePage]})\n");
            
            if (this[AccountItem.Sandbox] != null)
                description.Append($"[Sandbox]({this[AccountItem.Sandbox]})\n");
            
            if (this[AccountItem.WikidotUsername] != null)
                description.Append("[Wikidot profil](https://wikidot.com/user:info/" +
                                   $"{this[AccountItem.WikidotUsername].ToLower().Replace(" ", "-")})");

            return description.ToString();
        }

        public string this[AccountItem item]
        {
            get => Properties[item];
            set => Properties[item] = value;
        }
    }
}