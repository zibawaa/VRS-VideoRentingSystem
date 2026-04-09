namespace VideoRentingSystem.Api.Agent;

/// <summary>DTO for incoming chat messages from the client.</summary>
public sealed class ChatRequest
{
    public string Message { get; set; } = "";
}

/// <summary>DTO for the agent's reply, including which tool was used.</summary>
public sealed class ChatResponse
{
    public required string Reply { get; init; }
    public required string Timestamp { get; init; }
}
