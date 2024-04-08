using System;
using FluentAssertions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Utils;
using Xunit;

namespace ScrumBoard.Tests.Unit
{
    public class GitlabCredentialsTypeConverterTest
    {
        private readonly GitlabCredentialsTypeConverter _typeConverter = new();


        [Fact]
        public void CanConvertFrom_String_ReturnsTrue()
        {
           _typeConverter.CanConvertFrom(typeof(string)).Should().BeTrue(); 
        }

        [Fact]
        public void CanConvertFrom_NotString_ReturnsFalse()
        {
           _typeConverter.CanConvertFrom(typeof(int)).Should().BeFalse(); 
        }

        [Fact]
        public void CanConvertTo_String_ReturnsTrue()
        {
           _typeConverter.CanConvertTo(typeof(string)).Should().BeTrue(); 
        }

        [Fact]
        public void CanConvertTo_NotString_ReturnsFalse()
        {
           _typeConverter.CanConvertTo(typeof(int)).Should().BeFalse(); 
        }

        [Theory]
        [InlineData("https://localhost:10/hey", 20, "hunter2", "abc123")]
        [InlineData("https://google.com", 100, "something", "abc123")]
        public void ConvertFromThenTo_AnyInputDate_DateIsSameAsInput(string url, long projectId, string accessToken, string pushWebhookSecretToken)
        {
            var expectedCredentials = new GitlabCredentials(new Uri(url), projectId, accessToken, pushWebhookSecretToken);
            var dateString = _typeConverter.ConvertToString(null, null, expectedCredentials);
            var actualCredentials = _typeConverter.ConvertFromString(null, null, dateString);

            actualCredentials.Should().Be(expectedCredentials);
        }
    }
}