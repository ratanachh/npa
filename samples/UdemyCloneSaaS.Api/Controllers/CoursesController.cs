using Microsoft.AspNetCore.Mvc;
using UdemyCloneSaaS.Entities;
using UdemyCloneSaaS.Repositories;

namespace UdemyCloneSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseRepository _courseRepository;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(
        ICourseRepository courseRepository,
        ILogger<CoursesController> logger)
    {
        _courseRepository = courseRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all courses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Course>>> GetAll()
    {
        try
        {
            var courses = await _courseRepository.GetAllAsync();
            return Ok(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courses");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get course by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Course>> GetById(long id)
    {
        try
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
                return NotFound($"Course with ID {id} not found");

            return Ok(course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course {CourseId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get courses by instructor
    /// </summary>
    [HttpGet("by-instructor/{instructorId}")]
    public async Task<ActionResult<IEnumerable<Course>>> GetByInstructor(long instructorId)
    {
        try
        {
            var courses = await _courseRepository.FindByInstructorIdAsync(instructorId);
            return Ok(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courses for instructor {InstructorId}", instructorId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new course
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Course>> Create([FromBody] Course course)
    {
        try
        {
            await _courseRepository.AddAsync(course);
            return CreatedAtAction(nameof(GetById), new { id = course.Id }, course);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update a course
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(long id, [FromBody] Course course)
    {
        try
        {
            if (id != course.Id)
                return BadRequest("ID mismatch");

            var exists = await _courseRepository.GetByIdAsync(id);
            if (exists == null)
                return NotFound($"Course with ID {id} not found");

            await _courseRepository.UpdateAsync(course);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course {CourseId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a course
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(long id)
    {
        try
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null)
                return NotFound($"Course with ID {id} not found");

            await _courseRepository.DeleteAsync(course);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course {CourseId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
