Project Name: Sunday School Attendance Management System

Technology Stack:

Backend: ASP.NET Core Web Application

Hosting: Render (Free Tier)

Database: Supabase PostgreSQL

Frontend: Responsive (Mobile + Desktop friendly)

Authentication: Role-based login

Project Description

Create a web-based Sunday School Attendance Management System for churches to manage students, teachers, attendance, reports, and announcements. The system should be simple, secure, mobile-friendly, and optimized for free hosting.

User Roles

Headmaster (Admin)

Leader

Teacher

Core Modules
1. User Management

Registration and login for:

Headmaster

Leader

Teacher

Fields:

Name

Email / Username

Phone

Password (encrypted)

Role

Features:

Role-based access control

Update profile

Headmaster can create/edit/delete users

2. Student Management

Student Registration with fields:

Name

Class (Grade)

Division

Age / Date of Birth

Address

Father Name

Mother Name

Phone Number (optional)

Active / Inactive status

Features:

Add / Edit / Delete students

Filter by Class & Division

Search students

3. Attendance Management
Student Attendance

Date-wise attendance

Class & Division selection

Mark:

Present

Absent

Bulk marking option

Teacher Attendance

Teachers mark their own attendance

If a teacher is absent:

Headmaster or Leader can mark attendance on behalf of the teacher

4. Attendance Reports

View attendance by:

Date

Class

Student

Teacher

Monthly summary

Percentage calculation

Export reports to:

Excel (.xlsx)

5. Notification / Notice Board

Headmaster and Leader can:

Create notices

Edit / Delete notices

Fields:

Title

Message

Date

Target (All / Teachers / Leaders)

Teachers can view notices on dashboard

Dashboard Requirements
Headmaster Dashboard

Total Students

Total Teachers

Today’s Attendance Summary

Quick links to Reports and Notices

Teacher Dashboard

Mark attendance

View assigned class students

View notices

Database (Supabase PostgreSQL Tables)

Users (Id, Name, Email, PasswordHash, Role)

Students (Id, Name, Class, Division, Age, Address, Father, Mother)

StudentAttendance (Id, StudentId, Date, Status)

TeacherAttendance (Id, UserId, Date, Status, MarkedBy)

Notices (Id, Title, Message, Date, CreatedBy)

Additional Requirements

Mobile-friendly UI (important for Sunday use)

Simple and clean design

Secure authentication (JWT or Identity)

Error handling and validation

Environment configuration for Render deployment

Supabase connection using PostgreSQL connection string

Output Needed

Full ASP.NET Core project structure

API + UI (MVC or Razor Pages)

Database schema

Deployment instructions for Render

Supabase connection setup

6. Student Promotion (Next Academic Year)

Add a simple feature to promote students to the next class at the end of the academic year.

Requirements

Headmaster or Leader can perform Year Promotion

Option: “Promote Students to Next Class”

System should:

Increase Class by one level (e.g., Class 1 → Class 2)

Keep Division unchanged (or allow optional change)

Provide filters:

Promote by Class

Promote by Class & Division

Promote All Students

Show confirmation before promotion:

“Are you sure you want to promote selected students?”

Special Cases

Final class students:

Option to mark them as Inactive (Completed)

Maintain promotion history (optional if supported)

7. Student Status Management

Add Active / Inactive status for students.

Requirements

Field: Status

Active

Inactive

Default: Active

Only Active students should appear in:

Attendance

Class lists

Headmaster / Leader can:

Activate / Deactivate students

Bulk update status

Use cases:

Student left Sunday School

Completed final class

Temporarily inactive

Database Update

Add fields in Students table:

Status (Active/Inactive)

AcademicYear (optional, recommended)

LastPromotedDate (optional)

UI Requirements

Button: Promote to Next Year

Dropdown filter: Class / Division

Toggle switch for Active / Inactive

Option to view:

Active Students

Inactive Students

All Students