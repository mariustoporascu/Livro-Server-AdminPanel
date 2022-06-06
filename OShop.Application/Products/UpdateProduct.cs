﻿using OShop.Application.FileManager;
using OShop.Database;
using OShop.Domain.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OShop.Application.Products
{
    public class UpdateProduct
    {
        private readonly OnlineShopDbContext _context;

        public UpdateProduct(OnlineShopDbContext context)
        {
            _context = context;
        }

        public async Task Do(ProductVMUI vm)
        {
            var product = new Product
            {
                ProductId = vm.ProductId,
                Name = vm.Name,
                Description = vm.Description,
                Stock = vm.Stock,
                Price = vm.Price,
                Photo = vm.Photo,
                Gramaj = vm.Gramaj,
                MeasuringUnitId = vm.MeasuringUnitId,
                SubCategoryRefId = vm.SubCategoryRefId,
            };
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateStockAfterOrder(int productId, int usedQuantity)
        {
            var product = _context.Products.FirstOrDefault(product => product.ProductId == productId);
            product.Stock -= usedQuantity;
            await _context.SaveChangesAsync();
        }
    }
}
