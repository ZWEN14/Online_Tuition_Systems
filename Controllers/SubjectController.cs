using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnywhereEdureach.Controllers;

// NOTE: Full Subject Maintenance CRUD (Insert/Update/Delete) is owned by
// another team member. This controller only provides the read-only Index
// action needed by the Tutor Browsing / Booking flow below.
[Authorize]
public class SubjectController(ApplicationDbContext db) : Controller
{
    // GET: Subject/Index
    public IActionResult Index()
    {
        var model = db.Subjects
            .Select(s => new SubjectListVM
            {
                Subject = s,
                TutorCount = s.TutorSubjects.Count
            })
            .ToList();

        return View(model);
    }
}
