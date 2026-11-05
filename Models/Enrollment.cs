public class Enrollment
{
    public Guid EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
}