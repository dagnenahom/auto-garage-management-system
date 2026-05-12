using SmartGarageSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using SmartGarageSystem.Services;


namespace SmartGarageSystem.Services
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IPasswordHasher> _hasherMock;
        private readonly AuthenticationService _sut;  // system under test

        public AuthenticationServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _hasherMock = new Mock<IPasswordHasher>();
            _sut = new AuthenticationService(_userRepoMock.Object, _hasherMock.Object);
        }

        // White-box test: empty username or password → error message
        [Theory]
        [InlineData("", "valid")]
        [InlineData("user", "")]
        [InlineData(null, "pass")]
        [InlineData(" ", " ")]
        public async Task AuthenticateAsync_InvalidInput_ReturnsRequiredError(string username, string password)
        {
            // Act
            var result = await _sut.AuthenticateAsync(username, password);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Username and password are required.", result.ErrorMessage);
            // Verify that repository is never called (early exit)
            _userRepoMock.Verify(r => r.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never());
        }

        // White-box test: user not found → error
        [Fact]
        public async Task AuthenticateAsync_UserNotFound_ReturnsInvalidError()
        {
            // Arrange
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("unknown"))
                         .ReturnsAsync((User)null);

            // Act
            var result = await _sut.AuthenticateAsync("unknown", "pass");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid username or password.", result.ErrorMessage);
            // Password hasher should not be called
            _hasherMock.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        // White-box test: password mismatch → error
        [Fact]
        public async Task AuthenticateAsync_WrongPassword_ReturnsInvalidError()
        {
            // Arrange
            var user = new User { Username = "user", PasswordHash = "hashed" };
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("user")).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyPassword("wrong", "hashed")).Returns(false);

            // Act
            var result = await _sut.AuthenticateAsync("user", "wrong");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid username or password.", result.ErrorMessage);
            // Verify hasher was called with the correct parameters
            _hasherMock.Verify(h => h.VerifyPassword("wrong", "hashed"), Times.Once());
        }

        // White-box test: success path
        [Fact]
        public async Task AuthenticateAsync_ValidCredentials_ReturnsSuccessUser()
        {
            // Arrange
            var user = new User { Username = "user", PasswordHash = "hashed", FullName = "Test User" };
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("user")).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyPassword("pass", "hashed")).Returns(true);

            // Act
            var result = await _sut.AuthenticateAsync("user", "pass");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Same(user, result.AuthenticatedUser);
            Assert.Null(result.ErrorMessage);
        }

        // White-box test: database (repository) throws exception → error message
        [Fact]
        public async Task AuthenticateAsync_RepositoryThrows_ReturnsErrorMessage()
        {
            // Arrange
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("user"))
                         .ThrowsAsync(new Exception("DB failure"));

            // Act
            var result = await _sut.AuthenticateAsync("user", "pass");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Login error: DB failure", result.ErrorMessage);
        }
    }
}
