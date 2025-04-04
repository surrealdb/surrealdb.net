﻿using Range = SurrealDb.Net.Models.Range;

namespace SurrealDb.Net.Tests.Models;

public class ImplicitRangeTests
{
    [Test]
    public void CreateRangeFromSystemAllRange()
    {
        var result = Range.FromRange(..);
        result.Should().BeEquivalentTo(new Range<int, int>());
    }

    [Test]
    public void CreateRangeFromSystemStandardRange()
    {
        var result = Range.FromRange(2..10);
        result
            .Should()
            .BeEquivalentTo(new Range<int, int>(RangeBound.Inclusive(2), RangeBound.Exclusive(10)));
    }

    [Test]
    public void CreateRangeFromSystemLeftOnlyRange()
    {
        var result = Range.FromRange(2..);
        result.Should().BeEquivalentTo(new Range<int, int>(RangeBound.Inclusive(2), default));
    }

    [Test]
    public void CreateRangeFromSystemRightOnlyRange()
    {
        var result = Range.FromRange(..2);
        result.Should().BeEquivalentTo(new Range<int, int>(default, RangeBound.Exclusive(2)));
    }

    [Test]
    public void ShouldFailToCreateFromSystemRangeWithEndFrom()
    {
        {
            var act = () => Range.FromRange(^1..);
            act.Should()
                .Throw<NotSupportedException>()
                .WithMessage("Failed to convert because one index of the range is 'fromEnd'.");
        }

        {
            var act = () => Range.FromRange(..^2);
            act.Should()
                .Throw<NotSupportedException>()
                .WithMessage("Failed to convert because one index of the range is 'fromEnd'.");
        }

        {
            var act = () => Range.FromRange(^3..^4);
            act.Should()
                .Throw<NotSupportedException>()
                .WithMessage("Failed to convert because one index of the range is 'fromEnd'.");
        }

        {
            var act = () => Range.FromRange(^5..4);
            act.Should()
                .Throw<NotSupportedException>()
                .WithMessage("Failed to convert because one index of the range is 'fromEnd'.");
        }

        {
            var act = () => Range.FromRange(4..^5);
            act.Should()
                .Throw<NotSupportedException>()
                .WithMessage("Failed to convert because one index of the range is 'fromEnd'.");
        }
    }
}
