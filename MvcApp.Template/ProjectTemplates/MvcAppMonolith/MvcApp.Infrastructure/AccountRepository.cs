using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MvcApp.Core;
using MvcApp.Core.Abstractions;
using MvcApp.Core.Models;

namespace MvcApp.Infrastructure;

public class AccountRepository(UserManager<UserDetails> userManager,
                             SignInManager<UserDetails> signInManager) : IAccountRepository
{
    public async Task<UserDetails?> GetAccountAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username must not be null or empty");
        }

        var normalizedUsername = username.ToUpper();
        return await userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u =>
                u.NormalizedEmail == normalizedUsername
                || u.NormalizedUserName == normalizedUsername);
    }

    public async Task<List<UserDetails>> GetAccountsAsync()
    {
        return await userManager.Users
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<UserDetails> CreateAccountAsync(RegisterUserModel registerUser)
    {
        if (await UserExistsAsync(registerUser.Email))
        {
            throw new ArgumentException($"User is already registered with [{registerUser.Email}]");
        }

        UserDetails appUser = new()
        {
            UserName = registerUser.Username,
            Email = registerUser.Email
        };

        IdentityResult result = await userManager.CreateAsync(appUser, registerUser.Password);

        if (result.Succeeded == false)
        {
            throw new Exception($"Failed to register user [{appUser.Email}] -- {result}");
        }

        return appUser;
    }

    public async Task<UserDetails> LoginAsync(LoginUserModel loginUser)
    {
        var normalizedUsername = loginUser.Username.ToUpper();
        UserDetails? appUser = await userManager.Users
            .SingleOrDefaultAsync(u =>
                u.NormalizedEmail == normalizedUsername
                || u.NormalizedUserName == normalizedUsername) ?? throw new ArgumentException($"Invalid user [{loginUser.Username}]");
        SignInResult result = await signInManager.CheckPasswordSignInAsync(
                appUser, loginUser.Password, false);

        if (result.Succeeded == false)
        {
            throw new Exception($"Invalid password for user [{loginUser.Username}]");
        }

        return appUser;
    }

    public async Task UpdateAccountAsync(AccountUpdateModel updateAccount)
    {
        if (updateAccount is null)
        {
            throw new ArgumentException("Account for update must not be null");
        }
        else if (string.IsNullOrWhiteSpace(updateAccount.UserName))
        {
            throw new ArgumentException("Username cannot be null or empty");
        }
        else if (string.IsNullOrWhiteSpace(updateAccount.Email))
        {
            throw new ArgumentException("Email cannot be null or empty");
        }

        UserDetails? user = await userManager.Users
            .SingleOrDefaultAsync(u => u.Id == updateAccount.Id) ?? throw new ArgumentException($"Account not found for update [{updateAccount.Id}/{updateAccount.UserName} - {updateAccount.Email}]");
        user.Email = updateAccount.Email;
        user.UserName = updateAccount.UserName;
    }

    public async Task<IdentityResult> DeleteAccountAsync(string requestor, string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username must not be null or empty");
        }

        var normalizedUsername = username.ToUpper();
        UserDetails? appUser = await userManager.Users
            .SingleOrDefaultAsync(u =>
            u.NormalizedUserName == normalizedUsername
            || u.NormalizedEmail == normalizedUsername);

        if (appUser is null)
        {
            throw new Exception("Username not found");
        }

        if (appUser.UserName!.Equals(requestor, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Unable to delete your own account");
        }

        return await userManager.DeleteAsync(appUser);
    }

    private async Task<bool> UserExistsAsync(string username)
    {
        var normalizedUsername = username.ToUpper();
        return await userManager.Users.AnyAsync(e => e.NormalizedEmail == normalizedUsername);
    }
}
