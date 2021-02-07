﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OShop.Application.CartItemsA;
using OShop.Application.Products;
using OShop.Application.ShoppingCarts;
using OShop.Database;
using OShop.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OShop.UI.Areas.ShoppingCart.Pages
{
    public class AddToCartModel : PageModel
    {
        private readonly ApplicationDbContext _context;


        public AddToCartModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ProductViewModel Products { get; set; }

        [BindProperty]
        public ShoppingCartViewModel ShoppingCart { get; set; }


        public async Task<IActionResult> OnPost()
        {
            var cartItem = new GetCartItem(_context).Do(ShoppingCart.CartId, Products.ProductId);
            if (cartItem == null)
            {
                await new CreateCartItem(_context).Do(new CartItemsViewModel
                {
                    CartRefId = ShoppingCart.CartId,
                    ProductRefId = Products.ProductId,
                    Quantity = 1,
                });
                await new UpdateShoppingCart(_context).UpdateTotal(ShoppingCart.CartId, 1, Products.Price);
                return RedirectToPage("./Index");
            }
            else
                return RedirectToPage("./Index");
        }
    }
}
