using System.Collections.Concurrent;

public class CourseAllotmentService : ICourseAllotmentService
{

    private readonly object _lock = new();
    private readonly ConcurrentDictionary<int, object> _courseLocks = new();

    private object GetOrAddLockByCourseId(int courseId)
    {
        return _courseLocks.GetOrAdd(courseId, _ => new object());

    }
    private readonly List<Student> _students = new List<Student>
    {
      new Student{StudentId=1,Name="Rishabh"},
        new Student{StudentId=2,Name="Nam"},
          new Student{StudentId=3,Name="Anurag"},
            new Student{StudentId=4,Name="Shobhit"}
    };
    private readonly List<Course> _courses = new List<Course>
    {
        new Course
        {
            CourseId=101,
            CourseName="DSA",
            Capacity=3,
            PrerequisiteCourseId=null
        },
         new Course
        {
            CourseId=102,
            CourseName="DAA",
            Capacity=4,
            PrerequisiteCourseId=101
        },
         new Course
        {
            CourseId=103,
            CourseName="History",
            Capacity=2,
            PrerequisiteCourseId=null
        }
    };
    private readonly List<Enrollment> _enrolled = new();
    public Enrollment Enroll(EnrollCourseRequest request)
    {
        if (request.StudentId <= 0)
        {
            throw new ArgumentException("Student Id must be greater than 0");
        }
        if (request.CourseId <= 0)
        {
            throw new ArgumentException("Course Id must be greater than 0");
        }
        if (!_students.Any(student => student.StudentId == request.StudentId))
        {
            throw new KeyNotFoundException("Student not found");
        }
        var course = _courses.Find(c => c.CourseId == request.CourseId);
        if (course == null)
        {
            throw new KeyNotFoundException("Course not found");
        }

        // var _lock = GetOrAddLockByCourseId(request.CourseId);

        lock (_lock)
        {
            if (_enrolled.Any(enrolled => enrolled.CourseId == request.CourseId && enrolled.StudentId == request.StudentId && enrolled.EnrollmentStatus != EnrollmentStatus.Cancelled))
            {
                throw new InvalidOperationException("Duplicate enrollment");
            }
            var prereuisiteId = course.PrerequisiteCourseId;
            var prerequisiteCourse = _courses.Find(course => course.CourseId == prereuisiteId);
            if (course.PrerequisiteCourseId.HasValue && !_enrolled.Any(enrolled => enrolled.CourseId == prereuisiteId && enrolled.StudentId == request.StudentId && enrolled.EnrollmentStatus == EnrollmentStatus.Completed))
            {
                throw new InvalidOperationException($"You must complete {prerequisiteCourse?.CourseName} (Course ID: {course.PrerequisiteCourseId}) before enrolling in {course.CourseName}");
            }
            var enrolledCount = _enrolled.Count(enr => enr.CourseId == request.CourseId && enr.EnrollmentStatus == EnrollmentStatus.Enrolled);
            if (course?.Capacity - enrolledCount <= 0)
                throw new InvalidOperationException("Course full");

            var enrollment = new Enrollment
            {
                EnrollmentId = Guid.NewGuid(),
                StudentId = request.StudentId,
                CourseId = request.CourseId,
                EnrollmentStatus = EnrollmentStatus.Enrolled
            };
            _enrolled.Add(enrollment);

            return enrollment;
        }
    }
    public List<Course> GetCoursesByStudent(int studentId)
    {
        return _enrolled.Where(enrolled => enrolled.StudentId == studentId)
        .Join(_courses,
        e => e.CourseId,
        c => c.CourseId,
        (e, c) => c).ToList();
    }
    // public List<CourseAvailabilityResponse> GetCourseAvailability()
    // {
    //     return _courses.Select(
    //         course =>
    //          {
    //              int enrolled = _enrolled.Count(e => e.CourseId == course.CourseId);
    //              return new CourseAvailabilityResponse
    //              {
    //                  CourseName = course.CourseName,
    //                  Capacity = course.Capacity,
    //                  CourseId = course.CourseId,
    //                  PrerequisiteCourseId = course.PrerequisiteCourseId,
    //                  SeatsEnrolled = enrolled,
    //                  SeatsAvailable = course.Capacity - enrolled
    //              };
    //          }
    //     ).ToList();
    // }

    // Better Apprach time wise
    public List<CourseAvailabilityResponse> GetCourseAvailability()
    {
        var enrollmentCounts = _enrolled
            .GroupBy(e => e.CourseId)
            .ToDictionary(
                group => group.Key,
                group => group.Count()
            );

        return _courses.Select(course =>
        {
            int enrolled = enrollmentCounts.GetValueOrDefault(course.CourseId);

            return new CourseAvailabilityResponse
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                Capacity = course.Capacity,
                PrerequisiteCourseId = course.PrerequisiteCourseId,
                SeatsEnrolled = enrolled,
                SeatsAvailable = course.Capacity - enrolled
            };
        }).ToList();
    }
    public void MarkCourseCompleted(Guid enrollmentId)
    {
        var enrolledCourse = _enrolled.Find(enr => enr.EnrollmentId == enrollmentId);
        if (enrolledCourse == null)
            throw new KeyNotFoundException($"Enrollment does not exist with EnrollmentId:{enrollmentId}");
        if (enrolledCourse.EnrollmentStatus != EnrollmentStatus.Enrolled)
            throw new InvalidOperationException($"Cannot complete enrollment. Current status is {enrolledCourse.EnrollmentStatus}. Only enrolled courses can be marked as completed.");
        enrolledCourse.EnrollmentStatus = EnrollmentStatus.Completed;
    }
}