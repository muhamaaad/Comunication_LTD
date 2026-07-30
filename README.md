# Security Web Application (C# + SQL Express)

> **Academic Project**
>
> This project demonstrates secure web application development practices by implementing two versions of the same application:
>
> - **Version 1 (Vulnerable)** – intentionally contains security vulnerabilities for educational purposes.
> - **Version 2 (Secure)** – implements industry best practices to mitigate those vulnerabilities.

---

# Project Overview

The application focuses on authentication, password management, customer management, and secure software design.

## Features

- User Registration
- Configurable Password Policies
- Login Authentication
- Forgot Password
- Password Reset
- Customer Management Dashboard
- Email Notifications
- SQL Express Database
- Configuration using `appsettings.json`

---

# Security Objectives

The project demonstrates the difference between insecure and secure implementations.

| Vulnerable Version | Secure Version |
|-------------------|----------------|
| SQL Injection | Parameterized Queries |
| Stored XSS | Character Encoding |
| Unsafe Data Handling | Secure Input Validation |

---

# Project Architecture

```
SecurityWebApp/
│
├── src/
│   │
│   ├── Shared/
│   │   ├── Contracts/
│   │   ├── DTOs/
│   │   ├── Models/
│   │   ├── Enums/
│   │   ├── Common/
│   │   └── Validation/
│   │
│   ├── Infrastructure/
│   │   ├── Database/
│   │   ├── Repositories/
│   │   ├── Email/
│   │   ├── Logging/
│   │   ├── Security/
│   │   └── Configuration/
│   │
│   ├── Application/
│   │   ├── Services/
│   │   ├── Interfaces/
│   │   ├── Policies/
│   │   └── Mapping/
│   │
│   ├── Web/
│   │   ├── Controllers/
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Middleware/
│   │   ├── Filters/
│   │   └── wwwroot/
│   │
│   ├── Version1-Vulnerable/
│   │   ├── Controllers/
│   │   ├── Repository/
│   │   ├── Views/
│   │   └── Documentation/
│   │
│   └── Version2-Secure/
│       ├── Controllers/
│       ├── Repository/
│       ├── Views/
│       ├── Security/
│       └── Documentation/
│
├── Database/
│   ├── Schema/
│   ├── Seed/
│   └── Documentation/
│
├── Documentation/
│   ├── Architecture/
│   ├── ThreatModel/
│   ├── SecurityAnalysis/
│   └── TestCases/
│
├── appsettings.json
└── README.md
```

---

# Layered Architecture

```
Presentation Layer
        │
        ▼
Application Services
        │
        ▼
Repository Layer
        │
        ▼
SQL Express Database
```

Cross-cutting concerns:

- Configuration
- Email Service
- Logging
- Password Policy Engine
- Security Utilities

---

# Version Separation

```
                Shared Models
                     │
        ┌────────────┴─────────────┐
        │                          │
 Version 1                    Version 2
 Vulnerable                    Secure
        │                          │
 SQL Injection           Parameterized Queries
 Stored XSS              Output Encoding
```

---

# Database Design

## Users

| Column | Type | Description |
|---------|------|-------------|
| UserId | INT Identity | Primary Key |
| Email | NVARCHAR(255) | Unique Email |
| PasswordHash | VARBINARY(MAX) | HMAC Hash |
| PasswordSalt | VARBINARY(256) | Random Salt |
| FailedAttempts | INT | Login Counter |
| LockedUntil | DATETIME | Lockout Time |
| CreatedDate | DATETIME | Creation Date |
| LastLogin | DATETIME | Last Login |

Indexes

- Primary Key (UserId)
- Unique Index (Email)
- Index (FailedAttempts)

---

## PasswordHistory

| Column | Type |
|---------|------|
| HistoryId | INT |
| UserId | INT |
| PasswordHash | VARBINARY(MAX) |
| ChangedDate | DATETIME |

---

## PasswordResetTokens

| Column | Type |
|---------|------|
| TokenId | INT |
| UserId | INT |
| ResetToken | CHAR(40) |
| Expiration | DATETIME |
| Used | BIT |

---

## Customers

| Column | Type |
|---------|------|
| CustomerId | INT |
| CustomerName | NVARCHAR(150) |
| CreatedBy | INT |
| CreatedDate | DATETIME |

---

# Entity Relationships

```
User
 │
 ├──────< PasswordHistory
 │
 ├──────< PasswordResetTokens
 │
 └──────< Customers
```

---

# Application Components

## Domain Models

- User
- Customer
- PasswordHistory
- PasswordResetToken
- PasswordPolicy

