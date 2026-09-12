using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Online_Tuition_Systems.Services.Billing;

namespace AnywhereEdureach.Controllers;

[Authorize]
public class BookingController(
    ApplicationDbContext db,
    NotificationService ns,
    IStripeCheckoutService stripeCheckoutService) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET: Booking/Index
    public IActionResult Index()
    {
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";

        var userId = CurrentUserId;

        var model = db.Bookings
            .Where(b => b.StudentId == userId || b.TutorId == userId)
            .Include(b => b.Student)
            .Include(b => b.Tutor)
            .Include(b => b.Subject)
            .Include(b => b.Timeslot)
            .OrderByDescending(b => b.CreatedAt)
            .ToList();

        return View(model);
    }

    // GET: Booking/Create?subjectId=&tutorId=&timeslotId=
    public IActionResult Create(int? subjectId, int? tutorId, int? timeslotId)
    {
        if (!User.IsInRole(nameof(UserRole.Student))) return Forbid();

        var tutorsQuery = db.Users.Where(u => u.Role == UserRole.Tutor);

        if (subjectId.HasValue)
        {
            tutorsQuery = tutorsQuery.Where(u =>
                u.TutorSubjects.Any(ts => ts.SubjectId == subjectId.Value));
        }

        var vm = new BookingCreateVM
        {
            Tutors = tutorsQuery
                .Include(u => u.TutorSubjects)
                .ThenInclude(ts => ts.Subject)
                .ToList(),
            SelectedSubjectId = subjectId,
            SelectedSubject = subjectId.HasValue ?
                db.Subjects.Find(subjectId.Value) : null,
            SelectedTutorId = tutorId,
            SelectedTimeslotId = timeslotId,
        };

        if (tutorId.HasValue)
        {
            vm.Timeslots = db.Timeslots
                .Where(t => t.TutorId == tutorId.Value && t.IsActive)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToList();
        }

        if (timeslotId.HasValue)
        {
            var timeslot = db.Timeslots
                .Include(t => t.Tutor)
                .FirstOrDefault(t => t.Id == timeslotId.Value);

            if (timeslot != null)
            {
                vm.AvailableDates = GetUpcomingDates(timeslot, 2);
            }
        }

        return View(vm);
    }

    // POST: Booking/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(BookingInsertVM vm)
    {
        if (!User.IsInRole(nameof(UserRole.Student)))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
            return RedirectToAction("Create", new
            {
                subjectId = vm.SubjectId,
                tutorId = vm.TutorId,
                timeslotId = vm.TimeslotId
            });

        var tutor = db.Users
            .Include(u => u.TutorSubjects)
            .FirstOrDefault(u => u.Id == vm.TutorId && u.Role == UserRole.Tutor);

        if (tutor == null)
        {
            return BookingError("The selected user is not a tutor.", vm);
        }

        var subject = db.Subjects.Find(vm.SubjectId);
        if (subject == null)
        {
            return BookingError("The selected subject does not exist.", vm);
        }

        if (!tutor.TutorSubjects.Any(ts => ts.SubjectId == vm.SubjectId))
        {
            return BookingError("The selected tutor does not teach this subject.", vm);
        }

        var timeslot = db.Timeslots.FirstOrDefault(t =>
            t.Id == vm.TimeslotId && t.TutorId == tutor.Id && t.IsActive);

        if (timeslot == null)
        {
            return BookingError("The selected timeslot does not belong to this tutor or is inactive.", vm);
        }

        var bookingDate = vm.BookingDate.Date;

        if (bookingDate < DateTime.Today)
        {
            return BookingError("The booking date cannot be in the past.", vm);
        }

        if ((int)bookingDate.DayOfWeek != timeslot.DayOfWeek)
        {
            return BookingError("Selected date does not match chosen timeslot.", vm);
        }

        var start = bookingDate + timeslot.StartTime;
        var end = bookingDate + timeslot.EndTime;
        var durationHours = (decimal)(end - start).TotalHours;

        if (durationHours <= 0)
        {
            return BookingError("The selected timeslot has an invalid duration.", vm);
        }

        var finalCost = Math.Round(subject.BaseCost * durationHours, 2);

        var recentCancellation = db.Bookings.Any(b =>
            b.StudentId == CurrentUserId &&
            b.TimeslotId == timeslot.Id &&
            b.BookingDate == bookingDate &&
            b.Status == BookingStatus.Cancelled &&
            b.CancelledAt != null &&
            b.CancelledAt >= DateTime.Now.AddDays(-1));

        if (recentCancellation)
        {
            return BookingError("You cannot book this timeslot again within 24 hours after cancelling it.", vm);
        }

        var hasOverlappingBooking = db.Bookings
            .Include(b => b.Timeslot)
            .Any(b => b.Student.Id == CurrentUserId &&
                 b.BookingDate == bookingDate &&
                 (b.Status == BookingStatus.Pending ||
                  b.Status == BookingStatus.Accepted ||
                  b.Status == BookingStatus.Paid) &&
                  b.Timeslot!.StartTime < timeslot.EndTime &&
                  b.Timeslot.EndTime > timeslot.StartTime);

        if (hasOverlappingBooking)
        {
            return BookingError("You already have another booking uring this time. Please choose a different time slot.", vm);
        }

        Booking? booking = null;
        var alreadyBookedDuringTransaction = false;
        var executionStrategy = db.Database.CreateExecutionStrategy();

        executionStrategy.Execute(() =>
        {
            // A retry must start with a clean tracker and a new transaction.
            db.ChangeTracker.Clear();
            booking = null;
            alreadyBookedDuringTransaction = false;

            using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);

            alreadyBookedDuringTransaction = db.Bookings.Any(b =>
                b.TimeslotId == timeslot.Id &&
                b.BookingDate == bookingDate &&
                (b.Status == BookingStatus.Pending ||
                 b.Status == BookingStatus.Accepted ||
                 b.Status == BookingStatus.Paid));

            if (alreadyBookedDuringTransaction)
            {
                transaction.Rollback();
                return;
            }

            booking = new Booking
            {
                StudentId = CurrentUserId,
                TutorId = vm.TutorId,
                SubjectId = vm.SubjectId,
                TimeslotId = vm.TimeslotId,
                BookingDate = vm.BookingDate.Date,
                Cost = finalCost,
                Status = BookingStatus.Pending,
                PaymentStatus = "Unpaid"
            };

            db.Bookings.Add(booking);
            db.SaveChanges();
            transaction.Commit();
        });

        if (alreadyBookedDuringTransaction || booking is null)
        {
            return BookingError("This timeslot is already booked", vm);
        }

        ns.NotifyBookingCreated(booking);

        return RedirectToAction("Show", new { id = booking.Id });
    }

    // GET: Booking/Show/5
    public IActionResult Show(int id)
    {
        var booking = db.Bookings
            .Include(b => b.Student)
            .Include(b => b.Tutor)
            .Include(b => b.Subject)
            .Include(b => b.Timeslot)
            .FirstOrDefault(b => b.Id == id);

        if (booking == null) return RedirectToAction("Index");
        if (CurrentUserId != booking.StudentId &&
            CurrentUserId != booking.TutorId)
        {
            return Forbid();
        }

        if (booking.Status == BookingStatus.Pending && IsTutorDecisionExpired(booking))
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.Now;
            db.SaveChanges();
            ns.NotifyBookingStatusChanged(booking);
        }

        if (IsPaymentExpired(booking))
        {
            booking.Status = BookingStatus.Cancelled;
            booking.PaymentStatus = "failed";
            booking.CancelledAt = DateTime.Now;
            db.SaveChanges();
            ns.NotifyBookingStatusChanged(booking);
        }

        ViewBag.StripeAvailable = stripeCheckoutService.IsConfigured;
        return View(booking);
    }

    // POST: Booking/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStatus(int id, BookingStatus status)
    {
        var booking = db.Bookings
            .Include(b => b.Timeslot)
            .FirstOrDefault(b => b.Id == id);

        if (booking == null) return RedirectToAction("Index");

        if (status == BookingStatus.Accepted || status == BookingStatus.Rejected)
        {
            if (CurrentUserId != booking.TutorId || booking.Status != BookingStatus.Pending)
                return Forbid();

            if (IsTutorDecisionExpired(booking))
                return AutoCancel(id, "The tutor response deadline has passed. The booking has been automatically cancelled.");

            if (IsPaymentExpired(booking))
                return AutoCancel(id, "The payment deadline has passed. The booking has been automatically cancelled.");

            if (status == BookingStatus.Accepted)
            {
                booking.Status = BookingStatus.Accepted;
                booking.PaymentStatus = "unpaid";
                booking.PaymentDeadline = booking.BookingDate.Date.AddDays(-1);
            }
            else
            {
                booking.Status = BookingStatus.Rejected;
                booking.PaymentStatus = "unpaid";
                booking.PaymentDeadline = null;
            }
        }
        else if (status == BookingStatus.Cancelled)
        {
            if (CurrentUserId != booking.StudentId && CurrentUserId != booking.TutorId)
                return Forbid();

            if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Accepted)
                return Forbid();

            if (booking.PaymentStatus == "paid")
                return StatusError(id, "Paid bookings cannot be cancelled.");

            // Match the Laravel policy: cancellation is not allowed within 24 hours.
            var sessionStart = booking.BookingDate.Date + booking.Timeslot!.StartTime;
            if ((sessionStart - DateTime.Now).TotalHours < 24)
                return StatusError(id, "Bookings cannot be cancelled less than 24 hours before the scheduled session.");

            booking.CancelledAt = DateTime.Now;
            booking.Status = BookingStatus.Cancelled;
        }
        else
        {
            return BadRequest();
        }

        booking.UpdatedAt = DateTime.Now;
        db.SaveChanges();
        ns.NotifyBookingStatusChanged(booking);

        if (Request.IsAjax())
        {
            return Json(new { success = true, status = booking.Status.ToString(), message = $"Booking {booking.Status.ToString().ToLowerInvariant()}." });
        }

        return RedirectToAction("Show", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmCompletion(int id)
    {
        var booking = db.Bookings.Include(b => b.Timeslot).FirstOrDefault(b => b.Id == id);
        if (booking == null) return RedirectToAction("Index");

        if (booking.Status != BookingStatus.Paid)
            return StatusError(id, "Only paid bookings can be confirmed as completed.");

        var sessionEnd = booking.BookingDate.Date + booking.Timeslot!.EndTime;
        if (sessionEnd > DateTime.Now)
            return StatusError(id, "Session completion can only be confirmed after the scheduled session has ended.");

        if (CurrentUserId == booking.StudentId)
        {
            if (booking.StudentConfirmedAt.HasValue) return Forbid();
            booking.StudentConfirmedAt = DateTime.Now;
        }
        else if (CurrentUserId == booking.TutorId)
        {
            if (booking.TutorConfirmedAt.HasValue) return Forbid();
            booking.TutorConfirmedAt = DateTime.Now;
        }
        else return Forbid();

        if (booking.StudentConfirmedAt.HasValue && booking.TutorConfirmedAt.HasValue)
            booking.Status = BookingStatus.Completed;

        booking.UpdatedAt = DateTime.Now;
        db.SaveChanges();

        if (booking.Status == BookingStatus.Completed)
            ns.NotifyBookingStatusChanged(booking);

        TempData["Info"] = "Thanks for confirming.";
        if (Request.IsAjax())
        {
            return Json(new { success = true, status = booking.Status.ToString(), message = TempData["Info"]?.ToString() });
        }

        return RedirectToAction("Show", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Payment(int id, CancellationToken cancellationToken)
    {
        var booking = db.Bookings.FirstOrDefault(item => item.Id == id);
        if (booking == null) return RedirectToAction("Index");
        if (CurrentUserId != booking.StudentId) return Forbid();

        if (!stripeCheckoutService.IsConfigured)
            return StatusError(id, "Stripe test mode is not configured.");

        if (booking.Status != BookingStatus.Accepted || booking.PaymentStatus == "paid")
            return StatusError(id, "This booking is not available for payment.");

        var successUrl = Url.Action(
            nameof(PaymentSuccess), "Booking", new { id }, Request.Scheme);
        var cancelUrl = Url.Action(
            nameof(PaymentCancelled), "Booking", new { id }, Request.Scheme);
        if (string.IsNullOrWhiteSpace(successUrl) || string.IsNullOrWhiteSpace(cancelUrl))
            return StatusError(id, "Stripe return URLs could not be created.");

        successUrl += successUrl.Contains('?')
            ? "&session_id={CHECKOUT_SESSION_ID}"
            : "?session_id={CHECKOUT_SESSION_ID}";
        var result = await stripeCheckoutService.CreateBookingCheckoutAsync(
            CurrentUserId, id, successUrl, cancelUrl, cancellationToken);

        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.CheckoutUrl))
            return StatusError(id, result.Message ?? "Unable to start Stripe payment.");

        return Redirect(result.CheckoutUrl);
    }

    [HttpGet]
    public async Task<IActionResult> PaymentSuccess(
        int id,
        [FromQuery(Name = "session_id")] string sessionId,
        CancellationToken cancellationToken)
    {
        var result = await stripeCheckoutService.CompleteBookingCheckoutAsync(
            CurrentUserId, id, sessionId, cancellationToken);
        TempData["Info"] = result.Message;
        return RedirectToAction(nameof(Show), new { id });
    }

    [HttpGet]
    public IActionResult PaymentCancelled(int id)
    {
        TempData["Info"] = "Stripe Checkout was cancelled. No payment was recorded.";
        return RedirectToAction(nameof(Show), new { id });
    }

    private IActionResult BookingError(string message, BookingInsertVM vm)
    {
        TempData["Info"] = message;
        return RedirectToAction("Create", new
        {
            subjectId = vm.SubjectId,
            tutorId = vm.TutorId,
            timeslotId = vm.TimeslotId,
        });
    }
    private IActionResult StatusError(int id, string message)
    {
        TempData["Info"] = message;
        return RedirectToAction("Show", new { id });
    }

    private IActionResult AutoCancel(int id, string message)
    {
        var booking = db.Bookings.Find(id);
        if (booking != null && booking.Status != BookingStatus.Cancelled)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.Now;
            booking.PaymentStatus = "failed";
            db.SaveChanges();
            ns.NotifyBookingStatusChanged(booking);
        }
        return StatusError(id, message);
    }

    private bool IsTutorDecisionExpired(Booking booking) =>
        DateTime.Now >= booking.BookingDate.Date.AddDays(-1);

    private bool IsPaymentExpired(Booking booking) =>
        booking.Status == BookingStatus.Accepted &&
        booking.PaymentStatus != "Paid" &&
        booking.PaymentDeadline.HasValue &&
        DateTime.Now >= booking.PaymentDeadline.Value;

    // Ported from the original Laravel Timeslot::upcomingDates() logic:
    // finds the next `weeks` occurrences of this timeslot's weekday that
    // are not already booked (pending or accepted).
    private List<DateTime> GetUpcomingDates(Timeslot timeslot, int weeks)
    {
        var bookedDates = db.Bookings
            .Where(b => b.TimeslotId == timeslot.Id &&
                        (b.Status == BookingStatus.Pending ||
                         b.Status == BookingStatus.Accepted ||
                         b.Status == BookingStatus.Paid))
            .Select(b => b.BookingDate.Date)
            .ToList();

        var dates = new List<DateTime>();
        var cursor = DateTime.Today;
        var endDate = cursor.AddDays(weeks * 7);

        while (cursor <= endDate)
        {
            if ((int)cursor.DayOfWeek == timeslot.DayOfWeek &&
                cursor > DateTime.Today &&
                !bookedDates.Contains(cursor.Date))
                dates.Add(cursor);

            cursor = cursor.AddDays(1);
        }

        return dates;
    }
}
