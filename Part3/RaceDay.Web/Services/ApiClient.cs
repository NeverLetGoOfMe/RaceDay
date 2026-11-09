using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Success { get; set; }
    public HttpStatusCode Status { get; set; }
    public T? Data { get; set; }
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// The only way the MVC app reaches data. It calls the Part 2 API and replays that user's API session
/// cookie (kept in the MVC session) so the API knows who is logged in and what role they have.
/// </summary>
public class ApiClient
{
    private const string CookieKey = "ApiCookie";
    private const string ApiCookieName = ".RaceDay.Session";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _context;

    public ApiClient(HttpClient http, IHttpContextAccessor context)
    {
        _http = http;
        _context = context;
    }

    private ISession Session => _context.HttpContext!.Session;

    public Task<ApiResult<T>> GetAsync<T>(string path) => SendAsync<T>(HttpMethod.Get, path, null);

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null) => SendAsync<T>(HttpMethod.Post, path, ToJson(body));

    public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null) => SendAsync<T>(HttpMethod.Put, path, ToJson(body));

    public Task<ApiResult<object>> DeleteAsync(string path) => SendAsync<object>(HttpMethod.Delete, path, null);

    public async Task<ApiResult<T>> PostFileAsync<T>(string path, IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        return await SendAsync<T>(HttpMethod.Post, path, content);
    }

    /// <summary>Fetches raw bytes (used to show images that are stored behind the API).</summary>
    public async Task<(byte[]? Bytes, string ContentType)> GetBytesAsync(string path)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Get, path, null);
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return (null, string.Empty);
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            return (bytes, response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream");
        }
        catch (HttpRequestException)
        {
            return (null, string.Empty);
        }
    }

    private static HttpContent? ToJson(object? body) =>
        body == null ? null : new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

    private HttpRequestMessage BuildRequest(HttpMethod method, string path, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, path) { Content = content };
        var cookie = Session.GetString(CookieKey);
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        return request;
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, HttpContent? content)
    {
        try
        {
            using var request = BuildRequest(method, path, content);
            using var response = await _http.SendAsync(request);
            CaptureCookie(response);

            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                T? data = default;
                if (!string.IsNullOrWhiteSpace(body) && typeof(T) != typeof(object))
                {
                    data = JsonSerializer.Deserialize<T>(body, JsonOptions);
                }

                return new ApiResult<T> { Success = true, Status = response.StatusCode, Data = data };
            }

            return new ApiResult<T> { Status = response.StatusCode, Error = ReadError(body, response.StatusCode) };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>
            {
                Status = HttpStatusCode.ServiceUnavailable,
                Error = "RaceDay could not reach its service. Please try again in a moment."
            };
        }
        catch (JsonException)
        {
            return new ApiResult<T>
            {
                Status = HttpStatusCode.InternalServerError,
                Error = "RaceDay received a response it could not understand. Please try again."
            };
        }
    }

    // After login the API sends back its session cookie - keep it so later calls are "logged in"
    private void CaptureCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return;
        }

        foreach (var value in values)
        {
            if (value.StartsWith(ApiCookieName + "=", StringComparison.Ordinal))
            {
                Session.SetString(CookieKey, value.Split(';')[0]);
            }
        }
    }

    // Turns the API's error body into one short, friendly sentence (never raw JSON or stack traces)
    private static string ReadError(string body, HttpStatusCode status)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in errors.EnumerateObject())
                    {
                        foreach (var item in field.Value.EnumerateArray())
                        {
                            return item.GetString() ?? "Please check the details you entered.";
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // fall through to the generic messages below
        }

        return status switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "We could not find what you were looking for.",
            HttpStatusCode.BadRequest => "Please check the details you entered and try again.",
            HttpStatusCode.Conflict => "That action conflicts with something that already exists.",
            _ => "Something went wrong. Please try again."
        };
    }
}
