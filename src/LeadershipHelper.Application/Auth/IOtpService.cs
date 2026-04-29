namespace LeadershipHelper.Application.Auth;

public interface IOtpService
{
    OtpChallengeResult CreateChallenge();
    string HashCode(string code);
    bool Verify(string code, string hash);
}
