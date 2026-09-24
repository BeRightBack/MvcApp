using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using MvcApp.Core;
using Serilog;
using MvcApp.Core.Abstractions;

namespace MvcApp.Services
{
    public class SecureVerificationService
    {
        private readonly IConfiguration _configuration;
        private readonly IVerificationCodeRepository _repository;
        private readonly IBrandingService _branding;
        private readonly IEmailQueue _emailQueue;
        private readonly string _encryptionKey;

        public SecureVerificationService(
            IConfiguration configuration,
            IVerificationCodeRepository repository,
            IBrandingService branding,
            IEmailQueue emailQueue)
        {
            _configuration = configuration;
            _repository = repository;
            _branding = branding;
            _emailQueue = emailQueue;
            _encryptionKey = _configuration["Encryption:Key"] ?? throw new ArgumentNullException("Encryption:Key", "Encryption key is not configured");
        }

        public async Task<VerificationResult> SendVerificationCodeAsync(string userEmail, string userId, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                Log.Information("Sending verification code for user {UserId} to {Email}", userId, userEmail);

                // Generate verification code
                var verificationCode = GetGenerateVerificationCode();
                Log.Information("Generated verification code: {Code}", verificationCode);

                // Encrypt the code using a more robust method
                var encryptedCode = EncryptCode(verificationCode, userId);
                Log.Information("Encrypted code generated successfully");

                // Create database record
                var record = new VerificationCodeRecord
                {
                    UserId = userId,
                    Email = userEmail,
                    EncryptedCode = encryptedCode,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                await _repository.CreateAsync(record);
                Log.Information("Verification code record created with ID: {RecordId}", record.Id);

                // Send email with plain code (user can see it)
                await SendSecureEmailAsync(userEmail, verificationCode, userId);

                return new VerificationResult
                {
                    EncryptedCode = encryptedCode,
                    Code = verificationCode,
                    Expiry = record.ExpiresAt,
                    RecordId = record.Id
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send verification code for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> VerifyCodeAsync(string userId, string inputCode, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                Log.Information("Verifying code for user {UserId}", userId);

                // Get the active verification record
                var record = await _repository.GetActiveByUserIdAsync(userId);

                if (record == null)
                {
                    Log.Warning("No active verification record found for user {UserId}", userId);
                    return false;
                }

                Log.Information("Found verification record ID: {RecordId}", record.Id);

                // Check if expired
                if (record.ExpiresAt < DateTime.UtcNow)
                {
                    Log.Warning("Verification code expired for user {UserId}. Expired at: {ExpiresAt}", userId, record.ExpiresAt);
                    return false;
                }

                // Decrypt stored code and compare
                var decryptedCode = DecryptCode(record.EncryptedCode, userId);
                Log.Information("Decrypted code: {DecryptedCode}", decryptedCode);
                Log.Information("Input code: {InputCode}", inputCode);

                if (decryptedCode == inputCode)
                {
                    // Mark as used
                    await _repository.MarkAsUsedAsync(record.Id, ipAddress, userAgent);
                    Log.Information("Verification successful for user {UserId}", userId);
                    return true;
                }

                Log.Warning("Invalid verification code attempt for user {UserId}", userId);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Verification failed for user {UserId}", userId);
                return false;
            }
        }

        private static string GetGenerateVerificationCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return (BitConverter.ToUInt32(bytes, 0) % 1000000).ToString("D6");
        }

        private string EncryptCode(string code, string userId)
        {
            try
            {
                // Use a more deterministic approach for key derivation
                var key = GenerateKeyFromUserId(userId);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Generate a random IV for each encryption
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                var plainBytes = Encoding.UTF8.GetBytes(code);
                var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                // Combine IV and encrypted data
                var result = new byte[aes.IV.Length + encryptedBytes.Length];
                Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
                Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Encryption failed for user {UserId}", userId);
                throw;
            }
        }

        private string DecryptCode(string encryptedCode, string userId)
        {
            try
            {
                var key = GenerateKeyFromUserId(userId);

                var encryptedBytes = Convert.FromBase64String(encryptedCode);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Extract IV (first 16 bytes for AES)
                var iv = new byte[16];
                Array.Copy(encryptedBytes, 0, iv, 0, 16);
                aes.IV = iv;

                // Extract encrypted data (after IV)
                var data = new byte[encryptedBytes.Length - 16];
                Array.Copy(encryptedBytes, 16, data, 0, data.Length);

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(data, 0, data.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Decryption failed for user {UserId}. Encrypted code length: {Length}", userId, encryptedCode?.Length ?? 0);
                throw;
            }
        }

        private byte[] GenerateKeyFromUserId(string userId)
        {
            try
            {
                // Create a more robust key derivation using salt
                var salt = Encoding.UTF8.GetBytes("verification_salt_2024");
                var userIdBytes = Encoding.UTF8.GetBytes(userId);

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_encryptionKey));
                var key = hmac.ComputeHash(userIdBytes);
                return [.. key.Take(32)]; // 256-bit key
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Key generation failed for user {UserId}", userId);
                throw;
            }
        }

        private async Task SendSecureEmailAsync(string userEmail, string plainCode, string userId)
        {
            try
            {
                var siteName = await _branding.GetSiteNameAsync();
                var logoUrl = await _branding.GetLogoUrlAsync();
                var tagline = await _branding.GetTaglineAsync();

                var body = EmailTemplates.Build(siteName, "Secure Verification Code",
                    $"<p>Your verification code is:</p>" +
                    $"<div style=\"display:inline-block;padding:12px 24px;background:#f3f4f6;border-radius:6px;font-size:30px;font-weight:700;letter-spacing:5px;color:#111827;\">{plainCode}</div>" +
                    $"<p style=\"margin-top:16px;\">This code will expire in 10 minutes.</p>" +
                    $"<p style=\"color:#dc2626;\">This code is for your eyes only. Do not share it with anyone.</p>" +
                    $"<p style=\"color:#9ca3af;font-size:12px;\">Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</p>",
                    logoUrl, tagline, "Secure Verification Required");

                await _emailQueue.EnqueueAsync(new EmailQueueItem
                {
                    To = userEmail,
                    Subject = "Secure Verification Code",
                    RawHtmlBody = body,
                    Headers = new Dictionary<string, string>
                    {
                        ["X-Content-Type-Options"] = "nosniff",
                        ["X-Frame-Options"] = "DENY",
                        ["X-XSS-Protection"] = "1; mode=block"
                    }
                });

                Log.Information("Verification code queued securely to {UserEmail}", userEmail);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to queue secure email to {UserEmail}", userEmail);
                throw;
            }
        }
    }

    public class VerificationResult
    {
        public required string EncryptedCode { get; set; }
        public required string Code { get; set; }
        public DateTime Expiry { get; set; }
        public string? RecordId { get; set; } // Add this property to fix CS0117
    }
}
