using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using Mcrio.Configuration.Provider.Docker.Secrets;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Configuration.Provider.Docker.Secrets.Tests
{
    public class DockerSecretsConfigurationSourceTests
    {
        [Fact]
        public void Should_build_docker_secrets_provider()
        {
            var source = new DockerSecretsConfigurationsSource(
                "/run/secrets",
                "__",
                new List<string>()
            );
            IConfigurationProvider provider =  source.Build(new Mock<IConfigurationBuilder>().Object);
            provider.Should().BeOfType<DockerSecretsConfigurationProvider>();
        }

        [Fact]
        public void Secrets_directory_path_should_not_be_null()
        {
            Assert.Throws<ArgumentNullException>(() => new DockerSecretsConfigurationsSource(
                null,
                "__",
                new List<string>()
            ));
        }
        
        [Fact]
        public void Colon_plaecholder_path_should_not_be_null()
        {
            Assert.Throws<ArgumentNullException>(() => new DockerSecretsConfigurationsSource(
                "/run/secrets",
                null,
                new List<string>()
            ));
        }
    }
}