using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class RoomBookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    public RoomBookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }
    [HttpPost]
    public IActionResult CreateBooking(CreateBookingRequest request)
    {
        try
        {
            var booking = _bookingService.CreateBooking(request);
            return StatusCode(201, booking);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }

    }

    [HttpGet]
    public IActionResult GetAllBookings([FromQuery] GetBookingRequest request)
    {
        var bookings = _bookingService.GetAllBookings(request);
        return Ok(bookings);
    }
    [HttpGet("id/{bookingId}")]
    public IActionResult GetBookingById(Guid bookingId)
    {
        var booking = _bookingService.GetBookingById(bookingId);
        if (booking != null)
            return Ok(booking);
        return NotFound();
    }
    [HttpDelete("{bookingId}")]
    public IActionResult DeleteBooking(Guid bookingId)
    {
        var isCancelled = _bookingService.DeleteBooking(bookingId);
        if (!isCancelled)
            return NotFound();
        return NoContent();
    }

}