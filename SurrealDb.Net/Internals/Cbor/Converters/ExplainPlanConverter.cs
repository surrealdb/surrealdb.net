using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class ExplainPlanConverter : CborConverterBase<ExplainPlan>
{
    public override ExplainPlan Read(ref CborReader reader)
    {
        if (reader.GetCurrentDataItemType() == CborDataItemType.Null)
        {
            reader.ReadNull();
            return new ExplainPlan();
        }

        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        var plan = new ExplainPlan();

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("operator"u8))
            {
                plan.Operator = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("context"u8))
            {
                plan.Context = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("attributes"u8))
            {
                plan.Attributes = CborMapToDictionaryConverter.ReadNullableMap(ref reader);
                continue;
            }

            if (key.SequenceEqual("children"u8))
            {
                plan.Children = ReadChildren(ref reader);
                continue;
            }

            if (key.SequenceEqual("expressions"u8))
            {
                plan.Expressions = ReadExpressions(ref reader);
                continue;
            }

            if (key.SequenceEqual("metrics"u8))
            {
                plan.Metrics = ReadMetrics(ref reader);
                continue;
            }

            if (key.SequenceEqual("total_rows"u8))
            {
                plan.TotalRows = reader.ReadInt64();
                continue;
            }

            reader.SkipDataItem();
        }

        return plan;
    }

    public override void Write(ref CborWriter writer, ExplainPlan value)
    {
        throw new NotSupportedException($"Cannot write {nameof(ExplainPlan)} back to CBOR.");
    }

    private List<ExplainPlan>? ReadChildren(ref CborReader reader)
    {
        if (reader.GetCurrentDataItemType() == CborDataItemType.Null)
        {
            reader.ReadNull();
            return null;
        }

        reader.ReadBeginArray();

        int size = reader.ReadSize();
        var children = new List<ExplainPlan>(size);

        for (int i = 0; i < size; i++)
        {
            var child = Read(ref reader);
            children.Add(child);
        }

        return children;
    }

    private List<ExplainPlanExpression>? ReadExpressions(ref CborReader reader)
    {
        if (reader.GetCurrentDataItemType() == CborDataItemType.Null)
        {
            reader.ReadNull();
            return null;
        }

        reader.ReadBeginArray();

        int size = reader.ReadSize();
        var expressions = new List<ExplainPlanExpression>(size);

        for (int i = 0; i < size; i++)
        {
            var expr = ReadExpression(ref reader);
            if (expr is not null)
            {
                expressions.Add(expr);
            }
        }

        return expressions;
    }

    private ExplainPlanExpression? ReadExpression(ref CborReader reader)
    {
        if (reader.GetCurrentDataItemType() == CborDataItemType.Null)
        {
            reader.ReadNull();
            return null;
        }

        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        string? role = null;
        string? sql = null;
        List<ExplainEmbeddedOperator>? embeddedOperators = null;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("role"u8))
            {
                role = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("sql"u8))
            {
                sql = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("embedded_operators"u8))
            {
                embeddedOperators = ReadEmbeddedOperators(ref reader);
                continue;
            }

            reader.SkipDataItem();
        }

        if (role is null || sql is null)
        {
            return null;
        }

        return new ExplainPlanExpression
        {
            Role = role,
            Sql = sql,
            EmbeddedOperators = embeddedOperators,
        };
    }

    private List<ExplainEmbeddedOperator>? ReadEmbeddedOperators(ref CborReader reader)
    {
        if (reader.GetCurrentDataItemType() == CborDataItemType.Null)
        {
            reader.ReadNull();
            return null;
        }

        reader.ReadBeginArray();

        int size = reader.ReadSize();
        var embedded = new List<ExplainEmbeddedOperator>(size);

        for (int i = 0; i < size; i++)
        {
            var item = ReadEmbeddedOperator(ref reader);
            if (item is not null)
            {
                embedded.Add(item);
            }
        }

        return embedded;
    }

    private ExplainEmbeddedOperator? ReadEmbeddedOperator(ref CborReader reader)
    {
        if (reader.GetCurrentDataItemType() == CborDataItemType.Null)
        {
            reader.ReadNull();
            return null;
        }

        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        string? role = null;
        ExplainPlan? plan = null;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("role"u8))
            {
                role = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("plan"u8))
            {
                plan = Read(ref reader);
                continue;
            }

            reader.SkipDataItem();
        }

        if (role is null || plan is null)
        {
            return null;
        }

        return new ExplainEmbeddedOperator { Role = role, Plan = plan };
    }

    private static ExplainMetrics? ReadMetrics(ref CborReader reader)
    {
        if (reader.GetCurrentDataItemType() == CborDataItemType.Null)
        {
            reader.ReadNull();
            return null;
        }

        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        var metrics = new ExplainMetrics();

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("output_rows"u8))
            {
                metrics.OutputRows = reader.ReadInt64();
                continue;
            }

            if (key.SequenceEqual("output_batches"u8))
            {
                metrics.OutputBatches = reader.ReadInt64();
                continue;
            }

            if (key.SequenceEqual("elapsed_ns"u8))
            {
                metrics.ElapsedNs = reader.ReadInt64();
                continue;
            }

            reader.SkipDataItem();
        }

        return metrics;
    }
}
