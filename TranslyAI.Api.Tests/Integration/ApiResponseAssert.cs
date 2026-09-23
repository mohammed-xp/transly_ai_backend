using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Dtos;

namespace TranslyAI.Api.Tests.Integration;

public static class ApiResponseAssert
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    public static async Task<T> SuccessAsync<T>(
        HttpResponseMessage response,
        HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.Equal(expected, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(Json);

        Assert.NotNull(body);
        Assert.NotNull(body.Data);

        return body.Data;
    }

    public static async Task<ProblemDetails> ProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);

        Assert.NotNull(problem);
        Assert.Equal((int)expected, problem.Status);

        return problem;
    }
}
