# Comunication_LTD

Academic project. A website for an imaginary internet provider, built to
demonstrate secure development, and later the attacks it defends against.

| Folder / file | What it is |
|---|---|
| **`SecurityWebApp/`** | **The application. This is the one you run.** |
| `PRD.md` | What the system is supposed to do — screens, roles, security |
| `SecurityWebApp/CODE_GUIDE.md` | Every file and function explained |

A second, deliberately vulnerable copy (SQL injection + Stored XSS) will be made
later from this one, as a separate folder.

---

## 1. What you need

- **.NET 9 SDK** or newer — check with `dotnet --version`
  (download: <https://dotnet.microsoft.com/download>)
- **the `sqlite3` command** — used once, to create the tables
  (check with `sqlite3 --version`; see section 3 if you do not have it)

**There is no database server to install or start.** SQLite is just a file.

---

## 2. Run it

Two steps the first time, one step after that.

```bash
cd SecurityWebApp

# first time only - create the tables
sqlite3 app.db ".read Database/schema.sql"

# every time
dotnet run --launch-profile https
```

Then open **<https://localhost:7245>**

> If you skip the first command the application will not start. It stops with a
> message telling you to run exactly that line. It never creates tables by
> itself — the schema is yours to control.

> **Use `--launch-profile https`.** Plain `dotnet run` starts the HTTP-only
> profile, and login will silently fail there — the cookies are marked `Secure`,
> so the browser refuses to store them over plain HTTP. You will sign in and be
> bounced straight back to the login page with no error.

If the browser warns that the certificate is not trusted, run this once:

```bash
dotnet dev-certs https --trust
```

---

## 3. The database

The project uses **SQLite**. The whole database is a single file,
`SecurityWebApp/app.db`, and it always lives in that folder no matter which
directory you start the app from.

The tables come from one script you can read: **`SecurityWebApp/Database/schema.sql`**.
It creates exactly three tables — `Users`, `PasswordHistories`,
`PasswordResetTokens` — and their indexes. Nothing else.

```bash
sqlite3 app.db ".read Database/schema.sql"
```

Running it twice is harmless; every statement uses `IF NOT EXISTS`.

### Start over with an empty database

Stop the app, delete the file, create it again:

```bash
rm app.db
sqlite3 app.db ".read Database/schema.sql"
dotnet run --launch-profile https
```

### If you do not have `sqlite3`

Install *DB Browser for SQLite* (<https://sqlitebrowser.org>), create a new
database called `app.db` in the `SecurityWebApp` folder, open the
**Execute SQL** tab, paste the contents of `Database/schema.sql`, and run it.

On Windows you can also get the command line tool from
<https://sqlite.org/download.html> (the "sqlite-tools" bundle), or with
`winget install SQLite.SQLite`.

### If you change a model class

`Database/schema.sql` is **not** generated. If somebody adds or renames a
property in `Models/`, they have to edit the SQL file to match, then delete
`app.db` and create it again. Nothing warns you if the two drift apart.

---

## 4. Make the first admin

New accounts are always created as **Regular**. There is no way to sign up as an
admin — that is on purpose. The first admin is made by hand in the database.

**Step 1.** Register normally on the website, for example username `nadav`.

**Step 2.** Promote that account with one SQL statement:

```bash
cd SecurityWebApp
sqlite3 app.db "UPDATE Users SET IsAdmin = 1 WHERE Username = 'nadav';"
```

**Step 3.** **Sign out and sign in again.** The role is stored inside the login
cookie, so it only updates when you sign in.

The **System** item now appears in the menu. From there an admin can create new
users already set to Admin, or change a Regular user to Admin — so you only ever
do this once.

### Useful queries

```bash
sqlite3 -header -column app.db "SELECT Id, Username, Email, Role FROM Users;"

# unlock an account without waiting the hour
sqlite3 app.db "UPDATE Users SET IsLocked = 0, LoginAttempts = 0, LockedUntil = NULL WHERE Username = 'nadav';"
```

(An admin can also unlock from the System screen, which is easier.)

---

## 5. Email (optional)

Everything works without email except **Forgot password**, which sends the reset
code. Until you configure it, that page says it could not send the mail and logs
exactly which settings are missing.

Set the sender and credentials. The password and username are kept **out of
git** with user-secrets:

```bash
cd SecurityWebApp
dotnet user-secrets set "Email:SenderEmail" "you@gmail.com"
dotnet user-secrets set "Email:Username"    "you@gmail.com"
dotnet user-secrets set "Email:Password"    "your-app-password"
```

For Gmail you need an **App Password**, not your normal password: turn on
2-Step Verification, then create one at
<https://myaccount.google.com/apppasswords>.

`Host` and `Port` are already set for Gmail in `appsettings.json`.

---

## 6. Changing the rules

`SecurityWebApp/Properties/passwordOptions.json` holds the password policy
and the username rules — length, required character types, history depth,
dictionary check, login attempt limit.

The file is watched, so a change takes effect **on the next request**. No
restart, no rebuild.

`appsettings.json` holds the lockout time (60 minutes), the session timeout
(20 minutes) and the reset-token lifetime (5 minutes).

---

## 7. The pages

| Page | URL | Who can enter |
|---|---|---|
| Home | `/` | everyone |
| Register | `/Account/Register` | guest |
| Login | `/Account/Login` | guest |
| Forgot password | `/Account/ForgotPassword` | guest |
| Profile | `/Account/Profile` | signed in |
| Change password | `/Account/ChangePassword` | signed in |
| System screen | `/System` | **Admin only** |

---

## 8. If something goes wrong

| Problem | Cause and fix |
|---|---|
| "The database has no tables yet" on start-up | You skipped the schema step. `cd SecurityWebApp` then `sqlite3 app.db ".read Database/schema.sql"` |
| "no such column" errors while using the site | `Database/schema.sql` no longer matches the classes in `Models/`. Update the SQL, delete `app.db`, create it again |
| Login does nothing, returns to the login page | You are on `http://`. Use `--launch-profile https` and open `https://localhost:7245` |
| Browser warns about the certificate | `dotnet dev-certs https --trust` |
| "Address already in use" | Another copy is running. `netstat -ano \| findstr 7245` then `taskkill /PID <pid> /F` |
| No **System** menu item after the SQL update | Sign out and sign in again — the role lives in the login cookie |
| Account locked | Wait 60 minutes, unlock from the System screen, or run the unlock SQL above |
| Forgot password says it could not send | SMTP is not configured — see section 5 |
| Red errors in the IDE but `dotnet build` is clean | Open `Comunication_LTD.sln`, not the bare folder. Both projects share the `SecurityWebApp` namespace, and without the solution file the IDE merges them and reports duplicate types |

---

## 9. Building and testing

```bash
dotnet build                 # from the repo root, builds both projects
cd SecurityWebApp
dotnet build                 # just the secure version
```

There is no automated test suite yet; the flows are checked by hand.
