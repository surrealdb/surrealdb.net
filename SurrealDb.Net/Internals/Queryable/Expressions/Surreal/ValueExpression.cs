using System.Collections.Immutable;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Spatial;
using SurrealDb.Net.Internals.Formatters;
using SurrealDb.Net.Models;
using Range = System.Range;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal abstract class ValueExpression : SurrealExpression;

internal sealed class NoneValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append("NONE");
    }
}

internal sealed class NullValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append("null");
    }
}

internal sealed class BoolValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    private readonly bool Value;

    public BoolValueExpression(bool value)
    {
        Value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(Value);
    }
}

internal abstract class IntegerValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    public abstract void AppendTo(StringBuilder stringBuilder);
}

internal sealed class Int32ValueExpression : IntegerValueExpression
{
    public int Value { get; }

    public Int32ValueExpression(int value)
    {
        Value = value;
    }

    public override void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(Value);
    }
}

internal sealed class Int64ValueExpression : IntegerValueExpression
{
    public long Value { get; }

    public Int64ValueExpression(long value)
    {
        Value = value;
    }

    public override void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(Value);
    }
}

internal sealed class FormattedIntegerValueExpression : IntegerValueExpression
{
    private readonly string _formattedValue;

    public FormattedIntegerValueExpression(string formattedValue)
    {
        _formattedValue = formattedValue;
    }

    public override void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(_formattedValue);
    }
}

internal abstract class FloatValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    public abstract double ToInnerValue();
    public abstract void AppendTo(StringBuilder stringBuilder);
}

internal sealed class SingleValueExpression : FloatValueExpression
{
    public float Value { get; }

    public SingleValueExpression(float value)
    {
        Value = value;
    }

    public override double ToInnerValue()
    {
        return Value;
    }

    public override void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(Value.ToString(CultureInfo.InvariantCulture));
    }
}

internal sealed class DoubleValueExpression : FloatValueExpression
{
    public double Value { get; }

    public DoubleValueExpression(double value)
    {
        Value = value;
    }

    public override double ToInnerValue()
    {
        return Value;
    }

    public override void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(Value.ToString(CultureInfo.InvariantCulture));
    }
}

internal sealed class DecimalValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    private readonly decimal _value;

    public DecimalValueExpression(decimal value)
    {
        _value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(_value.ToString(CultureInfo.InvariantCulture));
        stringBuilder.Append("dec");
    }
}

internal sealed class CharValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    private readonly char _value;

    public CharValueExpression(char value)
    {
        _value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        if (_value == '"')
        {
            stringBuilder.Append('\'');
            stringBuilder.Append(_value);
            stringBuilder.Append('\'');
            return;
        }

        stringBuilder.Append('"');
        stringBuilder.Append(_value);
        stringBuilder.Append('"');
    }
}

internal sealed class StringValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    public string Value { get; }

    public StringValueExpression(string value)
    {
        Value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append('"');
        stringBuilder.Append(
            Value.Contains('"', StringComparison.Ordinal)
                ? Value.Replace("\"", "\\\"", StringComparison.Ordinal)
                : Value
        );
        stringBuilder.Append('"');
    }
}

internal sealed class DurationValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    public TimeSpan? OriginalValue { get; }
    private readonly Duration Value;

    public DurationValueExpression(TimeSpan value)
    {
        OriginalValue = value;

        var (seconds, nanos) = TimeSpanFormatter.Convert(value);
        Value = new Duration(seconds, nanos);
    }

    public DurationValueExpression(Duration value)
    {
        Value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(Value);
    }
}

internal sealed class DateTimeValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    public DateTime Value { get; }

    public DateTimeValueExpression(DateTime value)
    {
        Value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append('d');
        stringBuilder.Append('"');
        stringBuilder.Append(Value.ToUniversalTime().ToString("O"));
        stringBuilder.Append('"');
    }
}

internal sealed class GuidValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    private readonly Guid _value;

    public GuidValueExpression(Guid value)
    {
        _value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append('u');
        stringBuilder.Append('"');
        stringBuilder.Append(_value);
        stringBuilder.Append('"');
    }
}

