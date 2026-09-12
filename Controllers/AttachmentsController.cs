using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Services;

namespace Online_Tuition_Systems.Controllers;

[Authorize]
public class AttachmentsController(ApplicationDbContext db, ICurrentUserService currentUser) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var permitted = db.SubmissionAttachments.Where(x => x.Id == id &&
            (currentUser.IsInRole("Admin") ||
             (x.SurveyAnswer != null && x.SurveyAnswer.SurveyResponse!.UserId == currentUser.UserId) ||
             (x.Complaint != null && (x.Complaint.UserId == currentUser.UserId ||
                 (currentUser.IsInRole("Tutor") && x.Complaint.AssignedTutorId == currentUser.UserId)))));
        var file = await permitted.FirstOrDefaultAsync();
        if (file is null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "private, no-store";
        return File(file.Content, file.ContentType, file.FileName);
    }
}
