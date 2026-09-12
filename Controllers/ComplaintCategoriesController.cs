using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = "Admin")]
public class ComplaintCategoriesController(ApplicationDbContext db) : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Complaints");
    [HttpGet] public IActionResult Create() => View(new ComplaintCategory());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ComplaintCategory model)
    {
        model.Name = model.Name.Trim(); if (await db.ComplaintCategories.AnyAsync(x => x.Name == model.Name)) ModelState.AddModelError(nameof(model.Name), "Category already exists.");
        if (!ModelState.IsValid) return View(model); db.Add(model); await db.SaveChangesAsync(); return RedirectToAction("Index", "Complaints");
    }
    [HttpGet] public async Task<IActionResult> Edit(int id) { var item = await db.ComplaintCategories.FindAsync(id); return item is null ? NotFound() : View(item); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ComplaintCategory model)
    {
        model.Name = model.Name.Trim(); if (await db.ComplaintCategories.AnyAsync(x => x.Name == model.Name && x.Id != model.Id)) ModelState.AddModelError(nameof(model.Name), "Category already exists.");
        if (!ModelState.IsValid) return View(model); var item = await db.ComplaintCategories.FindAsync(model.Id); if (item is null) return NotFound(); item.Name = model.Name; item.IsActive = model.IsActive; await db.SaveChangesAsync(); return RedirectToAction("Index", "Complaints");
    }
}
