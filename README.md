# Comunication_LTD — Web Information System

## The group
Benji Ender 208196477

Inon Reaby 212431787

Oshri Halevi 318830569

Yam Reshef 212670103

Itay Fischer 323103317

---

A web-based information system for a fictional telecommunications company that sells internet browsing packages. Built with **Python Django** and **MySQL**.

This project demonstrates **secure development principles** alongside **intentionally vulnerable code** to showcase common web security attacks (SQL Injection, Stored XSS) and their mitigations.

---

## Project Structure

```
Comunication_LTD/
├── vulnerable/          # Version with intentional SQL Injection & XSS vulnerabilities
├── secure/              # Version with proper security defenses
└── README.md            # This file
```

Both `vulnerable/` and `secure/` are **fully independent Django projects** with identical functionality but different security implementations.

---


## Application Features (Part A — Secure Development)

### 1. User Registration (`/register/`)
- Define username, email, and password
- Password complexity enforced via `password_config.json`:
  - Minimum 10 characters
  - Must contain: uppercase, lowercase, digits, special characters
  - Cannot be in the common passwords dictionary (`common_passwords.txt`)
- Password stored using **HMAC-SHA256 + Salt**
  - Salt: randomly generated 32-byte hex string per user
  - HMAC key: stored in `settings.py` as `HMAC_SECRET_KEY`

### 2. Password Change (`/change-password/`)
- Requires current password verification
- New password must meet all complexity requirements from `password_config.json`
- **Password history**: cannot reuse current password or last 3 passwords (total 4 blocked)

### 3. Login (`/login/`)
- Username and password authentication
- Returns appropriate error messages for invalid credentials
- **Account lockout**: after 3 failed attempts, account is locked
- Locked accounts can only be unlocked via the Forgot Password flow

### 4. System Screen (`/system/`)
- Insert new customers with: first name, last name, email, phone, browsing package
- Displays the newly added customer's information on screen
- Requires authentication (session-based)

### 5. Forgot Password (`/forgot-password/` → `/reset-token/`)
- User enters their registered email
- System generates a **SHA-1 hash** of random bytes as a reset token
- Token is sent to the user's email via Gmail SMTP
- Token expires after **15 minutes**
- User enters the token at `/reset-token/` to access the password change screen

---

## Password Configuration (`password_config.json`)

```json
{
    "min_length": 10,
    "require_uppercase": true,
    "require_lowercase": true,
    "require_digits": true,
    "require_special": true,
    "password_history_count": 3,
    "max_login_attempts": 3,
    "dictionary_file": "common_passwords.txt"
}
```

All values can be modified by the system administrator. Changes take effect immediately (the file is read on each request).

---

## Security Details

### Session Management
- Sessions expire after **30 minutes** of inactivity
- Session timer resets on each request (`SESSION_SAVE_EVERY_REQUEST = True`)
- Sessions are destroyed on browser close

### Password Storage
- **Algorithm**: HMAC-SHA256
- **Salt**: Unique 32-byte random salt per user (stored alongside hash)
- **Key**: HMAC secret key stored in `settings.py`
- Plaintext passwords are **never stored**

---

## Part B — Vulnerability Demonstrations

### Vulnerable Version (`vulnerable/`)

### 1. Stored XSS – Add New Customer Screen

### What is the attack?
Stored XSS is an attack where malicious JavaScript code is saved inside the system and later executed in users’ browsers.

### How is it performed?
The attacker enters a payload such as `<script>alert()</script>` in one of the customer detail fields, and the alert appears whenever the customer list is loaded.

### What is the fix in the secure website?
The secure website uses Django auto-escaping, so the script is displayed as text and does not execute.

---

### 2. SQL Injection – Registration Screen

### What is the attack?
SQL Injection allows an attacker to manipulate a database query through user input.

### How is it performed?
The attacker enters a special character or SQL payload into one of the registration fields, which may cause the vulnerable site to run an unintended SQL query or show a SQL error.

### What is the fix in the secure website?
The secure website uses Django ORM and parameterized database operations, preventing user input from becoming part of the SQL command.

---

### 3. SQL Injection – Login Screen

### What is the attack?
SQL Injection on the login screen attempts to manipulate the authentication query.

### How is it performed?
The attacker enters malicious input into the username or password field to change the SQL query and potentially bypass authentication.

### What is the fix in the secure website?
The secure website retrieves users through Django ORM, so login input is safely handled and cannot alter the SQL query.

---

### 4. SQL Injection – Add New Customer Screen

### What is the attack?
SQL Injection on the customer screen attempts to manipulate the database query used to add a new customer.

### How is it performed?
The attacker enters a special character or SQL command into one of the customer fields, which may cause a malicious query to run against the database.

### What is the fix in the secure website?
The secure website creates customers using Django ORM, so customer input is inserted safely without allowing SQL Injection.


## Summary
Secure site: Django + Django ORM + auto-escaping.

Vulnerable site: Django + raw SQL f-strings + autoescape disabled.


## Database Schema

### Tables

| Table | Description |
|-------|-------------|
| `app_sector` | Market sectors (Residential, Business, Enterprise) |
| `app_package` | Internet browsing packages with prices |
| `app_appuser` | System users with HMAC+Salt password hashes |
| `app_passwordhistory` | Password history for reuse prevention |
| `app_passwordresettoken` | SHA-1 reset tokens with expiry |
| `app_customer` | Customer records linked to packages |



---

## URL Routes

| URL | View | Auth Required | Description |
|-----|------|---------------|-------------|
| `/` | `home_view` | No | Redirects to `/system/` or `/login/` |
| `/register/` | `register_view` | No | New user registration |
| `/login/` | `login_view` | No | User authentication |
| `/logout/` | `logout_view` | No | Session termination |
| `/change-password/` | `change_password_view` | Yes | Password change |
| `/forgot-password/` | `forgot_password_view` | No | Request reset token |
| `/reset-token/` | `reset_token_view` | No | Verify reset token |
| `/system/` | `system_view` | Yes | Customer management |

---

## Technologies

- **Backend**: Python 3.12, Django 6.0
- **Database**: MySQL
- **Frontend**: HTML5, Bootstrap 5
- **Password Hashing**: HMAC-SHA256 with per-user salt
- **Reset Tokens**: SHA-1 hash of cryptographic random bytes
- **Email**: Gmail SMTP with TLS
