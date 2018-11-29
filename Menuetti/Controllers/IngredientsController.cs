﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Menuetti.Models;
using System.Security.Claims;
using Newtonsoft.Json;
using System.IO;

namespace Menuetti.Controllers
{
    public class IngredientsController : Controller
    {
        private readonly MenuettiDBContext _context;

        public IngredientsController(MenuettiDBContext context)
        {
            _context = context;
        }

        // GET: Ingredients
        public async Task<IActionResult> Index()
        {
            string UserId=null;
            try
            {
                if (User.Claims.Count() > 0)
                {
                    UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
                    ViewBag.UserId = UserId;
                }
                else
                { return View("LoginRequired"); }
                var menuettiDBContext = _context.Ingredients.Include(i => i.Recipe).Where(r => r.Recipe.UserId == UserId);
                return View(await menuettiDBContext.ToListAsync());
            }
            catch (Exception)
            {
                return NotFound();
            }
            
        }

        // GET: Ingredients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ingredients = await _context.Ingredients
                .Include(i => i.Recipe)
                .FirstOrDefaultAsync(m => m.IngredientId == id);
            if (ingredients == null)
            {
                return NotFound();
            }
            if (User.Claims.Count() > 0)
            {
                string UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
                ViewBag.UserId = UserId;
            }

            return View(ingredients);
        }

        // GET: Ingredients/Create
        public IActionResult Create()
        {
            ViewData["RecipeId"] = new SelectList(_context.Recipes, "RecipeId", "RecipeName");
            //ViewData["IngredientName"] = new SelectList(LoadJson(), "name.fi");
            ViewBag.Json = new SelectList(LoadJson(),"name.fi", "name.fi");
            //ViewBag.units = new SelectList(LoadJson(), "units.Select(z=>z.description.fi))", "units.Select(z=>z.description.fi))");
            return View();

        }
        public IActionResult CreateToRecipe(int RecipeId)
        {
            var rec = _context.Recipes.Find(RecipeId);
            ViewBag.RecipeName = rec.RecipeName;
            ViewData["RecipeId"] = new SelectList(_context.Recipes, "RecipeId", "RecipeName");
            ViewBag.Json = new SelectList(LoadJson(), "name.fi", "name.fi");
            return View();
        }

        public List<Ingredientti> LoadJson()
        {
            using (StreamReader r = new StreamReader("ingredients.json"))
            {
                string json = r.ReadToEnd();
                List<Ingredientti> items = JsonConvert.DeserializeObject<List<Ingredientti>>(json);

                return items;

            }
        }


        // POST: Ingredients/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IngredientId,RecipeId,IngredientName,AmountG,RecipeUnit")] Ingredients ingredients)
        {
            if (User.Claims.Count() > 0)
            {
                string UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
                ViewBag.UserId = UserId;
            }
            if (ModelState.IsValid)
            {
                _context.Add(ingredients);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            Recipes recipes = new Recipes();
            string recipe = recipes.RecipeName;
            ViewBag.RecipeName = recipe;
            ViewData["RecipeId"] = new SelectList(_context.Recipes, "RecipeId", "Instructions", ingredients.RecipeId);
            return View(ingredients);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateToRecipe([Bind("IngredientId,RecipeId,IngredientName,AmountG,RecipeUnit")] Ingredients ingredients)
        {
            if (User.Claims.Count() > 0)
            {
                string UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
                ViewBag.UserId = UserId;
            }
            if (ModelState.IsValid)
            {
                _context.Add(ingredients);
                await _context.SaveChangesAsync();
                return RedirectToAction("Edit", "Recipes", new { id = ingredients.RecipeId });
            }
            Recipes recipes = new Recipes();
            string recipe = recipes.RecipeName;
            ViewBag.RecipeName = recipe;
            ViewData["RecipeId"] = new SelectList(_context.Recipes, "RecipeId", "Instructions", ingredients.RecipeId);

            return View(ingredients);
        }

        // GET: Ingredients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ingredient = await _context.Ingredients.FindAsync(id);
            string UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
            if (ingredient == null || ingredient.Recipe.UserId != UserId)
            {
                return NotFound();
            }
            ViewData["RecipeId"] = new SelectList(_context.Recipes, "RecipeId", "RecipeName", ingredient.RecipeId);
            return View(ingredient);
        }

        // POST: Ingredients/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IngredientId,RecipeId,IngredientName,AmountG,RecipeUnit")] Ingredients ingredients)
        {
            if (id != ingredients.IngredientId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ingredients);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IngredientsExists(ingredients.IngredientId))
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
            ViewData["RecipeId"] = new SelectList(_context.Recipes, "RecipeId", "Instructions", ingredients.RecipeId);
            return View(ingredients);
        }

        // GET: Ingredients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ingredient = await _context.Ingredients
                .Include(i => i.Recipe)
                .FirstOrDefaultAsync(m => m.IngredientId == id);
            string UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
            if (ingredient == null || ingredient.Recipe.UserId != UserId)
            {
                return NotFound();
            }

            return View(ingredient);
        }

        // POST: Ingredients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ingredients = await _context.Ingredients.FindAsync(id);
            _context.Ingredients.Remove(ingredients);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IngredientsExists(int id)
        {
            return _context.Ingredients.Any(e => e.IngredientId == id);
        }
    }
}
