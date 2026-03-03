using Attendance.Data;
using Attendance.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Services;

public interface IPromotionService
{
    Task<List<Student>> GetPromotionPreviewAsync(string? classNo, Division? division, Language? language, bool promoteAll, string finalClass);
    Task<int> PromoteStudentsAsync(List<int> studentIds, string finalClass, bool markFinalAsInactive);
}

public class PromotionService : IPromotionService
{
    private readonly ApplicationDbContext _db;
    public PromotionService(ApplicationDbContext db) => _db = db;

    public async Task<List<Student>> GetPromotionPreviewAsync(string? classNo, Division? division, Language? language, bool promoteAll, string finalClass)
    {
        var q = _db.Students.Where(s => s.Status == StudentStatus.Active);
        if (!promoteAll)
        {
            if (!string.IsNullOrEmpty(classNo)) q = q.Where(s => s.Class == classNo);
            if (division.HasValue) q = q.Where(s => s.Division == division.Value);
            if (language.HasValue) q = q.Where(s => s.Language == language.Value);
        }
        return await q.ToListAsync();
    }

    public async Task<int> PromoteStudentsAsync(List<int> studentIds, string finalClass, bool markFinalAsInactive)
    {
        var students = await _db.Students.Where(s => studentIds.Contains(s.Id)).ToListAsync();
        foreach (var s in students)
        {
            if (s.Class == finalClass)
            {
                if (markFinalAsInactive) s.Status = StudentStatus.Inactive;
            }
            else
            {
                s.Class = GetNextClass(s.Class);
                s.LastPromotedDate = DateTime.UtcNow;
            }
        }
        await _db.SaveChangesAsync();
        return students.Count;
    }

    private string GetNextClass(string current)
    {
        if (current == "NURSERY") return "LKG";
        if (current == "LKG") return "UKG";
        if (current == "UKG") return "1";
        if (int.TryParse(current, out int n)) return (n + 1).ToString();
        return current;
    }
}
