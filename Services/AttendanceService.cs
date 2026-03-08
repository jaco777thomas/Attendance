using Attendance.Data;
using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Services;

public interface IAttendanceService
{
    Task<Dictionary<int, AttendanceStatus>> GetStudentAttendanceAsync(List<int> studentIds, DateTime date);
    Task SaveStudentAttendanceAsync(DateTime date, List<StudentAttendanceRow> rows);
    Task<Dictionary<int, AttendanceStatus>> GetStaffAttendanceAsync(DateTime date);
    Task SaveStaffAttendanceAsync(DateTime date, int markedById, List<StaffAttendanceRow> rows);
    Task<List<StudentAttendance>> GetStudentAttendanceRangeAsync(int? studentId, string? classNo, Division? division, Language? language, DateTime from, DateTime to);
    Task<List<StaffAttendance>> GetStaffAttendanceRangeAsync(int? userId, DateTime from, DateTime to);
    Task<(int Present, int Absent)> GetTodaySummaryAsync();
    Task<(int Present, int Absent)> GetStaffTodaySummaryAsync();
    Task<List<string>> GetAbsentStudentNamesAsync(List<int>? studentIds, DateTime date);
    Task<List<string>> GetAbsentStaffNamesAsync(DateTime date);
}

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _db;
    public AttendanceService(ApplicationDbContext db) => _db = db;

    public async Task<Dictionary<int, AttendanceStatus>> GetStudentAttendanceAsync(List<int> studentIds, DateTime date)
    {
        var targetDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        var records = await _db.StudentAttendances
            .Where(a => a.Date == targetDate && studentIds.Contains(a.StudentId))
            .Select(a => new { a.StudentId, a.Status })
            .ToListAsync();
        return records.ToDictionary(a => a.StudentId, a => a.Status);
    }

    public async Task SaveStudentAttendanceAsync(DateTime date, List<StudentAttendanceRow> rows)
    {
        var targetDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        var ids = rows.Select(r => r.StudentId).ToList();
        var existing = await _db.StudentAttendances
            .Where(a => a.Date == targetDate && ids.Contains(a.StudentId))
            .ToListAsync();

        foreach (var row in rows)
        {
            var existing_record = existing.FirstOrDefault(a => a.StudentId == row.StudentId);
            if (existing_record != null)
                existing_record.Status = row.Status;
            else
                _db.StudentAttendances.Add(new StudentAttendance { StudentId = row.StudentId, Date = targetDate, Status = row.Status });
        }
        await _db.SaveChangesAsync();
    }

    public async Task<Dictionary<int, AttendanceStatus>> GetStaffAttendanceAsync(DateTime date)
    {
        var targetDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        var records = await _db.StaffAttendances
            .Where(a => a.Date == targetDate)
            .ToListAsync();
        return records.ToDictionary(a => a.UserId, a => a.Status);
    }

    public async Task SaveStaffAttendanceAsync(DateTime date, int markedById, List<StaffAttendanceRow> rows)
    {
        var targetDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        var ids = rows.Select(r => r.UserId).ToList();
        var existing = await _db.StaffAttendances
            .Where(a => a.Date == targetDate && ids.Contains(a.UserId))
            .ToListAsync();

        foreach (var row in rows)
        {
            var record = existing.FirstOrDefault(a => a.UserId == row.UserId);
            if (record != null)
            {
                record.Status = row.Status;
                record.MarkedById = markedById;
            }
            else
                _db.StaffAttendances.Add(new StaffAttendance { UserId = row.UserId, Date = targetDate, Status = row.Status, MarkedById = markedById });
        }
        await _db.SaveChangesAsync();
    }

    public Task<List<StudentAttendance>> GetStudentAttendanceRangeAsync(int? studentId, string? classNo, Division? division, Language? language, DateTime from, DateTime to)
    {
        var f = DateTime.SpecifyKind(from.Date, DateTimeKind.Unspecified);
        var t = DateTime.SpecifyKind(to.Date, DateTimeKind.Unspecified);
        return _db.StudentAttendances
            .Include(a => a.Student)
            .Where(a => a.Date.Date >= f && a.Date.Date <= t
                && (!studentId.HasValue || a.StudentId == studentId.Value)
                && (string.IsNullOrEmpty(classNo) || a.Student.Class.Trim().ToLower() == classNo.Trim().ToLower())
                && (!division.HasValue || a.Student.Division == division.Value)
                && (!language.HasValue || a.Student.Language == language.Value))
            .OrderBy(a => a.Date).ThenBy(a => a.Student.Name)
            .ToListAsync();
    }

    public Task<List<StaffAttendance>> GetStaffAttendanceRangeAsync(int? userId, DateTime from, DateTime to)
    {
        var f = DateTime.SpecifyKind(from.Date, DateTimeKind.Unspecified);
        var t = DateTime.SpecifyKind(to.Date, DateTimeKind.Unspecified);
        return _db.StaffAttendances
            .Include(a => a.User)
            .Where(a => a.Date.Date >= f && a.Date.Date <= t
                && (!userId.HasValue || a.UserId == userId.Value))
            .OrderBy(a => a.Date).ThenBy(a => a.User.Name)
            .ToListAsync();
    }

    public async Task<(int Present, int Absent)> GetTodaySummaryAsync()
    {
        var today = DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified);
        var records = await _db.StudentAttendances.Where(a => a.Date == today).ToListAsync();
        return (records.Count(r => r.Status == AttendanceStatus.Present), records.Count(r => r.Status == AttendanceStatus.Absent));
    }

    public async Task<(int Present, int Absent)> GetStaffTodaySummaryAsync()
    {
        var today = DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified);
        var records = await _db.StaffAttendances.Where(a => a.Date == today).ToListAsync();
        return (records.Count(r => r.Status == AttendanceStatus.Present), records.Count(r => r.Status == AttendanceStatus.Absent));
    }

    public async Task<List<string>> GetAbsentStudentNamesAsync(List<int>? studentIds, DateTime date)
    {
        var targetDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        var query = _db.StudentAttendances
            .Include(a => a.Student)
            .Where(a => a.Date == targetDate && a.Status == AttendanceStatus.Absent);
        
        if (studentIds != null && studentIds.Any())
            query = query.Where(a => studentIds.Contains(a.StudentId));

        return await query.Select(a => a.Student.Name).ToListAsync();
    }

    public async Task<List<string>> GetAbsentStaffNamesAsync(DateTime date)
    {
        var targetDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        return await _db.StaffAttendances
            .Include(a => a.User)
            .Where(a => a.Date == targetDate && a.Status == AttendanceStatus.Absent)
            .Select(a => a.User.Name)
            .ToListAsync();
    }
}
