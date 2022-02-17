using Lilia.Commons;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lilia.Tests;

[TestClass]
public class LiliaUtilitiesTests
{
    private const string JumpLinkTestString =
        "https://discord.com/channels/708668574201544745/923884254298005525/940474669759365160";

    private const string DiscordBotInvTestString =
        "https://discord.com/oauth2/authorize?client_id=177013&scope=bot&permissions=8";

    private const string DiscordGuildInvTestString = "https://discord.gg/discord";

    [TestMethod]
    public void JumpLinkResolveTest()
    {
        var result = JumpLinkTestString.ResolveDiscordMessageJumpLink();

        ulong actualGuildId = 708668574201544745;
        var expectedGuildId = result.Item1;

        ulong actualChannelId = 923884254298005525;
        var expectedChannelId = result.Item2;

        ulong actualMsgId = 940474669759365160;
        var expectedMsgId = result.Item3;

        Assert.AreEqual(actualGuildId, expectedGuildId);
        Assert.AreEqual(actualChannelId, expectedChannelId);
        Assert.AreEqual(actualMsgId, expectedMsgId);
    }

    [TestMethod]
    public void IsValidDiscordBotInviteSuccessTest()
    {
        var res = DiscordBotInvTestString.IsDiscordValidBotInvite();
        Assert.AreEqual(res, true);
    }

    [TestMethod]
    public void IsValidDiscordBotInviteFailTest()
    {
        var res1 = string.Empty.IsDiscordValidBotInvite();
        
        string? test = null;
        var res2 = test.IsDiscordValidBotInvite();

        var res3 = "https://rickroll.discord.com/".IsDiscordValidBotInvite();

        Assert.AreEqual(res1, false);
        Assert.AreEqual(res2, false);
        Assert.AreEqual(res3, false);
    }

    [TestMethod]
    public void IsValidDiscordGuildInviteSuccessTest()
    {
        var res = DiscordGuildInvTestString.IsDiscordValidGuildInvite();
        Assert.AreEqual(res, true);
    }

    [TestMethod]
    public void IsValidDiscordGuildInviteFailTest()
    {
        var res1 = string.Empty.IsDiscordValidGuildInvite();
        string? test = null;
        var res2 = test.IsDiscordValidGuildInvite();
        var res3 = "https://rolled.gg/discord".IsDiscordValidGuildInvite();

        Assert.AreEqual(res1, false);
        Assert.AreEqual(res2, false);
        Assert.AreEqual(res3, false);
    }
}