internal sealed class ArrayValueExpression : ValueExpression, IConstantValueExpression
{
    public ImmutableArray<ValueExpression> Values { get; }

    public ArrayValueExpression(ImmutableArray<ValueExpression> values)
    {
        Values = values;
    }
}

internal sealed class ObjectValueExpression : ValueExpression, IConstantValueExpression
{
    public ImmutableDictionary<string, ValueExpression> Fields { get; }

    public ObjectValueExpression(ImmutableDictionary<string, ValueExpression> fields)
    {
        Fields = fields;
    }
}

internal sealed class GeometryValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    private readonly ISpatial _value;

    public GeometryValueExpression(ISpatial value)
    {
        _value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        if (_value is Geometry geometry)
            AppendGeometry(stringBuilder, geometry);
        if (_value is Geography geography)
            AppendGeography(stringBuilder, geography);
        throw new ArgumentOutOfRangeException(nameof(_value));
    }

    private static void AppendGeometry(StringBuilder stringBuilder, Geometry geometry)
    {
        if (geometry is GeometryPoint geometryPoint)
        {
            stringBuilder.Append('(');
            stringBuilder.Append(geometryPoint.X);
            stringBuilder.Append(", ");
            stringBuilder.Append(geometryPoint.Y);
            stringBuilder.Append(')');
        }
        if (geometry is GeometryLineString geometryLineString)
        {
            AppendGeometryLineString(stringBuilder, geometryLineString, true);
        }
        if (geometry is GeometryPolygon geometryPolygon)
        {
            AppendGeometryPolygon(stringBuilder, geometryPolygon, true);
        }
        if (geometry is GeometryMultiPoint geometryMultiPoint)
        {
            AppendGeometryMultiPoint(stringBuilder, geometryMultiPoint, true);
        }
        if (geometry is GeometryMultiLineString geometryMultiLineString)
        {
            AppendGeometryMultiLineString(stringBuilder, geometryMultiLineString, true);
        }
        if (geometry is GeometryMultiPolygon geometryMultiPolygon)
        {
            AppendGeometryMultiPolygon(stringBuilder, geometryMultiPolygon, true);
        }
        if (geometry is GeometryCollection geometryCollection)
        {
            AppendGeometryCollection(stringBuilder, geometryCollection);
        }
        throw new ArgumentOutOfRangeException(nameof(geometry));
    }

    private static void AppendGeometryPoint(StringBuilder stringBuilder, GeometryPoint point)
    {
        stringBuilder.Append('[');
        stringBuilder.Append(point.X);
        stringBuilder.Append(", ");
        stringBuilder.Append(point.Y);
        stringBuilder.Append(']');
    }

    private static void AppendGeometryLineString(
        StringBuilder stringBuilder,
        GeometryLineString geometryLineString,
        bool withType
    )
    {
        if (withType)
        {
            stringBuilder.Append("{{ type: '");
            stringBuilder.Append("LineString");
            stringBuilder.Append("', coordinates: [");
        }

        for (int index = 0; index < geometryLineString.Points.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var point = geometryLineString.Points[index];
            AppendGeometryPoint(stringBuilder, point);
        }

        if (withType)
        {
            stringBuilder.Append("] }}");
        }
    }

    private static void AppendGeometryPolygon(
        StringBuilder stringBuilder,
        GeometryPolygon geometryPolygon,
        bool withType
    )
    {
        if (withType)
        {
            stringBuilder.Append("{{ type: '");
            stringBuilder.Append("Polygon");
            stringBuilder.Append("', coordinates: [");
        }

        for (int index = 0; index < geometryPolygon.Rings.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var lineString = geometryPolygon.Rings[index];
            stringBuilder.Append('[');
            AppendGeometryLineString(stringBuilder, lineString, false);
            stringBuilder.Append(']');
        }

        if (withType)
        {
            stringBuilder.Append("] }}");
        }
    }

    private static void AppendGeometryMultiPoint(
        StringBuilder stringBuilder,
        GeometryMultiPoint geometryMultiPoint,
        bool withType
    )
    {
        if (withType)
        {
            stringBuilder.Append("{{ type: '");
            stringBuilder.Append("MultiPoint");
            stringBuilder.Append("', coordinates: [");
        }

        for (int index = 0; index < geometryMultiPoint.Points.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var point = geometryMultiPoint.Points[index];
            AppendGeometryPoint(stringBuilder, point);
        }

        if (withType)
        {
            stringBuilder.Append("] }}");
        }
    }

    private static void AppendGeometryMultiLineString(
        StringBuilder stringBuilder,
        GeometryMultiLineString geometryMultiLineString,
        bool withType
    )
    {
        if (withType)
        {
            stringBuilder.Append("{{ type: '");
            stringBuilder.Append("MultiLineString");
            stringBuilder.Append("', coordinates: [");
        }

        for (int index = 0; index < geometryMultiLineString.LineStrings.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var lineString = geometryMultiLineString.LineStrings[index];
            stringBuilder.Append('[');
            AppendGeometryLineString(stringBuilder, lineString, false);
            stringBuilder.Append(']');
        }

        if (withType)
        {
            stringBuilder.Append("] }}");
        }
    }

    private static void AppendGeometryMultiPolygon(
        StringBuilder stringBuilder,
        GeometryMultiPolygon geometryMultiPolygon,
        bool withType
    )
    {
        if (withType)
        {
            stringBuilder.Append("{{ type: '");
            stringBuilder.Append("MultiPolygon");
            stringBuilder.Append("', coordinates: [");
        }

        for (int index = 0; index < geometryMultiPolygon.Polygons.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var polygon = geometryMultiPolygon.Polygons[index];
            stringBuilder.Append('[');
            AppendGeometryPolygon(stringBuilder, polygon, false);
            stringBuilder.Append(']');
        }

        if (withType)
        {
            stringBuilder.Append("] }}");
        }
    }

    private static void AppendGeometryCollection(
        StringBuilder stringBuilder,
        GeometryCollection geometryCollection
    )
    {
        stringBuilder.Append("{{ type: '");
        stringBuilder.Append("GeometryCollection");
        stringBuilder.Append("', geometries: [");

        for (int index = 0; index < geometryCollection.Geometries.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var geometry = geometryCollection.Geometries[index];
            AppendGeometry(stringBuilder, geometry);
        }

        stringBuilder.Append("] }}");
    }

    private static void AppendGeography(StringBuilder stringBuilder, Geography geography)
    {
        if (geography is GeographyPoint geographyPoint)
        {
            stringBuilder.Append('(');
            stringBuilder.Append(geographyPoint.Longitude);
            stringBuilder.Append(", ");
            stringBuilder.Append(geographyPoint.Latitude);
            stringBuilder.Append(')');
        }
        if (geography is GeographyLineString geographyLineString)
        {
            AppendGeographyLineString(stringBuilder, geographyLineString, true);
        }
        if (geography is GeographyPolygon geographyPolygon)
        {
            AppendGeographyPolygon(stringBuilder, geographyPolygon, true);
        }
        if (geography is GeographyMultiPoint geographyMultiPoint)
        {
            AppendGeographyMultiPoint(stringBuilder, geographyMultiPoint, true);
        }
        if (geography is GeographyMultiLineString geographyMultiLineString)
        {
            AppendGeographyMultiLineString(stringBuilder, geographyMultiLineString, true);
        }
        if (geography is GeographyMultiPolygon geographyMultiPolygon)
        {
            AppendGeographyMultiPolygon(stringBuilder, geographyMultiPolygon, true);
        }
        if (geography is GeographyCollection geographyCollection)
        {
            AppendGeographyCollection(stringBuilder, geographyCollection);
        }
        throw new ArgumentOutOfRangeException(nameof(geography));
    }

    private static void AppendGeographyPoint(StringBuilder stringBuilder, GeographyPoint point)
    {
        stringBuilder.Append('[');
        stringBuilder.Append(point.Longitude);
        stringBuilder.Append(", ");
        stringBuilder.Append(point.Latitude);
        stringBuilder.Append(']');
    }

    private static void AppendGeographyLineString(
        StringBuilder stringBuilder,
        GeographyLineString geographyLineString,
        bool withType
    )
    {
        if (withType)
        {
            stringBuilder.Append("{{ type: '");
            stringBuilder.Append("LineString");
            stringBuilder.Append("', coordinates: [");
        }

        for (int index = 0; index < geographyLineString.Points.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var point = geographyLineString.Points[index];
            AppendGeographyPoint(stringBuilder, point);
        }

        if (withType)
        {
            stringBuilder.Append("] }}");
        }
    }

    private static void AppendGeographyPolygon(
        StringBuilder stringBuilder,
        GeographyPolygon geographyPolygon,
        bool withType
    )
    {
        if (withType)
        {
            stringBuilder.Append("{{ type: '");
            stringBuilder.Append("Polygon");
            stringBuilder.Append("', coordinates: [");
        }

        for (int index = 0; index < geographyPolygon.Rings.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var lineString = geographyPolygon.Rings[index];
            stringBuilder.Append('[');
            AppendGeographyLineString(stringBuilder, lineString, false);
            stringBuilder.Append(']');
        }

        if (withType)
        {
            stringBuilder.Append("] }}");
        }
    }

    private static void AppendGeographyMultiPoint(
        StringBuilder stringBuilder,
        GeographyMultiPoint geographyMultiPoint,
        bool withType
    )
    {
        if (withType)
        {
            stringBuilder.Append("{{ type: '");
            stringBuilder.Append("MultiPoint");
            stringBuilder.Append("', coordinates: [");
        }

        for (int index = 0; index < geographyMultiPoint.Points.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var point = geographyMultiPoint.Points[index];
            AppendGeographyPoint(stringBuilder, point);
        }

        if (withType)
        {
            stringBuilder.Append("] }}");
        }
    }

    private static void AppendGeographyMultiLineString(
        StringBuilder stringBuilder,
        GeographyMultiLineString geographyMultiLineString,
        bool withType
    )
    {
        if (withType)
        {
            stringBuilder.Append("{{ type: '");
            stringBuilder.Append("MultiLineString");
            stringBuilder.Append("', coordinates: [");
        }

        for (int index = 0; index < geographyMultiLineString.LineStrings.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var lineString = geographyMultiLineString.LineStrings[index];
            stringBuilder.Append('[');
            AppendGeographyLineString(stringBuilder, lineString, false);
            stringBuilder.Append(']');
        }

        if (withType)
        {
            stringBuilder.Append("] }}");
        }
    }

    private static void AppendGeographyMultiPolygon(
        StringBuilder stringBuilder,
        GeographyMultiPolygon geographyMultiPolygon,
        bool withType
    )
    {
        if (withType)
        {
            stringBuilder.Append("{{ type: '");
            stringBuilder.Append("MultiPolygon");
            stringBuilder.Append("', coordinates: [");
        }

        for (int index = 0; index < geographyMultiPolygon.Polygons.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var polygon = geographyMultiPolygon.Polygons[index];
            stringBuilder.Append('[');
            AppendGeographyPolygon(stringBuilder, polygon, false);
            stringBuilder.Append(']');
        }

        if (withType)
        {
            stringBuilder.Append("] }}");
        }
    }

    private static void AppendGeographyCollection(
        StringBuilder stringBuilder,
        GeographyCollection geographyCollection
    )
    {
        stringBuilder.Append("{{ type: '");
        stringBuilder.Append("GeometryCollection");
        stringBuilder.Append("', geometries: [");

        for (int index = 0; index < geographyCollection.Geographies.Count; index++)
        {
            if (index > 0)
            {
                stringBuilder.Append(", ");
            }

            var geography = geographyCollection.Geographies[index];
            AppendGeography(stringBuilder, geography);
        }

        stringBuilder.Append("] }}");
    }
}

