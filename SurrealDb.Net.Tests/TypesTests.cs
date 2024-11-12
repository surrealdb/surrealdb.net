using System.Numerics;
using System.Text;
using FluentAssertions.Extensions;

namespace SurrealDb.Net.Tests;

public class TypesTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSupportString(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/string.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        await client.Delete("string");

        await client.Create("string", new NoneRecord { Name = "none", Value = new() });
        await client.Create(
            "string",
            new StringRecord { Name = "single-quote", Value = "Lorem ipsum dolor sit amet" }
        );
        await client.Create(
            "string",
            new StringRecord { Name = "double-quote", Value = "Lorem ipsum dolor sit amet" }
        );
        await client.Create(
            "string",
            new StringRecord { Name = "unicode", Value = "I ❤️ SurrealDB" }
        );

        var multilineStringBuilder = new StringBuilder();
        multilineStringBuilder.AppendLine("This");
        multilineStringBuilder.AppendLine("is");
        multilineStringBuilder.AppendLine("over");
        multilineStringBuilder.AppendLine("multiple");
        multilineStringBuilder.Append("lines");
        await client.Create(
            "string",
            new StringRecord { Name = "multiline", Value = multilineStringBuilder.ToString() }
        );

        var records = await client.Select<StringRecord>("string");

        {
            var noneRecord = records.First(r => r.Name == "none");
            noneRecord.Should().NotBeNull();
            noneRecord!.Value.Should().BeNull();
        }

        {
            var singleQuoteRecord = records.First(r => r.Name == "single-quote");
            singleQuoteRecord.Should().NotBeNull();
            singleQuoteRecord!.Value.Should().Be("Lorem ipsum dolor sit amet");
        }

        {
            var doubleQuoteRecord = records.First(r => r.Name == "double-quote");
            doubleQuoteRecord.Should().NotBeNull();
            doubleQuoteRecord!.Value.Should().Be("Lorem ipsum dolor sit amet");
        }

        {
            var unicodeRecord = records.First(r => r.Name == "unicode");
            unicodeRecord.Should().NotBeNull();
            unicodeRecord!.Value.Should().Be("I ❤️ SurrealDB");
        }

        {
            var expectedStringBuilder = new StringBuilder();
            expectedStringBuilder.AppendLine("This");
            expectedStringBuilder.AppendLine("is");
            expectedStringBuilder.AppendLine("over");
            expectedStringBuilder.AppendLine("multiple");
            expectedStringBuilder.Append("lines");

            string expected = expectedStringBuilder.ToString();

            var multilineRecord = records.First(r => r.Name == "multiline");
            multilineRecord.Should().NotBeNull();
            multilineRecord!.Value.Should().Be(expected);
        }
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSupportLong(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/number.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        await client.Delete("number");

        await client.Create("number", new NoneRecord { Name = "none", Value = new() });
        await client.Create(
            "number",
            new LongRecord { Name = "min", Value = -9223372036854775808 }
        );
        await client.Create("number", new LongRecord { Name = "max", Value = 9223372036854775807 });
        await client.Create("number", new LongRecord { Name = "zero", Value = 0 });

        var records = await client.Select<LongRecord>("number");

        {
            var noneRecord = records.First(r => r.Name == "none");
            noneRecord.Should().NotBeNull();
            noneRecord!.Value.Should().BeNull();
        }

        {
            var minRecord = records.First(r => r.Name == "min");
            minRecord.Should().NotBeNull();
            minRecord!.Value.Should().Be(-9223372036854775808);
        }

        {
            var maxRecord = records.First(r => r.Name == "max");
            maxRecord.Should().NotBeNull();
            maxRecord!.Value.Should().Be(9223372036854775807);
        }

        {
            var zeroRecord = records.First(r => r.Name == "zero");
            zeroRecord.Should().NotBeNull();
            zeroRecord!.Value.Should().Be(0);
        }
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShoulSupportDecimal(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/decimal.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        await client.Delete("decimal");

        await client.Create("decimal", new NoneRecord { Name = "none", Value = new() });
        await client.Create(
            "decimal",
            new DecimalRecord { Name = "min", Value = -9223372036854775808 }
        );
        await client.Create(
            "decimal",
            new DecimalRecord { Name = "max", Value = 9223372036854775807 }
        );
        await client.Create("decimal", new DecimalRecord { Name = "zero", Value = 0 });
        await client.Create("decimal", new DecimalRecord { Name = "decimal", Value = 41.5m });
        await client.Create(
            "decimal",
            new DecimalRecord
            {
                Name = "decimal-precision",
                Value = 13.5719384719384719385639856394139476937756394756m
            }
        );
        await client.Create(
            "decimal",
            new DecimalRecord
            {
                Name = "float",
                Value = 13.5719384719384719385639856394139476937756394756m
            }
        );

        var records = await client.Select<DecimalRecord>("decimal");

        {
            var noneRecord = records.First(r => r.Name == "none");
            noneRecord.Should().NotBeNull();
            noneRecord!.Value.Should().BeNull();
        }

        {
            var minRecord = records.First(r => r.Name == "min");
            minRecord.Should().NotBeNull();
            minRecord!.Value.Should().Be(-9223372036854775808);
        }

        {
            var maxRecord = records.First(r => r.Name == "max");
            maxRecord.Should().NotBeNull();
            maxRecord!.Value.Should().Be(9223372036854775807);
        }

        {
            var zeroRecord = records.First(r => r.Name == "zero");
            zeroRecord.Should().NotBeNull();
            zeroRecord!.Value.Should().Be(0);
        }

        {
            var decimalRecord = records.First(r => r.Name == "decimal");
            decimalRecord.Should().NotBeNull();
            decimalRecord!.Value.Should().Be(41.5m);
        }

        {
            var decimalPrecisionRecord = records.First(r => r.Name == "decimal-precision");
            decimalPrecisionRecord.Should().NotBeNull();
            decimalPrecisionRecord!
                .Value.Should()
                .Be(13.5719384719384719385639856394139476937756394756m);
        }

        {
            var floatRecord = records.First(r => r.Name == "float");
            floatRecord.Should().NotBeNull();
            floatRecord!.Value.Should().Be(13.571938471938471938563985639m);
        }
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSupportFloat(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/decimal.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        await client.Delete("decimal");

        await client.Create("decimal", new NoneRecord { Name = "none", Value = new() });
        await client.Create(
            "decimal",
            new FloatRecord { Name = "min", Value = -9223372036854775808 }
        );
        await client.Create(
            "decimal",
            new FloatRecord { Name = "max", Value = 9223372036854775807 }
        );
        await client.Create("decimal", new FloatRecord { Name = "zero", Value = 0 });
        await client.Create("decimal", new FloatRecord { Name = "decimal", Value = (float)41.5m });
        await client.Create(
            "decimal",
            new FloatRecord
            {
                Name = "decimal-precision",
                Value = (float)13.5719384719384719385639856394139476937756394756m
            }
        );
        await client.Create(
            "decimal",
            new FloatRecord
            {
                Name = "float",
                Value = (float)13.5719384719384719385639856394139476937756394756m
            }
        );

        var records = await client.Select<FloatRecord>("decimal");

        {
            var noneRecord = records.First(r => r.Name == "none");
            noneRecord.Should().NotBeNull();
            noneRecord!.Value.Should().BeNull();
        }

        {
            var minRecord = records.First(r => r.Name == "min");
            minRecord.Should().NotBeNull();
            minRecord!.Value.Should().Be(-9223372036854775808);
        }

        {
            var maxRecord = records.First(r => r.Name == "max");
            maxRecord.Should().NotBeNull();
            maxRecord!.Value.Should().Be(9223372036854775807);
        }

        {
            var zeroRecord = records.First(r => r.Name == "zero");
            zeroRecord.Should().NotBeNull();
            zeroRecord!.Value.Should().Be(0);
        }

        {
            var decimalRecord = records.First(r => r.Name == "decimal");
            decimalRecord.Should().NotBeNull();
            decimalRecord!.Value.Should().Be(41.5f);
        }

        {
            var decimalPrecisionRecord = records.First(r => r.Name == "decimal-precision");
            decimalPrecisionRecord.Should().NotBeNull();
            decimalPrecisionRecord!
                .Value.Should()
                .Be(13.5719384719384719385639856394139476937756394756f);
        }

        {
            var floatRecord = records.First(r => r.Name == "float");
            floatRecord.Should().NotBeNull();
            floatRecord!.Value.Should().Be(13.571938471938473f);
        }
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSupportDouble(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/decimal.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        await client.Delete("decimal");

        await client.Create("decimal", new NoneRecord { Name = "none", Value = new() });
        await client.Create(
            "decimal",
            new DoubleRecord { Name = "min", Value = -9223372036854775808 }
        );
        await client.Create(
            "decimal",
            new DoubleRecord { Name = "max", Value = 9223372036854775807 }
        );
        await client.Create("decimal", new DoubleRecord { Name = "zero", Value = 0 });
        await client.Create("decimal", new DoubleRecord { Name = "decimal", Value = 41.5 });
        await client.Create(
            "decimal",
            new DoubleRecord
            {
                Name = "decimal-precision",
                Value = 13.5719384719384719385639856394139476937756394756
            }
        );
        await client.Create(
            "decimal",
            new DoubleRecord
            {
                Name = "float",
                Value = 13.5719384719384719385639856394139476937756394756
            }
        );

        var records = await client.Select<DoubleRecord>("decimal");

        {
            var noneRecord = records.First(r => r.Name == "none");
            noneRecord.Should().NotBeNull();
            noneRecord!.Value.Should().BeNull();
        }

        {
            var minRecord = records.First(r => r.Name == "min");
            minRecord.Should().NotBeNull();
            minRecord!.Value.Should().Be(-9223372036854775808);
        }

        {
            var maxRecord = records.First(r => r.Name == "max");
            maxRecord.Should().NotBeNull();
            maxRecord!.Value.Should().Be(9223372036854775807);
        }

        {
            var zeroRecord = records.First(r => r.Name == "zero");
            zeroRecord.Should().NotBeNull();
            zeroRecord!.Value.Should().Be(0);
        }

        {
            var decimalRecord = records.First(r => r.Name == "decimal");
            decimalRecord.Should().NotBeNull();
            decimalRecord!.Value.Should().Be(41.5f);
        }

        {
            var decimalPrecisionRecord = records.First(r => r.Name == "decimal-precision");
            decimalPrecisionRecord.Should().NotBeNull();
            decimalPrecisionRecord!
                .Value.Should()
                .Be(13.5719384719384719385639856394139476937756394756d);
        }

        {
            var floatRecord = records.First(r => r.Name == "float");
            floatRecord.Should().NotBeNull();
            floatRecord!.Value.Should().Be(13.571938471938472d);
        }
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSupportDuration(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/duration.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        await client.Delete("duration");

        await client.Create("duration", new NoneRecord { Name = "none", Value = new() });
        await client.Create(
            "duration",
            new DurationRecord { Name = "nanosecond", Value = new Duration("2ns") }
        );
        await client.Create(
            "duration",
            new DurationRecord { Name = "microsecond", Value = new Duration("3µs") }
        );
        await client.Create(
            "duration",
            new DurationRecord { Name = "microsecond-alias", Value = new Duration("4us") }
        );
        await client.Create(
            "duration",
            new DurationRecord { Name = "millisecond", Value = new Duration("50ms") }
        );
        await client.Create(
            "duration",
            new DurationRecord { Name = "second", Value = new Duration("7s") }
        );
        await client.Create(
            "duration",
            new DurationRecord { Name = "minute", Value = new Duration("5m") }
        );
        await client.Create(
            "duration",
            new DurationRecord { Name = "hour", Value = new Duration("1h") }
        );
        await client.Create(
            "duration",
            new DurationRecord { Name = "day", Value = new Duration("6d") }
        );
        await client.Create(
            "duration",
            new DurationRecord { Name = "week", Value = new Duration("28w") }
        );
        await client.Create(
            "duration",
            new DurationRecord { Name = "year", Value = new Duration("12y") }
        );
        await client.Create(
            "duration",
            new DurationRecord { Name = "complex", Value = new Duration("1h30m20s1350ms") }
        );

        var records = await client.Select<DurationRecord>("duration");

        {
            var noneRecord = records.First(r => r.Name == "none");
            noneRecord.Should().NotBeNull();

            noneRecord!.Value.NanoSeconds.Should().Be(0);
            noneRecord!.Value.MilliSeconds.Should().Be(0);
            noneRecord!.Value.MicroSeconds.Should().Be(0);
            noneRecord!.Value.Seconds.Should().Be(0);
            noneRecord!.Value.Minutes.Should().Be(0);
            noneRecord!.Value.Hours.Should().Be(0);
            noneRecord!.Value.Days.Should().Be(0);
            noneRecord!.Value.Weeks.Should().Be(0);
            noneRecord!.Value.Years.Should().Be(0);
        }

        {
            var nanosecondRecord = records.First(r => r.Name == "nanosecond");
            nanosecondRecord.Should().NotBeNull();
            nanosecondRecord!.Value.NanoSeconds.Should().Be(2);
        }

        {
            var microsecondRecord = records.First(r => r.Name == "microsecond");
            microsecondRecord.Should().NotBeNull();
            microsecondRecord!.Value.MicroSeconds.Should().Be(3);
        }

        {
            var microsecondAliasRecord = records.First(r => r.Name == "microsecond-alias");
            microsecondAliasRecord.Should().NotBeNull();
            microsecondAliasRecord!.Value.MicroSeconds.Should().Be(4);
        }

        {
            var millisecondRecord = records.First(r => r.Name == "millisecond");
            millisecondRecord.Should().NotBeNull();
            millisecondRecord!.Value.MilliSeconds.Should().Be(50);
        }

        {
            var secondRecord = records.First(r => r.Name == "second");
            secondRecord.Should().NotBeNull();
            secondRecord!.Value.Seconds.Should().Be(7);
        }

        {
            var minuteRecord = records.First(r => r.Name == "minute");
            minuteRecord.Should().NotBeNull();
            minuteRecord!.Value.Minutes.Should().Be(5);
        }

        {
            var hourRecord = records.First(r => r.Name == "hour");
            hourRecord.Should().NotBeNull();
            hourRecord!.Value.Hours.Should().Be(1);
        }

        {
            var dayRecord = records.First(r => r.Name == "day");
            dayRecord.Should().NotBeNull();
            dayRecord!.Value.Days.Should().Be(6);
        }

        {
            var weekRecord = records.First(r => r.Name == "week");
            weekRecord.Should().NotBeNull();
            weekRecord!.Value.Weeks.Should().Be(28);
        }

        {
            var yearRecord = records.First(r => r.Name == "year");
            yearRecord.Should().NotBeNull();
            yearRecord!.Value.Years.Should().Be(12);
        }

        {
            var complexRecord = records.First(r => r.Name == "complex");
            complexRecord.Should().NotBeNull();
            complexRecord!.Value.MilliSeconds.Should().Be(350);
            complexRecord!.Value.Seconds.Should().Be(21);
            complexRecord!.Value.Minutes.Should().Be(30);
            complexRecord!.Value.Hours.Should().Be(1);
        }
    }

#if NET7_0_OR_GREATER
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSupportTimeSpan(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/duration.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        await client.Delete("duration");

        await client.Create("duration", new NoneRecord { Name = "none", Value = new() });
        await client.Create(
            "duration",
            new TimeSpanRecord { Name = "nanosecond", Value = new TimeSpan() }
        );
        await client.Create(
            "duration",
            new TimeSpanRecord { Name = "microsecond", Value = TimeSpan.FromMicroseconds(3) }
        );
        await client.Create(
            "duration",
            new TimeSpanRecord { Name = "microsecond-alias", Value = TimeSpan.FromMicroseconds(4) }
        );
        await client.Create(
            "duration",
            new TimeSpanRecord { Name = "millisecond", Value = TimeSpan.FromMilliseconds(50) }
        );
        await client.Create(
            "duration",
            new TimeSpanRecord { Name = "second", Value = TimeSpan.FromSeconds(7) }
        );
        await client.Create(
            "duration",
            new TimeSpanRecord { Name = "minute", Value = TimeSpan.FromMinutes(5) }
        );
        await client.Create(
            "duration",
            new TimeSpanRecord { Name = "hour", Value = TimeSpan.FromHours(1) }
        );
        await client.Create(
            "duration",
            new TimeSpanRecord { Name = "day", Value = TimeSpan.FromDays(6) }
        );
        await client.Create(
            "duration",
            new TimeSpanRecord { Name = "week", Value = TimeSpan.FromDays(7 * 28) }
        );
        await client.Create(
            "duration",
            new TimeSpanRecord { Name = "year", Value = TimeSpan.FromDays(365 * 12) }
        );

        var complexDuration = new TimeSpan(0, 1, 30, 21, 350);
        await client.Create(
            "duration",
            new TimeSpanRecord { Name = "complex", Value = complexDuration }
        );

        var records = await client.Select<TimeSpanRecord>("duration");

        {
            var noneRecord = records.First(r => r.Name == "none");
            noneRecord.Should().NotBeNull();
            noneRecord!.Value.TotalSeconds.Should().Be(0);
        }

        {
            var nanosecondRecord = records.First(r => r.Name == "nanosecond");
            nanosecondRecord.Should().NotBeNull();
            nanosecondRecord!.Value.TotalNanoseconds().Should(); // TODO : Value = 2
        }

        {
            var microsecondRecord = records.First(r => r.Name == "microsecond");
            microsecondRecord.Should().NotBeNull();
            microsecondRecord!.Value.TotalMicroseconds().Should().Be(3);
        }

        {
            var microsecondAliasRecord = records.First(r => r.Name == "microsecond-alias");
            microsecondAliasRecord.Should().NotBeNull();
            microsecondAliasRecord!.Value.TotalMicroseconds().Should().Be(4);
        }

        {
            var millisecondRecord = records.First(r => r.Name == "millisecond");
            millisecondRecord.Should().NotBeNull();
            millisecondRecord!.Value.TotalMilliseconds.Should().Be(50);
        }

        {
            var secondRecord = records.First(r => r.Name == "second");
            secondRecord.Should().NotBeNull();
            secondRecord!.Value.TotalSeconds.Should().Be(7);
        }

        {
            var minuteRecord = records.First(r => r.Name == "minute");
            minuteRecord.Should().NotBeNull();
            minuteRecord!.Value.TotalMinutes.Should().Be(5);
        }

        {
            var hourRecord = records.First(r => r.Name == "hour");
            hourRecord.Should().NotBeNull();
            hourRecord!.Value.TotalHours.Should().Be(1);
        }

        {
            var dayRecord = records.First(r => r.Name == "day");
            dayRecord.Should().NotBeNull();
            dayRecord!.Value.TotalDays.Should().Be(6);
        }

        {
            var weekRecord = records.First(r => r.Name == "week");
            weekRecord.Should().NotBeNull();
            weekRecord!.Value.TotalDays.Should().Be(196);
        }

        {
            var yearRecord = records.First(r => r.Name == "year");
            yearRecord.Should().NotBeNull();
            yearRecord!.Value.TotalDays.Should().Be(4380);
        }

        {
            var complexRecord = records.First(r => r.Name == "complex");
            complexRecord.Should().NotBeNull();
            complexRecord!.Value.TotalSeconds.Should().Be(5421.35);
        }
    }
#endif

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSupportDateTime(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/datetime.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        await client.Delete("datetime");

        await client.Create("datetime", new NoneRecord { Name = "none", Value = new() });
        await client.Create(
            "datetime",
            new DateTimeRecord
            {
                Name = "time",
                Value = new DateTime(2022, 7, 3, 7, 18, 52).AsUtc()
            }
        );
        await client.Create(
            "datetime",
            new DateTimeRecord
            {
                Name = "nano",
                Value = new DateTime(2022, 7, 3, 7, 18, 52).AddNanoseconds(841_147_000).AsUtc()
            }
        );
        await client.Create(
            "datetime",
            new DateTimeRecord
            {
                Name = "timezone",
                Value = new DateTime(2022, 7, 3, 7, 18, 52)
                    .AddNanoseconds(841_147_000)
                    .AsUtc()
                    .WithOffset(TimeSpan.FromHours(2))
                    .UtcDateTime
            }
        );
        await client.Create(
            "datetime",
            new DateTimeRecord
            {
                Name = "time+duration",
                Value = new DateTime(2022, 7, 17, 7, 18, 52).AsUtc()
            }
        );
        await client.Create(
            "datetime",
            new DateTimeRecord
            {
                Name = "nano+duration",
                Value = new DateTime(2022, 7, 3, 8, 49, 14).AddNanoseconds(191_147_000).AsUtc()
            }
        );
        await client.Create(
            "datetime",
            new DateTimeRecord
            {
                Name = "full-nano",
                Value = new DateTime(2022, 7, 3, 7, 18, 52).AddNanoseconds(123_456_789).AsUtc()
            }
        );

        var records = await client.Select<DateTimeRecord>("datetime");

        {
            var noneRecord = records.First(r => r.Name == "none");
            noneRecord.Should().NotBeNull();
            noneRecord!.Value.Should().BeNull();
        }

        {
            var timeRecord = records.First(r => r.Name == "time");
            timeRecord.Should().NotBeNull();
            timeRecord!.Value.Should().Be(new DateTime(2022, 7, 3, 7, 18, 52));
        }

        {
            var nanoRecord = records.First(r => r.Name == "nano");
            nanoRecord.Should().NotBeNull();
            nanoRecord!
                .Value.Should()
                .Be(new DateTime(2022, 7, 3, 7, 18, 52).AddNanoseconds(841_147_000));
        }

        {
            var timezoneRecord = records.First(r => r.Name == "timezone");
            timezoneRecord.Should().NotBeNull();
            timezoneRecord!
                .Value.Should()
                .Be(
                    new DateTime(2022, 7, 3, 7, 18, 52)
                        .AddNanoseconds(841_147_000)
                        .WithOffset(TimeSpan.FromHours(2))
                        .UtcDateTime
                );
        }

        {
            var timePlusDurationRecord = records.First(r => r.Name == "time+duration");
            timePlusDurationRecord.Should().NotBeNull();
            timePlusDurationRecord!.Value.Should().Be(new DateTime(2022, 7, 17, 7, 18, 52));
        }

        {
            var nanoPlusDurationRecord = records.First(r => r.Name == "nano+duration");
            nanoPlusDurationRecord.Should().NotBeNull();
            nanoPlusDurationRecord!
                .Value.Should()
                .Be(new DateTime(2022, 7, 3, 8, 49, 14).AddNanoseconds(191_147_000));
        }

        {
            var fullNanoRecord = records.First(r => r.Name == "full-nano");
            fullNanoRecord.Should().NotBeNull();
            fullNanoRecord!
                .Value.Should()
                .Be(new DateTime(2022, 7, 3, 7, 18, 52).AddNanoseconds(123_456_789));
        }
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSupportVector2(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/vector.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        await client.Delete("vector");

        await client.Create("vector", new NoneRecord { Name = "none", Value = new() });
        await client.Create(
            "vector",
            new Vector2Record { Name = "vector2", Value = new Vector2(2.5f, 0.5f) }
        );

        var records = (await client.Select<Vector2Record>("vector")).ToList();

        {
            var noneRecord = records.Find(r => r.Name == "none");
            noneRecord.Should().NotBeNull();
            noneRecord!.Value.Should().BeNull();
        }

        {
            var vector2Record = records.Find(r => r.Name == "vector2");
            vector2Record.Should().NotBeNull();
            vector2Record!.Value.Should().Be(new Vector2(2.5f, 0.5f));
        }
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldSupportThing(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        await client.Create(
            "thing",
            new ThingRecord { Name = "custom", Value = ("person", "tobie") }
        );

        var records = (await client.Select<ThingRecord>("thing")).ToList();

        {
            var customRecord = records.Find(r => r.Name == "custom");
            customRecord.Should().NotBeNull();
            customRecord!.Value.Should().NotBeNull();
            customRecord!.Value!.Table.Should().Be("person");
            customRecord!.Value!.DeserializeId<string>().Should().Be("tobie");
        }
    }
}
