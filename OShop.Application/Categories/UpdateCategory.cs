﻿using OShop.Application.FileManager;
using OShop.Database;
using OShop.Domain.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OShop.Application.Categories
{
    public class UpdateCategory
    {
        private readonly OnlineShopDbContext _context;

        public UpdateCategory(OnlineShopDbContext context)
        {
            _context = context;
        }

        public async Task Do(CategoryVMUI vm)
        {
            var category = new Category
            {
                CategoryId = vm.CategoryId,
                Name = vm.Name,
                Photo = vm.Photo,
                CompanieRefId = vm.CompanieRefId,
            };
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }


    }
}
