using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Ws;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class SurrealDbWsResponseConverterTests : BaseCborConverterTests
{
    [Fact]
    public void CannotSerialize()
    {
        ISurrealDbWsResponse value = new SurrealDbWsOkResponse(
            "7df5d2a9",
            new([0xc6, 0xf6]),
            SurrealDbCborOptions.Default
        );

        Func<Task> act = () => SerializeCborBinaryAsHexaAsync(value);

        act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task DeserializeWsResponseWithNoResult()
    {
        var response = await DeserializeCborBinaryAsHexaAsync<ISurrealDbWsResponse>(
            "a262696468376466356432613966726573756c74c6f6"
        );

        response.Should().BeOfType<SurrealDbWsOkResponse>();

        var okResponse = (SurrealDbWsOkResponse)response;

        okResponse.Id.Should().Be("7df5d2a9");
        okResponse.GetValue<string>().Should().BeNull();
    }

    [Fact]
    public async Task DeserializeWsResponseWithStringResult()
    {
        var response = await DeserializeCborBinaryAsHexaAsync<ISurrealDbWsResponse>(
            "a262696468363238363735313766726573756c7478ea65794a30655841694f694a4b563151694c434a68624763694f694a49557a55784d694a392e65794a70595851694f6a45334d5445794e7a49304e544573496d35695a6949364d5463784d5449334d6a51314d5377695a586877496a6f784e7a45784d6a63324d4455784c434a7063334d694f694a5464584a795a574673524549694c434a4a52434936496e4a766233516966512e4b494c677351784544316d4b4249697565754f6a37477757686e504d494f795f51473642323279347048656276394a773761733645525a3174314643532d65727042746c2d444749776363555a655857417931495541"
        );

        response.Should().BeOfType<SurrealDbWsOkResponse>();

        var okResponse = (SurrealDbWsOkResponse)response;

        okResponse.Id.Should().Be("62867517");
        okResponse
            .GetValue<string>()
            .Should()
            .Be(
                "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJpYXQiOjE3MTEyNzI0NTEsIm5iZiI6MTcxMTI3MjQ1MSwiZXhwIjoxNzExMjc2MDUxLCJpc3MiOiJTdXJyZWFsREIiLCJJRCI6InJvb3QifQ.KILgsQxED1mKBIiueuOj7GwWhnPMIOy_QG6B22y4pHebv9Jw7as6ERZ1t1FCS-erpBtl-DGIwccUZeXWAy1IUA"
            );
    }

    [Fact]
    public async Task DeserializeWsResponseWithSingleOkResult()
    {
        var response = await DeserializeCborBinaryAsHexaAsync<ISurrealDbWsResponse>(
            "a262696468363162353964396166726573756c7481a366726573756c74c6f666737461747573624f4b6474696d6566363630c2b573"
        );

        response.Should().BeOfType<SurrealDbWsOkResponse>();

        var okResponse = (SurrealDbWsOkResponse)response;
        okResponse.Id.Should().Be("61b59d9a");

        var results = okResponse.GetValue<List<ISurrealDbResult>>();
        results.Should().HaveCount(1);

        var firstResult = results!.First();
        firstResult.Should().BeOfType<SurrealDbOkResult>();

        var okResult = (SurrealDbOkResult)firstResult;
        okResult.Status.Should().Be("OK");
        okResult.IsOk.Should().BeTrue();

        okResult.GetValue<string>().Should().BeNull();
    }

    [Fact]
    public async Task DeserializeWsResponseWithMultipleOkResults()
    {
        var response = await DeserializeCborBinaryAsHexaAsync<ISurrealDbWsResponse>(
            "a262696468323662393063373766726573756c7489a366726573756c74c6f666737461747573624f4b6474696d65683138322e33c2b573a366726573756c74c6f666737461747573624f4b6474696d656738392e31c2b573a366726573756c74c6f666737461747573624f4b6474696d656737322e36c2b573a366726573756c74c6f666737461747573624f4b6474696d65683236352e37c2b573a366726573756c74c6f666737461747573624f4b6474696d656737382e33c2b573a366726573756c74c6f666737461747573624f4b6474696d656738332e35c2b573a366726573756c74c6f666737461747573624f4b6474696d656739342e37c2b573a366726573756c74c6f666737461747573624f4b6474696d656739322e31c2b573a366726573756c74c6f666737461747573624f4b6474696d656739322e36c2b573"
        );

        response.Should().BeOfType<SurrealDbWsOkResponse>();

        var okResponse = (SurrealDbWsOkResponse)response;

        var results = okResponse.GetValue<List<ISurrealDbResult>>();
        results.Should().HaveCount(9);
    }

    [Fact]
    public async Task DeserializeWsResponseWithError()
    {
        var response = await DeserializeCborBinaryAsHexaAsync<ISurrealDbWsResponse>(
            "a2656572726f72a264636f6465397cff676d657373616765783e54686572652077617320612070726f626c656d2077697468207468652064617461626173653a20546865207369676e7570207175657279206661696c6564626964683563633631663961"
        );

        response.Should().BeOfType<SurrealDbWsErrorResponse>();

        var errorResponse = (SurrealDbWsErrorResponse)response;

        errorResponse.Id.Should().Be("5cc61f9a");
        errorResponse.Error.Code.Should().Be(-32000);
        errorResponse
            .Error.Message.Should()
            .Be("There was a problem with the database: The signup query failed");
    }
}
