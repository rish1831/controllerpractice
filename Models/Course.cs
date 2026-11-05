public class Course
{
    public int CourseId { get; set; }
    public int Capacity { get; set; }
    public required string CourseName { get; set; }
    public int? PrerequisiteCourseId { get; set; }
}