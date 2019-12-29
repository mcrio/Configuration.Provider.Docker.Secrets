using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Mcrio.Configuration.Provider.Docker.Secrets
{
    internal class DockerSecretsConfigurationProvider : ConfigurationProvider
    {
        private readonly string _secretsDirectoryPath;
        private readonly string _colonPlaceholder;
        private readonly ICollection<string> _allowedPrefixes;
        private readonly IFileSystem _fileSystem;

        internal DockerSecretsConfigurationProvider(
            string secretsDirectoryPath,
            string colonPlaceholder,
            ICollection<string> allowedPrefixes
        )
            : this(secretsDirectoryPath, colonPlaceholder, allowedPrefixes, new FileSystem())
        {
        }

        internal DockerSecretsConfigurationProvider(
            string secretsDirectoryPath,
            string colonPlaceholder,
            ICollection<string> allowedPrefixes,
            IFileSystem fileSystem)
        {
            _secretsDirectoryPath = secretsDirectoryPath
                                    ?? throw new ArgumentNullException(nameof(secretsDirectoryPath));
            _colonPlaceholder = colonPlaceholder ?? throw new ArgumentNullException(nameof(colonPlaceholder));
            _allowedPrefixes = allowedPrefixes;
            _fileSystem = fileSystem;
        }

        public override void Load()
        {
            if (!_fileSystem.Directory.Exists(_secretsDirectoryPath))
            {
                return;
            }

            foreach (string secretFilePath in _fileSystem.Directory.EnumerateFiles(_secretsDirectoryPath))
            {
                ProcessFile(secretFilePath);
            }
        }

        private void ProcessFile(string secretFilePath)
        {
            if (string.IsNullOrWhiteSpace(secretFilePath) || !_fileSystem.File.Exists(secretFilePath))
            {
                return;
            }

            string secretFileName = _fileSystem.Path.GetFileName(secretFilePath);
            if (string.IsNullOrWhiteSpace(secretFileName))
            {
                return;
            }

            if (_allowedPrefixes != null && _allowedPrefixes.Count > 0)
            {
                if (!_allowedPrefixes.Any(
                    prefix => secretFileName.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
                ))
                {
                    return;
                }
            }

            using (var reader = new StreamReader(_fileSystem.File.OpenRead(secretFilePath)))
            {
                string secretValue = reader.ReadToEnd();
                if (secretValue.EndsWith(Environment.NewLine))
                {
                    secretValue = secretValue.Substring(0, secretValue.Length - 1);
                }

                string secretKey = secretFileName.Replace(
                    _colonPlaceholder,
                    ":"
                );
                Data.Add(secretKey, secretValue);
            }
        }
    }
}