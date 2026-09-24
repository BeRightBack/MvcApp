namespace MvcApp.Core.Abstractions;

public interface IVerificationCodeRepository
{
    Task<VerificationCodeRecord> CreateAsync(VerificationCodeRecord record);
    Task<VerificationCodeRecord?> GetActiveByUserIdAsync(string userId);
    Task<VerificationCodeRecord?> GetByIdAsync(string id);
    Task<bool> MarkAsUsedAsync(string id, string? ipAddress = null, string? userAgent = null);
    Task CleanupExpiredAsync();
    Task<VerificationCodeRecord?> GetByEncryptedCodeAsync(string encryptedCode);
}
