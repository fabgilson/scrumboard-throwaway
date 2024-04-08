using FluentAssertions;
using Xunit;
using ScrumBoard.Utils;

namespace ScrumBoard.Tests.Unit.Utils
{
    public class SnakeCaseNamingPolicyTest
    {

        private SnakeCaseNamingPolicy _namingPolicy = new();


        [Theory]
        [InlineData("", "")]
        [InlineData("A", "a")]
        [InlineData("a", "a")]
        [InlineData("lowercase", "lowercase")]
        [InlineData("Titlecase", "titlecase")]
        [InlineData("TwoWords", "two_words")]
        [InlineData("UPPERCASE", "u_p_p_e_r_c_a_s_e")]
        public void ConvertName_InputName_NameConvertedAsExpected(string inputName, string expectedOutputName)
        {
            var actualOutputName = _namingPolicy.ConvertName(inputName);
            actualOutputName.Should().Be(expectedOutputName);
        }
    }
}
