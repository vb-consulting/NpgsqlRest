namespace NpgsqlRestTests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher;

    public PasswordHasherTests()
    {
        _hasher = new PasswordHasher();
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsBase64Hash()
    {
        // Arrange
        string password = "MySecurePassword123!";

        // Act
        string hashedPassword = _hasher.HashPassword(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password); // Ensure it's not plaintext
        byte[] decoded = Convert.FromBase64String(hashedPassword);
        decoded.Should().HaveCount(16 + 32); // 16-byte salt + 32-byte hash
    }

    [Fact]
    public void HashPassword_NullPassword_ThrowsArgumentException()
    {
        // Arrange
        string password = null!;

        // Act & Assert
        Action act = () => _hasher.HashPassword(password);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Password cannot be null or empty. (Parameter 'password')");
    }

    [Fact]
    public void HashPassword_EmptyPassword_ThrowsArgumentException()
    {
        // Arrange
        string password = "";

        // Act & Assert
        Action act = () => _hasher.HashPassword(password);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Password cannot be null or empty. (Parameter 'password')");
    }

    [Fact]
    public void VerifyHashedPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        string password = "MySecurePassword123!";
        string hashedPassword = _hasher.HashPassword(password);

        // Act
        bool isValid = _hasher.VerifyHashedPassword(hashedPassword, password);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyHashedPassword_NullOrEmptyInputs_ReturnsFalse()
    {
        // Arrange
        string password = "MySecurePassword123!";
        string hashedPassword = _hasher.HashPassword(password);

        // Act & Assert
        _hasher.VerifyHashedPassword(null!, password).Should().BeFalse();
        _hasher.VerifyHashedPassword(hashedPassword, null!).Should().BeFalse();
        _hasher.VerifyHashedPassword("", password).Should().BeFalse();
        _hasher.VerifyHashedPassword(hashedPassword, "").Should().BeFalse();
    }
}