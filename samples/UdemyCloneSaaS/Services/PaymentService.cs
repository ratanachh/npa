using UdemyCloneSaaS.Entities;
using UdemyCloneSaaS.Repositories;

namespace UdemyCloneSaaS.Services;

/// <summary>
/// Business logic service for processing payments.
/// </summary>
public class PaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IStudentRepository _studentRepository;

    public PaymentService(
        IPaymentRepository paymentRepository,
        ICourseRepository courseRepository,
        IStudentRepository studentRepository)
    {
        _paymentRepository = paymentRepository;
        _courseRepository = courseRepository;
        _studentRepository = studentRepository;
    }

    public async Task<Payment> CreatePaymentAsync(long studentId, long courseId, string paymentMethod)
    {
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null)
        {
            throw new ArgumentException("Course not found");
        }

        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            throw new ArgumentException("Student not found");
        }

        var amount = course.DiscountPrice ?? course.Price;

        var payment = new Payment
        {
            StudentId = studentId,
            CourseId = courseId,
            TenantId = course.TenantId,
            Amount = amount,
            Currency = "USD",
            PaymentMethod = paymentMethod,
            Status = "Pending",
            TransactionId = GenerateTransactionId(),
            CreatedAt = DateTime.UtcNow
        };

        await _paymentRepository.AddAsync(payment);
        return payment;
    }

    public async Task<Payment?> CompletePaymentAsync(long paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null) return null;

        payment.Status = "Completed";
        payment.CompletedAt = DateTime.UtcNow;

        await _paymentRepository.UpdateAsync(payment);
        return payment;
    }

    public async Task<Payment?> RefundPaymentAsync(long paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null) return null;

        if (payment.Status != "Completed")
        {
            throw new InvalidOperationException("Can only refund completed payments");
        }

        payment.Status = "Refunded";
        await _paymentRepository.UpdateAsync(payment);
        return payment;
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime since)
    {
        return await _paymentRepository.GetTotalRevenueSinceAsync(since);
    }

    public async Task<IEnumerable<Payment>> GetStudentPaymentsAsync(long studentId)
    {
        return await _paymentRepository.FindByStudentIdAsync(studentId);
    }

    private string GenerateTransactionId()
    {
        return $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
    }
}
