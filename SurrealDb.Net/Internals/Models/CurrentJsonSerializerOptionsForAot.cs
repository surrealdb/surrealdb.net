using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Models;

internal record CurrentJsonSerializerOptionsForAot(
    JsonSerializerContext[]? JsonSerializerContextsPrepended,
    JsonSerializerContext[]? JsonSerializerContextsAppended,
    JsonSerializerOptions Options
)
{
    public bool Equals(
        JsonSerializerContext[]? jsonSerializerContextsToPrepend,
        JsonSerializerContext[]? jsonSerializerContextsToAppend
    )
    {
        return SameJsonSerializerContexts(
                this.JsonSerializerContextsPrepended,
                jsonSerializerContextsToPrepend
            )
            && SameJsonSerializerContexts(
                this.JsonSerializerContextsAppended,
                jsonSerializerContextsToAppend
            );
    }

    private bool SameJsonSerializerContexts(
        JsonSerializerContext[]? jsonSerializerContexts1,
        JsonSerializerContext[]? jsonSerializerContexts2
    )
    {
        if (jsonSerializerContexts1 == jsonSerializerContexts2)
        {
            return true;
        }

        if (jsonSerializerContexts1 is not null && jsonSerializerContexts2 is not null)
        {
            return jsonSerializerContexts1.SequenceEqual(jsonSerializerContexts2);
        }

        return false;
    }
}
