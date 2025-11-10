using UdemyCloneSaaS.Entities;
using UdemyCloneSaaS.Repositories;

namespace UdemyCloneSaaS.Services;

/// <summary>
/// Business logic service for managing courses.
/// </summary>
public class CourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IInstructorRepository _instructorRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CourseService(
        ICourseRepository courseRepository,
        ILessonRepository lessonRepository,
        IInstructorRepository instructorRepository,
        ICategoryRepository categoryRepository)
    {
        _courseRepository = courseRepository;
        _lessonRepository = lessonRepository;
        _instructorRepository = instructorRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<Course> CreateCourseAsync(Course course)
    {
        // Generate slug from title
        course.Slug = GenerateSlug(course.Title);
        course.Status = "Draft";
        course.CreatedAt = DateTime.UtcNow;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.AddAsync(course);
        return course;
    }

    public async Task<Course?> PublishCourseAsync(long courseId)
    {
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null) return null;

        // Validate course is ready to publish
        var lessons = await _lessonRepository.FindByCourseIdAsync(courseId);
        if (!lessons.Any())
        {
            throw new InvalidOperationException("Cannot publish course without lessons");
        }

        course.Status = "Published";
        course.PublishedAt = DateTime.UtcNow;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateAsync(course);
        return course;
    }

    public async Task<IEnumerable<Course>> GetFeaturedCoursesAsync()
    {
        return await _courseRepository.GetFeaturedCoursesAsync();
    }

    public async Task<IEnumerable<Course>> GetTopRatedCoursesAsync(int limit = 10)
    {
        return await _courseRepository.GetTopRatedCoursesAsync(limit);
    }

    public async Task<IEnumerable<Course>> GetMostPopularCoursesAsync(int limit = 10)
    {
        return await _courseRepository.GetMostPopularCoursesAsync(limit);
    }

    public async Task<IEnumerable<Course>> SearchCoursesAsync(string searchTerm)
    {
        return await _courseRepository.SearchCoursesAsync($"%{searchTerm}%");
    }

    public async Task UpdateCourseStatsAsync(long courseId)
    {
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null) return;

        var lessons = await _lessonRepository.FindByCourseIdAsync(courseId);
        course.TotalLessons = lessons.Count();
        course.DurationHours = lessons.Sum(l => l.DurationMinutes) / 60.0m;
        course.UpdatedAt = DateTime.UtcNow;

        await _courseRepository.UpdateAsync(course);
    }

    private string GenerateSlug(string title)
    {
        return title.ToLower()
            .Replace(" ", "-")
            .Replace(":", "")
            .Replace("?", "")
            .Replace("!", "");
    }
}
