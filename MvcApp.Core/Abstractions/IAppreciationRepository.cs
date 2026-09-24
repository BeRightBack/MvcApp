using MvcApp.Core.Models;

namespace MvcApp.Core.Abstractions;

public interface IAppreciationRepository
{
    Task<ProfileAppreciation?> GetAppreciationAsync(string sourceUserId, string targetUserId);
    Task<double> GetAverageScoreAsync(string targetUserId);
    Task<int> GetTotalAppreciationsAsync(string targetUserId);
    Task<bool> AddAppreciationAsync(ProfileAppreciation appreciation);
}
