using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Semver;

namespace SurrealDb.Net.Tests.Extensions;

public static class StringAssertionsExtensions
{
    public static AndConstraint<StringAssertions> BeNanoid(this StringAssertions assertions)
    {
        const string nanoidPattern = "[a-z0-9]{20}";

        Execute
            .Assertion.ForCondition(
                assertions.Subject != null && Regex.IsMatch(assertions.Subject, nanoidPattern)
            )
            .FailWith(
                $"Expected {{context:string}} to be a nanoid, but found {{0}}",
                assertions.Subject
            );

        return new AndConstraint<StringAssertions>(assertions);
    }

    public static AndConstraint<StringAssertions> BeUlid(this StringAssertions assertions)
    {
        Execute
            .Assertion.ForCondition(assertions.Subject != null && assertions.Subject.Length == 26)
            .FailWith(
                $"Expected {{context:string}} to be a ULID, but found {{0}}",
                assertions.Subject
            );

        return new AndConstraint<StringAssertions>(assertions);
    }

    public static AndConstraint<StringAssertions> BeUuid(this StringAssertions assertions)
    {
        Execute
            .Assertion.ForCondition(
                assertions.Subject != null && Guid.TryParse(assertions.Subject, out _)
            )
            .FailWith(
                $"Expected {{context:string}} to be a UUID, but found {{0}}",
                assertions.Subject
            );

        return new AndConstraint<StringAssertions>(assertions);
    }

    public static AndConstraint<StringAssertions> BeValidJwt(this StringAssertions assertions)
    {
        // TODO : Use System.IdentityModel.Tokens.Jwt library to check if the JWT is valid?
        const string jwtRegexPattern = @"^[A-Za-z0-9-_=]+\.[A-Za-z0-9-_=]+\.?[A-Za-z0-9-_.+/=]*$";

        Execute
            .Assertion.ForCondition(!string.IsNullOrWhiteSpace(assertions.Subject))
            .FailWith(
                $"Expected a non-null and non-empty JWT, but found {{0}}.",
                assertions.Subject
            );

        Execute
            .Assertion.ForCondition(Regex.IsMatch(assertions.Subject, jwtRegexPattern))
            .FailWith($"Expected a valid JWT, but found {{0}}.", assertions.Subject);

        return new AndConstraint<StringAssertions>(assertions);
    }

    public static AndConstraint<StringAssertions> BeValidSemver(
        this StringAssertions assertions,
        string? prefix = null,
        string? suffix = null
    )
    {
        string escapedPrefix = Regex.Escape(prefix ?? string.Empty);
        string escapedSuffix = Regex.Escape(suffix ?? string.Empty);
        string regexPattern = $"^{escapedPrefix}(.*){escapedSuffix}$";

        var match = Regex.Match(assertions.Subject, regexPattern);

        if (!match.Success)
        {
            Execute
                .Assertion.ForCondition(false)
                .FailWith(
                    $"Cannot match prefix '{escapedPrefix}' or suffix '{escapedSuffix}' in {{context:string}}",
                    assertions.Subject
                );

            return new AndConstraint<StringAssertions>(assertions);
        }

        var semverCandidate = match.Groups[1].Value;

        Execute
            .Assertion.ForCondition(
                assertions.Subject != null
                    && SemVersion.TryParse(semverCandidate, SemVersionStyles.Strict, out _)
            )
            .FailWith(
                $"Expected {{context:string}} to be a valid semver, but found {{0}}",
                assertions.Subject
            );

        return new AndConstraint<StringAssertions>(assertions);
    }
}
