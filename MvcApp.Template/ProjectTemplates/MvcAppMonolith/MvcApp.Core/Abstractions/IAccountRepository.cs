using Microsoft.AspNetCore.Identity;
using MvcApp.Core;
using MvcApp.Core.Models;

namespace MvcApp.Core.Abstractions;

public interface IAccountRepository
{
    Task<UserDetails?> GetAccountAsync(string username);
    Task<List<UserDetails>> GetAccountsAsync();
    Task<UserDetails> CreateAccountAsync(RegisterUserModel registerUser);
    Task<UserDetails> LoginAsync(LoginUserModel loginUser);
    Task UpdateAccountAsync(AccountUpdateModel updateAccount);
    Task<IdentityResult> DeleteAccountAsync(string requestor, string username);
}
