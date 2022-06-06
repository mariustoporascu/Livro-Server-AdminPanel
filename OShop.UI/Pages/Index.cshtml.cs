﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OShop.Application.Categories;
using OShop.Application.FileManager;
using OShop.Application.Orders;
using OShop.Application.ProductInOrders;
using OShop.Application.Products;
using OShop.Database;
using OShop.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OShop.UI.Pages
{
    [Authorize(Roles = "SuperAdmin, Admin")]
    public class IndexModel : PageModel
    {
        private readonly OnlineShopDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            OnlineShopDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public IEnumerable<ProductVMUI> Products { get; set; }

        [BindProperty]
        public IEnumerable<CategoryVMUI> Categ { get; set; }

        [BindProperty]
        public int ProduseVandute { get; set; }
        [BindProperty]
        public int TotalCLienti { get; set; }
        [BindProperty]
        public int Comenzi { get; set; }
        [BindProperty]
        public int Rating { get; set; }
        [BindProperty]
        public decimal TotalVanzari { get; set; }

        public async Task<IActionResult> OnGet()
        {
            if (_signInManager.IsSignedIn(User))
            {
                var startTime = DateTime.UtcNow;
                var user = await _userManager.GetUserAsync(User);
                if (user.CompanieRefId > 0)
                {
                    var orders = await new GetAllOrders(_context, _userManager).Do(user.CompanieRefId);
                    Comenzi = orders.Count();
                    if (Comenzi > 0)
                    {
                        foreach (var order in orders)
                        {
                            var productsInOrder = new GetAllProductInOrder(_context).Do(order.OrderId).Select(po => po.UsedQuantity);
                            foreach (var product in productsInOrder)
                                ProduseVandute += product;
                            TotalVanzari += order.TotalOrdered;
                        }
                        TotalCLienti = orders.Select(or => or.CustomerId).Distinct().Count();
                        var ratings = _context.RatingCompanies.AsNoTracking().AsEnumerable().Where(rat => rat.CompanieRefId == user.CompanieRefId).Select(ra => ra.Rating);
                        decimal sumRating = ratings.Count() * 5.0M;
                        decimal totalRating = 0.0M;
                        foreach (var rat in ratings)
                            totalRating += rat;
                        Rating = (int)Math.Abs(totalRating / sumRating * 100.0M);
                    }
                    else
                    {
                        TotalCLienti = 0;
                        TotalVanzari = 0;
                        Rating = 0;
                    }

                }
                else
                {
                    Comenzi = 0;
                    TotalCLienti = 0;
                    TotalVanzari = 0;
                    Rating = 0;
                }

                var timeEnd = DateTime.UtcNow;
                var runTime = timeEnd.Subtract(startTime).TotalSeconds;
                Console.WriteLine($"Total time : {runTime}");
                return Page();

            }
            return RedirectToPage("/Auth/Login");

        }

    }
}
