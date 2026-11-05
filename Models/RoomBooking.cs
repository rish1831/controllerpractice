public class RoomBooking
{
    public Guid BookingId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Bookingstatus Status { get; set; }
}