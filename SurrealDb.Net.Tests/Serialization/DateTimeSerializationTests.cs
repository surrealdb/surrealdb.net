using System.Runtime.InteropServices;

namespace SurrealDb.Net.Tests.Serialization;

public class DateTimeSerializationTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSerializeAndThenDeserialize(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        // 💡 Some tests can only succeed on Windows
        bool isWindowsOS = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        (string Name, DateTime Value)[] cases =
        [
            ("DateTime.Now", DateTime.Now),
            ("DateTime.Today", DateTime.Today),
            ("DateTime.MinValue", DateTime.MinValue),
            ("DateTime.MaxValue", DateTime.MaxValue),
            ("DateTime.UnixEpoch", DateTime.UnixEpoch),
            ("DateTime.UtcNow", DateTime.UtcNow),
            ("1", isWindowsOS ? new(1) : new()),
            ("2", isWindowsOS ? new(2) : new()),
            ("3", isWindowsOS ? new(3) : new()),
            ("4", isWindowsOS ? new(4) : new()),
            ("715615451235648454L", new(715615451235648454L)),
#if NET7_0_OR_GREATER
            (
                "(1981, 1, 1, 10, 58, 23, 122, 82, DateTimeKind.Unspecified)",
                new(1981, 1, 1, 10, 58, 23, 122, 82, DateTimeKind.Unspecified)
            ),
#endif
            ("(1913, 6, 3, 14, 38, 30, 164)", new(1913, 6, 3, 14, 38, 30, 164)),
            ("2024-11-08 12:29:58.6625489", DateTime.Parse("2024-11-08 12:29:58.6625489Z"))
        ];

        foreach (var @case in cases)
        {
            var result = await client.Create(
                "datetime",
                new DateTimeRecord { Name = @case.Name, Value = @case.Value, }
            );

            var expectedValue =
                @case.Value.Kind == DateTimeKind.Local
                    ? @case.Value.ToUniversalTime()
                    : @case.Value;

            result
                .Value.Should()
                .Be(
                    expectedValue,
                    because: $"Cannot serialize/deserialize successfully for test case {@case.Name}"
                );
            result.Value!.Value.Kind.Should().Be(DateTimeKind.Utc);
        }
    }
}
