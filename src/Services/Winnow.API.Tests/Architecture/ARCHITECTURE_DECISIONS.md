# Winnow Server Architecture Decisions

## Overview
This document outlines the architectural decisions and rules for the Winnow Server project, based on the analysis of the current codebase and architectural tests.

## Current Architecture Patterns

### 1. Vertical Slice Architecture
- **Pattern**: Features organized by business capability (Reports, Auth, Dashboard, etc.)
- **Rule**: Features should not depend on each other (except for Shared utilities)
- **Exception**: Debug features may depend on Reports for simulation purposes
- **Decision**: Allow Debug → Reports dependency as Debug is a testing/simulation feature

### 2. Clean/Hexagonal Architecture Layers
- **Entities**: Domain models with no dependencies on Infrastructure or ASP.NET Core
- **Infrastructure**: Implementation details (Persistence, MultiTenancy, Scheduling)
- **Features**: Application layer with business logic
- **Services**: Domain services and infrastructure services

### 3. Dependency Rules
- **Entities → Infrastructure**: ❌ Forbidden
- **Entities → Features**: ❌ Forbidden  
- **Entities → Services**: ✅ Allowed (Domain.Services)
- **Features → Infrastructure**: ✅ Limited (Persistence, MultiTenancy, Scheduling, Integrations)
- **Features → Services**: ✅ Allowed (via interfaces)
- **Services → Infrastructure**: ✅ Limited (Domain.Services should not depend on Infrastructure)
- **Features → Concrete Strategies**: ❌ Forbidden (Must use IExporterCreationStrategy/Factory)
- **Features → Concrete Configs**: ✅ Allowed (as data), but forbidden to instantiate logic classes directly

### 4. FastEndpoints Conventions
- **Endpoints**: Must be public and sealed
- **Requests**: Should exist in same namespace as Endpoint
- **Validators**: Should exist in same namespace as Request (when validation needed)
- **Exception**: Simple endpoints may skip Validator for trivial validation

## Architectural Test Refinements

### Vertical Slices Test
- **Original Rule**: No slices should depend on each other
- **Refined Rule**: No slices should depend on each other, except:
  - Debug can depend on Reports (for simulation)
  - Shared can be used by all slices

### Cross-Cutting Concerns Test
- **Original Rule**: No validation logic in Features
- **Refined Rule**: Simple validation in HandleAsync is acceptable
- **Best Practice**: Complex validation should use Validator classes

### Feature Endpoints Test
- **Original Rule**: All endpoints must have Request + Validator
- **Refined Rule**: Endpoints should have Request; Validators are recommended but optional for simple cases
- **Observation**: Many endpoints have Request but no Validator

### Infrastructure Usage Test
- **Original Rule**: Features should not depend on Infrastructure
- **Refined Rule**: Features can depend on specific Infrastructure namespaces:
  - Persistence (DbContext)
  - MultiTenancy (TenantContext)
  - Scheduling (Jobs)
  - Integrations (Strategies, Factory)

## Recommendations for Refactoring

### 1. Create Missing Validators
For endpoints with Request but no Validator, consider:
- Add Validator for complex validation
- Keep simple validation in HandleAsync (document decision)

### 2. Review Debug → Reports Dependency
- This is likely intentional for simulation
- Document this exception clearly

### 3. Document Architecture Exceptions
- Create this document in the Architecture folder
- Reference it in architectural tests

### 4. Service Naming Consistency
- Services implementing interfaces should follow `IService` → `Service` pattern
- Providers (ending with Provider) are exempt

### 5. Polymorphic Design Rules
- **Integration Configs**:
  - Must inherit from `IntegrationConfig` abstract base
  - Must be `sealed` records (Immutable)
  - Must reside in `Winnow.Integrations.Domain` namespace
- **Strategies**:
  - Must implement `IExporterCreationStrategy` (or similar strategy interface)
  - Must end with the suffix `Strategy`
  - Logic must be selected via `CanHandle()` pattern, never `switch` statements
- **Factories**:
  - Must implement corresponding factory interfaces (e.g., `IExporterFactory`)
  - Features should depend on factory interfaces, not concrete implementations

## Implementation Plan

### Phase 1: Document Current Architecture (Complete)
- Create this decisions document
- Update architectural tests with realistic rules

### Phase 2: Fix Critical Violations
- Address any architectural violations that break core principles
- Review Entities dependencies on Infrastructure

### Phase 3: Improve Consistency
- Add missing Validators where beneficial
- Standardize service naming

### Phase 4: Continuous Enforcement
- Run architectural tests in CI/CD
- Update rules as architecture evolves

## Exceptions List

### Allowed Exceptions
1. `Debug` slice can depend on `Reports` slice
2. Features can use Infrastructure.Persistence, MultiTenancy, Scheduling, Integrations
3. Simple validation can remain in HandleAsync methods
4. Endpoints without Validators are acceptable for simple cases
5. Providers (ending with Provider) exempt from service naming conventions
6. `ApplicationUser` (Entity) can depend on `Microsoft.AspNetCore.Identity` (Infrastructure)

### Prohibited Dependencies
1. Entities must not depend on Infrastructure or ASP.NET Core
2. Features should not depend on Infrastructure.Configuration
3. Domain.Services should not depend on Infrastructure
4. Slices should not have circular dependencies

## Future Considerations
- Consider extracting shared domain logic from Features.Shared
- Evaluate moving some Infrastructure dependencies to interfaces
- Monitor technical debt from architectural exceptions

## Quality Gates
- Enforce >80% Code Coverage on Domain/Logic
- Enforce Mutation Testing on Core Domain (Stryker)
- Zero Tolerance for Architecture Test failures