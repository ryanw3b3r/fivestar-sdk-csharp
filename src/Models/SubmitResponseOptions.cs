namespace FiveStarSupport.Models;

/// <summary>
/// Options for submitting a response.
/// </summary>
public record SubmitResponseOptions(
    string CustomerId,
    string Title,
    string Description,
    string TypeId,
    string? Email = null,
    string? Name = null
);
