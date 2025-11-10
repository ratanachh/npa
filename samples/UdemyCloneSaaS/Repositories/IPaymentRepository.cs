using NPA.Core.Annotations;
using NPA.Core.Repositories;
using UdemyCloneSaaS.Entities;

namespace UdemyCloneSaaS.Repositories;

/// <summary>
/// Repository for managing payment transactions.
/// </summary>
[Repository]
public interface IPaymentRepository : IRepository<Payment, long>
{
    [Query("SELECT p FROM Payment p WHERE p.StudentId = :studentId")]
    Task<IEnumerable<Payment>> FindByStudentIdAsync(long studentId);

    [Query("SELECT p FROM Payment p WHERE p.CourseId = :courseId")]
    Task<IEnumerable<Payment>> FindByCourseIdAsync(long courseId);

    [Query("SELECT p FROM Payment p WHERE p.Status = 'Completed' AND p.CompletedAt >= :startDate")]
    Task<IEnumerable<Payment>> GetCompletedPaymentsSinceAsync(DateTime startDate);

    [Query("SELECT SUM(p.Amount) FROM Payment p WHERE p.Status = 'Completed' AND p.CompletedAt >= :startDate")]
    Task<decimal> GetTotalRevenueSinceAsync(DateTime startDate);

    [Query("SELECT p FROM Payment p WHERE p.TransactionId = :transactionId")]
    Task<Payment?> FindByTransactionIdAsync(string transactionId);
}
