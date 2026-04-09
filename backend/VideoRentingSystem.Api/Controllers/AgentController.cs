using Microsoft.AspNetCore.Mvc;
using VideoRentingSystem.Api.Agent;
using VideoRentingSystem.Api.Bootstrap;
using VideoRentingSystem.Api.Security;

namespace VideoRentingSystem.Api.Controllers;

/// <summary>
/// Exposes the AI agent over HTTP. The /chat endpoint accepts a natural-language
/// message, runs it through the agent's perceive→reason→act loop, and returns
/// the agent's text response. Authentication is optional — unauthenticated
/// users can still search and get recommendations but cannot rent or return.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AgentController : ControllerBase
{
    private readonly StoreRuntime _runtime;
    private readonly AgentService _agent;

    public AgentController(StoreRuntime runtime, AgentService agent)
    {
        _runtime = runtime;
        _agent = agent;
    }

    [HttpPost("chat")]
    public ActionResult<ChatResponse> Chat([FromBody] ChatRequest request)
    {
        // build the execution context the agent needs: store access + optional identity
        HttpContext.TryGetAuthSession(out AuthSession? session);

        AgentContext context = new()
        {
            Runtime = _runtime,
            Session = session
        };

        // run the agent loop and capture the response
        string reply = _agent.ProcessMessage(request.Message, context);

        return Ok(new ChatResponse
        {
            Reply = reply,
            Timestamp = DateTime.UtcNow.ToString("o")
        });
    }

    /// <summary>
    /// Returns a list of the agent's capabilities so the UI can
    /// display them as hints or onboarding suggestions.
    /// </summary>
    [HttpGet("capabilities")]
    public ActionResult<string[]> GetCapabilities()
    {
        return Ok(_agent.GetCapabilities());
    }
}
