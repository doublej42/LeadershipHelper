using System.Text.RegularExpressions;
using LeadershipHelper.Application.Seed;

namespace LeadershipHelper.Infrastructure.Seed;

public sealed class LeadershipSeedParser : ILeadershipSeedParser
{
    private static readonly Regex SituationRegex = new(@"^Situation\s+\d+:\s*(.+)$", RegexOptions.Compiled);

    public IReadOnlyList<SeedSituationInput> ParseSituations(string markdown)
    {
        var results = new List<SeedSituationInput>();
        string? currentTitle = null;
        var prompts = new List<string>();

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.Trim();
            var match = SituationRegex.Match(line);

            if (match.Success)
            {
                AddCurrentIfAny(results, ref currentTitle, prompts);
                currentTitle = match.Groups[1].Value.Trim().TrimEnd('.');
                continue;
            }

            if (currentTitle is null)
            {
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                prompts.Add(line[2..].Trim());
            }
            else if (string.IsNullOrWhiteSpace(line) && prompts.Count > 0)
            {
                AddCurrentIfAny(results, ref currentTitle, prompts);
            }
        }

        AddCurrentIfAny(results, ref currentTitle, prompts);
        return results;
    }

    private static void AddCurrentIfAny(ICollection<SeedSituationInput> results, ref string? title, ICollection<string> prompts)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        results.Add(new SeedSituationInput(title, prompts.ToList()));
        title = null;
        prompts.Clear();
    }
}