---

## DTOs

- RegisterRequestDto
- LoginRequestDto
- ForgotPasswordRequestDto
- ResetPasswordRequestDto
- CustomerCreateDto
- CustomerResponseDto
- UserDto
- LoginResultDto

---

## Repository Interfaces

- IUserRepository
- ICustomerRepository
- IPasswordRepository
- IPasswordResetRepository

Responsibilities:

- CRUD Operations
- Data Access
- Persistence Abstraction

---

## Service Interfaces

- IAuthenticationService
- IRegistrationService
- IPasswordPolicyService
- IPasswordResetService
- ICustomerService
- IEmailService
- IHashingService
- ITokenService

---

## Validation Components

- PasswordComplexityValidator
- DictionaryValidator
- PasswordHistoryValidator
- EmailValidator
- CustomerValidator

---

## Security Components

- Hash Provider
- Token Generator
- Output Encoder
- Query Executor
- Authentication Manager

---

# Authentication Flow

```
User
   │
   ▼
Submit Registration
   │
   ▼
Validate Input
   │
   ▼
Validate Password Policy
   │
   ▼
Generate Random Salt
   │
   ▼
Generate HMAC Hash
   │
   ▼
Store Hash + Salt
   │
   ▼
Send Confirmation Email
```

---

# Login Flow

```
User
   │
   ▼
Lookup User
   │
   ▼
Hash Password
   │
   ▼
Compare Hash
   │
   ├── Success
   │      │
   │      ▼
   │  Reset Failed Attempts
   │
   └── Failure
          │
          ▼
 Increment Failed Attempts
          │
          ▼
   Lock Account After 3 Attempts
```

---

# Forgot Password Flow

```
User
   │
   ▼
Enter Email
   │
   ▼
Generate SHA-1 Token
   │
   ▼
Store Token
   │
   ▼
Send Email
   │
   ▼
Validate Token
   │
   ▼
Generate New Salt
   │
   ▼
Generate New HMAC Hash
   │
   ▼
Store New Password
```

---

# Customer Management

## Version 1 (Vulnerable)

```
Input
   │
   ▼
Store Directly
   │
   ▼
Render Without Encoding

→ Stored XSS Demonstration
```

---

## Version 2 (Secure)

```
Input
   │
   ▼
Validation
   │
   ▼
Parameterized Execution
   │
   ▼
Database
   │
   ▼
Output Encoding
```

---

# Configuration Structure

```json
Application
Database
PasswordPolicy
Authentication
PasswordReset
Hashing
Email
Security
Logging
```

Example hierarchy:

```
Application
│
├── Name
├── Environment
└── Version

Database
│
├── Server
├── Database
├── TrustedConnection
└── Timeout

PasswordPolicy
│
├── MinimumLength
├── RequireUppercase
├── RequireLowercase
├── RequireDigit
├── RequireSpecialCharacter
├── HistoryDepth
└── DictionaryValidationEnabled

Authentication
│
├── MaxLoginAttempts
├── LockoutMinutes
└── SessionTimeoutMinutes

PasswordReset
│
├── TokenExpirationMinutes
└── HashAlgorithm

Hashing
│
├── Algorithm
├── SaltLength
└── KeyLength

Email
│
├── SMTPServer
├── Port
├── Username
├── Sender
└── SSL

Security
│
├── EnableOutputEncoding
├── EnableParameterizedExecution
├── EnableAuditLogging
└── DemonstrationMode

Logging
│
├── Level
├── FileLocation
└── RetentionDays
```

---

# Design Patterns

| Component | Pattern |
|----------|---------|
| Controllers | MVC |
| Services | Service Layer |
| Database Access | Repository Pattern |
| DTO Mapping | Mapper Pattern |
| Password Validation | Strategy Pattern |
| Configuration | Options Pattern |
| Dependency Injection | Dependency Injection |
| Authentication | Facade Pattern |

---

# Technologies

- ASP.NET MVC (C#)
- SQL Server Express
- HTML / CSS / Bootstrap
- SMTP Email Service
- HMAC Password Hashing
- SHA-1 Password Reset Tokens
- appsettings.json Configuration

---

# Educational Purpose

This project is intended for academic use to demonstrate:

- Secure authentication design
- Password policy enforcement
- SQL Injection vulnerabilities and mitigation
- Stored XSS vulnerabilities and mitigation
- Secure software architecture
- Layered application design
- Repository and Service patterns

No production deployment is intended. The vulnerable implementation exists solely for security education and comparison with the secure implementation.