using MvcApp.Core;
using MvcApp.Core.Models;
using MvcApp.Core.Pagination;

namespace MvcApp.Core.Abstractions;

public interface IMemberRepository
{
    void UpdateMember(UserDetails user);
    Task<IEnumerable<UserDetails>> GetMembersAsync();
    Task<UserDetails?> GetMemberByIdAsync(string id);
    Task<UserDetails?> GetMemberByUsernameAsync(string username);
    Task<PaginationList<MemberModel>> GetMembersAsync(MemberParameters userParameters);
    Task<MemberModel?> GetMemberAsync(string username);
}
