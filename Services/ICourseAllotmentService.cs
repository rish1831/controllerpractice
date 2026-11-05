public interface ICourseAllotmentService
{
    public Enrollment Enroll(EnrollCourseRequest request);
    public List<Course> GetCoursesByStudent(int studentId);
    public List<CourseAvailabilityResponse> GetCourseAvailability();
    public void MarkCourseCompleted(Guid enrollmentId);
}