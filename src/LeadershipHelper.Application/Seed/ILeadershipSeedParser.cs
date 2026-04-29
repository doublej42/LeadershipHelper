namespace LeadershipHelper.Application.Seed;

public interface ILeadershipSeedParser
{
    IReadOnlyList<SeedSituationInput> ParseSituations(string markdown);
}
