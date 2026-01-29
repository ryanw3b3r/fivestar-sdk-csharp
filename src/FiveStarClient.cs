using System.Text.Json;
using FiveStarSupport.Models;

namespace FiveStarSupport;

/// <summary>
/// FiveStar Support Client.
///
/// Simplified client for interacting with the FiveStar Support API.
/// Customer IDs are now generated server-side for improved security.
/// </summary>
public class FiveStarClient : IAsyncDisposable
{
    private readonly string _clientId;
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly DeviceInfo _deviceInfo;

    /// <summary>
    /// Initialize a new FiveStar Support client.
    /// </summary>
    /// <param name="clientId">The client ID</param>
    /// <param name="apiUrl">Optional API URL (defaults to https://fivestar.support)</param>
    /// <param name="timeout">Optional request timeout</param>
    /// <param name="platform">Platform identifier (e.g., 'web', 'ios', 'android', 'flutter', 'laravel')</param>
    /// <param name="appVersion">App version string</param>
    /// <param name="deviceModel">Device model (e.g., 'iPhone14,2')</param>
    /// <param name="osVersion">OS version (e.g., '16.0')</param>
    public FiveStarClient(
        string clientId,
        string? apiUrl = null,
        TimeSpan? timeout = null,
        string? platform = null,
        string? appVersion = null,
        string? deviceModel = null,
        string? osVersion = null)
    {
        _clientId = clientId;
        _apiUrl = (apiUrl ?? "https://fivestar.support").TrimEnd('/');

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_apiUrl),
            Timeout = timeout ?? TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _deviceInfo = new DeviceInfo(
            Platform: platform,
            AppVersion: appVersion,
            DeviceModel: deviceModel,
            OsVersion: osVersion
        );

