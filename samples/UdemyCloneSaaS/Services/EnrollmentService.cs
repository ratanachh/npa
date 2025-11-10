using UdemyCloneSaaS.Entities;
using UdemyCloneSaaS.Repositories;

namespace UdemyCloneSaaS.Services;

/// <summary>
/// Business logic service for managing student enrollments.
/// </summary>
public class EnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IPaymentRepository _paymentRepository;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        ICourseRepository courseRepository,
        IStudentRepository studentRepository,
        IPaymentRepository paymentRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        _courseRepository = courseRepository;
        _studentRepository = studentRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<Enrollment> EnrollStudentAsync(long studentId, long courseId, Payment? payment = null)
    {
        // Check if already enrolled
        var existing = await _enrollmentRepository.FindByStudentIdAndCourseIdAsync(studentId, courseId);
        if (existing != null)
        {
            throw new InvalidOperationException("Student already enrolled in this course");
        }

        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            throw new ArgumentException("Course not found");
        }

        // Create enrollment
        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            TenantId = course.TenantId,
            EnrolledAt = DateTime.UtcNow,
            ProgressPercentage = 0.0m,
            CompletedLessonsCount = 0,
            IsCompleted = false
        };

        await _enrollmentRepository.AddAsync(enrollment);

        // Update course stats
        course.EnrolledStudentsCount++;
        await _courseRepository.UpdateAsync(course);

        // Update student stats
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student != null)
        {
            student.EnrolledCoursesCount++;
            student.LastActiveAt = DateTime.UtcNow;
            await _studentRepository.UpdateAsync(student);
        }

        // Link payment if provided
        if (payment != null)
        {
            payment.EnrollmentId = enrollment.Id;
            await _paymentRepository.UpdateAsync(payment);
        }

        return enrollment;
    }

    public async Task UpdateProgressAsync(long enrollmentId, int completedLessons, decimal progressPercentage)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId);
        if (enrollment == null) return;

        enrollment.CompletedLessonsCount = completedLessons;
        enrollment.ProgressPercentage = progressPercentage;
        enrollment.LastAccessedAt = DateTime.UtcNow;

        // Mark as completed if 100%
        if (progressPercentage >= 100.0m && !enrollment.IsCompleted)
        {
            enrollment.IsCompleted = true;
            enrollment.CompletedAt = DateTime.UtcNow;

            // Update student stats
            var student = await _studentRepository.GetByIdAsync(enrollment.StudentId);
            if (student != null)
            {
                student.CompletedCoursesCount++;
                await _studentRepository.UpdateAsync(student);
            }
        }

        await _enrollmentRepository.UpdateAsync(enrollment);
    }

    public async Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(long studentId)
    {
        return await _enrollmentRepository.FindByStudentIdAsync(studentId);
    }

    public async Task<IEnumerable<Enrollment>> GetActiveEnrollmentsAsync(long studentId)
    {
        return await _enrollmentRepository.GetActiveEnrollmentsByStudentAsync(studentId);
    }

    public async Task IssueCertificateAsync(long enrollmentId)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId);
        if (enrollment == null) return;

        if (!enrollment.IsCompleted)
        {
            throw new InvalidOperationException("Cannot issue certificate for incomplete course");
        }

        enrollment.CertificateIssued = true;
        await _enrollmentRepository.UpdateAsync(enrollment);
    }
}
