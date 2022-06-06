﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OShop.Application.Orders;
using OShop.Application.ProductInOrders;
using OShop.Database;
using OShop.Domain.Models;
using OShop.UI.ApiAuthManage;
using OShop.UI.Extras;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OShop.UI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class FoodAppManageController : ControllerBase
    {
        private readonly OnlineShopDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly int maxAllowedOrdersForDriver;
        private readonly string OneSignalApiKey;
        private readonly string OneSignalAppId;


        public FoodAppManageController(IConfiguration config, OnlineShopDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            OneSignalApiKey = config["ConnectionStrings:SignalApiKey"];
            OneSignalAppId = config["ConnectionStrings:SignalAppId"];
            maxAllowedOrdersForDriver = int.Parse(config["ConnectionStrings:MaxNrOrdersDriver"]);
        }
        [Authorize]
        [HttpGet("getalldriverorders")]
        public async Task<IActionResult> GetAllOrders() =>
            Ok(await new GetAllOrders(_context, _userManager).Do());
        [Authorize]
        [HttpGet("getallrestaurantorders/{restaurantRefId}")]
        public async Task<IActionResult> GetAllOrders(int restaurantRefId) =>
            Ok(await new GetAllOrders(_context, _userManager).Do(restaurantRefId));
        [Authorize]
        [HttpGet("updatestatus/{orderId}&{status}&{isOwner}")]
        public async Task<IActionResult> OrderStatus(int orderId, string status, bool isOwner)
        {

            if (await new UpdateOrder(_context).Do(orderId, status))
            {
                if (status == "In pregatire")
                {
                    var drivers = _userManager.Users.Where(us => us.IsDriver == true).ToList();
                    foreach (var driver in drivers)
                    {
                        var driverToken = _context.FBTokens.AsNoTracking().Where(tkn => tkn.UserId == driver.Id)
                        .Select(tkn => tkn.FBToken).Distinct().ToList();
                        if (driverToken != null)
                        {
                            foreach (var token in driverToken)
                                NotificationSender.SendNotif(OneSignalApiKey, OneSignalAppId, token, $"A aparut o noua comanda fara livrator!");
                        }
                    }
                }
                var orderVM = _context.Orders.AsNoTracking().FirstOrDefault(or => or.OrderId == orderId);

                if (!isOwner)
                {
                    var restaurant = _userManager.Users.FirstOrDefault(us => us.CompanieRefId == orderVM.CompanieRefId);
                    if (restaurant != null)
                    {
                        var restaurantToken = _context.FBTokens.AsNoTracking().Where(tkn => tkn.UserId == restaurant.Id)
                            .Select(tkn => tkn.FBToken).Distinct().ToList();
                        if (restaurantToken != null)
                        {
                            foreach (var token in restaurantToken)
                                NotificationSender.SendNotif(OneSignalApiKey, OneSignalAppId, token, $"Statusul comenzii {orderId} a fost schimbat in {status}!");
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(orderVM.DriverRefId))
                {
                    var driverToken = _context.FBTokens.AsNoTracking().Where(tkn => tkn.UserId == orderVM.DriverRefId)
                        .Select(tkn => tkn.FBToken).Distinct().ToList();
                    if (driverToken != null)
                    {
                        foreach (var token in driverToken)
                            NotificationSender.SendNotif(OneSignalApiKey, OneSignalAppId, token, $"Statusul comenzii {orderId} a fost schimbat in {status}!");
                    }
                }
                if (!orderVM.TelephoneOrdered)
                {
                    var userToken = _context.FBTokens.AsNoTracking().Where(tkn => tkn.UserId == orderVM.CustomerId)
                        .Select(tkn => tkn.FBToken).Distinct().ToList();
                    if (userToken != null)
                    {
                        foreach (var token in userToken)
                            NotificationSender.SendNotif(OneSignalApiKey, OneSignalAppId, token, status.Contains("In curs de livrare") ? $"Comanda {orderId} este in curs de livrare catre tine, o poti urmari pe harta."
                                : $"Statusul comenzii {orderId} a fost schimbat in {status}!");
                    }
                }

                return Ok("Order status updated.");

            }
            return Ok("Order not found!");
        }
        [Authorize]
        [HttpPost("adjustOrder/{orderId}&{comment}&{newTotal}")]
        public async Task<IActionResult> AdjustProducts(int orderId, string comment, decimal newTotal)
        {

            var order = _context.Orders.AsNoTracking().AsEnumerable().FirstOrDefault(ord => ord.OrderId == orderId);
            if (order != null)
            {
                order.Comments = comment;
                order.TotalOrdered = newTotal;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                return Ok("Comanda a fost modificata");
            }
            return Ok("Comanda nu a fost modificata");
        }
        [Authorize]
        [HttpGet("driverlockorder/{email}&{orderId}")]
        public async Task<IActionResult> DriverLockorder(string email, int orderId)
        {
            var driverId = (await _userManager.FindByEmailAsync(email)).Id;
            if (_context.Orders.AsNoTracking().Where(ord => ord.DriverRefId == driverId && ord.Status != "Refuzata" && ord.Status != "Livrata").Count() >= maxAllowedOrdersForDriver)
            {
                return Ok("Ai atins maximum de comenzi care pot fi luate.");
            }
            if (await new LockOrder(_context).Do(driverId, orderId))
            {
                var orderVM = _context.Orders.AsNoTracking().FirstOrDefault(or => or.OrderId == orderId);
                var restaurant = _userManager.Users.FirstOrDefault(us => us.CompanieRefId == orderVM.CompanieRefId);
                if (restaurant != null)
                {
                    var restaurantToken = _context.FBTokens.AsNoTracking().Where(tkn => tkn.UserId == restaurant.Id)
                        .Select(tkn => tkn.FBToken).Distinct().ToList();
                    if (restaurantToken != null)
                    {
                        foreach (var token in restaurantToken)
                            NotificationSender.SendNotif(OneSignalApiKey, OneSignalAppId, token, $"La comanda {orderId} s-a alaturat un livrator pentru livrarea ulterioara!");
                    }
                }
                if (!orderVM.TelephoneOrdered)
                {
                    var userToken = _context.FBTokens.AsNoTracking().Where(tkn => tkn.UserId == orderVM.CustomerId)
                        .Select(tkn => tkn.FBToken).Distinct().ToList();
                    if (userToken != null)
                    {
                        foreach (var token in userToken)
                            NotificationSender.SendNotif(OneSignalApiKey, OneSignalAppId, token, $"La comanda {orderId} s-a alaturat un livrator pentru livrarea ulterioara!");
                    }
                }

                return Ok("Order locked.");
            }

            return Ok("Order not locked.");
        }
        [Authorize]
        [HttpPost("driverupdatelocation")]
        public async Task<IActionResult> DriverUpdateLocation([FromBody] object driverLocation)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            var location = JsonConvert.DeserializeObject<UserLocations>(driverLocation.ToString(), settings);
            var user = await _userManager.FindByIdAsync(location.UserId);
            if (user != null)
            {
                var locationDb = _context.UserLocations.AsNoTracking().FirstOrDefault(loc => loc.UserId == user.Id);
                if (locationDb == null || locationDb.LocationId == 0)
                {
                    location.UserId = user.Id;
                    location.LocationName = user.FullName;
                    _context.UserLocations.Add(location);
                }

                else
                {
                    location.LocationId = locationDb.LocationId;
                    location.UserId = user.Id;
                    location.LocationName = user.FullName;

                    _context.UserLocations.Update(location);
                }
                await _context.SaveChangesAsync();
                return Ok("Location updated");
            }
            return Ok("Location not updated");
        }
        [Authorize]
        [HttpGet("setesttime/{orderId}&{esttime}")]
        public async Task<IActionResult> SetEstTime(int orderId, string esttime)
        {
            var orderVM = _context.Orders.AsNoTracking().FirstOrDefault(or => or.OrderId == orderId);
            if (!orderVM.TelephoneOrdered)
            {
                var userToken = _context.FBTokens.AsNoTracking().Where(tkn => tkn.UserId == orderVM.CustomerId)
                        .Select(tkn => tkn.FBToken).Distinct().ToList();
                if (userToken != null)
                {
                    foreach (var token in userToken)
                        NotificationSender.SendNotif(OneSignalApiKey, OneSignalAppId, token, $"Ai primit un timp estimat de pregatire al comenzii cu numarul {orderId}, te rugam sa iti exprimi acordul!");
                }
            }

            return Ok($"estTime : {await new UpdateOrder(_context).DoET(orderId, esttime)}");
        }
        [Authorize]
        [HttpGet("ratingclient/{isOwner}&{orderId}&{rating}")]
        public async Task<IActionResult> GiveRestaurantRating(bool isOwner, int orderId, int rating)
        {
            var order = _context.Orders.AsNoTracking().FirstOrDefault(or => or.OrderId == orderId);
            if (order == null) return BadRequest();
            var haveRating = _context.RatingClients.AsNoTracking().FirstOrDefault(rc => rc.OrderRefId == orderId);
            if (haveRating != null)
            {
                if (isOwner)
                    haveRating.RatingDeLaCompanie = rating;
                else
                    haveRating.RatingDeLaSofer = rating;
                _context.RatingClients.Update(haveRating);
            }
            else
            {
                haveRating = new RatingClient
                {
                    OrderRefId = orderId,
                    UserRefId = order.CustomerId,
                };
                if (isOwner)
                    haveRating.RatingDeLaCompanie = rating;
                else
                    haveRating.RatingDeLaSofer = rating;
                _context.RatingClients.Add(haveRating);

            }

            if (isOwner)
            {
                order.CompanieGaveRating = true;
            }
            else
            {
                order.DriverGaveRating = true;
            }
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return Ok("Rating acordat");
        }
        [HttpGet("getmyearnings")]
        public async Task<IActionResult> FetchTotalComenzi()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return BadRequest();
            var orders = await new GetAllOrders(_context, _userManager).Do(user.CompanieRefId);
            decimal[] valori = new decimal[12];
            orders = orders.OrderByDescending(o => o.Created);

            for (int i = 0; i < 12; i++)
            {
                var monthOrders = orders.Where(or => or.Created.Month == i + 1).ToList();
                decimal totalLuna = 0.0M;
                foreach (var order in monthOrders)
                    totalLuna += order.TotalOrdered;
                valori[i] = totalLuna;
            }
            return Ok(JsonConvert.SerializeObject(valori));
        }

    }
}
