using VUSYSAICodeReviewWebAPI.Application.Features;
using Xunit;

namespace VUSYSAICodeReviewWebAPI.Tests;

public sealed class UnifiedDiffParserTests
{
    [Theory]
    [InlineData("@@ -0,0 +1,2 @@\n+  first \n+\tsecond\n", 2, 0, null, 1, "Added", "  first ")]
    [InlineData("@@ -0,0 +1 @@\n+original CRLF\r\n", 1, 0, null, 1, "Added", "original CRLF\r")]
    [InlineData("@@ -1 +0,0 @@\n- old\n", 0, 1, 1, null, "Removed", " old")]
    [InlineData("@@ -4,2 +4,2 @@\n same\n-old\n+new\n", 1, 1, 4, 4, "Context", "same")]
    [InlineData("@@ -3,0 +4 @@\n+rename edit\n", 1, 0, null, 4, "Added", "rename edit")]
    public void PreservesLineNumbersAndWhitespace(string patch, int additions, int deletions,
        int? oldLine, int? newLine, string type, string content)
    {
        var parsed = UnifiedDiffParser.Parse(patch, additions, deletions);
        Assert.Equal("Complete", parsed.Completeness);
        var line = Assert.Single(parsed.Hunks).Lines[0];
        Assert.Equal((oldLine, newLine, type, content), (line.OldLineNumber, line.NewLineNumber, line.LineType, line.Content));
    }

    [Theory]
    [InlineData(null, 0, 0, "Unavailable")]
    [InlineData("", 0, 0, "Unavailable")]
    [InlineData("@@ -x +1 @@\n+line", 1, 0, "Malformed")]
    [InlineData("@@ -0 +1 @@\n+line", 1, 0, "Malformed")]
    [InlineData("@@ -0,0 +1,2 @@\n+line", 2, 0, "Partial")]
    [InlineData("@@ -0,0 +1 @@\n+line", 2, 0, "Partial")]
    [InlineData("@@ -0,0 +1 @@\n+line\n\n", 1, 0, "Malformed")]
    [InlineData("@@ -0,0 +2147483647,2 @@\n+line", 1, 0, "Malformed")]
    public void IdentifiesMissingMalformedOrPartialPatch(string? patch, int additions, int deletions, string expected)
        => Assert.Equal(expected, UnifiedDiffParser.Parse(patch, additions, deletions).Completeness);

    [Fact]
    public void ParsesMultipleHunksAndNoNewlineMarker()
    {
        var result = UnifiedDiffParser.Parse("@@ -1 +1 @@\n-one\n+ONE\n@@ -8 +8 @@ section\n-eight\n+EIGHT\n\\ No newline at end of file", 2, 2);
        Assert.Equal("Complete", result.Completeness);
        Assert.Equal(2, result.Hunks.Count);
        Assert.Equal("Metadata", result.Hunks[1].Lines[^1].LineType);
        Assert.Null(result.Hunks[1].Lines[^1].NewLineNumber);
        Assert.Equal(8, result.Hunks[1].Lines[1].NewLineNumber);
    }
}
