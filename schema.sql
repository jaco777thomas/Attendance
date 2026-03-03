-- Sunday School Attendance Management System
-- PostgreSQL Schema for Supabase
-- Run this in Supabase SQL Editor

-- Users
CREATE TABLE IF NOT EXISTS "Users" (
    "Id"           SERIAL PRIMARY KEY,
    "Name"         VARCHAR(100) NOT NULL,
    "Email"        VARCHAR(150) NOT NULL UNIQUE,
    "Phone"        VARCHAR(20),
    "PasswordHash" TEXT NOT NULL,
    "Role"         VARCHAR(255), -- Legacy role field (obsolete)
    "RoleId"       INTEGER, -- New dynamic role
    "IsActive"     BOOLEAN DEFAULT TRUE,
    "AssignedClass" VARCHAR(20), -- For Teachers
    "AssignedDivision" VARCHAR(10), -- For Teachers
    "AssignedLanguage" VARCHAR(20), -- For Teachers
    "CreatedAt"    TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Students
CREATE TABLE IF NOT EXISTS "Students" (
    "Id"               SERIAL PRIMARY KEY,
    "Name"             VARCHAR(150) NOT NULL,
    "Class"            VARCHAR(20) NOT NULL, -- LKG, UKG, 1-15
    "Division"         VARCHAR(5) NOT NULL DEFAULT 'A',
    "DateOfBirth"      DATE,
    "Address"          VARCHAR(500),
    "FatherName"       VARCHAR(150),
    "MotherName"       VARCHAR(150),
    "Phone"            VARCHAR(30),
    "Status"           VARCHAR(20) NOT NULL DEFAULT 'Active',
    "AcademicYear"     INTEGER,
    "Language"         VARCHAR(20) NOT NULL DEFAULT 'English',
    "LastPromotedDate" Date,
    "CreatedAt"        Date NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_students_class_div_lang_status ON "Students" ("Class", "Division", "Language", "Status");

-- Student Attendance
CREATE TABLE IF NOT EXISTS "StudentAttendances" (
    "Id"        SERIAL PRIMARY KEY,
    "StudentId" INTEGER NOT NULL REFERENCES "Students"("Id") ON DELETE CASCADE,
    "Date"      DATE NOT NULL,
    "Status"    VARCHAR(10) NOT NULL DEFAULT 'Present',
    UNIQUE("StudentId", "Date")
);
CREATE INDEX IF NOT EXISTS idx_student_att_date ON "StudentAttendances" ("Date");

-- Staff Attendance
CREATE TABLE IF NOT EXISTS "StaffAttendances" (
    "Id"         SERIAL PRIMARY KEY,
    "UserId"     INTEGER NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Date"       DATE NOT NULL,
    "Status"     VARCHAR(10) NOT NULL DEFAULT 'Present',
    "MarkedById" INTEGER REFERENCES "Users"("Id") ON DELETE SET NULL,
    UNIQUE("UserId", "Date")
);
CREATE INDEX IF NOT EXISTS idx_staff_att_date ON "StaffAttendances" ("Date");

-- Notices
CREATE TABLE IF NOT EXISTS "Notices" (
    "Id"          SERIAL PRIMARY KEY,
    "Title"       VARCHAR(200) NOT NULL,
    "Message"     TEXT NOT NULL,
    "Date"        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Target"      VARCHAR(20) NOT NULL DEFAULT 'All',
    "CreatedById" INTEGER NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS idx_notices_target ON "Notices" ("Target");

-- Roles
CREATE TABLE IF NOT EXISTS "Roles" (
    "Id"          SERIAL PRIMARY KEY,
    "Name"        VARCHAR(50) NOT NULL UNIQUE,
    "Description" VARCHAR(200)
);

-- Permissions
CREATE TABLE IF NOT EXISTS "Permissions" (
    "Id"   SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Key"  VARCHAR(50) NOT NULL UNIQUE
);

-- RolePermissions (Many-to-Many)
CREATE TABLE IF NOT EXISTS "RolePermissions" (
    "RoleId"       INTEGER NOT NULL REFERENCES "Roles"("Id") ON DELETE CASCADE,
    "PermissionId" INTEGER NOT NULL REFERENCES "Permissions"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("RoleId", "PermissionId")
);

-- Add Foreign Key to Users (if not exists)
-- This might fail if run multiple times, so we use a script block or handle in C#
-- For simplicity in schema.sql, we define the FK constraint here if we can.
-- ALTER TABLE "Users" ADD CONSTRAINT fk_user_role FOREIGN KEY ("RoleId") REFERENCES "Roles"("Id");

-- Default Headmaster...
-- Note: The application will auto-seed this on first run.
