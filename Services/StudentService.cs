using Attendance.Data;
using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Services;

public interface IStudentService
{
    Task<List<Student>> GetAllAsync(string? filterClass, Division? division, Language? language, StudentStatus? status, string? search);
    Task<Student?> GetByIdAsync(int id);
    Task<Student> CreateAsync(StudentCreateViewModel vm);
    Task<bool> UpdateAsync(StudentEditViewModel vm);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleStatusAsync(int id);
    Task<int> BulkStatusUpdateAsync(List<int> ids, StudentStatus status);
    Task<List<Student>> GetForAttendanceAsync(string classNo, Division division, Language language, DateTime date);
    Task<(int success, int total, string message)> ImportStudentsFromExcelAsync(Stream excelStream);
}

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _db;
    public StudentService(ApplicationDbContext db) => _db = db;

    public async Task<List<Student>> GetAllAsync(string? filterClass, Division? division, Language? language, StudentStatus? status, string? search)
    {
        var q = _db.Students.AsQueryable();
        if (!string.IsNullOrEmpty(filterClass)) 
        {
            var trimmedClass = filterClass.Trim().ToLower();
            q = q.Where(s => s.Class.Trim().ToLower() == trimmedClass);
        }
        if (division.HasValue) q = q.Where(s => s.Division == division.Value);
        if (language.HasValue) q = q.Where(s => s.Language == language.Value);
        if (status.HasValue) q = q.Where(s => s.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(s => s.Name.ToLower().Contains(search.ToLower()));
        return await q.OrderBy(s => s.Class).ThenBy(s => s.Division).ThenBy(s => s.Name).ToListAsync();
    }

    public Task<Student?> GetByIdAsync(int id) => _db.Students.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Student> CreateAsync(StudentCreateViewModel vm)
    {
        var s = new Student
        {
            Name = vm.Name, Class = vm.Class, Division = vm.Division,
            Language = vm.Language,
            DateOfBirth = vm.DateOfBirth, Address = vm.Address,
            FatherName = vm.FatherName, MotherName = vm.MotherName,
            Phone = vm.Phone, Status = vm.Status,
            AcademicYear = vm.AcademicYear ?? DateTime.UtcNow.Year
        };
        _db.Students.Add(s);
        await _db.SaveChangesAsync();
        return s;
    }

    public async Task<bool> UpdateAsync(StudentEditViewModel vm)
    {
        var s = await _db.Students.FindAsync(vm.Id);
        if (s == null) return false;
        s.Name = vm.Name; s.Class = vm.Class; s.Division = vm.Division;
        s.Language = vm.Language;
        s.DateOfBirth = vm.DateOfBirth; s.Address = vm.Address;
        s.FatherName = vm.FatherName; s.MotherName = vm.MotherName;
        s.Phone = vm.Phone; s.Status = vm.Status;
        s.AcademicYear = vm.AcademicYear;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var s = await _db.Students.FindAsync(id);
        if (s == null) return false;
        _db.Students.Remove(s);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var s = await _db.Students.FindAsync(id);
        if (s == null) return false;
        s.Status = s.Status == StudentStatus.Active ? StudentStatus.Inactive : StudentStatus.Active;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkStatusUpdateAsync(List<int> ids, StudentStatus status)
    {
        var students = await _db.Students.Where(s => ids.Contains(s.Id)).ToListAsync();
        foreach (var s in students) s.Status = status;
        await _db.SaveChangesAsync();
        return students.Count;
    }

    public async Task<List<Student>> GetForAttendanceAsync(string classNo, Division division, Language language, DateTime date)
    {
        return await _db.Students
            .Where(s => s.Class.ToLower().Trim() == classNo.ToLower().Trim() && s.Division == division && s.Language == language && s.Status == StudentStatus.Active)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<(int success, int total, string message)> ImportStudentsFromExcelAsync(Stream excelStream)
    {
        try
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook(excelStream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

            int success = 0;
            int total = 0;
            int currentYear = DateTime.UtcNow.Year;

            foreach (var row in rows)
            {
                total++;
                try
                {
                    var cls = row.Cell(1).GetValue<string>();
                    var name = row.Cell(2).GetValue<string>();
                    var langStr = row.Cell(3).GetValue<string>();
                    var dobStr = row.Cell(4).GetValue<string>();
                    // var age = row.Cell(5).GetValue<string>(); // Skip age
                    var father = row.Cell(6).GetValue<string>();
                    var phone = row.Cell(7).GetValue<string>();
                    var mother = row.Cell(8).GetValue<string>();
                    // var phone2 = row.Cell(9).GetValue<string>(); // Skip phone2
                    var address = row.Cell(10).GetValue<string>();

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(cls)) continue;

                    if (!Enum.TryParse<Language>(langStr, true, out var language)) language = Language.English;
                    
                    DateTime? dob = null;
                    if (DateTime.TryParse(dobStr, out var d)) dob = d;

                    var student = new Student
                    {
                        Name = name.Trim(),
                        Class = cls.Trim(),
                        Division = Division.A, // Defaulting to A
                        Language = language,
                        DateOfBirth = dob,
                        FatherName = father ?? "",
                        MotherName = mother ?? "",
                        Phone = phone ?? "",
                        Address = address ?? "",
                        Status = StudentStatus.Active,
                        AcademicYear = currentYear
                    };

                    _db.Students.Add(student);
                    success++;
                }
                catch { /* Skip invalid row */ }
            }

            await _db.SaveChangesAsync();
            return (success, total, "Import completed.");
        }
        catch (Exception ex)
        {
            return (0, 0, $"Error: {ex.Message}");
        }
    }
}
