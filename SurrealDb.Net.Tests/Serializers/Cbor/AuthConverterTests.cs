using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Cbor.Converters.Auth;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class AuthConverterTests : BaseCborConverterTests
{
    [Test]
    public void ShouldBeRegistered()
    {
        var converters = SurrealDbCborOptions.Default.Value.Registry.ConverterRegistry;

        converters.Lookup<RootAuth>().Should().BeOfType<RootAuthConverter>();
        converters.Lookup<NamespaceAuth>().Should().BeOfType<NamespaceAuthConverter>();
        converters.Lookup<DatabaseAuth>().Should().BeOfType<DatabaseAuthConverter>();
        converters.Lookup<ScopeAuth>().Should().BeOfType<ScopeAuthConverter>();
    }

    [Test]
    public void SerializeRootAuth()
    {
        var value = new RootAuth { Username = "root", Password = "secret" };

        var result = SerializeAsHexa(value);

        // {"user": "root", "pass": "secret"}
        result.Should().Be("a2647573657264726f6f74647061737366736563726574");
        result.Should().Be(SerializeWithReflectionAsHexa(value));
    }

    [Test]
    public void SerializeNamespaceAuth()
    {
        var value = new NamespaceAuth
        {
            Namespace = "test",
            Username = "root",
            Password = "secret",
        };

        var result = SerializeAsHexa(value);

        result.Should().Be(SerializeWithReflectionAsHexa(value));
    }

    [Test]
    public void SerializeDatabaseAuth()
    {
        var value = new DatabaseAuth
        {
            Namespace = "test",
            Database = "test",
            Username = "root",
            Password = "secret",
        };

        var result = SerializeAsHexa(value);

        result.Should().Be(SerializeWithReflectionAsHexa(value));
    }

    [Test]
    public void SerializeScopeAuth()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var value = new ScopeAuth
        {
            Namespace = "test",
            Database = "test",
            Scope = "user",
            Access = "user",
        };
#pragma warning restore CS0618 // Type or member is obsolete

        var result = SerializeAsHexa(value);

        result.Should().Be(SerializeWithReflectionAsHexa(value));
    }

    [Test]
    public void SerializeDerivedScopeAuthWithItsOwnConverter()
    {
        ScopeAuth value = new UserScopeAuth
        {
            Namespace = "test",
            Database = "test",
            Access = "user",
            Email = "john@example.com",
        };

        var result = SerializeAsHexa(value);

        result.Should().Be(SerializeWithReflectionAsHexa((UserScopeAuth)value));
    }

    [Test]
    public async Task DeserializeDatabaseAuth()
    {
        var value = new DatabaseAuth
        {
            Namespace = "ns",
            Database = "db",
            Username = "root",
            Password = "secret",
        };

        var result = await DeserializeCborBinaryAsHexaAsync<DatabaseAuth>(SerializeAsHexa(value));

        result.Should().BeEquivalentTo(value);
    }

    [Test]
    public async Task DeserializeShouldIgnoreUnknownProperties()
    {
        // {"unknown": [1, 2], "user": "root", "pass": "secret"}
        var result = await DeserializeCborBinaryAsHexaAsync<RootAuth>(
            "a367756e6b6e6f776e820102647573657264726f6f74647061737366736563726574"
        );

        result.Username.Should().Be("root");
        result.Password.Should().Be("secret");
    }

    [Test]
    public async Task DeserializeScopeAuth()
    {
        var value = new ScopeAuth
        {
            Namespace = "ns",
            Database = "db",
            Access = "user",
        };

        var result = await DeserializeCborBinaryAsHexaAsync<ScopeAuth>(SerializeAsHexa(value));

        result.Should().BeEquivalentTo(value);
    }

    private sealed class UserScopeAuth : ScopeAuth
    {
        public string Email { get; set; } = string.Empty;
    }
}
