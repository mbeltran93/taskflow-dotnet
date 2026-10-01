using TaskFlow.Infrastructure.Security;

namespace TaskFlow.UnitTests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ProducesADifferentValueThanThePlainPassword()
    {
        var hash = _sut.Hash("my-secret-password");

        Assert.NotEqual("my-secret-password", hash);
        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForTheCorrectPassword()
    {
        var hash = _sut.Hash("my-secret-password");

        Assert.True(_sut.Verify("my-secret-password", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForTheWrongPassword()
    {
        var hash = _sut.Hash("my-secret-password");

        Assert.False(_sut.Verify("not-the-password", hash));
    }
}
