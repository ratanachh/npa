# Sample Projects Task Documentation

This directory contains detailed task documentation for creating sample applications that demonstrate NPA features across all phases of development.

## 📋 Overview

Each sample project is designed to showcase specific features and best practices for using NPA in real-world scenarios. The samples are organized by phase and complexity level.

> **📊 Existing Samples Status**: The repository currently contains 5 sample projects in `samples/`. See [EXISTING-SAMPLES-STATUS.md](./EXISTING-SAMPLES-STATUS.md) for detailed status of each existing sample. Only **BasicUsage** is functional (with PostgreSQL provider), while others are placeholders for future phases.

## 🎯 Sample Project Goals

- **Educational**: Teach developers how to use NPA's JPA-like API effectively
- **Practical**: Show real-world Dapper-based ORM patterns
- **Progressive**: Build from simple to complex examples
- **Performance-Focused**: Demonstrate lightweight, high-performance patterns
- **Maintainable**: Serve as reference implementations for production use

## 📚 Sample Projects by Phase

### Phase 1: Core Foundation Samples (Currently Available)
- [Phase 1.1 - Basic Entity Mapping](./phase1.1-basic-entity-mapping-sample.md) ✅
- [Phase 1.2 - CRUD Operations Sample](./phase1.2-crud-operations-sample.md) ✅
- [Phase 1.3 - CPQL Query API Sample](./phase1.3-cpql-query-sample.md) ✅
- Phase 1.4 - SQL Server Provider Sample 🚧
- Phase 1.5 - MySQL/MariaDB Provider Sample 🚧
- Phase 1.6 - PostgreSQL Advanced Features 🚧

### Phase 2: Advanced Features Samples (Planned)
- [Phase 2.1 - Relationship Mapping Sample](./phase2.1-relationship-mapping-sample.md) 📋
- Phase 2.2 - Composite Keys Sample 📋
- Phase 2.3 - Enhanced CPQL Query Language Sample 📋
- Phase 2.4 - Repository Pattern Sample 📋
- Phase 2.5 - Multi-Provider Sample 📋

### Phase 3: Transaction & Performance Samples (Planned)
- [Phase 3.1 - Transaction Management Sample](./phase3.1-transaction-management-sample.md) 📋
- Phase 3.2 - Cascade Operations Sample 📋
- [Phase 3.3 - Bulk Operations Sample](./phase3.3-bulk-operations-sample.md) 📋
- Phase 3.4 - Lazy Loading Sample 📋
- Phase 3.5 - Connection Pooling & Performance 📋

### Phase 4: Source Generator Samples (Planned)
- [Phase 4.1 - Repository Generation Basics](./phase4.1-repository-generation-sample.md) 📋
- Phase 4.2 - Query Method Generation Sample 📋
- Phase 4.3 - Advanced Generator Patterns 📋

### Phase 5: Enterprise Features Samples (Planned)
- [Phase 5.1 - Caching Sample](./phase5.1-caching-sample.md) 📋
- Phase 5.2 - Migration Sample 📋
- Phase 5.3 - Performance Monitoring Sample 📋
- Phase 5.4 - Audit Logging Sample 📋
- Phase 5.5 - Multi-Tenant Sample 📋

### Phase 6: Tooling & Integration Samples (Planned)
- [Phase 6.1 - ASP.NET Core Integration](./phase6.1-aspnet-core-integration-sample.md) 📋
- Phase 6.2 - Microservices Sample 📋
- [Phase 6.3 - Real-World Application](./phase6.3-real-world-application-sample.md) 📋

## 🏗️ Sample Project Structure

Each sample project follows a consistent structure:

```
samples/
└── [SampleName]/
    ├── [SampleName].csproj
    ├── Program.cs
    ├── README.md
    ├── Entities/
    │   └── *.cs (Entity classes)
    ├── Repositories/ (if applicable)
    │   └── *.cs (Repository classes)
    ├── Services/ (if applicable)
    │   └── *.cs (Business logic)
    └── appsettings.json (if needed)
```

## 📋 Task Document Format

Each task document includes:

1. **Task Overview** - Objective, priority, estimated time
2. **Success Criteria** - What defines completion
3. **Detailed Requirements** - Specific features to implement
4. **Implementation Plan** - Step-by-step guide
5. **Code Examples** - Sample code snippets
6. **Test Cases** - What to test
7. **Documentation Requirements** - README and comments
8. **Dependencies** - Required packages and projects

## 🎓 Learning Path

### Beginner Path
1. Phase 1.1 - Basic Entity Mapping
2. Phase 1.2 - CRUD Operations
3. Phase 1.3 - Query API

### Intermediate Path
1. Phase 2.1 - Relationship Mapping
2. Phase 2.4 - Repository Pattern
3. Phase 3.1 - Transaction Management

### Advanced Path
1. Phase 4.1 - Advanced Repository Generation
2. Phase 5.1 - Caching
3. Phase 6.1 - ASP.NET Core Integration

## 🚀 Getting Started

To create a new sample project:

1. Choose the appropriate phase and feature
2. Read the corresponding task document
3. Follow the implementation plan
4. Test thoroughly
5. Document usage in README
6. Update this index

## 📞 Questions/Issues

If you encounter issues while creating samples:
- Review the phase documentation
- Check existing samples for patterns
- Consult the main checklist
- Ask for clarification

---

*Created: October 8, 2025*  
*Last Updated: October 8, 2025*  
*Maintainer: NPA Development Team*
