namespace LeadershipHelper.Application.Auth;

public sealed record OtpChallengeResult(Guid ChallengeId, string Code);
