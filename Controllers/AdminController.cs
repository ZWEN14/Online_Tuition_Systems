using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Runtime.Intrinsics.X86;

namespace AnywhereEdureach.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(ApplicationDbContext db, Helper hp) : Controller
{
    // GET: Admin/Index
    public IActionResult Index(string? query, string sort = "name", int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var usersQuery = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.Trim();
            usersQuery = usersQuery.Where(u => u.Name.Contains(query) || u.Email.Contains(query) || u.Role.ToString().Contains(query));
        }

        usersQuery = sort.ToLowerInvariant() switch
        {
            "email" => usersQuery.OrderBy(u => u.Email),
            "role" => usersQuery.OrderBy(u => u.Role).ThenBy(u => u.Name),
            "status" => usersQuery.OrderBy(u => u.IsBlocked).ThenBy(u => u.Name),
            _ => usersQuery.OrderBy(u => u.Name),
        };

        var totalCount = usersQuery.Count();
        var users = usersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var tutorRatings = db.Tutors.ToDictionary(t => t.UserId, t => t.Rating);
        var studentLevels = db.Students.ToDictionary(s => s.UserId, s => s.EducationLevel);

        var model = new AdminUsersVM
        {
            Query = query,
            Sort = sort,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Users = users.Select(u => new UserListVM
            {
                User = u,
                ExtraInfo = u.Role == UserRole.Tutor
                    ? $"{(tutorRatings.TryGetValue(u.Id, out var rating) ? rating : 0):0.00} rating"
                    : u.Role == UserRole.Student
                        ? (studentLevels.TryGetValue(u.Id, out var level) ? level : "")
                        : ""
            }).ToList()
        };

        return Request.IsAjax() ? PartialView("_UserTable", model) : View(model);
    }

    // GET: Admin/Insert
    public IActionResult Insert()
    {
        ViewBag.EducationLevel = new SelectList(Helper.EducationLevels);
        return View(new UserInsertVM());
    }

    // POST: Admin/Insert
    [HttpPost]
    public IActionResult Insert(UserInsertVM vm)
    {
        if (ModelState.IsValid("Email") && db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        if (vm.Role == UserRole.Student && string.IsNullOrWhiteSpace(vm.EducationLevel))
        {
            ModelState.AddModelError("EducationLevel", "Education level is required.");
        }

        if (ModelState.IsValid)
        {
            var u = new User
            {
                Name = vm.Name,
                Email = vm.Email,
                Hash = hp.HashPassword(vm.Password),
                Role = vm.Role,
            };
            db.Users.Add(u);
            db.SaveChanges();

            if (u.Role == UserRole.Tutor)
            {
                db.Tutors.Add(new()
                {
                    UserId = u.Id,
                    Rating = 0,
                });
                db.SaveChanges();
            }

            if (u.Role == UserRole.Student)
            {
                db.Students.Add(new()
                {
                    UserId = u.Id,
                    EducationLevel = vm.EducationLevel ?? string.Empty,
                });
                db.SaveChanges();
            }

            TempData["Info"] = "User Inserted.";
            return RedirectToAction("Index");
        }

        return View(vm);
    }

    // GET: Admin/Update
    public IActionResult Update(int id)
    {
        var u = db.Users.Find(id);
        if (u == null)
        {
            return RedirectToAction("Index");
        }

        var vm = new UserUpdateVM
        {
            Id = u.Id,
            Email = u.Email,
            Role = u.Role,
            Name = u.Name,
        };

        if (u.Role == UserRole.Tutor)
        {
            vm.Rating = db.Tutors.Where(t => t.UserId == u.Id).Select(t => t.Rating).FirstOrDefault();
        }

        if (u.Role == UserRole.Student)
        {
            var student = db.Students.FirstOrDefault(s => s.UserId == u.Id);
            vm.EducationLevel = student?.EducationLevel;
            ViewBag.EducationLevel = new SelectList(Helper.EducationLevels, vm.EducationLevel);
        }

        return View(vm);
    }

    // POST: Admin/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int id, [Bind("Id,Name,Rating,EducationLevel")] UserUpdateVM vm)
    {
        id = id > 0 ? id : vm.Id;
        var u = db.Users.Find(id);
        if (u == null)
        {
            return RedirectToAction("Index");
        }


        ModelState.Remove(nameof(vm.Id));
        ModelState.Remove(nameof(vm.Email));
        ModelState.Remove(nameof(vm.Role));
        if (string.IsNullOrWhiteSpace(vm.Name))
        {
            ModelState.AddModelError(nameof(vm.Name), "Name is required.");
        }

        if (ModelState.IsValid)
        {
            var name = vm.Name.Trim();
            var affectedRows = db.Users
                .Where(user => user.Id == id)
                .ExecuteUpdate(setters => setters.SetProperty(user => user.Name, name));

            if (affectedRows != 1)
            {
                TempData["Info"] = "The user name was not changed because the user record could not be updated.";
                return RedirectToAction(nameof(Index));
            }

            if (u.Role == UserRole.Tutor && vm.Rating.HasValue)
            {
                var tutor = db.Tutors.FirstOrDefault(t => t.UserId == id);
                if (tutor == null)
                {
                    db.Tutors.Add(new Tutor { UserId = id, Rating = vm.Rating.Value });
                }
                else
                {
                    tutor.Rating = vm.Rating.Value;
                }
            }

            if (u.Role == UserRole.Student)
            {
                if (string.IsNullOrWhiteSpace(vm.EducationLevel))
                {
                    ModelState.AddModelError(nameof(vm.EducationLevel), "Education level is required.");
                    vm.Id = u.Id;
                    vm.Email = u.Email;
                    vm.Role = u.Role;
                    ViewBag.EducationLevel = new SelectList(Helper.EducationLevels, vm.EducationLevel);
                    return View(vm);
                }

                var student = db.Students.FirstOrDefault(s => s.UserId == id);
                if (student == null)
                {
                    db.Students.Add(new Student { UserId = id, EducationLevel = vm.EducationLevel });
                }
                else
                {
                    student.EducationLevel = vm.EducationLevel;
                }
            }

            db.SaveChanges();

            TempData["Info"] = "User updated.";
            return RedirectToAction(nameof(Index));
        }
        vm.Id = u.Id;
        vm.Email = u.Email;
        return View(vm);
    }

    // POST: Admin/Delete
    [HttpPost]
    public IActionResult Delete(int id)
    {
        var u = db.Users.Find(id);
        if (u == null)
        {
            return RedirectToAction("Index");
        }

        try
        {
            db.Users.Remove(u);
            db.SaveChanges();

            TempData["Info"] = "User Deleted";
        }
        catch (DbUpdateException ex)
        {
            throw ex;
            TempData["Info"] = "User Deleted";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult ToggleBlock(int id)
    {
        var u = db.Users.Find(id);
        if (u == null) return NotFound();
        u.IsBlocked = !u.IsBlocked;
        u.LockoutEnd = u.IsBlocked ? DateTime.UtcNow.AddYears(10) : null;
        db.SaveChanges();
        return Request.IsAjax() ? Json(new { success = true, blocked = u.IsBlocked, reload = true }) : RedirectToAction(nameof(Index));
    }
}
