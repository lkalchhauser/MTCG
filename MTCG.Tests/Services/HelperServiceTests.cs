using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Services;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util.Enums;
using NSubstitute;
using NUnit.Framework;

namespace MTCG.Tests.Services;

[TestFixture]
public class HelperServiceTests
{
	private IHelperService _helperService;

	[SetUp]
	public void SetUp()
	{
		_helperService = new HelperService();
	}

	[Test]
	public void HashPassword_ShouldReturnHashedPassword()
	{
		var password = "securePassword";

		var hashedPw = BCrypt.Net.BCrypt.HashPassword(password);
		var result = _helperService.HashPassword(password);

		Assert.That(BCrypt.Net.BCrypt.Verify(password, hashedPw), Is.True);
	}

	[Test]
	public void VerifyPassword_ShouldReturnTrueWhenPasswordIsCorrect()
	{
		var password = "securePassword";
		var hash = _helperService.HashPassword(password);

		var result = _helperService.VerifyPassword(password, hash);

		Assert.That(result, Is.True);
	}

	[Test]
	public void VerifyPassword_ShouldReturnFalseWhenPasswordIsIncorrect()
	{
		var password = "securePassword";
		var incorrectPassword = "wrongPassword";
		var hashPassword = _helperService.HashPassword(password);

		var result = _helperService.VerifyPassword(incorrectPassword, hashPassword);

		Assert.That(result, Is.False);
	}

	[Test]
	public void GenerateToken_ShouldReturnCorrectToken()
	{
		var username = "testUser";
		var expectedToken = "testUser-mtcgToken";

		var result = _helperService.GenerateToken(username);

		Assert.That(result, Is.EqualTo(expectedToken));
	}

	[Test]
	public void IsRequestedUserAuthorizedUser_ShouldReturnTrueIfUserIsAuthorized()
	{
		var handler = TestHelper.CreateMockHandler("testUser", 1);
		handler.Path = "/testUser";

		var result = _helperService.IsRequestedUserAuthorizedUser(handler);

		Assert.That(result, Is.True);
	}

	[Test]
	public void IsRequestedUserAuthorizedUser_ShouldReturnFalseIfUserIsNotAuthorized()
	{
		var handler = TestHelper.CreateMockHandler("testUser", 1);
		handler.Path = "/otherUser";

		var result = _helperService.IsRequestedUserAuthorizedUser(handler);

		Assert.That(result, Is.False);
	}

	[Test]
	public void ExtractUsernameFromPath_ShouldExtractCorrectUsernameFromPath()
	{
		var path = "/testUser";

		var result = _helperService.ExtractUsernameFromPath(path);

		Assert.That(result, Is.EqualTo("testUser"));
	}

	[Test]
	public void ParseEnumOrNull_ShouldReturnEnumIfValid()
	{
		var value = "MONSTER";

		var result = _helperService.ParseEnumOrNull<CardType>(value);

		Assert.That(result, Is.EqualTo(CardType.MONSTER));
	}

	[Test]
	public void ParseEnumOrNull_ShouldReturnNullIfInvalid()
	{
		var value = "GONSTER";

		var result = _helperService.ParseEnumOrNull<CardType>(value);

		Assert.That(result, Is.EqualTo(null));
	}
}