using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.Services.Courses;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.Tutor)]
public class TutorCoursesController(
    ICourseService courseService,
    ICourseWorkspaceService workspaceService,
    ICourseStreamService streamService,
    ICourseworkService courseworkService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var courses = await courseService.GetTutorCoursesAsync(
            tutorId,
            cancellationToken);

        return View(courses);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new CourseFormViewModel();
        await courseService.PopulateCategoriesAsync(model, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Workspace(
        int id,
        string? tab,
        string? studentSort,
        string? streamSort,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var model = await workspaceService.GetTutorWorkspaceAsync(
            tutorId,
            id,
            cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        model.ActiveTab = CourseWorkspaceTabs.Normalize(tab, allowStudents: true);
        if (model.ActiveTab == CourseWorkspaceTabs.Stream)
        {
            model.Stream = await streamService.GetForTutorAsync(
                tutorId,
                id,
                streamSort,
                cancellationToken);
        }
        else if (model.ActiveTab == CourseWorkspaceTabs.Coursework)
        {
            model.Coursework = await courseworkService.GetForTutorAsync(
                tutorId,
                id,
                cancellationToken);
        }
        model.StudentSort = CourseStudentSortOptions.Normalize(studentSort);
        model.Students = model.StudentSort switch
        {
            CourseStudentSortOptions.Newest => model.Students
                .OrderByDescending(student => student.EnrolledAtUtc)
                .ThenBy(student => student.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CourseStudentSortOptions.Oldest => model.Students
                .OrderBy(student => student.EnrolledAtUtc)
                .ThenBy(student => student.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ => model.Students
                .OrderBy(student => student.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(student => student.StudentId)
                .ToList()
        };
        ViewData["SidebarSection"] = "TutorCourses";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateLesson(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var workspace = await workspaceService.GetTutorWorkspaceAsync(
            tutorId,
            id,
            cancellationToken);
        if (workspace is null || workspace.CourseStatus == CourseStatus.Suspended)
        {
            return NotFound();
        }

        ViewData["CourseTitle"] = workspace.Title;
        ViewData["SidebarSection"] = "TutorCourses";
        return View(new CourseLessonFormViewModel
        {
            CourseId = id,
            DisplayOrder = workspace.Lessons.Count == 0
                ? 1
                : workspace.Lessons.Max(lesson => lesson.DisplayOrder) + 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(55L * 1024 * 1024)]
    public async Task<IActionResult> CreateLesson(
        CourseLessonFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var workspace = await workspaceService.GetTutorWorkspaceAsync(
            tutorId,
            model.CourseId,
            cancellationToken);
        if (workspace is null || workspace.CourseStatus == CourseStatus.Suspended)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewData["CourseTitle"] = workspace.Title;
            ViewData["SidebarSection"] = "TutorCourses";
            return View(model);
        }

        var result = await workspaceService.CreateLessonAsync(
            tutorId,
            model,
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            ViewData["CourseTitle"] = workspace.Title;
            ViewData["SidebarSection"] = "TutorCourses";
            return View(model);
        }

        TempData["SuccessMessage"] = "Lesson created successfully.";
        return RedirectToAction(nameof(Workspace), new
        {
            id = model.CourseId,
            tab = CourseWorkspaceTabs.Lessons
        });
    }

    [HttpGet]
    public async Task<IActionResult> EditLesson(
        int id,
        int lessonId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var model = await workspaceService.GetLessonFormAsync(
            tutorId,
            id,
            lessonId,
            cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        ViewData["SidebarSection"] = "TutorCourses";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(55L * 1024 * 1024)]
    public async Task<IActionResult> EditLesson(
        CourseLessonFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var existing = model.CourseLessonId.HasValue
            ? await workspaceService.GetLessonFormAsync(
                tutorId,
                model.CourseId,
                model.CourseLessonId.Value,
                cancellationToken)
            : null;
        if (existing is null)
        {
            return NotFound();
        }

        model.ExistingResourceFileName = existing.ExistingResourceFileName;

        if (!ModelState.IsValid)
        {
            ViewData["SidebarSection"] = "TutorCourses";
            return View(model);
        }

        var result = await workspaceService.UpdateLessonAsync(
            tutorId,
            model,
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            ViewData["SidebarSection"] = "TutorCourses";
            return View(model);
        }

        TempData["SuccessMessage"] = "Lesson updated successfully.";
        return RedirectToAction(nameof(Workspace), new
        {
            id = model.CourseId,
            tab = CourseWorkspaceTabs.Lessons
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLesson(
        int id,
        int lessonId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var result = await workspaceService.DeleteLessonAsync(
            tutorId,
            id,
            lessonId,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Lesson removed." : result.Error;

        return RedirectToAction(nameof(Workspace), new
        {
            id,
            tab = CourseWorkspaceTabs.Lessons
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CourseFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await courseService.PopulateCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        var result = await courseService.CreateDraftAsync(
            tutorId,
            model,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            await courseService.PopulateCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Course draft created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var result = await courseService.SubmitForReviewAsync(
            tutorId,
            id,
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded
                ? "Course submitted for Administrator review."
                : result.Error;

        return RedirectToAction(nameof(Workspace), new
        {
            id,
            tab = CourseWorkspaceTabs.Settings
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var model = await courseService.GetEditModelAsync(
            tutorId,
            id,
            cancellationToken);

        if (model is null)
        {
            return NotFound();
        }

        ViewData["SidebarSection"] = "TutorCourses";
        ViewData["CourseId"] = id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        CourseFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await courseService.PopulateCategoriesAsync(model, cancellationToken);
            ViewData["SidebarSection"] = "TutorCourses";
            ViewData["CourseId"] = id;
            return View(model);
        }

        var result = await courseService.UpdateDraftAsync(
            tutorId,
            id,
            model,
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await courseService.PopulateCategoriesAsync(model, cancellationToken);
            ViewData["SidebarSection"] = "TutorCourses";
            ViewData["CourseId"] = id;
            return View(model);
        }

        TempData["SuccessMessage"] = "Course draft updated successfully.";
        return RedirectToAction(nameof(Workspace), new
        {
            id,
            tab = CourseWorkspaceTabs.Settings
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var tutorId))
        {
            return Challenge();
        }

        var result = await courseService.ArchiveAsync(
            tutorId,
            id,
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Course archived." : result.Error;

        return RedirectToAction(nameof(Workspace), new
        {
            id,
            tab = CourseWorkspaceTabs.Settings
        });
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}
