#if NET8_0_OR_GREATER
using FlakyTest.XUnit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurrealDb.Net.Extensions.DependencyInjection;

namespace SurrealDb.Net.Tests.DependencyInjection;

public class SurrealDbOptionsValidationTests
{
    [FlakyFact("May fail due to timeout")]
    public async Task EndpointShouldBeRequired()
    {
        var func = () => CreateHostRunner(_ => { });

        await func.Should()
            .ThrowAsync<OptionsValidationException>()
            .WithMessage("Endpoint is required.");
    }

    [FlakyFact("May fail due to timeout")]
    public async Task EndpointShouldBeAValidUrl()
    {
        var func = () =>
            CreateHostRunner(options =>
            {
                options.Endpoint = "abc://localhost:5001";
            });

        await func.Should()
            .ThrowAsync<OptionsValidationException>()
            .WithMessage("Endpoint should be a valid URL.");
    }

    [FlakyFact("May fail due to timeout")]
    public async Task NamingPolicyShouldBeValid()
    {
        var func = () =>
            CreateHostRunner(options =>
            {
                options.Endpoint = "https://localhost:5001";
                options.NamingPolicy = "abcd";
            });

        await func.Should()
            .ThrowAsync<OptionsValidationException>()
            .WithMessage("Naming policy should be valid.");
    }

    [FlakyFact("May fail due to timeout")]
    public async Task ShouldValidateSuccessfully()
    {
        var func = () =>
            CreateHostRunner(options =>
            {
                options.Endpoint = "https://localhost:5001";
                options.NamingPolicy = "camelCase";
            });

        await func.Should().NotThrowAsync();
    }

    private static async Task CreateHostRunner(Action<SurrealDbOptions> configureOptions)
    {
        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());

        builder.ConfigureServices(
            (services) =>
            {
                services
                    .AddOptionsWithValidateOnStart<SurrealDbOptions>()
                    .Configure(configureOptions);

                services.AddSingleton<
                    IValidateOptions<SurrealDbOptions>,
                    SurrealDbOptionsValidation
                >();
            }
        );

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        await builder.RunConsoleAsync(cts.Token);
    }
}
#endif