internal sealed class RecordIdValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    private readonly RecordId _value;

    private RecordIdValueExpression(RecordId value)
    {
        _value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(_value);
    }
}

internal sealed class ParameterValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    private readonly string _name;

    public ParameterValueExpression(string name)
    {
        _name = name;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append('$');
        stringBuilder.Append(_name);
    }
}

internal sealed class IdiomValueExpression : ValueExpression
{
    public IdiomExpression Idiom { get; }

    public IdiomValueExpression(IdiomExpression idiom)
    {
        Idiom = idiom;
    }
}

internal sealed class TableValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    private readonly string _value;

    public TableValueExpression(string value)
    {
        _value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        bool shouldEscape = ShouldEspaceIdent(_value);

        if (shouldEscape)
        {
            stringBuilder.Append('`');
        }
        stringBuilder.Append(_value);
        if (shouldEscape)
        {
            stringBuilder.Append('`');
        }
    }

    private static bool ShouldEspaceIdent(string value)
    {
        return char.IsDigit(value[0]) || value.Any(c => !char.IsLetterOrDigit(c) && c != '_');
    }
}

internal sealed class RegexValueExpression : ValueExpression, IPrintableExpression
{
    private readonly Regex _regex;

    public RegexValueExpression(Regex regex)
    {
        // TODO : Check compatibility
        _regex = regex;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append("<regex>");
        stringBuilder.Append(' ');
        stringBuilder.Append('"');
        stringBuilder.Append(_regex);
        stringBuilder.Append('"');
    }
}

