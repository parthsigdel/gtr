using Gtr.Utils;

namespace Gtr.Tests;

public class StringUtilsTest
{
    [Fact]
    public void ParseRepoAndOwnerName_ShouldReturnRepoAndName()
    {
        string repoUrl = "https://api.github.com/repos/i-tester-arch/asterik";
        var (repo, owner) = StringUtils.ParseRepoAndOwnerName(repoUrl);
        Assert.Equal("asterik", repo);
        Assert.Equal("i-tester-arch", owner);
    }

    [Fact]
    public void GetAgoTime_ShouldReturnMinute()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var agoTime = StringUtils.GetAgoTime(createdAt);
        Assert.Equal("0m ago", agoTime);
    }

    [Fact]
    public void GetAgoTime_ShouldReturnHour()
    {
        var createdAt = DateTimeOffset.UtcNow.AddHours(-1);
        var agoTime = StringUtils.GetAgoTime(createdAt);
        Assert.Equal("1h ago", agoTime);
    }

    [Fact]
    public void GetAgoTime_ShouldReturnDay()
    {
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var agoTime = StringUtils.GetAgoTime(createdAt);
        Assert.Equal("1d ago", agoTime);
    }

    [Fact]
    public void GetAgoTime_ShouldReturnTwoHours()
    {
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2).AddMinutes(-30);
        var agoTime = StringUtils.GetAgoTime(createdAt);
        Assert.Equal("2h ago", agoTime);
    }
}
