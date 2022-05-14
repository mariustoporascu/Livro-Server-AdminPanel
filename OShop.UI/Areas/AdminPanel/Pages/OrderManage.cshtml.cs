using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OShop.Application.Orders;
using OShop.Database;
using OShop.Domain.Models;

using System.Collections.Generic;
using System.Linq;

namespace OShop.UI.Areas.AdminPanel.Pages
{
    [Authorize(Roles = "SuperAdmin, Admin")]
    public class OrderManageModel : PageModel
    {
        private readonly OnlineShopDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderManageModel(OnlineShopDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public IEnumerable<OrderViewModel> Orders { get; set; }


        [BindProperty]
        public IEnumerable<ApplicationUser> UsersVM { get; set; }

        public async void OnGet()
        {

            Orders = await new GetAllOrders(_context, _userManager).Do();
            UsersVM = _userManager.Users.AsNoTracking().AsEnumerable();
        }
    }
}