internal sealed class CastValueExpression : ValueExpression
{
    public string Kind { get; } // TODO : KindExpression?
    public ValueExpression Value { get; }

    public CastValueExpression(string kind, ValueExpression value)
    {
        Kind = kind;
        Value = value;
    }

    public static CastValueExpression Set(ValueExpression valueExpression)
    {
        return new CastValueExpression("set", valueExpression);
    }
}

internal sealed class EmptyBlockValueExpression : ValueExpression, IPrintableExpression
{
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append("{}");
    }
}

internal sealed class BlockValueExpression : ValueExpression
{
    public ImmutableArray<SurrealExpression> Expressions { get; }

    public BlockValueExpression(SurrealExpression[] expressions)
    {
        Expressions = [.. expressions];
    }
}

internal sealed class RangeValueExpression : ValueExpression
{
    public RangeBoundType BeginBoundType { get; init; }
    public ValueExpression? BeginValue { get; init; }
    public RangeBoundType EndBoundType { get; init; }
    public ValueExpression? EndValue { get; init; }

    private RangeValueExpression() { }

    public static RangeValueExpression Range(ValueExpression beginValue, ValueExpression endValue)
    {
        return new RangeValueExpression
        {
            BeginBoundType = RangeBoundType.Inclusive,
            BeginValue = beginValue,
            EndBoundType = RangeBoundType.Exclusive,
            EndValue = endValue,
        };
    }
}

