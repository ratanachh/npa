using Microsoft.AspNetCore.Mvc;
using UdemyCloneSaaS.Entities;
using UdemyCloneSaaS.Repositories;

namespace UdemyCloneSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ILogger<EnrollmentsController> _logger;

    public EnrollmentsController(
        IEnrollmentRepository enrollmentRepository,
        ILogger<EnrollmentsController> logger)
    {
        _enrollmentRepository = enrollmentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all enrollments
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Enrollment>>> GetAll()
    {
        try
        {
            var enrollments = await _enrollmentRepository.GetAllAsync();
            return Ok(enrollments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollments");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get enrollment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Enrollment>> GetById(long id)
    {
        try
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(id);
            if (enrollment == null)
                return NotFound($"Enrollment with ID {id} not found");

            return Ok(enrollment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollment {EnrollmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get enrollments by student
    /// </summary>
    [HttpGet("by-student/{studentId}")]
    public async Task<ActionResult<IEnumerable<Enrollment>>> GetByStudent(long studentId)
    {
        try
        {
            var enrollments = await _enrollmentRepository.FindByStudentIdAsync(studentId);
            return Ok(enrollments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollments for student {StudentId}", studentId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get enrollments by course
    /// </summary>
    [HttpGet("by-course/{courseId}")]
    public async Task<ActionResult<IEnumerable<Enrollment>>> GetByCourse(long courseId)
    {
        try
        {
            var enrollments = await _enrollmentRepository.FindByCourseIdAsync(courseId);
            return Ok(enrollments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollments for course {CourseId}", courseId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new enrollment
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Enrollment>> Create([FromBody] Enrollment enrollment)
    {
        try
        {
            await _enrollmentRepository.AddAsync(enrollment);
            return CreatedAtAction(nameof(GetById), new { id = enrollment.Id }, enrollment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating enrollment");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an enrollment
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(long id, [FromBody] Enrollment enrollment)
    {
        try
        {
            if (id != enrollment.Id)
                return BadRequest("ID mismatch");

            var exists = await _enrollmentRepository.GetByIdAsync(id);
            if (exists == null)
                return NotFound($"Enrollment with ID {id} not found");

            await _enrollmentRepository.UpdateAsync(enrollment);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating enrollment {EnrollmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete an enrollment
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(long id)
    {
        try
        {
            var enrollment = await _enrollmentRepository.GetByIdAsync(id);
            if (enrollment == null)
                return NotFound($"Enrollment with ID {id} not found");

            await _enrollmentRepository.DeleteAsync(enrollment);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting enrollment {EnrollmentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
