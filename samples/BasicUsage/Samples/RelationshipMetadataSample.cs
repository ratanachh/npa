using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Annotations;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Core.Metadata;
using NPA.Providers.Sqlite.Extensions;
using NPA.Samples.Core;

namespace NPA.Samples.Features;

/// <summary>
/// Demonstrates relationship metadata generation with JoinColumn, JoinTable, and MappedBy attributes.
/// Shows how the EntityMetadataGenerator extracts and generates complete relationship metadata.
/// </summary>
public class RelationshipMetadataSample : ISample
{
    public string Name => "Relationship Metadata Generation";
    public string Description => "Demonstrates JoinColumn (with all attributes), JoinTable for ManyToMany, and MappedBy for bidirectional relationships.";

    public async Task RunAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSqliteProvider("Data Source=:memory:");

        await using var serviceProvider = services.BuildServiceProvider();

        var metadataProvider = serviceProvider.GetRequiredService<IMetadataProvider>();

        Console.WriteLine("\n=== Relationship Metadata Sample ===\n");

        // Example 1: ManyToOne with full JoinColumn attributes
        Console.WriteLine("1. ManyToOne Relationship (BlogPost -> Author)");
        Console.WriteLine("   Demonstrates JoinColumn with Unique, Nullable, Insertable, Updatable");
        DemonstrateJoinColumn(metadataProvider);

        // Example 2: OneToMany with MappedBy
        Console.WriteLine("\n2. OneToMany Relationship (Author -> BlogPosts)");
        Console.WriteLine("   Demonstrates MappedBy for bidirectional relationship");
        DemonstrateMappedBy(metadataProvider);

        // Example 3: ManyToMany with JoinTable
        Console.WriteLine("\n3. ManyToMany Relationship (Student <-> Course)");
        Console.WriteLine("   Demonstrates JoinTable with JoinColumns and InverseJoinColumns");
        DemonstrateJoinTable(metadataProvider);

        // Example 4: OneToOne relationship
        Console.WriteLine("\n4. OneToOne Relationship (Author <-> AuthorProfile)");
        Console.WriteLine("   Demonstrates OneToOne owner and inverse sides");
        DemonstrateOneToOne(metadataProvider);

