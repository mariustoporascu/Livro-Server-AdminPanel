﻿using Microsoft.EntityFrameworkCore;
using OShop.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OShop.Application.Orders
{
    public class GetOrder
    {
        private readonly ApplicationDbContext _context;

        public GetOrder(ApplicationDbContext context)
        {
            _context = context;
        }

        public OrderViewModel Do(string customerId, string status)
        {
            var order = _context.Orders.AsNoTracking().FirstOrDefault(order => order.CustomerId == customerId && order.Status == status);
            if (order == null)
                return null;
            else
                return new OrderViewModel
                {
                    OrderId = order.OrderId,
                    Status = order.Status,
                    CustomerId = order.CustomerId,
                    TotalOrdered = order.TotalOrdered,
                    Created = order.Created,
                };
        }

        public OrderViewModel Do(int orderId, string status)
        {
            var order = _context.Orders.AsNoTracking().FirstOrDefault(order => order.OrderId == orderId && order.Status == status);
            if (order == null)
                return null;
            else
                return new OrderViewModel
                {
                    OrderId = order.OrderId,
                    Status = order.Status,
                    CustomerId = order.CustomerId,
                    TotalOrdered = order.TotalOrdered,
                    Created = order.Created,
                };
        }
    }
}