internal sealed class ConstantValueExpression
    : ValueExpression,
        IPrintableExpression,
        IConstantValueExpression
{
    private readonly BuiltinConstant _constant;

    public ConstantValueExpression(BuiltinConstant constant)
    {
        _constant = constant;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(ToString(_constant));
    }

    private static string ToString(BuiltinConstant constant)
    {
        return constant switch
        {
            BuiltinConstant.MathE => "math::E",
            BuiltinConstant.MathFrac1Pi => "math::FRAC_1_PI",
            BuiltinConstant.MathFrac1Sqrt2 => "math::FRAC_1_SQRT_2",
            BuiltinConstant.MathFrac2Pi => "math::FRAC_2_PI",
            BuiltinConstant.MathFrac2SqrtPi => "math::FRAC_2_SQRT_PI",
            BuiltinConstant.MathFracPi2 => "math::FRAC_PI_2",
            BuiltinConstant.MathFracPi3 => "math::FRAC_PI_3",
            BuiltinConstant.MathFracPi4 => "math::FRAC_PI_4",
            BuiltinConstant.MathFracPi6 => "math::FRAC_PI_6",
            BuiltinConstant.MathFracPi8 => "math::FRAC_PI_8",
            BuiltinConstant.MathInf => "math::INF",
            BuiltinConstant.MathLn10 => "math::LN_10",
            BuiltinConstant.MathLn2 => "math::LN_2",
            BuiltinConstant.MathLog102 => "math::LOG10_2",
            BuiltinConstant.MathLog10E => "math::LOG10_E",
            BuiltinConstant.MathLog210 => "math::LOG2_10",
            BuiltinConstant.MathLog2E => "math::LOG2_E",
            BuiltinConstant.MathNegInf => "math::NEG_INF",
            BuiltinConstant.MathPi => "math::PI",
            BuiltinConstant.MathSqrt2 => "math::SQRT_2",
            BuiltinConstant.MathTau => "math::TAU",
            BuiltinConstant.TimeEpoch => "time::EPOCH",
            BuiltinConstant.TimeMin => "time::MINIMUM",
            BuiltinConstant.TimeMax => "time::MAXIMUM",
            BuiltinConstant.DurationMax => "duration::MAX",
            _ => throw new ArgumentOutOfRangeException(nameof(constant)),
        };
    }
}