        Console.WriteLine("\n=== All relationship metadata generated successfully! ===\n");
    }

    private void DemonstrateJoinColumn(IMetadataProvider metadataProvider)
    {
        var blogMetadata = metadataProvider.GetEntityMetadata<BlogPost>();
        var authorRelationship = blogMetadata.Relationships["Author"];

        Console.WriteLine($"   Relationship Type: {authorRelationship.RelationshipType}");
        Console.WriteLine($"   Is Owner: {authorRelationship.IsOwner}");
        
        if (authorRelationship.JoinColumn != null)
        {
            Console.WriteLine("   JoinColumn:");
            Console.WriteLine($"     Name: {authorRelationship.JoinColumn.Name}");
            Console.WriteLine($"     ReferencedColumnName: {authorRelationship.JoinColumn.ReferencedColumnName}");
            Console.WriteLine($"     Unique: {authorRelationship.JoinColumn.Unique}");
            Console.WriteLine($"     Nullable: {authorRelationship.JoinColumn.Nullable}");
            Console.WriteLine($"     Insertable: {authorRelationship.JoinColumn.Insertable}");
            Console.WriteLine($"     Updatable: {authorRelationship.JoinColumn.Updatable}");
        }
    }

    private void DemonstrateMappedBy(IMetadataProvider metadataProvider)
    {
        var authorMetadata = metadataProvider.GetEntityMetadata<Author>();
        var blogsRelationship = authorMetadata.Relationships["BlogPosts"];

        Console.WriteLine($"   Relationship Type: {blogsRelationship.RelationshipType}");
        Console.WriteLine($"   MappedBy: {blogsRelationship.MappedBy}");
        Console.WriteLine($"   Is Owner: {blogsRelationship.IsOwner}");
        Console.WriteLine($"   Target Entity: {blogsRelationship.TargetEntityType.Name}");
    }

    private void DemonstrateJoinTable(IMetadataProvider metadataProvider)
    {
        var studentMetadata = metadataProvider.GetEntityMetadata<Student>();
        var coursesRelationship = studentMetadata.Relationships["Courses"];

        Console.WriteLine($"   Relationship Type: {coursesRelationship.RelationshipType}");
        Console.WriteLine($"   Is Owner: {coursesRelationship.IsOwner}");
        
        if (coursesRelationship.JoinTable != null)
        {
            Console.WriteLine("   JoinTable:");
            Console.WriteLine($"     Name: {coursesRelationship.JoinTable.Name}");
            Console.WriteLine($"     Schema: {coursesRelationship.JoinTable.Schema}");
            Console.WriteLine($"     JoinColumns: [{string.Join(", ", coursesRelationship.JoinTable.JoinColumns)}]");
            Console.WriteLine($"     InverseJoinColumns: [{string.Join(", ", coursesRelationship.JoinTable.InverseJoinColumns)}]");
            Console.WriteLine($"     FullName: {coursesRelationship.JoinTable.FullName}");
        }
    }

    private void DemonstrateOneToOne(IMetadataProvider metadataProvider)
    {
        var authorMetadata = metadataProvider.GetEntityMetadata<Author>();
        var profileRelationship = authorMetadata.Relationships["AuthorProfile"];

        Console.WriteLine($"   Relationship Type: {profileRelationship.RelationshipType}");
        Console.WriteLine($"   MappedBy: {profileRelationship.MappedBy ?? "null (owner side)"}");
        Console.WriteLine($"   Is Owner: {profileRelationship.IsOwner}");
        
        if (profileRelationship.JoinColumn != null)
        {
            Console.WriteLine("   JoinColumn:");
            Console.WriteLine($"     Name: {profileRelationship.JoinColumn.Name}");
            Console.WriteLine($"     Nullable: {profileRelationship.JoinColumn.Nullable}");
        }
    }

    // Sample entities demonstrating various relationship types and metadata

    [Entity]
    [Table("authors")]
    public class Author
    {
        [Id]
        [Column("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        // OneToMany - inverse side with MappedBy
        [OneToMany(MappedBy = "Author")]
        public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();

        // OneToOne - owner side with JoinColumn
        [OneToOne]
        [JoinColumn("profile_id", Nullable = true)]
        public AuthorProfile? AuthorProfile { get; set; }
    }

    [Entity]
    [Table("blog_posts")]
    public class BlogPost
    {
        [Id]
        [Column("id")]
        public long Id { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("content")]
        public string Content { get; set; } = string.Empty;

        // ManyToOne - owner side with comprehensive JoinColumn attributes
        [ManyToOne]
        [JoinColumn("author_id", 
            ReferencedColumnName = "id", 
            Unique = false, 
            Nullable = false, 
            Insertable = true, 
            Updatable = true)]
        public Author Author { get; set; } = null!;
    }

    [Entity]
    [Table("author_profiles")]
    public class AuthorProfile
    {
        [Id]
        [Column("id")]
        public long Id { get; set; }

        [Column("bio")]
        public string Bio { get; set; } = string.Empty;

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        // OneToOne - inverse side with MappedBy
        [OneToOne(MappedBy = "AuthorProfile")]
        public Author? Author { get; set; }
    }

    [Entity]
    [Table("students")]
    public class Student
    {
        [Id]
        [Column("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        // ManyToMany - owner side with JoinTable
        [ManyToMany]
        [JoinTable("student_courses", 
            Schema = "public",
            JoinColumns = new[] { "student_id" }, 
            InverseJoinColumns = new[] { "course_id" })]
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }

    [Entity]
    [Table("courses")]
    public class Course
    {
        [Id]
        [Column("id")]
        public long Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("credits")]
        public int Credits { get; set; }

        // ManyToMany - inverse side with MappedBy
        [ManyToMany(MappedBy = "Courses")]
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
