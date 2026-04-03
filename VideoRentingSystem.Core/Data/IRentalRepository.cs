// Interface = promise to the Core layer: "any database wrapper you plug in must implement these operations".
// VideoStore depends on this instead of Sqlite types so tests can stub things out easily.
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Data;

public interface IRentalRepository
{
    Rental[] LoadAllRentals();
    void InsertRental(Rental rental);
    void DeleteRental(int userId, int videoId);
    bool TryGetRentDate(int userId, int videoId, out DateTime rentDate);
}
