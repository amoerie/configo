using Configo.Server.Domain;

namespace Configo.Tests.Server.Domain;

public class ApiKeyGeneratorTests
{
    private readonly ApiKeyGenerator _generator = new ApiKeyGenerator();

    [Fact]
    public void ShouldThrowForInvalidLength()
    {
        Assert.Throws<ArgumentException>(() => _generator.Generate(-1));
        Assert.Throws<ArgumentException>(() => _generator.Generate(0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void ShouldGenerateStringsWithCorrectLength(int length)
    {
        // Act
        var output = _generator.Generate(length);

        // Assert
        Assert.Equal(length, output.Length);
    }

    [Fact]
    public void ShouldGenerateUniqueStrings()
    {
        // Arrange
        var outputs = new HashSet<string>(StringComparer.Ordinal);

        // Act
        for (var i = 0; i < 1000; i++)
        {
            // Length = 6, chance of collision is already very low
            outputs.Add(_generator.Generate(6));
        }

        // Assert
        Assert.Equal(1000, outputs.Count);
    }

    [Fact]
    public void ShouldGenerateStringWithDigitsOrUpperCaseCharacters()
    {
        // Act + Assert
        for (var i = 0; i < 1000; i++)
        {
            var output = _generator.Generate(6);
            foreach (var character in output)
            {
                Assert.True((char.IsUpper(character) || char.IsDigit(character)));
            }
        }
    }

    [Fact]
    public void ShouldNotContainForbiddenCharacters()
    {
        // Act
        var forbiddenCharacters = new HashSet<char>(ApiKeyGenerator.ForbiddenCharacters);
        for (var i = 0; i < 1000; i++)
        {
            var output = new HashSet<char>(_generator.Generate(6));
            Assert.False(forbiddenCharacters.Overlaps(output));
        }
    }

    [Fact]
    public void ShouldEventuallyUseAllAllowedCharacters()
    {
        // Arrange
        var allowedCharacters = new HashSet<char>(ApiKeyGenerator.AllowedCharacters);
        var usedCharacters = new HashSet<char>();

        // Act
        for (var i = 0; i < 1000; i++)
        {
            var output = new HashSet<char>(_generator.Generate(6));
            usedCharacters.UnionWith(output);
        }

        // Assert
        Assert.True(allowedCharacters.SetEquals(usedCharacters));
    }
}
