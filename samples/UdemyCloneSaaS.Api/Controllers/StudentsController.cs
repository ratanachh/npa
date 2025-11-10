using Microsoft.AspNetCore.Mvc;
using UdemyCloneSaaS.Entities;
using UdemyCloneSaaS.Repositories;

namespace UdemyCloneSaaS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentRepository _studentRepository;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(
        IStudentRepository studentRepository,
        ILogger<StudentsController> logger)
    {
        _studentRepository = studentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all students
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetAll()
    {
        try
        {
            var students = await _studentRepository.GetAllAsync();
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving students");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get student by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetById(long id)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
                return NotFound($"Student with ID {id} not found");

            return Ok(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student {StudentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get student by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<Student>> GetByEmail(string email)
    {
        try
        {
            var student = await _studentRepository.FindByEmailAsync(email);
            if (student == null)
                return NotFound($"Student with email {email} not found");

            return Ok(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student by email {Email}", email);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new student
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Student>> Create([FromBody] Student student)
    {
        try
        {
            await _studentRepository.AddAsync(student);
            return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update a student
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(long id, [FromBody] Student student)
    {
        try
        {
            if (id != student.Id)
                return BadRequest("ID mismatch");

            var exists = await _studentRepository.GetByIdAsync(id);
            if (exists == null)
                return NotFound($"Student with ID {id} not found");

            await _studentRepository.UpdateAsync(student);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student {StudentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a student
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(long id)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
                return NotFound($"Student with ID {id} not found");

            await _studentRepository.DeleteAsync(student);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student {StudentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
