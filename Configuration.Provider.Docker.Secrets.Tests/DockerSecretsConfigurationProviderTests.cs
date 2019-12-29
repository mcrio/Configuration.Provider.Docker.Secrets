using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Mcrio.Configuration.Provider.Docker.Secrets;
using Xunit;

namespace Configuration.Provider.Docker.Secrets.Tests
{
    public class DockerSecretsConfigurationProviderTests
    {
        [Fact]
        public void Secrets_directory_path_must_not_be_null()
        {
            Assert.Throws<ArgumentNullException>(() => new DockerSecretsConfigurationProvider(
                null,
                "__",
                new List<string>()
            ));
        }

        [Fact]
        public void Colon_placeholder_must_not_be_null()
        {
            Assert.Throws<ArgumentNullException>(() => new DockerSecretsConfigurationProvider(
                "/run/secrets",
                null,
                new List<string>()
            ));
        }

        [Fact]
        public void Load_secrets_from_file_and_replace_colon_placeholder_with_colon()
        {
            var fileSystemMock = new MockFileSystem();
            var secretsProvider = new DockerSecretsConfigurationProvider(
                "/run/secrets",
                "__",
                new List<string>(),
                fileSystemMock
            );

            fileSystemMock.AddFile("/run/secrets/secret_one", new MockFileData("secret one content"));
            fileSystemMock.AddFile("/run/secrets/two", new MockFileData("two content"));
            fileSystemMock.AddFile("/run/secrets/foo__bar__baz", new MockFileData("foo bar baz content"));
            fileSystemMock.AddFile("/run/secrets/bar__foo", new MockFileData("bar foo content"));

            secretsProvider.Load();

            secretsProvider.TryGet("secret_one", out string secretOneValue).Should().BeTrue();
            secretOneValue.Should().BeEquivalentTo("secret one content");

            secretsProvider.TryGet("two", out string twoValue).Should().BeTrue();
            twoValue.Should().BeEquivalentTo("two content");

            secretsProvider.TryGet("foo:bar:baz", out string fooBarBazValue).Should().BeTrue();
            fooBarBazValue.Should().BeEquivalentTo("foo bar baz content");
            secretsProvider.TryGet("foo__bar__baz", out string _).Should().BeFalse("__ was replaced with :");

            secretsProvider.TryGet("bar:foo", out string barFooValue).Should().BeTrue();
            barFooValue.Should().BeEquivalentTo("bar foo content");
            secretsProvider.TryGet("bar__foo", out string _).Should().BeFalse("__ was replaced with :");
        }

        [Fact]
        public void Load_secrets_from_which_start_with_predefined_prefix()
        {
            var fileSystemMock = new MockFileSystem();
            var secretsProvider = new DockerSecretsConfigurationProvider(
                "/run/secrets",
                "__",
                new List<string>
                {
                    "foo__",
                    "Bar__Baz"
                },
                fileSystemMock
            );

            fileSystemMock.AddFile("/run/secrets/secret_one", new MockFileData("secret one content"));
            fileSystemMock.AddFile("/run/secrets/two", new MockFileData("two content"));
            fileSystemMock.AddFile("/run/secrets/foo__bar__baz", new MockFileData("foo bar baz content"));
            fileSystemMock.AddFile("/run/secrets/bar__foo", new MockFileData("bar foo content"));
            fileSystemMock.AddFile("/run/secrets/baz__foo__bar", new MockFileData("baz foo bar content"));
            fileSystemMock.AddFile("/run/secrets/Bar__Baz__Secret_One", new MockFileData("Bar Baz Secret One content"));

            secretsProvider.Load();

            secretsProvider.TryGet("secret_one", out string secretOneValue).Should()
                .BeFalse("does not start with prefix");
            secretOneValue.Should().BeNull();

            secretsProvider.TryGet("two", out string twoValue).Should().BeFalse("does not start with prefix");
            twoValue.Should().BeNull();

            secretsProvider.TryGet("foo:bar:baz", out string fooBarBazValue).Should().BeTrue();
            fooBarBazValue.Should().BeEquivalentTo("foo bar baz content");

            secretsProvider.TryGet("bar:foo", out string barFooValue).Should().BeFalse("does not start with prefix");
            barFooValue.Should().BeNull();
            secretsProvider.TryGet("bar__foo", out string _).Should().BeFalse("does not start with prefix");

            secretsProvider.TryGet("baz:foo:bar", out string bazFooBarValue).Should()
                .BeFalse("does not start with prefix");
            bazFooBarValue.Should().BeNull();
            secretsProvider.TryGet("baz__foo__bar", out string _).Should().BeFalse("does not start with prefix");

            secretsProvider.TryGet("Bar:Baz:Secret_One", out string barBazSecretOne).Should().BeTrue();
            barBazSecretOne.Should().BeEquivalentTo("Bar Baz Secret One content");
            secretsProvider.TryGet("Bar__Baz__Secret_One", out string _).Should().BeFalse();
        }

        [Fact]
        public void Configuration_keys_should_be_case_insensitive()
        {
            var fileSystemMock = new MockFileSystem();
            var secretsProvider = new DockerSecretsConfigurationProvider(
                "/run/secrets",
                "__",
                new List<string>
                {
                    "foo__",
                },
                fileSystemMock
            );

            fileSystemMock.AddFile("/run/secrets/Foo__Secret_One", new MockFileData("secret one content"));
            fileSystemMock.AddFile("/run/secrets/foo__secret_two", new MockFileData("secret two content"));

            secretsProvider.Load();

            secretsProvider.TryGet("Foo:Secret_One", out string secretOne).Should()
                .BeTrue("prefix matches as casing is ignored");
            secretOne.Should().BeEquivalentTo("secret one content");
            secretsProvider.TryGet("Foo__Secret_One", out string _).Should().BeFalse();

            secretsProvider.TryGet("foo:secret_two", out string secretTwo).Should().BeTrue("prefix matches");
            secretTwo.Should().BeEquivalentTo("secret two content");
            secretsProvider.TryGet("foo__secret_two", out string _).Should().BeFalse();
        }

        [Fact]
        public void Return_no_configuration_if_secrets_folder_path_does_not_exist()
        {
            var fileSystemMock = new MockFileSystem();
            var secretsProvider = new DockerSecretsConfigurationProvider(
                "/run/secrets_two",
                "__",
                new List<string>(),
                fileSystemMock
            );

            fileSystemMock.AddFile("/run/secrets/secret_one", new MockFileData("secret one content"));

            secretsProvider.Load();

            secretsProvider.TryGet("secret_one", out _).Should()
                .BeFalse("specified folder path differs from path where the secret is stored");
        }

        [Fact]
        public void Return_no_configuration_if_secret_file_does_not_exist()
        {
            var fileSystemMock = new MockFileSystem();
            var secretsProvider = new DockerSecretsConfigurationProvider(
                "/run/secrets",
                "__",
                new List<string>(),
                fileSystemMock
            );

            secretsProvider.Load();

            secretsProvider.TryGet("secret_one", out _).Should()
                .BeFalse("/run/secrets/secret_one file does not exist");
        }
    }
}