using Attendance.Data;
using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Services;

public interface IExportService
{
    Task<byte[]> ExportStudentAttendanceExcelAsync(ReportFilterViewModel filter);
    Task<byte[]> ExportStudentAttendancePdfAsync(ReportFilterViewModel filter);
    Task<byte[]> ExportStaffAttendanceExcelAsync(ReportFilterViewModel filter);
    Task<byte[]> ExportStaffAttendancePdfAsync(ReportFilterViewModel filter);
}

public class ExportService : IExportService
{
    private readonly ApplicationDbContext _db;
    public ExportService(ApplicationDbContext db) => _db = db;

    private (DateTime from, DateTime to) GetDateRange(ReportFilterViewModel f)
    {
        if (f.Month.HasValue && f.Year.HasValue)
            return (new DateTime(f.Year.Value, f.Month.Value, 1), new DateTime(f.Year.Value, f.Month.Value, DateTime.DaysInMonth(f.Year.Value, f.Month.Value)));
        return (f.FromDate ?? DateTime.Today.AddMonths(-1), f.ToDate ?? DateTime.Today);
    }

    private async Task<List<StudentAttendanceReportRow>> BuildStudentRows(ReportFilterViewModel f)
    {
        var (from, to) = GetDateRange(f);
        var query = _db.StudentAttendances.Include(a => a.Student)
            .Where(a => a.Date.Date >= from.Date && a.Date.Date <= to.Date);
        if (f.StudentId.HasValue) query = query.Where(a => a.StudentId == f.StudentId.Value);
        if (!string.IsNullOrEmpty(f.Class)) query = query.Where(a => a.Student.Class == f.Class);
        if (f.Division.HasValue) query = query.Where(a => a.Student.Division == f.Division.Value);
        if (f.Language.HasValue) query = query.Where(a => a.Student.Language == f.Language.Value);

        var records = await query.ToListAsync();
        return records.GroupBy(a => new { a.StudentId, a.Student.Name, a.Student.Class, Division = a.Student.Division ?? Division.A, Language = a.Student.Language ?? Attendance.Models.Entities.Language.English })
            .Select(g => new StudentAttendanceReportRow
            {
                StudentId = g.Key.StudentId, StudentName = g.Key.Name,
                Class = g.Key.Class, Division = g.Key.Division, Language = g.Key.Language,
                TotalDays = g.Count(), PresentDays = g.Count(a => a.Status == AttendanceStatus.Present)
            }).OrderBy(r => r.Class).ThenBy(r => r.StudentName).ToList();
    }

    private async Task<List<StaffAttendanceReportRow>> BuildStaffRows(ReportFilterViewModel f)
    {
        var (from, to) = GetDateRange(f);
        var query = _db.StaffAttendances.Include(a => a.User)
            .Where(a => a.Date.Date >= from.Date && a.Date.Date <= to.Date);
        if (f.TeacherId.HasValue) query = query.Where(a => a.UserId == f.TeacherId.Value);
        var records = await query.ToListAsync();
        return records.GroupBy(a => new { a.UserId, a.User.Name })
            .Select(g => new StaffAttendanceReportRow
            {
                UserId = g.Key.UserId, StaffName = g.Key.Name,
                TotalDays = g.Count(), PresentDays = g.Count(a => a.Status == AttendanceStatus.Present)
            }).OrderBy(r => r.StaffName).ToList();
    }

