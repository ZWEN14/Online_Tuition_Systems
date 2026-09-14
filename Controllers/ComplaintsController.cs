using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.Services;
using Online_Tuition_Systems.ViewModels;

namespace Online_Tuition_Systems.Controllers;

[Authorize]
public class ComplaintsController(ApplicationDbContext db, ICurrentUserService currentUser, SubmissionUploadService uploads) : Controller
{
    // Students see their own complaints; tutors also see assigned complaints.
    public async Task<IActionResult> Index(string? status, string? search)
    {
        var query = db.Complaints.Include(x => x.Category).Include(x => x.User).Include(x => x.AssignedTutor).AsQueryable();
        if (currentUser.IsInRole("Student"))
            query = query.Where(x => x.UserId == currentUser.UserId);
        else if (currentUser.IsInRole("Tutor"))
            query = query.Where(x => x.UserId == currentUser.UserId || x.AssignedTutorId == currentUser.UserId);
        else if (!currentUser.IsInRole("Admin"))
            return Forbid();
        if (Enum.TryParse<ComplaintStatus>(status, out var parsed))
            query = query.Where(x => x.Status == parsed);
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(x => x.Title.Contains(search) || x.Description.Contains(search));
        }
        ViewBag.Status = status;
        ViewBag.Search = search;
        return View(new ComplaintIndexViewModel
        {
            Complaints = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(),
            Categories = currentUser.IsInRole("Admin")
                ? await db.ComplaintCategories.OrderBy(x => x.Name).ToListAsync()
                : []
        });
    }

    [HttpGet, Authorize(Roles = "Student,Tutor")]
    public async Task<IActionResult> Create()
    {
        await LoadCategories();
        return View(new ComplaintCreateViewModel());
    }

    // Photos are optional. Validate any selected files before saving the complaint.
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Student,Tutor"), RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Create(ComplaintCreateViewModel model)
    {
        if (!await db.ComplaintCategories.AnyAsync(x => x.Id == model.CategoryId && x.IsActive))
            ModelState.AddModelError(nameof(model.CategoryId), "Select a valid category.");
        var photos = new List<SubmissionAttachment>();
        if (ModelState.IsValid)
        {
            try
            {
                photos = await uploads.ReadAsync(model.Photos ?? [], true, HttpContext.RequestAborted);
            }
            catch (InvalidDataException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        }
        if (!ModelState.IsValid)
        {
            if (model.Photos?.Count > 0)
                ModelState.AddModelError(string.Empty, "Please select your photos again after correcting the form.");
            await LoadCategories(model.CategoryId);
            return View(model);
        }
        var complaint = new Complaint { Attachments = photos, Title = model.Title.Trim(), Description = model.Description.Trim(), CategoryId = model.CategoryId, UserId = currentUser.UserId };
        complaint.StatusHistory.Add(new ComplaintStatusHistory { Status = ComplaintStatus.Pending, Note = "Complaint submitted.", UpdatedByUserId = currentUser.UserId });
        var adminIds = await db.Users.Where(x => x.Role == UserRole.Admin && x.Id != currentUser.UserId).Select(x => x.Id).ToListAsync();
        db.Notifications.AddRange(adminIds.Select(userId => new UserNotification
        {
            UserId = userId,
            Type = UserNotificationType.ComplaintSubmitted,
            Title = "New complaint",
            Message = $"A new complaint, '{complaint.Title}', needs review.",
            TargetUrl = "/Complaints/Index"
        }));
        db.Add(complaint);
        await db.SaveChangesAsync();
        TempData["Success"] = "Complaint submitted successfully.";
        return RedirectToAction(nameof(Details), new
        {
            id = complaint.Id
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var complaint = await db.Complaints.Include(x => x.Attachments).Include(x => x.Category).Include(x => x.User).Include(x => x.AssignedTutor).Include(x => x.StatusHistory.OrderByDescending(h => h.ChangedAt)).ThenInclude(h => h.UpdatedByUser).FirstOrDefaultAsync(x => x.Id == id);
        if (complaint is null)
            return NotFound();
        if (!CanView(complaint))
            return Forbid();
        return View(complaint);
    }

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Manage(int id)
    {
        var c = await db.Complaints.FindAsync(id);
        if (c is null)
            return NotFound();
        await LoadCategories(c.CategoryId, true);
        await LoadTutors(c.AssignedTutorId);
        return View(new ComplaintManageViewModel { Id = c.Id, CategoryId = c.CategoryId, AssignedTutorId = c.AssignedTutorId, Status = c.Status, ResolutionNotes = c.ResolutionNotes });
    }

    // Admins assign tutors and manage categories, status and resolution notes.
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Manage(ComplaintManageViewModel model)
    {
        if (!await db.ComplaintCategories.AnyAsync(x => x.Id == model.CategoryId))
            ModelState.AddModelError(nameof(model.CategoryId), "Select a valid category.");
        if (model.AssignedTutorId.HasValue && !await db.Users.AnyAsync(x => x.Id == model.AssignedTutorId && x.Role == UserRole.Tutor))
            ModelState.AddModelError(nameof(model.AssignedTutorId), "Select a valid tutor.");
        if (!Enum.IsDefined(model.Status))
            ModelState.AddModelError(nameof(model.Status), "Select a valid status.");
        if (!ModelState.IsValid)
        {
            await LoadCategories(model.CategoryId, true);
            await LoadTutors(model.AssignedTutorId);
            return View(model);
        }
        var c = await db.Complaints.FindAsync(model.Id);
        if (c is null)
            return NotFound();
        var oldStatus = c.Status;
        var oldTutorId = c.AssignedTutorId;
        c.CategoryId = model.CategoryId;
        c.AssignedTutorId = model.AssignedTutorId;
        c.Status = model.Status;
        c.ResolutionNotes = model.ResolutionNotes?.Trim();
        c.UpdatedAt = DateTime.UtcNow;
        if (oldStatus != model.Status)
            AddStatusChange(c, model.Status, model.ResolutionNotes);
        if (model.AssignedTutorId.HasValue && model.AssignedTutorId != oldTutorId)
            db.Notifications.Add(new UserNotification
            {
                UserId = model.AssignedTutorId.Value,
                Type = UserNotificationType.ComplaintAssigned,
                Title = "Complaint assigned to you",
                Message = $"You have been assigned '{c.Title}'.",
                TargetUrl = $"/Complaints/Details/{c.Id}"
            });
        await db.SaveChangesAsync();
        TempData["Success"] = "Complaint updated.";
        return RedirectToAction(nameof(Details), new
        {
            id = c.Id
        });
    }

    // A tutor may update only a complaint assigned to their own user account.
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Tutor")]
    public async Task<IActionResult> TutorUpdateStatus(int id, ComplaintStatus status, string? note)
    {
        var c = await db.Complaints.FirstOrDefaultAsync(x => x.Id == id && x.AssignedTutorId == currentUser.UserId);
        if (c is null)
            return Forbid();
        if (status is not (ComplaintStatus.InProgress or ComplaintStatus.Resolved))
        {
            TempData["Error"] = "Tutors may only use In Progress or Resolved.";
            return RedirectToAction(nameof(Details), new
            {
                id
            });
        }
        if (note?.Length > 1000)
        {
            TempData["Error"] = "The update note cannot exceed 1000 characters.";
            return RedirectToAction(nameof(Details), new
            {
                id
            });
        }
        if (c.Status != status)
        {
            c.Status = status;
            c.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(note))
                c.ResolutionNotes = note.Trim();
            AddStatusChange(c, status, note);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new
        {
            id
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Student,Tutor")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Complaints.FindAsync(id);
        if (c is null)
            return NotFound();
        if (c.UserId != currentUser.UserId || c.Status != ComplaintStatus.Pending)
            return Forbid();
        db.Remove(c);
        await db.SaveChangesAsync();
        TempData["Success"] = "Complaint deleted.";
        return RedirectToAction(nameof(Index));
    }

    // Keep the history entry and notification in the same database save.
    private void AddStatusChange(Complaint c, ComplaintStatus status, string? note)
    {
        db.ComplaintStatusHistories.Add(new ComplaintStatusHistory { ComplaintId = c.Id, Status = status, Note = note?.Trim(), UpdatedByUserId = currentUser.UserId });
        if (c.UserId != currentUser.UserId)
            db.Notifications.Add(new UserNotification { Type = UserNotificationType.ComplaintStatusChanged, Title = "Complaint updated", UserId = c.UserId, Message = $"'{c.Title}' status changed to {FormatStatus(status)}.", TargetUrl = $"/Complaints/Details/{c.Id}" });
    }
    // Keep the role rules explicit so they are easy to explain and change.
    private bool CanView(Complaint complaint)
    {
        if (currentUser.IsInRole("Admin"))
            return true;
        bool isOwner = complaint.UserId == currentUser.UserId;
        if (currentUser.IsInRole("Student"))
            return isOwner;
        if (currentUser.IsInRole("Tutor"))
            return isOwner || complaint.AssignedTutorId == currentUser.UserId;
        return false;
    }
    private async Task LoadCategories(int? selected = null, bool includeInactive = false)
    {
        var q = db.ComplaintCategories.AsQueryable();
        if (!includeInactive)
            q = q.Where(x => x.IsActive);
        ViewBag.CategoryId = new SelectList(await q.OrderBy(x => x.Name).ToListAsync(), "Id", "Name", selected);
    }
    private async Task LoadTutors(int? selected = null) => ViewBag.AssignedTutorId = new SelectList(await db.Users.Where(x => x.Role == UserRole.Tutor).OrderBy(x => x.Name).ToListAsync(), "Id", "Name", selected);
    private static string FormatStatus(ComplaintStatus status) => status == ComplaintStatus.InProgress ? "In Progress" : status.ToString();
}
