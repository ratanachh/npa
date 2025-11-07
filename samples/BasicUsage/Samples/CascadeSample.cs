using NPA.Core.Annotations;
using NPA.Core.Core;
using NPA.Core.Metadata;

namespace NPA.Samples.Samples;

/// <summary>
/// Demonstrates cascade operations for managing related entities.
/// 
/// Cascade operations automatically propagate persist, merge, and remove operations
/// to related entities based on the CascadeType annotation.
/// 
/// This sample demonstrates:
/// 1. CascadeType.Persist - Auto-persisting related entities when parent is persisted
/// 2. CascadeType.Merge - Auto-updating related entities when parent is merged
/// 3. CascadeType.Remove - Auto-deleting related entities when parent is removed
/// 4. OrphanRemoval - Auto-deleting orphaned child entities
/// 5. CascadeType.All - All cascade operations enabled
/// </summary>
public class CascadeSample
{
    private readonly IEntityManager _entityManager;

    public CascadeSample(IEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    /// <summary>
    /// Demo 1: Cascade Persist
    /// 
    /// Demonstrates automatic persistence of related entities.
    /// When a Department is persisted with CascadeType.Persist, all related Employees
    /// are automatically persisted without explicit PersistAsync calls.
    /// </summary>
    public async Task Demo1_CascadePersist()
    {
        Console.WriteLine("\n=== Demo 1: Cascade Persist ===");
        Console.WriteLine("Demonstrates automatic persistence of related entities");

        // Create department with employees
        var department = new Department
        {
            Name = "Engineering",
            Employees = new List<Employee>
            {
                new Employee { Name = "Alice", Position = "Senior Developer", Salary = 95000 },
                new Employee { Name = "Bob", Position = "Developer", Salary = 75000 },
                new Employee { Name = "Charlie", Position = "Junior Developer", Salary = 55000 }
            }
        };

        Console.WriteLine($"\nPersisting department '{department.Name}' with {department.Employees.Count} employees...");
        
        // Persist department - employees are automatically persisted due to CascadeType.Persist
        await _entityManager.PersistAsync(department);
        await _entityManager.FlushAsync();

        Console.WriteLine($"✓ Department saved with ID: {department.Id}");
        foreach (var emp in department.Employees)
        {
            Console.WriteLine($"  ✓ Employee '{emp.Name}' saved with ID: {emp.Id} (auto-persisted)");
        }
    }

    /// <summary>
    /// Demo 2: Cascade Merge (Update)
    /// 
    /// Demonstrates automatic updating of related entities.
    /// When a Department is merged with CascadeType.Merge, all modified Employees
    /// are automatically updated without explicit MergeAsync calls.
    /// </summary>
    public async Task Demo2_CascadeMerge()
    {
        Console.WriteLine("\n=== Demo 2: Cascade Merge ===");
        Console.WriteLine("Demonstrates automatic updating of related entities");

        // Create and persist department with employees
        var department = new Department
        {
            Name = "Marketing",
            Employees = new List<Employee>
            {
                new Employee { Name = "David", Position = "Marketing Manager", Salary = 85000 },
                new Employee { Name = "Eva", Position = "Marketing Specialist", Salary = 65000 }
            }
        };

        await _entityManager.PersistAsync(department);
        await _entityManager.FlushAsync();
        Console.WriteLine($"\n✓ Initial department saved with ID: {department.Id}");

        // Modify department and employees
        department.Name = "Digital Marketing";
        department.Employees[0].Salary = 90000; // Give David a raise
        department.Employees[1].Position = "Senior Marketing Specialist"; // Promote Eva

        Console.WriteLine("\nUpdating department and employee details...");
        
        // Merge department - employees are automatically updated due to CascadeType.Merge
        await _entityManager.MergeAsync(department);
        await _entityManager.FlushAsync();

        Console.WriteLine($"✓ Department updated: '{department.Name}'");
        Console.WriteLine($"  ✓ {department.Employees[0].Name}'s salary: ${department.Employees[0].Salary} (auto-updated)");
        Console.WriteLine($"  ✓ {department.Employees[1].Name}'s position: {department.Employees[1].Position} (auto-updated)");
    }

    /// <summary>
    /// Demo 3: Cascade Remove
    /// 
    /// Demonstrates automatic deletion of related entities.
    /// When a Department is removed with CascadeType.Remove, all related Employees
    /// are automatically deleted without explicit RemoveAsync calls.
    /// </summary>
    public async Task Demo3_CascadeRemove()
    {
        Console.WriteLine("\n=== Demo 3: Cascade Remove ===");
        Console.WriteLine("Demonstrates automatic deletion of related entities");

        // Create and persist department with employees
        var department = new Department
        {
            Name = "Research",
            Employees = new List<Employee>
            {
                new Employee { Name = "Frank", Position = "Researcher", Salary = 80000 },
                new Employee { Name = "Grace", Position = "Lab Technician", Salary = 60000 }
            }
        };

        await _entityManager.PersistAsync(department);
        await _entityManager.FlushAsync();
        
        var deptId = department.Id;
        var employeeIds = department.Employees.Select(e => e.Id).ToList();
        
        Console.WriteLine($"\n✓ Department saved with ID: {deptId}");
        Console.WriteLine($"  Employee IDs: {string.Join(", ", employeeIds)}");

        Console.WriteLine("\nRemoving department (employees will be cascade deleted)...");
        
        // Remove department - employees are automatically removed due to CascadeType.Remove
        await _entityManager.RemoveAsync(department);
        await _entityManager.FlushAsync();

        Console.WriteLine($"✓ Department {deptId} removed");
        Console.WriteLine($"  ✓ All employees automatically deleted via cascade remove");

        // Verify employees were deleted
        foreach (var empId in employeeIds)
        {
            var emp = await _entityManager.FindAsync<Employee>(empId);
            Console.WriteLine($"  ✓ Employee {empId}: {(emp == null ? "Deleted ✓" : "Still exists ✗")}");
        }
    }

    /// <summary>
    /// Demo 4: Orphan Removal
    /// 
    /// Demonstrates automatic deletion of orphaned child entities.
    /// With OrphanRemoval=true, when an Employee is removed from a Company's
    /// employee list, it's automatically deleted from the database.
    /// </summary>
    public async Task Demo4_OrphanRemoval()
    {
        Console.WriteLine("\n=== Demo 4: Orphan Removal ===");
        Console.WriteLine("Demonstrates automatic deletion of orphaned entities");

        // Create company with employees
        var company = new Company
        {
            Name = "TechCorp",
            Employees = new List<Employee>
            {
                new Employee { Name = "Henry", Position = "CEO", Salary = 150000 },
                new Employee { Name = "Iris", Position = "CTO", Salary = 130000 },
                new Employee { Name = "Jack", Position = "CFO", Salary = 120000 }
            }
        };

        await _entityManager.PersistAsync(company);
        await _entityManager.FlushAsync();
        
        Console.WriteLine($"\n✓ Company saved with ID: {company.Id}");
        Console.WriteLine($"  Initial employees: {string.Join(", ", company.Employees.Select(e => e.Name))}");

        // Remove employee from collection (making it an orphan)
        var orphanedEmployee = company.Employees[2]; // Jack
        var orphanId = orphanedEmployee.Id;
        company.Employees.RemoveAt(2);

        Console.WriteLine($"\nRemoving '{orphanedEmployee.Name}' from company (orphan removal enabled)...");
        
        // Merge company - orphaned employee is automatically removed due to OrphanRemoval
        await _entityManager.MergeAsync(company);
        await _entityManager.FlushAsync();

        Console.WriteLine($"✓ Company updated");
        Console.WriteLine($"  Current employees: {string.Join(", ", company.Employees.Select(e => e.Name))}");

        // Verify orphaned employee was deleted
        var deletedEmp = await _entityManager.FindAsync<Employee>(orphanId);
        Console.WriteLine($"  ✓ Orphaned employee '{orphanedEmployee.Name}': {(deletedEmp == null ? "Auto-deleted ✓" : "Still exists ✗")}");
    }

    /// <summary>
    /// Demo 5: Cascade All Operations
    /// 
    /// Demonstrates using CascadeType.All for complete lifecycle management.
    /// All operations (persist, merge, remove) cascade to related entities.
    /// </summary>
    public async Task Demo5_CascadeAll()
    {
        Console.WriteLine("\n=== Demo 5: Cascade All Operations ===");
        Console.WriteLine("Demonstrates CascadeType.All for complete lifecycle management");

        // Create project with tasks
        var project = new Project
        {
            Name = "NPA ORM",
            Description = "Next-generation ORM for .NET",
            Tasks = new List<ProjectTask>
            {
                new ProjectTask { Title = "Design entity framework", Status = "Completed" },
                new ProjectTask { Title = "Implement cascade operations", Status = "In Progress" },
                new ProjectTask { Title = "Write documentation", Status = "Pending" }
            }
        };

        Console.WriteLine($"\n1. Persisting project '{project.Name}' with {project.Tasks.Count} tasks...");
        await _entityManager.PersistAsync(project);
        await _entityManager.FlushAsync();
        Console.WriteLine($"   ✓ Project and all tasks persisted (cascade persist)");

        // Update project and tasks
        project.Description = "Advanced ORM framework for .NET 8+";
        project.Tasks[1].Status = "Completed"; // Mark cascade task as done
        project.Tasks.Add(new ProjectTask { Title = "Performance optimization", Status = "Pending" });

        Console.WriteLine($"\n2. Updating project and tasks...");
        await _entityManager.MergeAsync(project);
        await _entityManager.FlushAsync();
        Console.WriteLine($"   ✓ Project and all tasks updated (cascade merge)");
        Console.WriteLine($"   ✓ New task added via cascade persist");

        var projectId = project.Id;
        var taskIds = project.Tasks.Select(t => t.Id).ToList();
        
        Console.WriteLine($"\n3. Removing project (all tasks will be cascade deleted)...");
        await _entityManager.RemoveAsync(project);
        await _entityManager.FlushAsync();
        Console.WriteLine($"   ✓ Project {projectId} and all tasks removed (cascade remove)");

        // Verify all tasks were deleted
        foreach (var taskId in taskIds)
        {
            var task = await _entityManager.FindAsync<ProjectTask>(taskId);
            Console.WriteLine($"   ✓ Task {taskId}: {(task == null ? "Deleted" : "Still exists")}");
        }
    }

    /// <summary>
    /// Demo 6: No Cascade (Manual Management)
    /// 
    /// Demonstrates behavior when cascade is disabled (CascadeType.None).
    /// Related entities must be manually persisted, merged, or removed.
    /// </summary>
    public async Task Demo6_NoCascade()
    {
        Console.WriteLine("\n=== Demo 6: No Cascade (Manual Management) ===");
        Console.WriteLine("Demonstrates manual entity management when cascade is disabled");

        // Create team with members (no cascade configured)
        var team = new Team
        {
            Name = "Alpha Team"
        };

        var member1 = new TeamMember { Name = "Kevin", Role = "Lead" };
        var member2 = new TeamMember { Name = "Laura", Role = "Developer" };

        Console.WriteLine($"\n1. Persisting team '{team.Name}' (no cascade)...");
        await _entityManager.PersistAsync(team);
        await _entityManager.FlushAsync();
        Console.WriteLine($"   ✓ Team saved with ID: {team.Id}");

        Console.WriteLine($"\n2. Manually persisting team members (cascade disabled)...");
        member1.TeamId = team.Id;
        member2.TeamId = team.Id;
        
        await _entityManager.PersistAsync(member1);
        await _entityManager.PersistAsync(member2);
        await _entityManager.FlushAsync();
        
        Console.WriteLine($"   ✓ Member '{member1.Name}' saved with ID: {member1.Id}");
        Console.WriteLine($"   ✓ Member '{member2.Name}' saved with ID: {member2.Id}");
        Console.WriteLine($"\n   Note: Manual persistence required because CascadeType.None");
    }
}

// ============================================================================
// Entity Definitions with Cascade Annotations
// ============================================================================

/// <summary>
/// Department entity with CascadeType.All on Employees relationship.
/// All operations cascade to employees.
/// </summary>
[Entity]
[Table("cascade_departments")]
public class Department
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    // Cascade.All: Persist, Merge, and Remove operations cascade to employees
    [OneToMany(MappedBy = "DepartmentId", Cascade = CascadeType.All)]
    public List<Employee> Employees { get; set; } = new();
}

