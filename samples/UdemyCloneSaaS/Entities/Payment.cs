using NPA.Core.Annotations;

namespace UdemyCloneSaaS.Entities;

/// <summary>
/// Represents a payment transaction for course enrollment.
/// Multi-tenant: Payments are scoped to tenants.
/// </summary>
[Entity]
[Table("payments")]
[MultiTenant]
public class Payment
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [Column("student_id")]
    public long StudentId { get; set; }

    [Column("course_id")]
    public long CourseId { get; set; }

    [Column("enrollment_id")]
    public long? EnrollmentId { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; } = 0.0m;

    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "CreditCard"; // CreditCard, PayPal, Stripe

    [Column("transaction_id")]
    public string? TransactionId { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Refunded

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    public override string ToString()
    {
        return $"Payment[{Id}] ${Amount} {Currency} - {Status} ({PaymentMethod})";
    }
}
