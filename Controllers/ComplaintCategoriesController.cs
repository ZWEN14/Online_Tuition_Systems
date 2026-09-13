using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = "Admin")]
public class ComplaintCategoriesController(ApplicationDbContext db) : Controller
{
    // Category management is displayed on the complaint index.
    public IActionResult Index() => RedirectToAction("Index", "Complaints");

    // Delete unused categories only; existing complaints must keep their category.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.ComplaintCategories.FindAsync(id);
        if (category is null)
            return NotFound();

        if (await db.Complaints.AnyAsync(complaint => complaint.CategoryId == id))
        {
            TempData["Error"] = "This category is used by complaints. Deactivate it instead.";
            return RedirectToAction("Index", "Complaints");
        }

        db.ComplaintCategories.Remove(category);
        try
        {
            await db.SaveChangesAsync();
            TempData["Success"] = "Category deleted.";
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
        {
            // A complaint may have been submitted after the check above.
            TempData["Error"] = "This category is now in use. Deactivate it instead.";
        }
        return RedirectToAction("Index", "Complaints");
    }

    [HttpGet] public IActionResult Create() => View(new ComplaintCategory());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ComplaintCategory model)
    {
        // Empty input must return validation errors instead of calling Trim on null.
        model.Name = model.Name?.Trim() ?? string.Empty;
        if (await db.ComplaintCategories.AnyAsync(x => x.Name == model.Name))
            ModelState.AddModelError(nameof(model.Name), "Category already exists.");
        if (!ModelState.IsValid)
            return View(model);
        db.Add(model);
        await db.SaveChangesAsync();
        return RedirectToAction("Index", "Complaints");
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.ComplaintCategories.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ComplaintCategory model)
    {
        // Empty input must return validation errors instead of calling Trim on null.
        model.Name = model.Name?.Trim() ?? string.Empty;
        if (await db.ComplaintCategories.AnyAsync(x => x.Name == model.Name && x.Id != model.Id))
            ModelState.AddModelError(nameof(model.Name), "Category already exists.");
        if (!ModelState.IsValid)
            return View(model);
        var item = await db.ComplaintCategories.FindAsync(model.Id);
        if (item is null)
            return NotFound();
        item.Name = model.Name;
        item.IsActive = model.IsActive;
        await db.SaveChangesAsync();
        return RedirectToAction("Index", "Complaints");
    }
}
