public interface IBookingService
{
    public RoomBooking CreateBooking(CreateBookingRequest request);
    public List<RoomBooking> GetAllBookings(GetBookingRequest request);
    public RoomBooking? GetBookingById(Guid bookingId);
    public bool DeleteBooking(Guid bookingId);
}