using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Services.Courses;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Controllers;

public class CoursesController(ICourseService courseService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] CourseCatalogViewModel query,
        CancellationToken cancellationToken)
    {
        var model = await courseService.GetPublishedAsync(query, cancellationToken);
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

        return model is null ? NotFound() : View(model);
    }
}
