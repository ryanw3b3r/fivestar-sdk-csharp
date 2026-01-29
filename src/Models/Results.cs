using System.Text.Json;
using System.Text.Json.Serialization;

namespace FiveStarSupport.Models;

/// <summary>
/// Result of generating a customer ID from the server.
/// </summary>
public record GenerateCustomerIdResult(
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("expiresAt")] string ExpiresAt,
    [property: JsonPropertyName("deviceId")] string DeviceId
);

/// <summary>
/// Options for registering a customer.
/// </summary>
public record RegisterCustomerOptions(
    string? Email = null,
    string? Name = null
);

/// <summary>
/// Result of submitting a response.
/// </summary>
public record SubmitResponseResult(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("responseId")] string ResponseId,
    [property: JsonPropertyName("message")] string? Message = null
);

/// <summary>
/// Customer information.
/// </summary>
public record CustomerInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("email")] string? Email = null,
    [property: JsonPropertyName("name")] string? Name = null
)
{
    /// <summary>
    /// Creates a CustomerInfo from JSON data.
    /// </summary>
    public static CustomerInfo FromJson(JsonElement json) => new(
        Id: json.GetProperty("id").GetString() ?? string.Empty,
        CustomerId: json.GetProperty("customerId").GetString() ?? string.Empty,
        Email: json.GetProperty("email").GetString(),
        Name: json.GetProperty("name").GetString()
    );
}

/// <summary>
/// Result of registering a customer.
/// </summary>
public record RegisterCustomerResult(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("customer")] CustomerInfo? Customer = null,
    [property: JsonPropertyName("message")] string? Message = null
)
{
    /// <summary>
    /// Creates a RegisterCustomerResult from JSON data.
    /// </summary>
    public static RegisterCustomerResult FromJson(JsonElement json)
    {
        var customerElement = json.GetProperty("customer");
        CustomerInfo? customer = null;
        if (customerElement.ValueKind != JsonValueKind.Undefined && customerElement.ValueKind != JsonValueKind.Null)
        {
            customer = CustomerInfo.FromJson(customerElement);
        }

        return new RegisterCustomerResult(
            Success: json.GetProperty("success").GetBoolean(),
            Customer: customer,
            Message: json.GetProperty("message").GetString()
        );
    }
}

/// <summary>
/// Customer verification result.
/// </summary>
public record VerifyCustomerResult(
    [property: JsonPropertyName("valid")] bool Valid,
    [property: JsonPropertyName("message")] string? Message = null
);

/// <summary>
/// API Error from FiveStar Support.
/// </summary>
public class FiveStarAPIError(string message, int? statusCode = null) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
}
