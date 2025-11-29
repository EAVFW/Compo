using Compo.Functions.String;

namespace Compo.Test.Functions;

public class TrimWithNullCharTest
{
    [Fact]
    public void CSharp_Trim_DoesNOTRemoveNullCharacters()
    {
        // Create string with actual null character (char 0)
        var input = new string(new[] { '\0', '\0', 'h', 'e', 'l', 'l', 'o', '\0', '\0' });
        var result = input.Trim();

        // Trim() does NOT remove \0 characters!
        result.Should().Be(input, "Trim() only removes whitespace, not null chars");
        result.Length.Should().Be(9);
    }

    [Fact]
    public void CSharp_Trim_OnlyRemovesWhitespace()
    {
        var input = new string(new[] { '\0', ' ', '\t', 'h', 'e', 'l', 'l', 'o', '\t', ' ', '\0' });
        var result = input.Trim();

        // Only space and tab are removed, not \0
        result.Should().StartWith("\0");
        result.Should().EndWith("\0");
        result.Should().Contain("hello");
    }

    [Fact]
    public void IsWhitespace_NullCharIsNOTWhitespace()
    {
        // Verify that \0 is NOT considered whitespace
        char.IsWhiteSpace('\0').Should().BeFalse("null char (0x00) is NOT whitespace");
        char.IsWhiteSpace(' ').Should().BeTrue("space IS whitespace");
        char.IsWhiteSpace('\t').Should().BeTrue("tab IS whitespace");
        char.IsWhiteSpace('\n').Should().BeTrue("newline IS whitespace");
    }

    [Fact]
    public void TrimFunction_RemovesNullChars()
    {
        var trimFunc = new TrimFunction();
        var input = new string(new[] { '\0', '\0', 'h', 'e', 'l', 'l', 'o', '\0', '\0' });
        var result = trimFunc.Execute(input);

        // TrimFunction now removes both whitespace AND \0
        result.Should().Be("hello");
        result.Length.Should().Be(5);
    }

    [Fact]
    public void TrimFunction_RemovesWhitespaceAndNullChars()
    {
        var trimFunc = new TrimFunction();
        var input = new string(new[] { '\0', ' ', '\t', 'h', 'e', 'l', 'l', 'o', '\t', ' ', '\0' });
        var result = trimFunc.Execute(input);

        result.Should().Be("hello");
    }

    [Fact]
    public void TrimFunction_WithOnlyNullChars_ReturnsEmpty()
    {
        var trimFunc = new TrimFunction();
        var input = new string(new[] { '\0', '\0', '\0' });
        var result = trimFunc.Execute(input);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void TrimFunction_WithMixedWhitespaceAndNullChars_RemovesAll()
    {
        var trimFunc = new TrimFunction();
        var input = new string(new[] { ' ', '\0', '\t', '\0', 't', 'e', 's', 't', '\0', '\t', '\0', ' ' });
        var result = trimFunc.Execute(input);

        result.Should().Be("test");
    }
}