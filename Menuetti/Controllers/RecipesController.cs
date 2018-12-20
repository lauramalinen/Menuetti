﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Menuetti.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Menuetti.Controllers
{
    public class RecipesController : Controller
    {
        private readonly MenuettiDBContext _context;
        
        public RecipesController(MenuettiDBContext context)
        {
            _context = context;
        }

        // GET: Recipes
        public async Task<IActionResult> Index()
        {
            if (User.Claims.Count() > 0)
            {
                string UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
                ViewBag.UserId = UserId;
            }
            var menuettiDBContext = _context.Recipes.Include(r => r.User);
            return View(await menuettiDBContext.ToListAsync());
        }

        // GET: Recipes/UserRecipes

        public async Task<IActionResult> UserRecipes()
        {
            string UserId = null;
            try
            {
                if (User.Claims.Count() > 0)
                {
                    UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
                    ViewBag.UserId = UserId;
                    var menuettiDBContext = _context.Recipes.Where(r => r.UserId == UserId);
                    return View(await menuettiDBContext.ToListAsync());
                }
                else
                { return View("LoginRequired"); }
            }
            catch (Exception)
            {
                return View("NotFound");
            }
            //if (User.Claims.Count() > 0)
            //{
            //    string UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
            //    ViewBag.UserId = UserId;
            //}
            //var menuettiDBContext = _context.Recipes.Include(r => r.User);
            //return View(await menuettiDBContext.ToListAsync());
        }

        // GET: Recipes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return View("NotFound");
            }

            var recipes = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(m => m.RecipeId == id);
            if (recipes == null)
            {
                return View("NotFound");
            }
            ViewBag.NameOfRecipe = recipes.RecipeName;
            return View(recipes);
        }

        // GET: Recipes/Details/5
        public async Task<IActionResult> DetailsCarousel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipes = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(m => m.RecipeId == id);
            if (recipes == null)
            {
                return NotFound();
            }
            ViewBag.CarouselIngredients = recipes.Ingredients.ToList();
            return View(recipes);
        }

        // GET: Recipes/DetailsShoppinglist/5
        public async Task<IActionResult> DetailsShoppinglist(int? id)
        {
            if (id == null)
            {
                return View("NotFound");
            }

            var recipes = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(m => m.RecipeId == id);
            if (recipes == null)
            {
                return View("NotFound");
            }

            return View(recipes);
        }

        // GET: Recipes/Create
        public IActionResult Create()
        {
            if (User.Claims.Count() == 0)
            {
                return View("LoginRequired");
            }
            else
            {
                //ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");
                ViewData["UserId"] = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
                return View();
            }
        }

        // POST: Recipes/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RecipeId,UserId,RecipeName,Portions,Instructions,Time,DietType")] Recipes recipes, string iname, string submitButton)
        {
            if (User.Claims.Count() == 0)
            {
                return View("NotFound");
            }
            else
            {

                if (User.Claims.Count() > 0)
                {
                    string userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
                }
                if (ModelState.IsValid & submitButton == "Lisää uusi raaka-aine reseptiin") //Create a new recipe first to get an ID, then move to the CreateToRecipe of that particular ID
                {
                    if (_context.Users.Find(recipes.UserId) == null)
                    {
                        Users user = new Users() { UserId = recipes.UserId };
                        _context.Users.Add(user);
                    }
                    _context.Add(recipes);
                    await _context.SaveChangesAsync();
                    bool showBadge = ShowBadgeMessage(recipes.DietType, recipes.UserId);
                    if (showBadge)
                    { return RedirectToAction("Profile", "Account"); }
                    return RedirectToAction("CreateToRecipe", "Ingredients", new { RecipeId = recipes.RecipeId });
                }
                else if (ModelState.IsValid & submitButton == "Tallenna resepti") //Create a new recipe and move to the recipe list Index
                {
                    if (_context.Users.Find(recipes.UserId) == null)
                    {
                        Users user = new Users() { UserId = recipes.UserId };
                        _context.Users.Add(user);
                    }
                    _context.Add(recipes);
                    await _context.SaveChangesAsync();
                    bool showBadge = ShowBadgeMessage(recipes.DietType, recipes.UserId);
                    if (showBadge)
                    { return RedirectToAction("Profile", "Account"); }
                    return RedirectToAction(nameof(Index));
                }
                return View(recipes);
            }
        }

        [NonAction]
        private bool ShowBadgeMessage(string DietType, string UserId)
        {
            var userrecipes = _context.Recipes.Where(r => r.UserId == UserId).ToList();
            //Linq for checking the amount of recipes byt DietType added by user
            var result = (from s in userrecipes
                          where s.DietType == DietType
                          select s).Count();
            //Most likely useless code, but here for storage for a while:
            //var resultVegan = (from s in userrecipes
            //                   where s.DietType == "Vegaaninen"
            //                   select s).Count();

            //var resultOmni = (from s in userrecipes
            //                  where s.DietType == "Sekaruoka"
            //                  select s).Count();

            //var resultVegetarian = (from s in userrecipes
            //                        where s.DietType == "Kasvis"
            //                        select s).Count();

            ////ViewsBag for the amount of vegan recipes by user
            //ViewBag.Vegan = resultVegan;
            ////ViewsBag for the amount of omni recipes by user sekaruoka
            //ViewBag.Omni = resultOmni;
            ////ViewsBag for the amount of vegetarian recipes by user kasvis
            //ViewBag.Vegetarian = resultVegetarian;

            if (result == 3)
            { return true; }
            else if (result == 10)
            { return true; }
            else if (result == 20)
            { return true; }
            else
            { return false; }
        }

        // GET: Recipes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return View("NotFound");
            }
            else if (User.Claims.Count() == 0)
            {
                return View("LoginRequired");
            }
            else
            {
                string UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
                var recipes = await _context.Recipes
                    .Include(r => r.User)
                    .Include(r => r.Ingredients)
                    .FirstOrDefaultAsync(m => m.RecipeId == id && m.UserId == UserId);
                if (recipes == null)
                {
                    return View("NoPermission");
                }

                ViewBag.UserId = UserId;
                ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", recipes.UserId);
                return View(recipes);
            }
        }

        // POST: Recipes/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RecipeId,UserId,RecipeName,Portions,Instructions,Time,DietType")] Recipes recipes)
        {
            if (id != recipes.RecipeId)
            {
                return View("NotFound");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recipes);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecipesExists(recipes.RecipeId))
                    {
                        return View("NotFound");
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", recipes.UserId);
            return View(recipes);
        }

        // GET: Recipes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return View("NotFound");
            }
            else if (User.Claims.Count() == 0)
            {
                return View("LoginRequired");
            }
            else 
            {
                string UserId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
                var recipes = await _context.Recipes
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(m => m.RecipeId == id && m.UserId == UserId);
                if (recipes == null)
                {
                    return View("NoPermission");
                }

                return View(recipes);
            }
        }

        // POST: Recipes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var recipes = await _context.Recipes.FindAsync(id);
            _context.Recipes.Remove(recipes);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecipesExists(int id)
        {
            return _context.Recipes.Any(e => e.RecipeId == id);
        }


        public ActionResult<Ingredientti> LoadJson()
        {
            using (StreamReader r = new StreamReader("ingredients.json"))
            {
                string json = r.ReadToEnd();
                List<Ingredientti> items = JsonConvert.DeserializeObject<List<Ingredientti>>(json);
                return View(items);
            }
        }


    }
}
