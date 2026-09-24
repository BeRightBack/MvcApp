using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MvcApp.Core;
using MvcApp.Core.Abstractions;

namespace MvcApp.Infrastructure
{
    public class VerificationCodeRepository : IVerificationCodeRepository
    {
        private readonly UserDbContext _context;
        private readonly ILogger<VerificationCodeRepository> _logger;

        public VerificationCodeRepository(UserDbContext context, ILogger<VerificationCodeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<VerificationCodeRecord> CreateAsync(VerificationCodeRecord record)
        {
            _context.VerificationCodes.Add(record);
            await _context.SaveChangesAsync();
            return record;
        }

        public async Task<VerificationCodeRecord?> GetActiveByUserIdAsync(string userId)
        {
            return await _context.VerificationCodes
                .Where(v => v.UserId == userId &&
                           v.IsUsed == false &&
                           v.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<VerificationCodeRecord?> GetByIdAsync(string id)
        {
            return await _context.VerificationCodes.FindAsync(id);
        }

        public async Task<bool> MarkAsUsedAsync(string id, string? ipAddress = null, string? userAgent = null)
        {
            var record = await _context.VerificationCodes.FindAsync(id);
            if (record == null) return false;

            record.IsUsed = true;
            record.UsedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(ipAddress))
                record.IpAddress = ipAddress;
            if (!string.IsNullOrEmpty(userAgent))
                record.UserAgent = userAgent;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CleanupExpiredAsync()
        {
            var expiredRecords = await _context.VerificationCodes
                .Where(v => v.ExpiresAt < DateTime.UtcNow && v.IsUsed == false)
                .ToListAsync();

            _context.VerificationCodes.RemoveRange(expiredRecords);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired verification codes", expiredRecords.Count);
        }

        public async Task<VerificationCodeRecord?> GetByEncryptedCodeAsync(string encryptedCode)
        {
            return await _context.VerificationCodes
                .Where(v => v.EncryptedCode == encryptedCode &&
                           v.IsUsed == false &&
                           v.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }
    }
}
