using System.Text;
using SurrealDb.Net.Exceptions;

namespace SurrealDb.Net.Tests;

public class InsertRelationTests
{
    /// <summary>
    /// This test exists to do regression testing for this issue:
    /// https://github.com/surrealdb/surrealdb.net/issues/151
    /// </summary>
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task EnsureInsertRelationWithoutClientInitialization(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        WroteRelation? result = null;
        var now = DateTime.UtcNow;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(
                connectionString,
                dbInfo.Namespace,
                dbInfo.Database
            );
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            result = await client.InsertRelation(
                new WroteRelation
                {
                    Id = ("wrote", "w1"),
                    In = ("user", "u1"),
                    Out = ("post", "p1"),
                    CreatedAt = now,
                    NumberOfPages = 144,
                }
            );
        };

        if (version.Major < 2)
        {
            await func.Should().ThrowAsync<NotImplementedException>();
            return;
        }

        await func.Should().NotThrowAsync();

        result
            .Should()
            .NotBeNull()
            .And.BeEquivalentTo(
                new WroteRelation
                {
                    Id = ("wrote", "w1"),
                    In = ("user", "u1"),
                    Out = ("post", "p1"),
                    CreatedAt = now,
                    NumberOfPages = 144,
                }
            );
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldInsertRelationFromRecord(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        WroteRelation? result = null;
        var now = DateTime.UtcNow;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.InsertRelation(
                new WroteRelation
                {
                    Id = ("wrote", "w1"),
                    In = ("user", "u1"),
                    Out = ("post", "p1"),
                    CreatedAt = now,
                    NumberOfPages = 144,
                }
            );
        };

        if (version.Major < 2)
        {
            await func.Should().ThrowAsync<NotImplementedException>();
            return;
        }

        await func.Should().NotThrowAsync();

        result
            .Should()
            .NotBeNull()
            .And.BeEquivalentTo(
                new WroteRelation
                {
                    Id = ("wrote", "w1"),
                    In = ("user", "u1"),
                    Out = ("post", "p1"),
                    CreatedAt = now,
                    NumberOfPages = 144,
                }
            );
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldFailToInsertRelationFromRecordIfNoId(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        WroteRelation? result = null;
        var now = DateTime.UtcNow;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.InsertRelation(
                new WroteRelation
                {
                    In = ("user", "u1"),
                    Out = ("post", "p1"),
                    CreatedAt = now,
                    NumberOfPages = 144,
                }
            );
        };

        if (version.Major < 2)
        {
            await func.Should().ThrowAsync<NotImplementedException>();
            return;
        }

        await func.Should()
            .ThrowAsync<SurrealDbException>()
            .WithMessage("Cannot create a relation record without an Id");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldInsertRelationFromTable(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        WroteRelation? result = null;
        var now = DateTime.UtcNow;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.InsertRelation(
                "wrote",
                new WroteRelation
                {
                    In = ("user", "u1"),
                    Out = ("post", "p1"),
                    CreatedAt = now,
                    NumberOfPages = 144,
                }
            );
        };

        if (version.Major < 2)
        {
            await func.Should().ThrowAsync<NotImplementedException>();
            return;
        }

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Id.Should().NotBeNull();
        result.Id!.Table.Should().Be("wrote");

        var resultId = result.Id;

        result
            .Should()
            .BeEquivalentTo(
                new WroteRelation
                {
                    Id = resultId,
                    In = ("user", "u1"),
                    Out = ("post", "p1"),
                    CreatedAt = now,
                    NumberOfPages = 144,
                }
            );
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldFailToInsertRelationFromTableIfAnIdIsPresentInTheRecord(
        string connectionString
    )
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        WroteRelation? result = null;
        var now = DateTime.UtcNow;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.InsertRelation(
                "wrote",
                new WroteRelation
                {
                    Id = ("wrote", "w1"),
                    In = ("user", "u1"),
                    Out = ("post", "p1"),
                    CreatedAt = now,
                    NumberOfPages = 144,
                }
            );
        };

        if (version.Major < 2)
        {
            await func.Should().ThrowAsync<NotImplementedException>();
            return;
        }

        await func.Should()
            .ThrowAsync<SurrealDbException>()
            .WithMessage(
                "You cannot provide both the table and an Id for the record. Either use the method overload without 'table' param or set the Id property to null."
            );
    }
}
