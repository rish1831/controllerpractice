using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class CourseAllotmentController : ControllerBase
{
    private readonly ICourseAllotmentService _courseAllotmentService;
    public CourseAllotmentController(ICourseAllotmentService courseAllotmentService)
    {
        _courseAllotmentService = courseAllotmentService;
    }
    [HttpPost]
    public IActionResult Enroll(EnrollCourseRequest request)
    {
        try
        {
            return StatusCode(201, _courseAllotmentService.Enroll(request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }

    }

    [HttpGet("student/{studentId}/courses")]
    public IActionResult GetCoursesByStudent(int studentId)
    {
        return Ok(_courseAllotmentService.GetCoursesByStudent(studentId));
    }

    [HttpGet("courses/availability")]
    public IActionResult GetCourseAvailability()
    {
        return Ok(_courseAllotmentService.GetCourseAvailability());
    }
    [HttpPatch("{enrollemtnId}/complete")]
    public IActionResult MarkCouseCompleted(Guid enrollemtnId)
    {
        try
        {
            _courseAllotmentService.MarkCourseCompleted(enrollemtnId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}