/// <summary>
/// Employee entity (child of Department).
/// </summary>
[Entity]
[Table("cascade_employees")]
public class Employee
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("position")]
    public string Position { get; set; } = string.Empty;

    [Column("salary")]
    public decimal Salary { get; set; }

    [Column("department_id")]
    public long? DepartmentId { get; set; }

    [Column("company_id")]
    public long? CompanyId { get; set; }
}

/// <summary>
/// Company entity with OrphanRemoval enabled.
/// Employees removed from the collection are automatically deleted.
/// </summary>
[Entity]
[Table("cascade_companies")]
public class Company
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    // OrphanRemoval: Employees removed from collection are automatically deleted
    [OneToMany(MappedBy = "CompanyId", Cascade = CascadeType.Persist | CascadeType.Merge, OrphanRemoval = true)]
    public List<Employee> Employees { get; set; } = new();
}

/// <summary>
/// Project entity with CascadeType.All on Tasks relationship.
/// </summary>
[Entity]
[Table("cascade_projects")]
public class Project
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [OneToMany(MappedBy = "ProjectId", Cascade = CascadeType.All)]
    public List<ProjectTask> Tasks { get; set; } = new();
}

/// <summary>
/// ProjectTask entity (child of Project).
/// </summary>
[Entity]
[Table("cascade_tasks")]
public class ProjectTask
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("project_id")]
    public long? ProjectId { get; set; }
}

/// <summary>
/// Team entity with CascadeType.None (no cascade).
/// Related entities must be managed manually.
/// </summary>
[Entity]
[Table("cascade_teams")]
public class Team
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    // No cascade - members must be manually persisted/removed
    [OneToMany(MappedBy = "TeamId", Cascade = CascadeType.None)]
    public List<TeamMember> Members { get; set; } = new();
}

/// <summary>
/// TeamMember entity (child of Team with no cascade).
/// </summary>
[Entity]
[Table("cascade_team_members")]
public class TeamMember
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("role")]
    public string Role { get; set; } = string.Empty;

    [Column("team_id")]
    public long? TeamId { get; set; }
}
