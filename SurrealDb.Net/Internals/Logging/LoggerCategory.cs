namespace SurrealDb.Net.Internals.Logging;

internal abstract class LoggerCategory<T>
{
    public static string Name { get; } = ToName(typeof(T));

    public static implicit operator string(LoggerCategory<T> loggerCategory) =>
        loggerCategory.ToString();

    public override string ToString() => Name;

    private static string ToName(Type loggerCategoryType)
    {
        string name = loggerCategoryType.FullName!.Replace('+', '.');

        const string OUTER_CLASS_NAME = "." + nameof(DbLoggerCategory);
        int mostOuterClassIndex = name.IndexOf(OUTER_CLASS_NAME, StringComparison.Ordinal);

        if (mostOuterClassIndex >= 0)
        {
            return string.Concat(
                DbLoggerCategory.Name,
                name[(mostOuterClassIndex + OUTER_CLASS_NAME.Length)..]
            );
        }

        return name;
    }
}
