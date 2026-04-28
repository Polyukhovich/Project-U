using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectU.Core.Models;
using ProjectU.Data;

namespace Project_U
{
    public class LabWorksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LabWorksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LabWorks
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.LabWorks.Include(l => l.Course).Include(l => l.Student);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: LabWorks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var labWork = await _context.LabWorks
                .Include(l => l.Course)
                .Include(l => l.Student)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (labWork == null)
            {
                return NotFound();
            }

            return View(labWork);
        }

        // GET: LabWorks/Create
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Description");
            ViewData["StudentId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: LabWorks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Content,UploadedAt,StudentId,CourseId")] LabWork labWork)
        {
            if (ModelState.IsValid)
            {
                _context.Add(labWork);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Description", labWork.CourseId);
            ViewData["StudentId"] = new SelectList(_context.Users, "Id", "Id", labWork.StudentId);
            return View(labWork);
        }

        // GET: LabWorks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var labWork = await _context.LabWorks.FindAsync(id);
            if (labWork == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Description", labWork.CourseId);
            ViewData["StudentId"] = new SelectList(_context.Users, "Id", "Id", labWork.StudentId);
            return View(labWork);
        }

        // POST: LabWorks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,UploadedAt,StudentId,CourseId")] LabWork labWork)
        {
            if (id != labWork.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(labWork);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LabWorkExists(labWork.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Description", labWork.CourseId);
            ViewData["StudentId"] = new SelectList(_context.Users, "Id", "Id", labWork.StudentId);
            return View(labWork);
        }

        // GET: LabWorks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var labWork = await _context.LabWorks
                .Include(l => l.Course)
                .Include(l => l.Student)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (labWork == null)
            {
                return NotFound();
            }

            return View(labWork);
        }

        // POST: LabWorks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var labWork = await _context.LabWorks.FindAsync(id);
            if (labWork != null)
            {
                _context.LabWorks.Remove(labWork);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LabWorkExists(int id)
        {
            return _context.LabWorks.Any(e => e.Id == id);
        }
    }
}
