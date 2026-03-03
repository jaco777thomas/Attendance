# Sunday School Attendance Management System

A full-featured, mobile-friendly web application for managing Sunday School students, teachers, attendance, reports, and announcements.

## 🛠 Tech Stack
- **Backend**: ASP.NET Core 8 MVC
- **Database**: Supabase PostgreSQL (via EF Core + Npgsql)
- **Auth**: Cookie-based authentication with role claims
- **Hosting**: Render (Free Tier, Docker)
- **Reports**: Excel (.xlsx via ClosedXML) + PDF (via itext7)

---

## 🚀 Getting Started Locally

### 1. Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A [Supabase](https://supabase.com) project with PostgreSQL

### 2. Configure Database

Open `appsettings.json` and update the connection string:

```json
"DefaultConnection": "Host=YOUR_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SslMode=Require;Trust Server Certificate=true"
```

You can find your Supabase connection details in:
`Supabase Dashboard → Project Settings → Database`

### 3. Create Tables in Supabase

Open the **SQL Editor** in Supabase and run the contents of `schema.sql`.

> The app also auto-creates tables via `EnsureCreated()` on first startup.

### 4. Run the App

```powershell
cd d:\Personal\Websites\Attendance
dotnet run
```

Open `https://localhost:5001` in your browser.

### 5. Default Login

| Field    | Value              |
|----------|--------------------|
| Email    | admin@church.org   |
| Password | Admin@123          |

> **Change the password immediately after first login** (Profile → New Password).

---

## 📋 Feature Summary

| Module            | Roles                         | Features                                                      |
|-------------------|-------------------------------|---------------------------------------------------------------|
| Dashboard         | All                           | Stats, quick actions, recent notices                          |
| Student Management| Teacher, Leader, Headmaster   | Add/Edit/Delete, filter, search, active/inactive toggle       |
| Student Attendance| All                           | Date+Class+Division based, bulk mark, AJAX                    |
| Teacher Attendance| All (self); Admin (all)       | Mark own; Headmaster/Attendance Incharge,Co-Ordinator, Teacher can mark on behalf                |
| Reports           | All                           | Filter by date/class/student/teacher, Excel + PDF export      |
| Notice Board      | View: All; Post: Attendance Incharge,Co-Ordinator, Teacher, Headmaster | Target: All / Teachers / Leaders                              |
| User Management   | Headmaster only               | CRUD, password reset, activate/deactivate                     |
| Year Promotion    | Headmaster, Attendance Incharge,Co-Ordinator, Teacher            | Promote by class/division/all; final class → Inactive         |

---

## ☁️ Deploying to Render

### Step 1: Push to GitHub
```powershell
git init
git add .
git commit -m "Initial commit"
git remote add origin https://github.com/YOUR_USER/sunday-school-attendance.git
git push -u origin main
```

### Step 2: Create Render Service
1. Go to [render.com](https://render.com) → **New → Web Service**
2. Connect your GitHub repository
3. Select **Docker** as the runtime
4. Set the following **Environment Variables**:

| Key                    | Value                                              |
|------------------------|----------------------------------------------------|
| `DATABASE_URL`         | Your Supabase PostgreSQL connection string         |
| `ASPNETCORE_ENVIRONMENT` | `Production`                                     |

5. Click **Deploy**

> ⚠️ **Free tier note**: The app sleeps after 15 mins of inactivity. The first request after idle may take ~30 seconds to spin up.

### Step 3: Post-Deploy
- Visit your Render URL (e.g., `https://sunday-school-attendance.onrender.com`)
- Login with `admin@church.org / Admin@123`
- Change password immediately!

---

## 📁 Project Structure

```
Attendance/
├── Controllers/          # MVC controllers for each module
├── Models/
│   ├── Entities/         # EF Core entity models
│   └── ViewModels/       # View-specific models
├── Data/                 # ApplicationDbContext
├── Services/             # Business logic layer
├── Views/                # Razor views for all modules
├── wwwroot/css/          # Custom CSS (no Tailwind/Bootstrap)
├── schema.sql            # PostgreSQL DDL for Supabase
├── Dockerfile            # Multi-stage Docker build
├── render.yaml           # Render deployment config
└── appsettings.json      # App configuration
```
