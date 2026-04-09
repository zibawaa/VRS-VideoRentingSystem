namespace VideoRentingSystem.Api.Agent;

/// <summary>
/// Represents a single capability ("arm") the AI agent can invoke.
/// Each tool has a name the intent classifier maps to, a human-readable
/// description shown when the agent explains what it can do, and a
/// delegate that executes the action against the live stores.
/// </summary>
public sealed class AgentTool
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required Func<AgentContext, string[], string> Execute { get; init; }
}

/// <summary>
/// Carries the per-request state the agent needs when executing a tool:
/// the caller's identity and access to the shared store runtime.
/// </summary>
public sealed class AgentContext
{
    public required Bootstrap.StoreRuntime Runtime { get; init; }
    public Security.AuthSession? Session { get; init; }
}
