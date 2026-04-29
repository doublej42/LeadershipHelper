using System.Security.Cryptography;
using System.Text;

namespace LeadershipHelper.Application.Auth;

public sealed class OtpService : IOtpService
{
    public OtpChallengeResult CreateChallenge()
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        return new OtpChallengeResult(Guid.NewGuid(), code);
    }

    public string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string code, string hash)
    {
        return string.Equals(HashCode(code), hash, StringComparison.OrdinalIgnoreCase);
    }
}
