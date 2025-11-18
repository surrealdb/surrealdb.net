using System.Linq.Expressions;
using System.Numerics;
using SurrealDb.Net.Tests.Queryable.Models;

namespace SurrealDb.Net.Tests.Queryable.Value;

public class DataTypesValueTests : BaseQueryableTests
{
    [Test]
    public void NullType()
    {
        string query = ToSurql(Expression.Constant(null));

        query.Should().Be("null");
    }

    [Test]
    public void NoneType()
    {
        string query = ToSurql(Expression.Constant(new None()));

        query.Should().Be("NONE");
    }

    [Test]
    [Arguments(true, "True")]
    [Arguments(false, "False")]
    public void BoolType(bool value, string expectedValue)
    {
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be(expectedValue);
    }

    [Test]
    public void SbyteType()
    {
        const sbyte value = 42;
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be("42");
    }

    [Test]
    public void ByteType()
    {
        const byte value = 42;
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be("42");
    }

    [Test]
    public void ShortType()
    {
        const short value = 42;
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be("42");
    }

    [Test]
    public void UshortType()
    {
        const ushort value = 42;
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be("42");
    }

    [Test]
    public void IntType()
    {
        const int value = 42;
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be("42");
    }

    [Test]
    public void UintType()
    {
        const uint value = 42;
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be("42");
    }

    [Test]
    public void LongType()
    {
        const long value = 42;
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be("42");
    }

    [Test]
    public void UlongType()
    {
        const ulong value = 42;
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be("42");
    }

    [Test]
    public void FloatType()
    {
        const float value = 21.57f;
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be("21.57");
    }

    [Test]
    public void DoubleType()
    {
        const double value = 21.57d;
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be("21.57");
    }

    [Test]
    public void DecimalType()
    {
        const decimal value = 21.57m;
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be("21.57dec");
    }

    [Test]
    public void CharType()
    {
        const char value = ':';
        string query = ToSurql(Expression.Constant(value));

        query
            .Should()
            .Be(
                """
                ":"
                """
            );
    }

    [Test]
    public void QuoteAsChar()
    {
        const char value = '"';
        string query = ToSurql(Expression.Constant(value));

        query
            .Should()
            .Be(
                """
                '"'
                """
            );
    }

    [Test]
    public void StringType()
    {
        const string value = "Hello, world!";
        string query = ToSurql(Expression.Constant(value));

        query
            .Should()
            .Be(
                """
                "Hello, world!"
                """
            );
    }

    [Test]
    public void StringTypeWithQuotes()
    {
        const string value = "Hello, \"world\"!";
        string query = ToSurql(Expression.Constant(value));

        query
            .Should()
            .Be(
                """
                "Hello, \"world\"!"
                """
            );
    }

    [Test]
    [Arguments(TestEnum.Alpha, "\"Alpha\"")]
    [Arguments(TestEnum.Beta, "\"Beta\"")]
    public void EnumType(TestEnum value, string expected)
    {
        string query = ToSurql(Expression.Constant(value));

        query.Should().Be(expected);
    }

    [Test]
    public void DurationType()
    {
        string query = ToSurql(Expression.Constant(new Duration(100, 1495)));

        query.Should().Be("1m40s1us495ns");
    }

    [Test]
    public void DurationTypeFromTimeSpan()
    {
        string query = ToSurql(Expression.Constant(new TimeSpan(1, 2, 3, 4, 5, 6)));

        query.Should().Be("1d2h3m4s5ms6us");
    }

    [Test]
    public void DateTimeType()
    {
        string query = ToSurql(
            Expression.Constant(new DateTime(2025, 5, 15, 0, 0, 0, DateTimeKind.Utc))
        );

        query
            .Should()
            .Be(
                """
                d"2025-05-15T00:00:00.0000000Z"
                """
            );
    }

    [Test]
    public void GuidType()
    {
        string query = ToSurql(
            Expression.Constant(new Guid("a8f30d8b-db67-47ec-8b38-ef703e05ad1b"))
        );

        query
            .Should()
            .Be(
                """
                u"a8f30d8b-db67-47ec-8b38-ef703e05ad1b"
                """
            );
    }

    [Test]
    public void Vector2Type()
    {
        string query = ToSurql(Expression.Constant(new Vector2(1, 2)));

        query
            .Should()
            .Be(
                """
                [1, 2]
                """
            );
    }

    [Test]
    public void RecordIdType()
    {
        // 💡 We avoid stringify RecordId by using query parameters (that will use existing serialization)
        string query = ToSurql(Expression.Constant(new RecordIdOfString("table", "id")));

        query.Should().Be("$_rid0");
    }

    [Test]
    public void RecordIdTypeWithMultipleInstances()
    {
        // 💡 We avoid stringify RecordId by using query parameters (that will use existing serialization)
        string query = ToSurql(
            Expression.MakeBinary(
                ExpressionType.Equal,
                Expression.Constant(new RecordIdOfString("table", "id")),
                Expression.Constant(new RecordIdOfString("table", "id"))
            )
        );

        query.Should().Be("$_rid0 == $_rid1");
    }
}
