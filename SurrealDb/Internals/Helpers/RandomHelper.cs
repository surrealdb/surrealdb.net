namespace SurrealDb.Internals.Helpers;

internal class RandomHelper
{
#if NET6_0_OR_GREATER
	private static Random _random => Random.Shared;
#else
	private static readonly Random _random = new();
	private static readonly object _randomLock = new();
#endif

	/// <summary>
	/// Generates a random string with 8 characters
	/// </summary>
	public static string CreateRandomId()
	{
#if !NET6_0_OR_GREATER
		lock (_randomLock)
		{
#endif
			return _random.Next().ToString("x");
#if !NET6_0_OR_GREATER
		}
#endif
	}
}
