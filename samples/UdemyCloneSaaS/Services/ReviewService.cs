using UdemyCloneSaaS.Entities;
using UdemyCloneSaaS.Repositories;

namespace UdemyCloneSaaS.Services;

/// <summary>
/// Business logic service for managing course reviews and ratings.
/// </summary>
public class ReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IInstructorRepository _instructorRepository;

    public ReviewService(
        IReviewRepository reviewRepository,
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        IInstructorRepository instructorRepository)
    {
        _reviewRepository = reviewRepository;
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
        _instructorRepository = instructorRepository;
    }

    public async Task<Review> CreateReviewAsync(Review review)
    {
        // Check if student is enrolled
        var enrollment = await _enrollmentRepository.FindByStudentIdAndCourseIdAsync(review.StudentId, review.CourseId);
        if (enrollment == null)
        {
            throw new InvalidOperationException("Student must be enrolled to review the course");
        }

        // Check if already reviewed
        var existing = await _reviewRepository.FindByStudentIdAndCourseIdAsync(review.StudentId, review.CourseId);
        if (existing != null)
        {
            throw new InvalidOperationException("Student has already reviewed this course");
        }

        review.IsVerifiedPurchase = true;
        review.CreatedAt = DateTime.UtcNow;

        await _reviewRepository.AddAsync(review);

        // Update course stats
        await UpdateCourseRatingStatsAsync(review.CourseId);

        return review;
    }

    public async Task<Review?> UpdateReviewAsync(long reviewId, int rating, string? title, string? comment)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null) return null;

        review.Rating = rating;
        review.Title = title;
        review.Comment = comment;
        review.UpdatedAt = DateTime.UtcNow;

        await _reviewRepository.UpdateAsync(review);

        // Update course stats
        await UpdateCourseRatingStatsAsync(review.CourseId);

        return review;
    }

    public async Task<IEnumerable<Review>> GetCourseReviewsAsync(long courseId)
    {
        return await _reviewRepository.FindByCourseIdAsync(courseId);
    }

    public async Task<IEnumerable<Review>> GetMostHelpfulReviewsAsync(long courseId, int limit = 5)
    {
        return await _reviewRepository.GetMostHelpfulReviewsAsync(courseId, limit);
    }

    public async Task MarkReviewHelpfulAsync(long reviewId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null) return;

        review.HelpfulCount++;
        await _reviewRepository.UpdateAsync(review);
    }

    private async Task UpdateCourseRatingStatsAsync(long courseId)
    {
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null) return;

        var averageRating = await _reviewRepository.GetAverageRatingByCourseAsync(courseId);
        var totalReviews = await _reviewRepository.CountReviewsByCourseAsync(courseId);

        course.AverageRating = averageRating;
        course.TotalReviews = (int)totalReviews;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateAsync(course);

        // Update instructor stats
        var instructor = await _instructorRepository.GetByIdAsync(course.InstructorId);
        if (instructor != null)
        {
            await UpdateInstructorStatsAsync(instructor.Id);
        }
    }

    private async Task UpdateInstructorStatsAsync(long instructorId)
    {
        var instructor = await _instructorRepository.GetByIdAsync(instructorId);
        if (instructor == null) return;

        var courses = await _courseRepository.FindByInstructorIdAsync(instructorId);
        var courseList = courses.ToList();

        instructor.TotalStudents = courseList.Sum(c => c.EnrolledStudentsCount);
        instructor.TotalReviews = courseList.Sum(c => c.TotalReviews);
        instructor.AverageRating = courseList.Any() 
            ? courseList.Average(c => c.AverageRating) 
            : 0.0m;

        await _instructorRepository.UpdateAsync(instructor);
    }
}
