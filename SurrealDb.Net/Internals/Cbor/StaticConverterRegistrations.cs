using System.Runtime.CompilerServices;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Cbor.Converters;
using SurrealDb.Net.Models;
#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
#else
using SystemTextJsonPatch;
#endif

namespace SurrealDb.Net.Internals.Cbor;

/// <summary>
/// Registers converters for generic types whose type arguments are only known to a generic method.
/// </summary>
/// <remarks>
/// When the converter registry meets an unknown generic type, its providers build the converter with
/// <c>MakeGenericType</c>, which fails under Native AOT for value type arguments and needs the
/// converter constructor to survive trimming. A generic method of the library knows its type
/// arguments at compile time, so it can construct the converter statically and register it before
/// the registry is asked for it. Each converter is registered once per <see cref="CborOptions"/>.
/// </remarks>
internal static class StaticConverterRegistrations
{
    /// <summary>
    /// Deserializes a CBOR array into an <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static IEnumerable<T> DeserializeEnumerable<T>(
        ReadOnlySpan<byte> data,
        CborOptions options
    )
    {
        EnsureEnumerable<T>(options);
        return CborSerializer.Deserialize<IEnumerable<T>>(data, options);
    }

    public static void EnsureEnumerable<T>(CborOptions options)
    {
        Registration<IEnumerable<T>>.Ensure(
            options,
            static o => new InterfaceCollectionConverter<List<T>, IEnumerable<T>, T>(o)
        );
    }

    public static void EnsureRecordIdRange<TStart, TEnd>(CborOptions options)
    {
        // A converter looks up the converters it depends on when it is constructed,
        // so the dependencies are registered first.
        Registration<RangeBound<TStart>>.Ensure(
            options,
            static o => new RangeBoundConverter<TStart>(o)
        );
        Registration<RangeBound<TEnd>>.Ensure(
            options,
            static o => new RangeBoundConverter<TEnd>(o)
        );
        Registration<Range<TStart, TEnd>>.Ensure(
            options,
            static o => new RangeConverter<TStart, TEnd>(o)
        );
        Registration<RecordIdRange<TStart, TEnd>>.Ensure(
            options,
            static o => new RecordIdRangeConverter<TStart, TEnd>(o)
        );
    }

    public static void EnsureJsonPatchDocument<T>(CborOptions options)
        where T : class
    {
        Registration<JsonPatchDocument<T>>.Ensure(
            options,
            static o => new JsonPatchDocumentConverter<T>(o)
        );
    }

    private static class Registration<T>
    {
        private static readonly object Registered = new();
        private static readonly ConditionalWeakTable<CborOptions, object> RegisteredOptions = new();

        public static void Ensure(CborOptions options, Func<CborOptions, ICborConverter<T>> factory)
        {
            if (RegisteredOptions.TryGetValue(options, out _))
            {
                return;
            }

            // Does nothing if the registry already has a converter for the type,
            // for example one registered by a user-provided CborSerializerContext.
            options.Registry.ConverterRegistry.RegisterConverter(typeof(T), factory(options));
            RegisteredOptions.AddOrUpdate(options, Registered);
        }
    }
}
