using NPA.Core.Core;

namespace ConsoleAppSync.Features;

/// <summary>
/// Demonstrates synchronous methods in NPA.
/// All operations use blocking synchronous calls - ideal for console apps.
/// </summary>
public static class SyncMethodsDemo
{
    public static void RunCrudOperations(IEntityManager entityManager)
    {
        Console.WriteLine("--- CRUD Operations (Synchronous) ---\n");

        // CREATE
        Console.WriteLine("1. Creating new customer...");
        var customer = new Customer
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Phone = "+1-555-1234",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        entityManager.Persist(customer);
        Console.WriteLine($"   ✓ Created customer ID: {customer.Id}");

        // READ
        Console.WriteLine("\n2. Finding customer...");
        var foundCustomer = entityManager.Find<Customer>(customer.Id);
        if (foundCustomer != null)
        {
            Console.WriteLine($"   ✓ Found: {foundCustomer.Name} ({foundCustomer.Email})");
        }

        // UPDATE
        Console.WriteLine("\n3. Updating customer...");
        if (foundCustomer != null)
        {
            foundCustomer.Email = "john.doe.updated@example.com";
            foundCustomer.Phone = "+1-555-5678";
            entityManager.Merge(foundCustomer);
            Console.WriteLine($"   ✓ Updated email: {foundCustomer.Email}");
        }

        // DELETE
        Console.WriteLine("\n4. Deleting customer...");
        if (foundCustomer != null)
        {
            entityManager.Remove(foundCustomer);
            Console.WriteLine($"   ✓ Deleted customer ID: {customer.Id}");
        }
    }

    public static void RunQueryOperations(IEntityManager entityManager)
    {
        Console.WriteLine("\n--- Query Operations (Synchronous) ---\n");

        // Create test data
        Console.WriteLine("Creating test customers...");
        var customers = new[]
        {
            new Customer { Name = "Alice Smith", Email = "alice@example.com", Phone = "555-0001", CreatedAt = DateTime.UtcNow, IsActive = true },
            new Customer { Name = "Bob Johnson", Email = "bob@example.com", Phone = "555-0002", CreatedAt = DateTime.UtcNow, IsActive = true },
            new Customer { Name = "Carol Williams", Email = "carol@example.com", Phone = "555-0003", CreatedAt = DateTime.UtcNow, IsActive = false },
            new Customer { Name = "David Brown", Email = "david@example.com", Phone = "555-0004", CreatedAt = DateTime.UtcNow, IsActive = true }
        };

        foreach (var c in customers)
        {
            entityManager.Persist(c);
        }
        Console.WriteLine($"   ✓ Created {customers.Length} customers\n");

        // Query 1: Get all active customers
        Console.WriteLine("1. Finding all active customers...");
        var activeCustomers = entityManager
            .CreateQuery<Customer>("SELECT c FROM Customer c WHERE c.IsActive = :active")
            .SetParameter("active", true)
            .GetResultList();
        Console.WriteLine($"   ✓ Found {activeCustomers.Count()} active customers");
        foreach (var c in activeCustomers)
        {
            Console.WriteLine($"      - {c.Name} ({c.Email})");
        }

        // Query 2: Get single customer by email
        Console.WriteLine("\n2. Finding customer by email...");
        var customer = entityManager
            .CreateQuery<Customer>("SELECT c FROM Customer c WHERE c.Email = :email")
            .SetParameter("email", "alice@example.com")
            .GetSingleResult();
        if (customer != null)
        {
            Console.WriteLine($"   ✓ Found: {customer.Name}");
        }

        // Query 3: Count customers
        Console.WriteLine("\n3. Counting total customers...");
        var count = entityManager
            .CreateQuery<Customer>("SELECT COUNT(c) FROM Customer c")
            .ExecuteScalar();
        Console.WriteLine($"   ✓ Total customers: {count}");

        // Query 4: Search customers
        Console.WriteLine("\n4. Searching customers by name pattern...");
        var searchResults = entityManager
            .CreateQuery<Customer>("SELECT c FROM Customer c WHERE c.Name LIKE :pattern")
            .SetParameter("pattern", "%Smith%")
            .GetResultList();
        Console.WriteLine($"   ✓ Found {searchResults.Count()} matching customers");

        // Cleanup - Note: Using proper CPQL DELETE syntax
        Console.WriteLine("\n5. Cleaning up test data...");
        // Delete one by one using entity IDs (type parameter required)
        foreach (var c in customers)
        {
            entityManager.Remove<Customer>(c.Id);
        }
        Console.WriteLine($"   ✓ Deleted {customers.Length} customers");
    }

    public static void RunBatchOperations(IEntityManager entityManager)
    {
        Console.WriteLine("\n--- Batch Operations (Synchronous) ---\n");

        // Create multiple customers
        Console.WriteLine("1. Creating batch of customers...");
        var batchCustomers = new List<Customer>();
        for (int i = 1; i <= 10; i++)
        {
            var customer = new Customer
            {
                Name = $"Batch Customer {i}",
                Email = $"batch{i}@example.com",
                Phone = $"555-{i:D4}",
                CreatedAt = DateTime.UtcNow,
                IsActive = i % 2 == 0
            };
            entityManager.Persist(customer);
            batchCustomers.Add(customer);
        }
        Console.WriteLine($"   ✓ Created {batchCustomers.Count} customers");

        // Update batch
        Console.WriteLine("\n2. Updating batch of customers...");
        var activeCustomers = batchCustomers.Where(c => c.IsActive).ToList();
        foreach (var customer in activeCustomers)
        {
            customer.Email = customer.Email.Replace("@example.com", "@newdomain.com");
            entityManager.Merge(customer);
        }
        Console.WriteLine($"   ✓ Updated {activeCustomers.Count} customers");

        // Delete batch
        Console.WriteLine("\n3. Deleting batch of customers...");
        foreach (var customer in batchCustomers)
        {
            entityManager.Remove(customer);
        }
        Console.WriteLine($"   ✓ Deleted {batchCustomers.Count} customers");
    }
}

