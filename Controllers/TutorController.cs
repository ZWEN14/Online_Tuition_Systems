using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnywhereEdureach.Controllers;

[Authorize]
public class TutorController(DB db) : Controller
{
    // GET: Tutor/Index
    public IActionResult Index(int? subjectId)
    {
        var query = db.Users
            .Where(u => u.Role == UserRole.Tutor)
            .Include(u => u.Tutor)
            .Include(u => u.TutorSubjects)
                .ThenInclude(ts => ts.Subject)
            .AsQueryable();

        Subject? selectedSubject = null;

        if (subjectId.HasValue)
        {
            selectedSubject = db.Subjects.Find(subjectId.Value);
            if (selectedSubject != null)
                query = query.Where(u =>
                    u.TutorSubjects.Any(ts => ts.SubjectId == selectedSubject.Id));
        }

        return View(new TutorIndexVM
        {
            Tutors = query.ToList(),
            SelectedSubject = selectedSubject
        });
    }

    // GET: Tutor/Show/5?subjectId=1
    public IActionResult Show(int id, int? subjectId)
    {
        var tutor = db.Users
            .Include(u => u.Tutor)
            .Include(u => u.TutorSubjects)
                .ThenInclude(ts => ts.Subject)
            .FirstOrDefault(u => u.Id == id && u.Role == UserRole.Tutor);

        if (tutor == null) return NotFound();

        int? selectedSubjectId = subjectId;

        if (!selectedSubjectId.HasValue && tutor.TutorSubjects.Count == 1)
            selectedSubjectId = tutor.TutorSubjects.First().SubjectId;

        Subject? selectedSubject = null;

        if (selectedSubjectId.HasValue)
        {
            var tutorSubject = tutor.TutorSubjects
                .FirstOrDefault(ts => ts.SubjectId == selectedSubjectId.Value);

            if (tutorSubject == null)
                return NotFound("The selected subject is not offered by this tutor.");

            selectedSubject = tutorSubject.Subject;
        }

        var availableSlots = db.Timeslots
            .Where(t => t.TutorId == tutor.Id && t.IsActive)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.StartTime)
            .ToList();

        return View(new TutorShowVM
        {
            Tutor = tutor,
            AvailableSlots = availableSlots,
            SelectedSubjectId = selectedSubjectId,
            SelectedSubject = selectedSubject
        });
    }
}
