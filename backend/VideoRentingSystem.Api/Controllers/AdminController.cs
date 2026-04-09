using Microsoft.AspNetCore.Mvc;
using VideoRentingSystem.Api.Bootstrap;
using VideoRentingSystem.Api.Contracts.Admin;
using VideoRentingSystem.Api.Contracts.Auth;
using VideoRentingSystem.Api.Contracts.Publisher;
using VideoRentingSystem.Api.Security;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Api.Controllers;

/// <summary>
/// Admin-only controller that exposes full system management: user listing with
/// active session tokens, video CRUD (including edit and publish toggle), global
/// rental view, and the ability to rent/return on behalf of any user.
/// Every endpoint requires an authenticated Admin session except the login.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AdminController : ControllerBase
{
    private readonly StoreRuntime _runtime;
    private readonly AuthSessionService _sessionService;

    public AdminController(StoreRuntime runtime, AuthSessionService sessionService)
    {
        _runtime = runtime;
        _sessionService = sessionService;
    }

    /* ═══════ AUTH ═══════ */

    /// <summary>
    /// Admin-specific login. Same credentials flow as regular login but verifies
    /// the authenticated user holds the Admin role before returning a token.
    /// </summary>
    [HttpPost("login")]
    public ActionResult<AuthResponse> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required.");
        }

        User? user;
        lock (_runtime.SyncRoot)
        {
            user = _runtime.UserStore.Login(request.Username, request.Password);
        }

        if (user == null)
        {
            return Unauthorized("Invalid username or password.");
        }

        // only admin accounts can access the admin panel
        if (user.Role != UserRole.Admin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "This account does not have admin privileges.");
        }

        AuthSession session = _sessionService.CreateSession(user);
        return Ok(new AuthResponse
        {
            Token = session.Token,
            UserId = session.UserId,
            Username = session.Username,
            Role = session.Role.ToString(),
            StudioName = session.StudioName,
            ExpiresAtUtc = session.ExpiresAtUtc
        });
    }

    /* ═══════ USERS ═══════ */

    /// <summary>
    /// Lists every registered user with their active session tokens.
    /// </summary>
    [HttpGet("users")]
    public IActionResult GetAllUsers()
    {
        if (!TryRequireAdmin(out AuthSession? _))
        {
            return Unauthorized("Admin session required.");
        }

        User[] users;
        lock (_runtime.SyncRoot)
        {
            users = _runtime.UserStore.GetAllUsers();
        }

        // enrich each user with their active session tokens
        AdminUserResponse[] result = new AdminUserResponse[users.Length];
        for (int i = 0; i < users.Length; i++)
        {
            AuthSession[] sessions = _sessionService.GetSessionsByUser(users[i].UserId);
            AdminSessionInfo[] sessionInfos = new AdminSessionInfo[sessions.Length];
            for (int j = 0; j < sessions.Length; j++)
            {
                sessionInfos[j] = new AdminSessionInfo
                {
                    Token = sessions[j].Token,
                    ExpiresAtUtc = sessions[j].ExpiresAtUtc
                };
            }

            result[i] = new AdminUserResponse
            {
                UserId = users[i].UserId,
                Username = users[i].Username,
                Role = users[i].Role.ToString(),
                StudioName = users[i].StudioName,
                Sessions = sessionInfos
            };
        }

        return Ok(result);
    }

    /* ═══════ VIDEOS ═══════ */

    /// <summary>
    /// Lists the entire video catalog (published and unpublished).
    /// </summary>
    [HttpGet("videos")]
    public IActionResult GetAllVideos()
    {
        if (!TryRequireAdmin(out AuthSession? _))
        {
            return Unauthorized("Admin session required.");
        }

        Video[] videos;
        lock (_runtime.SyncRoot)
        {
            videos = _runtime.VideoStore.DisplayAllVideos();
        }

        return Ok(videos.Select(v => new
        {
            v.VideoId, v.Title, v.Genre, v.ReleaseYear, v.IsRented,
            v.OwnerPublisherId, Type = v.Type.ToString(),
            v.RentalPrice, v.RentalHours, v.IsPublished
        }));
    }

    /// <summary>
    /// Creates a new video in the catalog. Admin can assign any owner publisher ID.
    /// </summary>
    [HttpPost("videos")]
    public IActionResult CreateVideo([FromBody] CreateVideoRequest request)
    {
        if (!TryRequireAdmin(out AuthSession? session))
        {
            return Unauthorized("Admin session required.");
        }

        if (!Enum.TryParse<VideoType>(request.Type, ignoreCase: true, out VideoType videoType))
        {
            return BadRequest("Type must be Movie or Series.");
        }

        int ownerId = request.OwnerPublisherId ?? session!.UserId;
        Video video;
        try
        {
            video = new Video(
                request.VideoId, request.Title, request.Genre, request.ReleaseYear,
                false, ownerId, videoType, request.RentalPrice, request.RentalHours, request.IsPublished);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        bool added;
        lock (_runtime.SyncRoot)
        {
            added = _runtime.VideoStore.AddVideo(video);
        }

        if (!added)
        {
            return Conflict("Video ID already exists.");
        }

        return Ok(new { message = $"Video #{request.VideoId} created." });
    }

    /// <summary>
    /// Updates all mutable fields on an existing video and re-indexes.
    /// </summary>
    [HttpPut("videos/{id:int}")]
    public IActionResult EditVideo(int id, [FromBody] AdminEditVideoRequest request)
    {
        if (!TryRequireAdmin(out AuthSession? _))
        {
            return Unauthorized("Admin session required.");
        }

        if (!Enum.TryParse<VideoType>(request.Type, ignoreCase: true, out VideoType videoType))
        {
            return BadRequest("Type must be Movie or Series.");
        }

        bool updated;
        try
        {
            lock (_runtime.SyncRoot)
            {
                updated = _runtime.VideoStore.UpdateVideo(id, request.Title, request.Genre,
                    request.ReleaseYear, request.OwnerPublisherId, videoType,
                    request.RentalPrice, request.RentalHours, request.IsPublished);
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        if (!updated)
        {
            return NotFound($"Video #{id} not found.");
        }

        return Ok(new { message = $"Video #{id} updated." });
    }

    /// <summary>
    /// Toggles the IsPublished flag on a video.
    /// </summary>
    [HttpPatch("videos/{id:int}/publish")]
    public IActionResult TogglePublish(int id)
    {
        if (!TryRequireAdmin(out AuthSession? _))
        {
            return Unauthorized("Admin session required.");
        }

        bool toggled;
        lock (_runtime.SyncRoot)
        {
            toggled = _runtime.VideoStore.TogglePublish(id);
        }

        if (!toggled)
        {
            return NotFound($"Video #{id} not found.");
        }

        return Ok(new { message = $"Publish toggled for video #{id}." });
    }

    /// <summary>
    /// Permanently removes a video from the catalog.
    /// </summary>
    [HttpDelete("videos/{id:int}")]
    public IActionResult DeleteVideo(int id)
    {
        if (!TryRequireAdmin(out AuthSession? _))
        {
            return Unauthorized("Admin session required.");
        }

        bool removed;
        lock (_runtime.SyncRoot)
        {
            removed = _runtime.VideoStore.RemoveVideo(id);
        }

        if (!removed)
        {
            return NotFound($"Video #{id} not found.");
        }

        return Ok(new { message = $"Video #{id} deleted." });
    }

    /* ═══════ RENTALS ═══════ */

    /// <summary>
    /// Lists every active rental across all users with video and user metadata.
    /// </summary>
    [HttpGet("rentals")]
    public IActionResult GetAllRentals()
    {
        if (!TryRequireAdmin(out AuthSession? _))
        {
            return Unauthorized("Admin session required.");
        }

        Rental[] rentals;
        lock (_runtime.SyncRoot)
        {
            rentals = _runtime.VideoStore.GetAllRentals();
        }

        // enrich each rental row with the video title and user info
        User[] allUsers;
        lock (_runtime.SyncRoot)
        {
            allUsers = _runtime.UserStore.GetAllUsers();
        }

        var result = new AdminRentalResponse[rentals.Length];
        for (int i = 0; i < rentals.Length; i++)
        {
            Rental r = rentals[i];

            // resolve the video title from the id index
            string title = "Unknown";
            string genre = "";
            lock (_runtime.SyncRoot)
            {
                if (_runtime.VideoStore.TrySearchById(r.VideoId, out Video? v) && v != null)
                {
                    title = v.Title;
                    genre = v.Genre;
                }
            }

            // resolve username from the user list
            string username = "Unknown";
            for (int u = 0; u < allUsers.Length; u++)
            {
                if (allUsers[u].UserId == r.UserId)
                {
                    username = allUsers[u].Username;
                    break;
                }
            }

            result[i] = new AdminRentalResponse
            {
                UserId = r.UserId,
                Username = username,
                VideoId = r.VideoId,
                Title = title,
                Genre = genre,
                RentDateUtc = r.RentDate,
                ExpiryUtc = r.ExpiryUtc,
                PaidAmount = r.PaidAmount
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Rents a video on behalf of any user. The admin specifies the target userId in the body.
    /// </summary>
    [HttpPost("rentals/{videoId:int}/rent")]
    public IActionResult RentForUser(int videoId, [FromBody] AdminRentRequest request)
    {
        if (!TryRequireAdmin(out AuthSession? _))
        {
            return Unauthorized("Admin session required.");
        }

        if (request.UserId <= 0)
        {
            return BadRequest("A valid userId is required.");
        }

        bool ok;
        lock (_runtime.SyncRoot)
        {
            ok = _runtime.VideoStore.RentVideo(videoId, request.UserId);
        }

        if (!ok)
        {
            return Conflict("Rent action was denied. The video may already be rented or not published.");
        }

        return Ok(new { message = $"Video #{videoId} rented for user #{request.UserId}." });
    }

    /// <summary>
    /// Returns a video on behalf of any user. The admin specifies the target userId in the body.
    /// </summary>
    [HttpPost("rentals/{videoId:int}/return")]
    public IActionResult ReturnForUser(int videoId, [FromBody] AdminRentRequest request)
    {
        if (!TryRequireAdmin(out AuthSession? _))
        {
            return Unauthorized("Admin session required.");
        }

        if (request.UserId <= 0)
        {
            return BadRequest("A valid userId is required.");
        }

        bool ok;
        lock (_runtime.SyncRoot)
        {
            ok = _runtime.VideoStore.ReturnVideo(videoId, request.UserId);
        }

        if (!ok)
        {
            return Conflict("Return action was denied.");
        }

        return Ok(new { message = $"Video #{videoId} returned for user #{request.UserId}." });
    }

    /* ═══════ HELPERS ═══════ */

    // extracts the session from the request and verifies it belongs to an admin
    private bool TryRequireAdmin(out AuthSession? session)
    {
        if (!HttpContext.TryGetAuthSession(out session) || session == null)
        {
            return false;
        }

        return session.Role == UserRole.Admin;
    }
}
