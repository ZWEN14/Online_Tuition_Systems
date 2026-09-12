using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Services.Courses;
using Online_Tuition_Systems.Services.Enrollments;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Controllers;

public class CoursesController(
    ICourseService courseService,
    IEnrollmentService enrollmentService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] CourseCatalogViewModel query,
        CancellationToken cancellationToken)
    {
        var model = await courseService.GetPublishedAsync(query, cancellationToken);

        if (string.Equals(
                Request.Headers["X-Requested-With"].ToString(),
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase))
        {
            return PartialView("_CourseCatalogResults", model);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        string slug,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var model = await courseService.GetPublishedDetailsAsync(
            slug,
            cancellationToken);

        if (model is null)
        {
            return NotFound();
        }

        if (User.IsInRole(AppRoles.Student)
            && int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var studentId))
        {
            model.CurrentStudentEnrollmentStatus =
                await enrollmentService.GetStatusAsync(
                    studentId,
                    model.CourseId,
                    cancellationToken);
        }

        return View(model);
    }
}
