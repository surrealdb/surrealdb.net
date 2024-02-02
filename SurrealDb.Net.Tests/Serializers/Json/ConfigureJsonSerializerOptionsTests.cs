using System.Text.Json;

namespace SurrealDb.Net.Tests.Serializers.Json;

public class DeviceInput : SurrealDbRecord
{
    public bool Mouse { get; set; }
    public bool MechanicalKeyboard { get; set; }
}

public class ConfigureJsonSerializerOptionsTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldUseCamelCasePolicyOnSelect(string url)
    {
        IEnumerable<DeviceInput>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(
                url,
                configureJsonSerializerOptions: (options) =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            );
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.Create(
                new DeviceInput
                {
                    Id = ("device_input", "primary"),
                    Mouse = true,
                    MechanicalKeyboard = true,
                }
            );

            result = await client.Select<DeviceInput>("device_input");
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(1);

        var list = result!.ToList();
        var firstRecord = list.First();

        firstRecord.Should().NotBeNull();
        firstRecord!.Mouse.Should().BeTrue();
        firstRecord!.MechanicalKeyboard.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldUseCamelCasePolicyOnQuery(string url)
    {
        IEnumerable<DeviceInput>? result = null;
        string? rawValue = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(
                url,
                configureJsonSerializerOptions: (options) =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            );
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.Create(
                new DeviceInput
                {
                    Id = ("device_input", "primary"),
                    Mouse = true,
                    MechanicalKeyboard = true,
                }
            );

            var response = await client.Query($"SELECT * FROM device_input");

            rawValue = response.FirstOk?.RawValue.ToString();
            result = response.GetValues<DeviceInput>(0);
        };

        await func.Should().NotThrowAsync();

        rawValue
            .Should()
            .Be("[{\"id\":\"device_input:primary\",\"mechanicalKeyboard\":true,\"mouse\":true}]");

        result.Should().NotBeNull().And.HaveCount(1);

        var list = result!.ToList();
        var firstRecord = list.First();

        firstRecord.Should().NotBeNull();
        firstRecord!.Mouse.Should().BeTrue();
        firstRecord!.MechanicalKeyboard.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldUseKebabCasePolicyOnSelect(string url)
    {
        IEnumerable<DeviceInput>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(
                url,
                configureJsonSerializerOptions: (options) =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
                }
            );
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.Create(
                new DeviceInput
                {
                    Id = ("device_input", "primary"),
                    Mouse = true,
                    MechanicalKeyboard = true,
                }
            );

            result = await client.Select<DeviceInput>("device_input");
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(1);

        var list = result!.ToList();
        var firstRecord = list.First();

        firstRecord.Should().NotBeNull();
        firstRecord!.Mouse.Should().BeTrue();
        firstRecord!.MechanicalKeyboard.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldUseKebabCasePolicyOnQuery(string url)
    {
        IEnumerable<DeviceInput>? result = null;
        string? rawValue = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(
                url,
                configureJsonSerializerOptions: (options) =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
                }
            );
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.Create(
                new DeviceInput
                {
                    Id = ("device_input", "primary"),
                    Mouse = true,
                    MechanicalKeyboard = true,
                }
            );

            var response = await client.Query($"SELECT * FROM device_input");

            rawValue = response.FirstOk?.RawValue.ToString();
            result = response.GetValues<DeviceInput>(0);
        };

        await func.Should().NotThrowAsync();

        rawValue
            .Should()
            .Be("[{\"id\":\"device_input:primary\",\"mechanical-keyboard\":true,\"mouse\":true}]");

        result.Should().NotBeNull().And.HaveCount(1);

        var list = result!.ToList();
        var firstRecord = list.First();

        firstRecord.Should().NotBeNull();
        firstRecord!.Mouse.Should().BeTrue();
        firstRecord!.MechanicalKeyboard.Should().BeTrue();
    }
}
