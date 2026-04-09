using Microsoft.AspNetCore.Mvc;
using VideoRentingSystem.Api.Bootstrap;
using VideoRentingSystem.Api.Contracts.Videos;
using VideoRentingSystem.Api.Security;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Api.Controllers;

/// <summary>
/// Public catalog endpoints – browse/filter published videos and fetch details by ID.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class VideosController : ControllerBase
{
    private readonly StoreRuntime _runtime;

    public VideosController(StoreRuntime runtime)
    {
        _runtime = runtime;
    }

    [HttpGet]
    public ActionResult<VideoResponse[]> Browse(
        [FromQuery] string? keyword,
        [FromQuery] string? genre,
        [FromQuery] decimal? maxPrice)
    {
        // pass the optional filter parameters through to the core FilterCatalog method
        Video[] videos;
        lock (_runtime.SyncRoot)
        {
            videos = _runtime.VideoStore.FilterCatalog(keyword, genre, maxPrice);
        }

        // project domain models into lightweight API response DTOs
        VideoResponse[] response = new VideoResponse[videos.Length];
        for (int i = 0; i < videos.Length; i++)
        {
            response[i] = VideoResponse.FromVideo(videos[i]);
        }

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public ActionResult<VideoResponse> GetById(int id)
    {
        // look up the title in the hash index while holding the sync lock
        Video? video;
        lock (_runtime.SyncRoot)
        {
            if (!_runtime.VideoStore.TrySearchById(id, out Video? found) || found == null)
            {
                return NotFound();
            }

            video = found;
        }

        // unpublished content is hidden except for the owning publisher and admin operators
        if (!video.IsPublished)
        {
            if (!HttpContext.TryGetAuthSession(out AuthSession? session) || session == null)
            {
                return NotFound();
            }

            bool canSeeUnpublished = session.Role == UserRole.Admin
                || (session.Role == UserRole.Publisher && session.UserId == video.OwnerPublisherId);
            if (!canSeeUnpublished)
            {
                return NotFound();
            }
        }

        return Ok(VideoResponse.FromVideo(video));
    }
}
