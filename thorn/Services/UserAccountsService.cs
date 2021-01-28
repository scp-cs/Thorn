using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using thorn.UserAccounts;

namespace thorn.Services
{
    public class UserAccountsService
    {
        private const string FilePath = "Config/accounts.json";

        private readonly DataStorageService _data;
        private readonly ILogger<UserAccountsService> _logger;
        private List<UserAccount> _accounts;

        public UserAccountsService(DataStorageService data, ILogger<UserAccountsService> logger)
        {
            _data = data;
            _logger = logger;
            _accounts = DataStorageService.LoadUserAccounts(FilePath);
        }

        public async Task SaveAccountsAsync() => await _data.SaveAllUserAccounts(_accounts, FilePath);

        public Task LoadAccountsAsync()
        {
            _accounts = DataStorageService.LoadUserAccounts(FilePath);
            _logger.LogInformation("User Account data loaded");
            return Task.CompletedTask;
        }

        public async Task<UserAccount> GetAccountAsync(SocketUser user) => await GetAccountAsync(user.Id);

        public async Task<UserAccount> GetAccountAsync(ulong id) => await GetOrCreateAccount(id);

        private async Task<UserAccount> GetOrCreateAccount(ulong id)
        {
            var result = from a in _accounts where a.Id == id select a;
            var account = result.FirstOrDefault() ?? await CreateAccountAsync(id);
            return account;
        }

        private async Task<UserAccount> CreateAccountAsync(ulong id)
        {
            var newAccount = new UserAccount(id);

            _accounts.Add(newAccount);
            _logger.LogInformation($"Created account for user {id}");

            await SaveAccountsAsync();
            return newAccount;
        }

        public IEnumerable<UserAccount> GetTypeLeaderboard(PointType type)
        {
            var a = _accounts.OrderByDescending(x => x.Points[type]).ToList();
            a.RemoveAll(x => x.Points[type] == 0);
            return a;
        }

        public IEnumerable<UserAccount> GetLeaderboard() =>
            _accounts.OrderByDescending(x => x.Points.Sum(y => y.Value)).ToList();

        public async Task UpdateRanks(PointType type)
        {
            foreach (var account in GetTypeLeaderboard(type).Select((value, index) => new {value, index}))
                account.value.Ranks[type] = account.index + 1;
            await SaveAccountsAsync();
        }
    }
}