using System.Collections.Concurrent;

class BookingService : IBookingService
{
    private readonly List<RoomBooking> _bookings = new();
    private readonly ConcurrentDictionary<int, object> _concurrentDictionary = new();
    public RoomBooking CreateBooking(CreateBookingRequest request)
    {
        var lockobj = _concurrentDictionary.GetOrAdd(request.RoomId, _ => new object());
        if (request.RoomId <= 0)
            throw new ArgumentException("Invalid RoomId");
        if (request.StartTime >= request.EndDate)
            throw new ArgumentException("Start time must be earlier than end time");
        lock (lockobj)
        {
            if (!IsBookingAllowed(request.RoomId, request.StartTime, request.EndDate))
                throw new InvalidOperationException("Room is already booked in this time period");
            var booking = new RoomBooking
            {
                BookingId = Guid.NewGuid(),
                Status = Bookingstatus.Confirmed,
                RoomId = request.RoomId,
                StartTime = request.StartTime,
                EndTime = request.EndDate,

            };
            _bookings.Add(booking);
            return booking;
        }
    }
    public List<RoomBooking> GetAllBookings(GetBookingRequest request)
    {
        UpdateBooking();
        return _bookings.FindAll(booking => (request.RoomId == null || request.RoomId == booking.RoomId) &&
        (request.Status == null || request.Status == booking.Status));
    }
    public RoomBooking? GetBookingById(Guid bookingId)
    {
        UpdateBooking();
        return _bookings.Find(booking => booking.BookingId == bookingId);
    }
    public bool DeleteBooking(Guid bookingId)
    {
        var booking = _bookings.Find(booking => booking.BookingId == bookingId);
        if (booking == null)
            return false;
        booking.Status = Bookingstatus.Cancelled;
        return true;
    }
    private void UpdateBooking()
    {
        var time = DateTime.Now;
        foreach (var booking in _bookings)
        {
            if (booking.Status == Bookingstatus.Confirmed && booking.EndTime <= time)
            {
                booking.Status = Bookingstatus.Expired;
            }
        }
    }
    private bool IsBookingAllowed(int roomId, DateTime startTime, DateTime endTime)
    {
        return !_bookings.Any(booking => booking.RoomId == roomId && booking.Status == Bookingstatus.Confirmed
        && startTime < booking.EndTime && endTime > booking.StartTime);
    }
}