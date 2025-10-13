# NPA Framework Samples

This directory contains sample projects demonstrating the features of the NPA (Nasal Persistence Architecture) framework.

## Consolidated Sample Runner

To simplify the experience, all individual console-based samples have been consolidated into a single, menu-driven application located in the `BasicUsage/` directory (soon to be renamed `NPA.Samples`).

This application acts as a host for all feature demonstrations, from basic CRUD to advanced queries and the repository pattern. Each sample is self-contained and uses Testcontainers to spin up a necessary database in Docker, so there are no external dependencies to configure.

### How to Run the Samples

1.  **Navigate to the project directory:**
    ```sh
    cd samples/BasicUsage
    ```

2.  **Run the application:**
    ```sh
    dotnet run
    ```

3.  **Choose a sample:**
    You will be presented with an interactive menu. Simply enter the number of the sample you wish to run, or select 'A' to run all samples sequentially.

    ```
    === NPA Framework Samples ===
    Please choose a sample to run:
      1. Advanced CPQL Queries
         Demonstrates advanced CPQL features like JOINs, GROUP BY, aggregates, and functions.
      2. Basic CRUD Operations
         Demonstrates basic entity mapping, EntityManager CRUD operations, and simple CPQL queries using the PostgreSQL provider.
      3. Repository Pattern
         Demonstrates base repositories, custom repositories, and the repository factory.
      4. Source Generators
         Explains the benefits of the Repository and Metadata source generators.
      5. Synchronous API Usage
         Demonstrates the use of synchronous (blocking) API methods, ideal for console applications.

      A. Run All Samples
      Q. Quit

    Enter your choice:
    ```
