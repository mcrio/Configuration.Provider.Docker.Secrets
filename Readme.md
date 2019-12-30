# .NET Core Configuration provider for Docker Secrets

> Ability to map docker secrets files to .net core configuration. 


[![Build status](https://dev.azure.com/midnight-creative/Configuration.Provider.Docker.Secrets/_apis/build/status/Build?branchName=master)](https://dev.azure.com/midnight-creative/Configuration.Provider.Docker.Secrets/_apis/build/status/Build?branchName=master)
![Nuget](https://img.shields.io/nuget/v/Mcrio.Configuration.Provider.Docker.Secrets)

This package allows reading docker secrets files and pull them into the .net core configuration.
Docker by default mounts secrets as files at the `/run/secrets` directory. The secrets file names
are used to identify the configuration targets.

### About Docker Secrets
Docker secrets are part of the Docker swarm services. They are used to manage sensitive data which 
a container needs at runtime but which should not be stored in the container image or source control.
Read more about docker secrets on the [official docker documentation pages](https://docs.docker.com/engine/swarm/secrets/).

## Getting Started

Using the NuGet package manager install the [Mcrio.Configuration.Provider.Docker.Secrets](https://www.nuget.org/packages/Mcrio.Configuration.Provider.Docker.Secrets/) 
package, or add the following line to the `.csproj` file:

```xml
<ItemGroup>
    <PackageReference Include="Mcrio.Configuration.Provider.Docker.Secrets">
        <Version>1.0.0</Version>
    </PackageReference>
</ItemGroup>
``` 
**Note:** Replace version value with the latest version available.

## Usage

By default all files within the directory `/run/secrets` are scanned and processed as configuration.
.NET Core configuration uses `:` as the section delimiter.
As `:` cannot be used in file names, use `__` in place where `:` is needed.

`AddDockerSecrets()` allows overriding of the default values 
for the secrets directory path and the colon placeholder.

Often we want to process just specific secrets files. By setting
allowed prefixes we can narrow down which files will be processed.

#### Simple usage
```cs
var configuration = new ConfigurationBuilder()
                        .AddDockerSecrets()
                        .Build();
var secretValue = configuration["mysecret"];
```

#### ASP.NET Core
```cs
// Program.cs
public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.AddDockerSecrets();

                    // allow command line arguments to override docker secrets
                    if (args != null)
                    {
                        configBuilder.AddCommandLine(args);
                    }
                })
                .UseStartup<Startup>();
```

#### Only process files that start with a predefined prefix

```cs
configBuilder.AddDockerSecrets(
    allowedPrefixes: new List<string> 
    { 
        "ConfigSection1__", 
        "Foo__Bar__Baz" 
    }
);
```

#### Specify environment variable name that holds comma delimited list of allowed prefixes

```bash
setenv MY_SECRETS_PREFIXES "ConfigSection1__,Foo__Bar__Baz"
```
```cs
configBuilder.AddDockerSecrets(MY_SECRETS_PREFIXES);
```


#### Docker compose example

```yaml
# docker compose compatible file
services:
    myservice:
      environment:
        - MY_SECRETS_PREFIXES=ConfigSection1__,Foo__Bar__Baz
    secrets:
      - source: myservice_foobarbaz_dbpass
        target: Foo__Bar__Baz__DbPassword

secrets:
    myservice_foobarbaz_dbpass:
        external: true
        name: myservice_foobarbaz_dbpass_2019_12_30_1
```
```cs
// Program.cs
public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.AddDockerSecrets(
                        allowedPrefixesEnvVariableName: "MY_SECRETS_PREFIXES"
                    );

                    // allow command line arguments to override docker secrets
                    if (args != null)
                    {
                        configBuilder.AddCommandLine(args);
                    }
                })
                .UseStartup<Startup>();
```

## Release History

- **1.0.1**
    - Stable version that reads secret values from mounted files
    and pulls those into the configuration. Optionally
    filters the files to process by defined allowed prefixes.

## Meta

Nikola Josipovic

This project is licensed under the MIT License. See [License.md](License.md) for more information.