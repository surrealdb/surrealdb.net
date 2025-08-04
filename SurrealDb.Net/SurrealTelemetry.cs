using System.Diagnostics;

namespace SurrealDb.Net
{
    internal static class SurrealTelemetry
    {
        internal const string ActivitySourceName = "SurrealDb.Net";
        private static readonly ActivitySource activitySource = new ActivitySource(
            ActivitySourceName
        );

        public static Activity? StartActivity(Uri uri, string operation, string? table = null)
        {
            var summary = string.IsNullOrEmpty(table) ? $"{operation};" : $"{operation} {table};";
            var activity = activitySource.StartActivity(summary, ActivityKind.Client);
            if (activity?.IsAllDataRequested == true)
            {
                activity.AddTag("server.address", uri.Host);
                activity.AddTag("server.port", uri.Port);
                activity.SetTag("db.system.name", "surrealdb");
                activity.SetTag("db.operation.name", operation);
                if (!string.IsNullOrEmpty(table))
                {
                    activity.SetTag("db.collection.name", table);
                    activity.SetTag("db.query.summary", summary);
                }
            }
            return activity;
        }
    }
}
