using System.Text.RegularExpressions;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;

namespace VUSYSAICodeReviewWebAPI.Application.Features;

/// <summary>Parses GitHub file patches and checks hunk ranges and the upstream change totals.</summary>
public static partial class UnifiedDiffParser
{
    [GeneratedRegex(@"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@(?: .*)?$")]
    private static partial Regex HeaderPattern();

    public static DiffResult Parse(string? patch, int additions, int deletions)
    {
        if (string.IsNullOrEmpty(patch))
            return new("Unavailable", []);

        var hunks = new List<DiffHunk>();
        DiffHunk? hunk = null;
        int oldUsed = 0, newUsed = 0, added = 0, removed = 0;
        bool malformed = false, incomplete = false;
        int? previousOldEnd = null, previousNewEnd = null;
        string[] lines = patch.Split('\n');
        int lineCount = patch.EndsWith('\n') ? lines.Length - 1 : lines.Length;
        for (int index = 0; index < lineCount; index++)
        {
            string line = lines[index];
            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                incomplete |= hunk is not null && (oldUsed != hunk.OldLineCount || newUsed != hunk.NewLineCount);
                string header = line.TrimEnd('\r');
                var match = HeaderPattern().Match(header);
                if (!match.Success || !ReadRange(match, 1, out int oldStart, out int oldCount)
                                   || !ReadRange(match, 3, out int newStart, out int newCount))
                {
                    malformed = true;
                    hunk = null;
                    continue;
                }

                int oldFirst = oldStart + (oldCount == 0 ? 1 : 0);
                int newFirst = newStart + (newCount == 0 ? 1 : 0);
                malformed |= oldFirst < previousOldEnd || newFirst < previousNewEnd || (oldCount == 0 && newCount == 0);
                previousOldEnd = oldFirst + oldCount;
                previousNewEnd = newFirst + newCount;
                hunk = new(header, oldStart, oldCount, newStart, newCount, []);
                hunks.Add(hunk);
                oldUsed = newUsed = 0;
                continue;
            }

            if (hunk is null || line.Length == 0)
            {
                malformed = true;
                continue;
            }

            if (line.TrimEnd('\r') == @"\ No newline at end of file")
            {
                malformed |= hunk.Lines.Count == 0 || hunk.Lines[^1].LineType == "Metadata";
                hunk.Lines.Add(new("Metadata", null, null, line));
                continue;
            }

            char prefix = line[0];
            bool usesOld = prefix is ' ' or '-';
            bool usesNew = prefix is ' ' or '+';
            if ((!usesOld && !usesNew) || (usesOld && oldUsed >= hunk.OldLineCount)
                                        || (usesNew && newUsed >= hunk.NewLineCount))
            {
                malformed = true;
                continue;
            }

            string type = prefix switch { '+' => "Added", '-' => "Removed", _ => "Context" };
            hunk.Lines.Add(new(type, usesOld ? hunk.OldStartLine + oldUsed++ : null,
                                     usesNew ? hunk.NewStartLine + newUsed++ : null, line[1..]));
            if (prefix == '+') added++;
            if (prefix == '-') removed++;
        }

        incomplete |= hunk is not null && (oldUsed != hunk.OldLineCount || newUsed != hunk.NewLineCount);
        string status = malformed || hunks.Count == 0 || added > additions || removed > deletions ? "Malformed"
            : incomplete || added != additions || removed != deletions ? "Partial" : "Complete";
        return new(status, hunks);
    }

    private static bool ReadRange(Match match, int index, out int start, out int count)
    {
        count = 1;
        return int.TryParse(match.Groups[index].Value, out start)
            && (!match.Groups[index + 1].Success || int.TryParse(match.Groups[index + 1].Value, out count))
            && (count == 0 || start > 0)
            && (long)start + Math.Max(1, count) <= int.MaxValue;
    }
}
