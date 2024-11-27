namespace SurrealDb.Net.Tests.Serialization;

public class TimeSpanSerializationTests
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

        (string Name, TimeSpan Value)[] cases =
        [
            ("TimeSpan.Zero", TimeSpan.Zero),
            // ("TimeSpan.MinValue", TimeSpan.MinValue), 💡 Does not support negative duration yet
            ("TimeSpan.MaxValue", TimeSpan.MaxValue),
            ("TimeSpan.FromSeconds(1)", TimeSpan.FromSeconds(1)),
            ("TimeSpan.FromMinutes(1)", TimeSpan.FromMinutes(1)),
            ("TimeSpan.FromHours(1)", TimeSpan.FromHours(1)),
            ("TimeSpan.FromMilliseconds(1)", TimeSpan.FromMilliseconds(1)),
#if NET7_0_OR_GREATER
            ("TimeSpan.FromMicroseconds(1)", TimeSpan.FromMicroseconds(1)),
#endif
            ("TimeSpan.FromTicks(100)", TimeSpan.FromTicks(100)),
            ("TimeSpan.FromTicks(4715615451235648454L)", TimeSpan.FromTicks(4715615451235648454L)),
        ];

        foreach (var @case in cases)
        {
            var result = await client.Create(
                "timespan",
                new TimeSpanRecord { Name = @case.ToString(), Value = @case.Value, }
            );

            result
                .Value.Should()
                .Be(
                    @case.Value,
                    because: $"Cannot serialize/deserialize successfully for test case {@case.Name}"
                );
        }
    }
}
