using Attendance.Data;
using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Services;

public interface INoticeService
{
    Task<List<Notice>> GetForRoleAsync(UserRole role);
    Task<Notice?> GetByIdAsync(int id);
    Task<Notice> CreateAsync(NoticeCreateViewModel vm, int createdById);
    Task<bool> UpdateAsync(NoticeEditViewModel vm);
    Task<bool> DeleteAsync(int id);
}

public class NoticeService : INoticeService
{
    private readonly ApplicationDbContext _db;
    public NoticeService(ApplicationDbContext db) => _db = db;

    public Task<List<Notice>> GetForRoleAsync(UserRole role)
    {
        var targets = new List<NoticeTarget> { NoticeTarget.All };
        if (role.HasFlag(UserRole.HeadMaster) || role.HasFlag(UserRole.Admin))
        {
             targets.Add(NoticeTarget.Teachers);
             targets.Add(NoticeTarget.AttendanceIncharges);
        }
        else
        {
            if (role.HasFlag(UserRole.Teacher)) targets.Add(NoticeTarget.Teachers);
            if (role.HasFlag(UserRole.AttendanceIncharge)) targets.Add(NoticeTarget.AttendanceIncharges);
        }

        return _db.Notices
            .Include(n => n.CreatedBy)
            .Where(n => targets.Contains(n.Target))
            .OrderByDescending(n => n.Date)
            .ToListAsync();
    }

    public Task<Notice?> GetByIdAsync(int id) =>
        _db.Notices.Include(n => n.CreatedBy).FirstOrDefaultAsync(n => n.Id == id);

    public async Task<Notice> CreateAsync(NoticeCreateViewModel vm, int createdById)
    {
        var n = new Notice { Title = vm.Title, Message = vm.Message, Target = vm.Target, CreatedById = createdById, Date = DateTime.UtcNow };
        _db.Notices.Add(n);
        await _db.SaveChangesAsync();
        return n;
    }

    public async Task<bool> UpdateAsync(NoticeEditViewModel vm)
    {
        var n = await _db.Notices.FindAsync(vm.Id);
        if (n == null) return false;
        n.Title = vm.Title; n.Message = vm.Message; n.Target = vm.Target;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var n = await _db.Notices.FindAsync(id);
        if (n == null) return false;
        _db.Notices.Remove(n);
        await _db.SaveChangesAsync();
        return true;
    }
}