        // Add device fingerprinting headers
        AddDeviceHeaders();
    }

    /// <summary>
    /// Get all available response types for this client.
    /// </summary>
    /// <returns>Array of response types</returns>
    public async Task<ResponseType[]> GetResponseTypesAsync(CancellationToken cancellationToken = default)
    {
        var data = await GetAsync("/api/responses/types", cancellationToken);
        var typesArray = data.GetProperty("types");

        if (typesArray.ValueKind == JsonValueKind.Undefined || typesArray.ValueKind == JsonValueKind.Null)
            return Array.Empty<ResponseType>();

        var types = new ResponseType[typesArray.GetArrayLength()];
        int i = 0;
        foreach (var element in typesArray.EnumerateArray())
        {
            types[i++] = ResponseType.FromJson(element);
        }

        return types;
    }

    /// <summary>
    /// Generate a new customer ID from the server.
    ///
    /// Customer IDs are now generated server-side with cryptographic signing.
    /// This replaces the previous client-side generation approach.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated customer ID with expiration info</returns>
    public async Task<GenerateCustomerIdResult> GenerateCustomerIdAsync(CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            clientId = _clientId
        };

        var data = await PostAsync("/api/customers/generate", payload, cancellationToken);
        return JsonSerializer.Deserialize<GenerateCustomerIdResult>(data.GetRawText(), _jsonOptions)
            ?? throw new FiveStarAPIError("Failed to deserialize response");
    }

    /// <summary>
    /// Register a customer ID for this client.
    ///
    /// This should be called after generating a customer ID to associate
    /// it with optional customer information (email, name).
    /// </summary>
    /// <param name="customerId">The customer ID from GenerateCustomerIdAsync()</param>
    /// <param name="options">Optional customer information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result</returns>
    public async Task<RegisterCustomerResult> RegisterCustomerAsync(
        string customerId,
        RegisterCustomerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            clientId = _clientId,
            customerId = customerId,
            email = options?.Email,
            name = options?.Name
        };

        var data = await PostAsync("/api/customers", payload, cancellationToken);
        return RegisterCustomerResult.FromJson(data);
    }

    /// <summary>
    /// Check if a customer ID is valid and registered for this client.
    /// </summary>
    /// <param name="customerId">The customer ID to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    public async Task<VerifyCustomerResult> VerifyCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                clientId = _clientId,
                customerId = customerId
            };

            var data = await PostAsync("/api/customers/verify", payload, cancellationToken);
            return JsonSerializer.Deserialize<VerifyCustomerResult>(data.GetRawText(), _jsonOptions)
                ?? new VerifyCustomerResult(Valid: false);
        }
        catch (FiveStarAPIError)
        {
            return new VerifyCustomerResult(Valid: false, Message: "Verification failed");
        }
    }

    /// <summary>
    /// Submit a new response/feedback on behalf of a customer.
    /// </summary>
    /// <param name="options">Response options including customer ID, title, description, and type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The submitted response result</returns>
    public async Task<SubmitResponseResult> SubmitResponseAsync(
        SubmitResponseOptions options,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            clientId = _clientId,
            customerId = options.CustomerId,
            title = options.Title,
            description = options.Description,
            responseTypeId = options.TypeId,
            customerEmail = options.Email,
            customerName = options.Name
        };

        var data = await PostAsync("/api/responses", payload, cancellationToken);
        return JsonSerializer.Deserialize<SubmitResponseResult>(data.GetRawText(), _jsonOptions)
            ?? throw new FiveStarAPIError("Failed to deserialize response");
    }

    /// <summary>
    /// Get a public feedback page URL for this client.
    /// </summary>
    /// <param name="locale">Optional locale for the page</param>
    /// <returns>The public URL</returns>
    public string GetPublicUrl(string? locale = null)
    {
        var localePrefix = !string.IsNullOrEmpty(locale) ? $"/{locale}" : "";
        return $"{_apiUrl}{localePrefix}/c/{_clientId}";
    }

    /// <summary>
    /// Add device fingerprinting headers to the HTTP client.
    /// </summary>
    private void AddDeviceHeaders()
    {
        if (!string.IsNullOrEmpty(_deviceInfo.Platform))
            _httpClient.DefaultRequestHeaders.Add("X-FiveStar-Platform", _deviceInfo.Platform);

        if (!string.IsNullOrEmpty(_deviceInfo.AppVersion))
            _httpClient.DefaultRequestHeaders.Add("X-FiveStar-App-Version", _deviceInfo.AppVersion);

        if (!string.IsNullOrEmpty(_deviceInfo.DeviceModel))
            _httpClient.DefaultRequestHeaders.Add("X-FiveStar-Device-Model", _deviceInfo.DeviceModel);

        if (!string.IsNullOrEmpty(_deviceInfo.OsVersion))
            _httpClient.DefaultRequestHeaders.Add("X-FiveStar-OS-Version", _deviceInfo.OsVersion);
    }

    /// <summary>
    /// Perform a GET request.
    /// </summary>
    private async Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(path, cancellationToken);
        var statusCode = (int)response.StatusCode;

        if (statusCode != 200)
        {
            throw new FiveStarAPIError($"HTTP {statusCode}", statusCode);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions)
            ?? throw new FiveStarAPIError("Failed to deserialize response");
    }

    /// <summary>
    /// Perform a POST request.
    /// </summary>
    private async Task<JsonElement> PostAsync(
        string path,
        object payload,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(path, content, cancellationToken);
        var statusCode = (int)response.StatusCode;

        if (statusCode != 200)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                var errorData = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
                var errorMessage = errorData.GetProperty("error").GetString()
                    ?? errorData.GetProperty("message").GetString()
                    ?? $"HTTP {statusCode}";
                throw new FiveStarAPIError(errorMessage, statusCode);
            }
            catch
            {
                throw new FiveStarAPIError($"HTTP {statusCode}", statusCode);
            }
        }

        var responseContent2 = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<JsonElement>(responseContent2, _jsonOptions)
            ?? throw new FiveStarAPIError("Failed to deserialize response");
    }

    /// <summary>
    /// Dispose the HTTP client.
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    /// <summary>
    /// Dispose the HTTP client asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        await Task.CompletedTask;
    }
}

/// <summary>
/// Device information for fingerprinting.
/// </summary>
internal record DeviceInfo(
    string? Platform,
    string? AppVersion,
    string? DeviceModel,
    string? OsVersion
);
