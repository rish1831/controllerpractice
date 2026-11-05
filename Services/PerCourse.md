# Per-Course Locking — Student Course Enrollment API

## Goal

Allow enrollments into **different courses** to proceed concurrently, while preventing two requests from overbooking the **same course**.

## Why we can't just replace the global lock

Our current service stores every enrollment in one shared `List<Enrollment> _enrolled`.

A lock specific to Course 101 protects only operations that also take the Course 101 lock. A simultaneous request for Course 103 uses another lock and could modify the same shared `List` at the same time. `List<T>` is not safe for concurrent modification. Reads/enumeration can race with writes too.

## Implementation steps

### Step 1 — Create a state object for each course

Keep the lock and its enrollment list together:

```csharp
public sealed class CourseEnrollmentState
{
    public object SyncRoot { get; } = new();
    public List<Enrollment> Enrollments { get; } = new();
}
```

### Step 2 — Store course states in a concurrent dictionary

The concurrent dictionary safely creates/retrieves state for each course ID.

```csharp
private readonly ConcurrentDictionary<int, CourseEnrollmentState> _courseStates = new();

private CourseEnrollmentState GetState(int courseId) =>
    _courseStates.GetOrAdd(courseId, _ => new CourseEnrollmentState());
```

Add `using System.Collections.Concurrent;`. For a fixed course catalog, you can initialize states for every course once during service construction.

### Step 3 — Validate the request

Check positive IDs and existence of the student and course. If these catalogs can change at runtime, synchronize their updates and reads too.

### Step 4 — Obtain the target course state and lock it

```csharp
var state = GetState(request.CourseId);
lock (state.SyncRoot)
{
    // The check-and-insert sequence belongs here.
}
```

Requests for the **same course** wait for each other. Requests for **different courses** can enter their own locks independently.

### Step 5 — Inside that lock, enforce course-local rules

- Reject an active duplicate enrollment for the student and course.
- Count only `EnrollmentStatus.Enrolled` toward capacity (to match our existing API's rule).
- Reject when the enrolled count is `>= course.Capacity`.
- Create the `Enrollment`, add it to `state.Enrollments`, and return it **before releasing the lock**.

```csharp
lock (state.SyncRoot)
{
    if (state.Enrollments.Any(e =>
        e.StudentId == request.StudentId &&
        e.EnrollmentStatus != EnrollmentStatus.Cancelled))
        throw new InvalidOperationException("Duplicate enrollment");

    int enrolledCount = state.Enrollments.Count(e =>
        e.EnrollmentStatus == EnrollmentStatus.Enrolled);

    if (enrolledCount >= course.Capacity)
        throw new InvalidOperationException("Course full");

    var enrollment = new Enrollment
    {
        EnrollmentId = Guid.NewGuid(),
        StudentId = request.StudentId,
        CourseId = request.CourseId,
        EnrollmentStatus = EnrollmentStatus.Enrolled
    };

    state.Enrollments.Add(enrollment);
    return enrollment;
}
```

**Important:** The snippet above demonstrates only course-local duplicate/capacity enforcement. Do **not** omit prerequisite validation from the final API.

### Step 6 — Handle prerequisites across course states

If Course 102 requires Course 101, enrollment into 102 must check whether the student has a **Completed** enrollment in 101. That information lives in **another course's state**.

Pick a consistent policy:

- **Simple practical approach:** Treat course completion as stable/irreversible: once completed, it cannot be cancelled or reverted. Read the prerequisite course's state under its own lock, then lock the target course to enroll. This is safe for prerequisite eligibility **only if completion cannot be undone**; it does not give a transactionally consistent snapshot across courses.
- **If status can be reversed or multiple course states must change atomically:** Acquire all needed course locks in a deterministic order (e.g. ascending CourseId), do the check and insert while holding them, then release in reverse order. Always apply the same order in all code paths to avoid deadlocks.

Keep operations under those locks synchronous. Do not `await` inside a `lock`.

### Step 7 — Update every operation accessing enrollment state

- `MarkCourseCompleted`: locate the enrollment and change status under its course's lock. You may maintain a separate thread-safe `EnrollmentId -> CourseId` index to locate it efficiently, but keep that index synchronized with inserts/removals.
- `GetCoursesByStudent`: read each course's enrollment data under its own lock and copy relevant records; do not enumerate lists while other threads might mutate them.
- `GetCourseAvailability`: lock each course's state while counting its active enrollments.
- Cancellation/re-enrollment: follow the same per-course synchronization rules.
- Avoid returning mutable `Enrollment` instances that callers can alter without holding the appropriate lock. Return response DTOs or copies.

A multi-course GET that acquires and releases locks one at a time returns a **best-effort snapshot**, not necessarily one globally consistent instant. If exact cross-course consistency is required, take a coordinated snapshot under locks acquired in stable order.

### Step 8 — Test concurrent calls

1. **Same course, one seat:** send two parallel enrollments for different students. Exactly one should succeed; the other should receive a capacity conflict.
2. **Same course, same student:** send two parallel enrollments. Exactly one should succeed; the other should be rejected as a duplicate.
3. **Different courses:** enroll students concurrently. Both should succeed without modifying a shared unsafe list.
4. **Prerequisite:** verify incomplete prerequisite is rejected and completed prerequisite is accepted; test against concurrent status changes if reversals are permitted.
5. **Concurrent reads/writes:** GET endpoints should not throw collection-modified exceptions or return invalid counts.

## Global lock vs per-course lock

|                                          | One global lock                         | Per-course locks + separate course states   |
| ---------------------------------------- | --------------------------------------- | ------------------------------------------- |
| Implementation                           | Simplest                                | More code and edge cases                    |
| Different courses processed concurrently | No                                      | Yes                                         |
| Safe with one shared `List<Enrollment>`  | Yes, if every access uses the same lock | No                                          |
| Cross-course prerequisite logic          | Straightforward                         | Requires a clear locking/consistency policy |
| Protects multiple application instances  | No                                      | No                                          |

## What to say in the interview

> “I started with a global lock because it is simple and correct for our shared in-memory list. To improve concurrency, I would keep a separate enrollment list and lock per course, stored in a `ConcurrentDictionary`. I would execute duplicate, capacity and insert operations under that course's lock. For cross-course prerequisite checks, I would coordinate the relevant course locks in a fixed order if atomic consistency is required.”

**Scope:** These locks protect only a single application instance. A multi-instance deployment needs database transactions/constraints or shared coordination to enforce capacity reliably.
