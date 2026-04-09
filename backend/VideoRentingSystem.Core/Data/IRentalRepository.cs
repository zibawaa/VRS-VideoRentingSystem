using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Data;

public interface IRentalRepository
{
    Rental[] LoadAllRentals();
    void InsertRental(Rental rental);
    void DeleteRental(int userId, int videoId);
    bool TryGetRentDate(int userId, int videoId, out DateTime rentDate);
    bool TryGetRentalInfo(int userId, int videoId, out DateTime rentDate, out DateTime expiryUtc, out decimal paidAmount);
    // load-all + insert + delete + lookup rent timestamp for one (user,video) pair
}