    // EXCEL exports
    public async Task<byte[]> ExportStudentAttendanceExcelAsync(ReportFilterViewModel f)
    {
        var rows = await BuildStudentRows(f);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Student Attendance");
        ws.Cell(1, 1).Value = "No"; ws.Cell(1, 2).Value = "Student Name";
        ws.Cell(1, 3).Value = "Class"; ws.Cell(1, 4).Value = "Division"; ws.Cell(1, 5).Value = "Language";
        ws.Cell(1, 6).Value = "Total Days"; ws.Cell(1, 7).Value = "Present";
        ws.Cell(1, 8).Value = "Absent"; ws.Cell(1, 9).Value = "Percentage";
        var headerRow = ws.Row(1); headerRow.Style.Font.Bold = true; headerRow.Style.Fill.BackgroundColor = XLColor.SteelBlue;
        headerRow.Style.Font.FontColor = XLColor.White;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i]; int row = i + 2;
            ws.Cell(row, 1).Value = i + 1; ws.Cell(row, 2).Value = r.StudentName;
            ws.Cell(row, 3).Value = r.Class; ws.Cell(row, 4).Value = r.Division.ToString();
            ws.Cell(row, 5).Value = r.Language.ToString();
            ws.Cell(row, 6).Value = r.TotalDays; ws.Cell(row, 7).Value = r.PresentDays;
            ws.Cell(row, 8).Value = r.TotalDays - r.PresentDays;
            ws.Cell(row, 9).Value = $"{r.Percentage}%";
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms); return ms.ToArray();
    }

    public async Task<byte[]> ExportStaffAttendanceExcelAsync(ReportFilterViewModel f)
    {
        var rows = await BuildStaffRows(f);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Staff Attendance");
        ws.Cell(1, 1).Value = "No"; ws.Cell(1, 2).Value = "Staff Name";
        ws.Cell(1, 3).Value = "Total Days"; ws.Cell(1, 4).Value = "Present";
        ws.Cell(1, 5).Value = "Absent"; ws.Cell(1, 6).Value = "Percentage";
        var headerRow = ws.Row(1); headerRow.Style.Font.Bold = true; headerRow.Style.Fill.BackgroundColor = XLColor.SteelBlue;
        headerRow.Style.Font.FontColor = XLColor.White;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i]; int row = i + 2;
            ws.Cell(row, 1).Value = i + 1; ws.Cell(row, 2).Value = r.StaffName;
            ws.Cell(row, 3).Value = r.TotalDays; ws.Cell(row, 4).Value = r.PresentDays;
            ws.Cell(row, 5).Value = r.TotalDays - r.PresentDays; ws.Cell(row, 6).Value = $"{r.Percentage}%";
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms); return ms.ToArray();
    }

    // PDF exports
    public async Task<byte[]> ExportStudentAttendancePdfAsync(ReportFilterViewModel f)
    {
        var rows = await BuildStudentRows(f);
        using var ms = new MemoryStream();
        var writer = new PdfWriter(ms);
        var pdf = new PdfDocument(writer); var doc = new Document(pdf);
        doc.Add(new Paragraph("Student Attendance Report").SetFontSize(16).SetBold());
        var table = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 3, 20, 5, 5, 10, 8, 8, 8, 8 })).UseAllAvailableWidth();
        foreach (var h in new[] { "No", "Student Name", "Class", "Div", "Language", "Total", "Present", "Absent", "%" })
            table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetBold()));
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            table.AddCell((i + 1).ToString()); table.AddCell(r.StudentName);
            table.AddCell(r.Class); table.AddCell(r.Division.ToString());
            table.AddCell(r.Language.ToString());
            table.AddCell(r.TotalDays.ToString()); table.AddCell(r.PresentDays.ToString());
            table.AddCell((r.TotalDays - r.PresentDays).ToString()); table.AddCell($"{r.Percentage}%");
        }
        doc.Add(table); doc.Close(); return ms.ToArray();
    }

    public async Task<byte[]> ExportStaffAttendancePdfAsync(ReportFilterViewModel f)
    {
        var rows = await BuildStaffRows(f);
        using var ms = new MemoryStream();
        var writer = new PdfWriter(ms);
        var pdf = new PdfDocument(writer); var doc = new Document(pdf);
        doc.Add(new Paragraph("Staff Attendance Report").SetFontSize(16).SetBold());
        var table = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 3, 20, 8, 8, 8, 8 })).UseAllAvailableWidth();
        foreach (var h in new[] { "No", "Staff Name", "Total", "Present", "Absent", "%" })
            table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetBold()));
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            table.AddCell((i + 1).ToString()); table.AddCell(r.StaffName);
            table.AddCell(r.TotalDays.ToString()); table.AddCell(r.PresentDays.ToString());
            table.AddCell((r.TotalDays - r.PresentDays).ToString()); table.AddCell($"{r.Percentage}%");
        }
        doc.Add(table); doc.Close(); return ms.ToArray();
    }
}
