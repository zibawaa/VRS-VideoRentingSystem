using Microsoft.AspNetCore.Mvc;
using VideoRentingSystem.Api.Bootstrap;
using VideoRentingSystem.Api.Contracts.Rentals;
using VideoRentingSystem.Api.Security;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Api.Controllers;

/// <summary>
/// Customer-facing endpoints for renting, returning, and listing active rentals.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class RentalsController : ControllerBase
{
    private readonly StoreRuntime _runtime;

    public RentalsController(StoreRuntime runtime)
    {
        _runtime = runtime;
    }

    [HttpPost("{videoId:int}/rent")]
    public IActionResult Rent(int videoId)
    {
        // all rental actions require a valid bearer token
        if (!HttpContext.TryGetAuthSession(out AuthSession? session) || session == null)
        {
            return Unauthorized("Missing or invalid bearer token.");
        }

        // delegate to the core VideoStore which checks published status and availability
        bool ok;
        lock (_runtime.SyncRoot)
        {
            ok = _runtime.VideoStore.RentVideo(videoId, session.UserId);
        }

        if (!ok)
        {
            return Conflict("Rent action was denied.");
        }

        return NoContent();
    }

    [HttpPost("{videoId:int}/return")]
    public IActionResult Return(int videoId)
    {
        // require identity before allowing a return
        if (!HttpContext.TryGetAuthSession(out AuthSession? session) || session == null)
        {
            return Unauthorized("Missing or invalid bearer token.");
        }

        // core store handles ownership verification and state reset
        bool ok;
        lock (_runtime.SyncRoot)
        {
            ok = _runtime.VideoStore.ReturnVideo(videoId, session.UserId);
        }

        if (!ok)
        {
            return Conflict("Return action was denied.");
        }

        return NoContent();
    }

    [HttpGet("me")]
    public ActionResult<RentalResponse[]> GetMine()
    {
        // must be signed in to list personal rentals
        if (!HttpContext.TryGetAuthSession(out AuthSession? session) || session == null)
        {
            return Unauthorized("Missing or invalid bearer token.");
        }

        // fetch all videos currently rented by this user
        Video[] videos;
        lock (_runtime.SyncRoot)
        {
            videos = _runtime.VideoStore.GetUserRentedVideos(session.UserId);
        }

        // enrich each video with its rental metadata (date, expiry, price paid)
        RentalResponse[] rentals = new RentalResponse[videos.Length];
        for (int i = 0; i < videos.Length; i++)
        {
            DateTime rentDate = default;
            DateTime expiryUtc = default;
            decimal paidAmount = 0m;

            lock (_runtime.SyncRoot)
            {
                _runtime.VideoStore.TryGetRentalInfo(session.UserId, videos[i].VideoId, out rentDate, out expiryUtc, out paidAmount);
            }

            rentals[i] = new RentalResponse
            {
                VideoId = videos[i].VideoId,
                Title = videos[i].Title,
                Genre = videos[i].Genre,
                RentDateUtc = rentDate,
                ExpiryUtc = expiryUtc,
                PaidAmount = paidAmount,
                IsRented = videos[i].IsRented
            };
        }

        return Ok(rentals);
    }
}