internal sealed class FunctionValueExpression : ValueExpression
{
    public string Fullname { get; }
    public ImmutableArray<ValueExpression> Parameters { get; }

    public FunctionValueExpression(string fullname, ImmutableArray<ValueExpression> parameters)
    {
        Fullname = fullname;
        Parameters = parameters;
    }
}

internal sealed class SubqueryValueExpression : ValueExpression
{
    public SurrealExpression Expression { get; }

    public SubqueryValueExpression(SurrealExpression expression)
    {
        Expression = expression;
    }
}

internal sealed class UnaryValueExpression : ValueExpression
{
    public IOperator Operator { get; }
    public ValueExpression Value { get; }

    public UnaryValueExpression(SimpleOperator @operator, ValueExpression value)
    {
        // TODO : Restrict to "!", "+" and "-"
        Operator = @operator;
        Value = value;
    }

    public static UnaryValueExpression Not(ValueExpression value)
    {
        return new UnaryValueExpression(new SimpleOperator(OperatorType.Not), value);
    }
}

internal sealed class BinaryValueExpression : ValueExpression
{
    public ValueExpression Left { get; }
    public IOperator Operator { get; }
    public ValueExpression Right { get; }

    public BinaryValueExpression(ValueExpression left, IOperator @operator, ValueExpression right)
    {
        Left = left;
        Operator = @operator;
        Right = right;
    }

    public BinaryValueExpression Invert()
    {
        return new BinaryValueExpression(Right, Operator, Left);
    }

    public static BinaryValueExpression Or(ValueExpression left, ValueExpression right)
    {
        return new BinaryValueExpression(left, new SimpleOperator(OperatorType.Or), right);
    }

    public static BinaryValueExpression Add(ValueExpression left, ValueExpression right)
    {
        return new BinaryValueExpression(left, new SimpleOperator(OperatorType.Add), right);
    }

    public static BinaryValueExpression Sub(ValueExpression left, ValueExpression right)
    {
        return new BinaryValueExpression(left, new SimpleOperator(OperatorType.Sub), right);
    }

    public static BinaryValueExpression Contains(ValueExpression left, ValueExpression right)
    {
        return new BinaryValueExpression(left, new SimpleOperator(OperatorType.Contain), right);
    }
}

internal sealed class IfElseStatementValueExpression : ValueExpression
{
    /// <summary>
    /// The first <c>if</c> condition followed by any number of <c>else if</c>s
    /// </summary>
    public ImmutableArray<(ValueExpression, ValueExpression)> Expressions { get; }

    /// <summary>
    /// The final <c>else</c> body, if there is one
    /// </summary>
    public ValueExpression? Else { get; }

    public IfElseStatementValueExpression((ValueExpression, ValueExpression) @if)
    {
        Expressions = [@if];
    }

    public IfElseStatementValueExpression(
        (ValueExpression, ValueExpression) @if,
        ValueExpression @else
    )
    {
        Expressions = [@if];
        Else = @else;
    }

    public IfElseStatementValueExpression((ValueExpression, ValueExpression)[] ifs)
    {
        Expressions = [.. ifs];
    }

    public IfElseStatementValueExpression(
        (ValueExpression, ValueExpression)[] ifs,
        ValueExpression @else
    )
    {
        Expressions = [.. ifs];
        Else = @else;
    }
}
