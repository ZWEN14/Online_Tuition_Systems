using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AnywhereEdureach.Controllers;

[Authorize]
public class TimeslotController(DB db) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET: Timeslot/Index
    public IActionResult Index()
    {
        if (!User.IsInRole(nameof(UserRole.Tutor))) return Forbid();

        return View(db.Timeslots
            .Where(t => t.TutorId == CurrentUserId)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.StartTime)
            .ToList());
    }

    // GET: Timeslot/Insert
    public IActionResult Insert()
    {
        if (!User.IsInRole(nameof(UserRole.Tutor))) return Forbid();

        ViewBag.ExistingTimeslots = db.Timeslots
            .Where(t => t.TutorId == CurrentUserId)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.StartTime)
            .ToList();

        return View(new TimeslotInsertVM { IsActive = false });
    }

    // POST: Timeslot/Insert
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Insert(TimeslotInsertVM vm)
    {
        if (!User.IsInRole(nameof(UserRole.Tutor))) return Forbid();

        if (vm.EndTime <= vm.StartTime)
            ModelState.AddModelError("EndTime", "The end time must be after the start time.");

        if (vm.StartTime.Minutes % 30 != 0 || vm.EndTime.Minutes % 30 != 0)
            ModelState.AddModelError("StartTime", "Times must fall on a 30-minute block (e.g. 09:00 or 09:30).");

        if (vm.StartTime < TimeSpan.FromHours(8))
            ModelState.AddModelError("StartTime", "Availability cannot start before 8:00 AM.");

        if (vm.EndTime > TimeSpan.FromHours(23))
            ModelState.AddModelError("EndTime", "Availability cannot run past 11:00 PM.");

        var duration = vm.EndTime - vm.StartTime;
        if (duration.TotalMinutes < 60)
            ModelState.AddModelError("EndTime", "The end time must be at least 60 minutes after the start time.");

        if (duration.TotalMinutes > 180)
            ModelState.AddModelError("EndTime", "The end time must be no more than 180 minutes after the start time.");

        var overlaps = db.Timeslots.Any(t =>
            t.TutorId == CurrentUserId &&
            t.DayOfWeek == vm.DayOfWeek &&
            t.StartTime < vm.EndTime &&
            t.EndTime > vm.StartTime);

        if (overlaps)
            ModelState.AddModelError("StartTime", "This overlaps with an existing timeslot on that day.");

        if (!ModelState.IsValid)
        {
            ViewBag.ExistingTimeslots = db.Timeslots
                .Where(t => t.TutorId == CurrentUserId)
                .OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime).ToList();
            return View(vm);
        }

        db.Timeslots.Add(new Timeslot
        {
            TutorId = CurrentUserId,
            DayOfWeek = vm.DayOfWeek,
            StartTime = vm.StartTime,
            EndTime = vm.EndTime,
            IsActive = vm.IsActive
        });

        db.SaveChanges();
        TempData["Info"] = "Timeslot created successfully.";
        return RedirectToAction("Index");
    }

    // POST: Timeslot/Activate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Activate(int id)
    {
        if (!User.IsInRole(nameof(UserRole.Tutor))) return Forbid();

        var slot = db.Timeslots.Find(id);
        if (slot == null) return RedirectToAction("Index");
        if (slot.TutorId != CurrentUserId) return Forbid();

        slot.IsActive = !slot.IsActive;
        db.SaveChanges();

        TempData["Info"] = slot.IsActive
            ? "Timeslot activated successfully."
            : "Timeslot deactivated successfully.";

        if (Request.IsAjax())
        {
            return Json(new
            {
                success = true,
                id = slot.Id,
                isActive = slot.IsActive,
                message = TempData["Info"]?.ToString()
            });
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        if (!User.IsInRole(nameof(UserRole.Tutor))) return Forbid();

        var slot = db.Timeslots.Find(id);
        if (slot == null) return RedirectToAction("Index");
        if (slot.TutorId != CurrentUserId) return Forbid();

        if (db.Bookings.Any(b => b.TimeslotId == id))
        {
            TempData["Info"] = "This slot has booking history and cannot be deleted; deactivate it instead.";
            return RedirectToAction("Index");
        }

        db.Timeslots.Remove(slot);
        db.SaveChanges();
        TempData["Info"] = "Timeslot deleted successfully.";
        return RedirectToAction("Index");
    }
}
