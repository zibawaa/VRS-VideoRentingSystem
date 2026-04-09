using Microsoft.AspNetCore.Mvc;
using VideoRentingSystem.Api.Bootstrap;
using VideoRentingSystem.Api.Contracts.Publisher;
using VideoRentingSystem.Api.Contracts.Videos;
using VideoRentingSystem.Api.Security;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Api.Controllers;

/// <summary>
/// Studio endpoints for publishers and admins to create, list, and delete catalog titles.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PublisherController : ControllerBase
{
    private readonly StoreRuntime _runtime;

    public PublisherController(StoreRuntime runtime)
    {
        _runtime = runtime;
    }

    [HttpGet("videos/me")]
    public IActionResult GetMyVideos()
    {
        // gate access to publisher/admin roles before touching any data
        if (!TryRequirePublisherOrAdmin(out AuthSession? session, out IActionResult? error))
        {
            return error!;
        }

        // admin can inspect the full catalogue while publishers only see their own titles
        Video[] videos;
        lock (_runtime.SyncRoot)
        {
            videos = session!.Role == UserRole.Admin
                ? _runtime.VideoStore.DisplayAllVideos()
                : _runtime.VideoStore.GetPublisherVideos(session.UserId);
        }

        // project each domain Video into a serializable response DTO
        VideoResponse[] response = new VideoResponse[videos.Length];
        for (int i = 0; i < videos.Length; i++)
        {
            response[i] = VideoResponse.FromVideo(videos[i]);
        }

        return Ok(response);
    }

    [HttpPost("videos")]
    public IActionResult Create([FromBody] CreateVideoRequest request)
    {
        // enforce role gate first
        if (!TryRequirePublisherOrAdmin(out AuthSession? session, out IActionResult? error))
        {
            return error!;
        }

        // validate the video type string resolves to the domain enum
        if (!Enum.TryParse<VideoType>(request.Type, ignoreCase: true, out VideoType type))
        {
            return BadRequest("Type must be Movie or Series.");
        }

        // admin can assign ownership explicitly; publishers are pinned to their own identity
        int ownerId = session!.Role == UserRole.Admin
            ? request.OwnerPublisherId ?? session.UserId
            : session.UserId;

        // construct the domain model, letting the constructor validate invariants
        Video video;
        try
        {
            video = new Video(
                request.VideoId,
                request.Title,
                request.Genre,
                request.ReleaseYear,
                false,
                ownerId,
                type,
                request.RentalPrice,
                request.RentalHours,
                request.IsPublished);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        // persist via the core store which updates all custom indexes atomically
        bool added;
        lock (_runtime.SyncRoot)
        {
            added = _runtime.VideoStore.AddVideo(video, ToActorUser(session));
        }

        if (!added)
        {
            return Conflict("Create failed due to duplicate ID or ownership rule.");
        }

        return Ok(VideoResponse.FromVideo(video));
    }

    [HttpDelete("videos/{id:int}")]
    public IActionResult Delete(int id)
    {
        // enforce role gate before deletion
        if (!TryRequirePublisherOrAdmin(out AuthSession? session, out IActionResult? error))
        {
            return error!;
        }

        // remove via core store which enforces ownership rules for publisher actors
        bool removed;
        lock (_runtime.SyncRoot)
        {
            removed = _runtime.VideoStore.RemoveVideo(id, ToActorUser(session!));
        }

        if (!removed)
        {
            return Conflict("Delete failed due to ownership rule or missing title.");
        }

        return NoContent();
    }

    // checks that the current request carries a publisher or admin session
    private bool TryRequirePublisherOrAdmin(out AuthSession? session, out IActionResult? error)
    {
        if (!HttpContext.TryGetAuthSession(out session) || session == null)
        {
            error = Unauthorized("Missing or invalid bearer token.");
            return false;
        }

        if (session.Role != UserRole.Publisher && session.Role != UserRole.Admin)
        {
            error = StatusCode(StatusCodes.Status403Forbidden);
            return false;
        }

        error = null;
        return true;
    }

    // creates a lightweight User from the session so Core layer can check ownership
    private static User ToActorUser(AuthSession session)
    {
        return new User(session.UserId, session.Username, "SESSION", session.Role, session.StudioName);
    }
}
