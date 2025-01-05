using System.Text.Json;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Services;
using NSubstitute;
using NUnit.Framework;
using MTCG.Server.HTTP;
using MTCG.Server.Models;

namespace MTCG.Tests.Services;

[TestFixture]
public class UserServiceTests
{
	private IUserService _userService;
	private IUserRepository _userRepository;
	private IHelperService _helperService;

	[SetUp]
	public void SetUp()
	{
		_userRepository = Substitute.For<IUserRepository>();
		_helperService = Substitute.For<IHelperService>();

		_userService = new UserService(_userRepository, _helperService);
	}

	[Test]
	public void RegisterUser_ShouldReturnErrorIfPayloadIsInvalid()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		handler.GetContentType().Returns("application/xml");
		handler.Payload.Returns((string)null);

		var result = _userService.RegisterUser(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(400));
		Assert.That(result.Message, Is.EqualTo("Badly formatted data sent!"));
	}

	[Test]
	public void RegisterUser_ShouldReturnErrorIfUserAlreadyExists()
	{
		var handler = TestHelper.CreateMockHandler("username", 5);
		var existingUserCredentials = TestHelper.CreateSimpleUser(1);
		var userCredentials = new UserCredentials { Username = "testUser", Password = "password123" };
		handler.Payload.Returns(JsonSerializer.Serialize(userCredentials));
		_userRepository.GetUserByUsername("testUser").Returns(existingUserCredentials);

		var result = _userService.RegisterUser(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(409));
		Assert.That(result.Message, Is.EqualTo("User already exists!"));
	}

	[Test]
	public void RegisterUser_ShouldReturnSuccessIfRegistered()
	{
		var handler = TestHelper.CreateMockHandler("user1", 1);
		var userCredentials = new UserCredentials { Username = "newUser", Password = "password123" };
		handler.Payload.Returns(JsonSerializer.Serialize(userCredentials));

		_userRepository.GetUserByUsername("newUser").Returns((UserCredentials)null);
		_helperService.HashPassword("password123").Returns("hashedPassword");
		_userRepository.AddUser(Arg.Any<UserCredentials>()).Returns(1);
		_userRepository.AddUserStats(Arg.Any<UserStats>()).Returns(true);

		var result = _userService.RegisterUser(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(201));
		Assert.That(result.Message, Is.EqualTo("Successfully registered!"));
	}

	[Test]
	public void LoginUser_ShouldReturnErrorIfInvalidPayload()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		handler.GetContentType().Returns("application/xml");
		handler.Payload.Returns((string)null);

		var result = _userService.LoginUser(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(400));
		Assert.That(result.Message, Is.EqualTo("Badly formatted data sent!"));
	}

	[Test]
	public void LoginUser_ShouldReturnErrorIfUserNotFound()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		var userCredentials = new UserCredentials { Username = "nonExistentUser", Password = "password123" };
		handler.Payload.Returns(JsonSerializer.Serialize(userCredentials));
		_userRepository.GetUserByUsername("nonExistentUser").Returns((UserCredentials)null);

		var result = _userService.LoginUser(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(404));
		Assert.That(result.Message, Is.EqualTo("Login failed - User does not exist"));
	}

	[Test]
	public void LoginUser_ShouldReturnErrorIfPasswordIsIncorrect()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		var userCredentials = new UserCredentials { Username = "existingUser", Password = "incorrectPassword" };
		handler.Payload.Returns(JsonSerializer.Serialize(userCredentials));

		var userFromDb = TestHelper.CreateSimpleUser(1, "existingUser", password: "hashedPassword");
		_userRepository.GetUserByUsername("existingUser").Returns(userFromDb);
		_helperService.VerifyPassword("incorrectPassword", "hashedPassword").Returns(false);

		var result = _userService.LoginUser(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(401));
		Assert.That(result.Message, Is.EqualTo("Login failed - Login Data not correct"));
	}

	[Test]
	public void LoginUser_ShouldReturnSuccessIfLoginSuccessful()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		var userCredentials = new UserCredentials { Username = "existingUser", Password = "correctPassword" };
		handler.Payload.Returns(JsonSerializer.Serialize(userCredentials));

		var userFromDb = TestHelper.CreateSimpleUser(1, "existingUser", password: "hashedPassword");
		_userRepository.GetUserByUsername("existingUser").Returns(userFromDb);
		_helperService.VerifyPassword("correctPassword", "hashedPassword").Returns(true);
		_helperService.GenerateToken("existingUser").Returns("validToken");
		_userRepository.UpdateUser(userFromDb).Returns(true);

		var result = _userService.LoginUser(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Does.Contain("validToken"));
	}

	[Test]
	public void GetUserInformationForUser_ShouldReturnErrorIfNoInfoFound()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		var user = TestHelper.CreateSimpleUser(1, "testUser", password: "password123");
		handler.AuthorizedUser.Returns(user);
		_userRepository.GetUserInfoByUser(user).Returns((UserInfo)null);

		var result = _userService.GetUserInformationForUser(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(404));
		Assert.That(result.Message, Is.EqualTo("Failed to get user information - no data found"));
	}

	[Test]
	public void GetUserInformationForUser_ShouldReturnUserInfo()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		var user = TestHelper.CreateSimpleUser(1, "testUser", password: "password123");
		handler.AuthorizedUser.Returns(user);
		var userInfo = new UserInfo { Id = 1, Bio = "This is a bio" };
		_userRepository.GetUserInfoByUser(user).Returns(userInfo);

		var result = _userService.GetUserInformationForUser(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Is.EqualTo(JsonSerializer.Serialize(userInfo)));
	}

	[Test]
	public void UpdatePassword_ShouldReturnErrorIfPayloadIsInvalid()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		handler.GetContentType().Returns("application/xml");
		handler.Payload.Returns((string)null);

		var result = _userService.UpdatePassword(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(400));
		Assert.That(result.Message, Is.EqualTo("Badly formatted data sent!"));
	}

	[Test]
	public void UpdatePassword_ShouldReturnSuccessIfPasswordUpdated()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		var userCredentials = new UserCredentials { Username = "existingUser", Password = "newPassword123" };
		handler.Payload.Returns(JsonSerializer.Serialize(userCredentials));

		var userFromDb = TestHelper.CreateSimpleUser(1, "testUser", password: "password123");
		_userRepository.GetUserById(1).Returns(userFromDb);
		_helperService.HashPassword("newPassword123").Returns("newHashedPassword");
		_userRepository.UpdateUser(userFromDb).Returns(true);

		var result = _userService.UpdatePassword(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Is.EqualTo("Password successfully updated"));
	}

	[Test]
	public void GetScoreboard_ShouldReturn404IfNoStatsFound()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		_userRepository.GetAllStats().Returns([]);

		var result = _userService.GetScoreboard(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(404));
		Assert.That(result.Message, Is.EqualTo("No stats found"));
	}

	[Test]
	public void GetScoreboard_ShouldReturnScoreboard()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		var userStatsList = new List<UserStats>
		{
			new UserStats { Id = 1, Elo = 100, Wins = 5, Losses = 2, Draws = 1 },
			new UserStats { Id = 2, Elo = 150, Wins = 10, Losses = 5, Draws = 0 }
		};
		_userRepository.GetAllStats().Returns(userStatsList);

		var user1 = TestHelper.CreateSimpleUser(1, "User1");
		var user2 = TestHelper.CreateSimpleUser(2, "User2");
		_userRepository.GetUserById(1).Returns(user1);
		_userRepository.GetUserById(2).Returns(user2);

		var result = _userService.GetScoreboard(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Does.Contain("User1"));
		Assert.That(result.Message, Does.Contain("User2"));
	}

	[Test]
	public void IsUserAuthorized_ShouldReturnFalseIfUserNotAuthorized()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		handler.GetAuthorizationToken().Returns("");
		_userRepository.GetUserByToken("invalidToken").Returns((UserCredentials)null);

		var result = _userService.IsUserAuthorized(handler);

		Assert.That(result, Is.False);
	}

	[Test]
	public void IsUserAuthorized_ShouldReturnTrueIfUserAuthorized()
	{
		var handler = TestHelper.CreateMockHandler("User1", 1);
		var user = TestHelper.CreateSimpleUser(1, "User1");
		handler.GetAuthorizationToken().Returns("User1-mtcgToken");
		_userRepository.GetUserByToken("User1-mtcgToken").Returns(user);

		var result = _userService.IsUserAuthorized(handler);

		Assert.That(result, Is.True);
		Assert.That(handler.AuthorizedUser, Is.EqualTo(user));
	}

	[Test]
	public void AddOrUpdateUserInfo_ShouldReturnErrorIfNoPayload()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		handler.Payload.Returns((string)null);

		var result = _userService.AddOrUpdateUserInfo(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(400));
		Assert.That(result.Message, Is.EqualTo("No payload data found"));
	}

	[Test]
	public void AddOrUpdateUserInfo_ShouldReturnSuccessIfAddingNewInfo()
	{
		var handler = TestHelper.CreateMockHandler("User1", 1);
		var newUserInfo = new UserInfo { Id = 1, Bio = "Updated bio" };
		handler.Payload.Returns(JsonSerializer.Serialize(newUserInfo));

		_userRepository.GetUserInfoByUser(handler.AuthorizedUser).Returns((UserInfo)null);
		_userRepository.AddUserInfo(Arg.Any<UserInfo>()).Returns(true);

		var result = _userService.AddOrUpdateUserInfo(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(201));
		Assert.That(result.Message, Is.EqualTo(JsonSerializer.Serialize(newUserInfo)));
	}

	[Test]
	public void AddOrUpdateUserInfo_ShouldReturnSuccessIfUpdatingExistingInfo()
	{
		var handler = TestHelper.CreateMockHandler("User1", 1);
		var existingUserInfo = new UserInfo { Id = 1, Bio = "Existing bio" };
		var updatedUserInfo = new UserInfo { Id = 1, Bio = "Updated bio" };
		handler.Payload.Returns(JsonSerializer.Serialize(updatedUserInfo));

		_userRepository.GetUserInfoByUser(handler.AuthorizedUser).Returns(existingUserInfo);
		_userRepository.UpdateUserInfo(Arg.Any<UserInfo>()).Returns(true);

		var result = _userService.AddOrUpdateUserInfo(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Is.EqualTo(JsonSerializer.Serialize(updatedUserInfo)));
	}

	[Test]
	public void GetUserStats_ShouldReturnErrorIfNoStatsFound()
	{
		var handler = TestHelper.CreateMockHandler("User1", 1);
		_userRepository.GetUserStats(handler).Returns((UserStats)null);

		var result = _userService.GetUserStats(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(404));
		Assert.That(result.Message, Is.EqualTo("No user stats found"));
	}

	[Test]
	public void GetUserStats_ShouldReturnStats()
	{
		var handler = TestHelper.CreateMockHandler("User1", 1);
		var userStats = new UserStats { Id = 1, Elo = 100, Wins = 5, Losses = 3, Draws = 2 };
		_userRepository.GetUserStats(handler).Returns(userStats);

		var result = _userService.GetUserStats(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Is.EqualTo(JsonSerializer.Serialize(userStats)));
	}

	[Test]
	public void UpdateUserStats_ShouldReturnSuccessIfStatsUpdated()
	{
		var handler = TestHelper.CreateMockHandler("User1", 1);
		var userStats = new UserStats { Id = 1, Elo = 150, Wins = 6, Losses = 2, Draws = 1 };
		_userRepository.UpdateUserStats(userStats).Returns(true);

		var result = _userService.UpdateUserStats(handler, userStats);

		Assert.That(result.Success, Is.True);
		Assert.That(result.Message, Is.EqualTo("User stats successfully updated"));
	}

	[Test]
	public void UpdateUserStats_ShouldReturnErrorIfUpdateFailed()
	{
		var handler = TestHelper.CreateMockHandler("User1", 1);
		var userStats = new UserStats { Id = 1, Elo = 150, Wins = 6, Losses = 2, Draws = 1 };
		_userRepository.UpdateUserStats(userStats).Returns(false);

		var result = _userService.UpdateUserStats(handler, userStats);

		Assert.That(result.Success, Is.False);
		Assert.That(result.Message, Is.EqualTo("Error while updating user stats"));
	}
}