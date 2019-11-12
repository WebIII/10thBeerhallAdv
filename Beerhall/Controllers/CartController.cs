﻿using Beerhall.Filters;
using Beerhall.Models.Domain;
using Beerhall.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Beerhall.Controllers {
    [ServiceFilter(typeof(CartSessionFilter))]
    public class CartController : Controller {

        private readonly IBeerRepository _beerRepository;
        private readonly ILocationRepository _locationRepository;

        public CartController(IBeerRepository beerRepository, ILocationRepository locationRepository) {
            _beerRepository = beerRepository;
            _locationRepository = locationRepository;
        }

        public IActionResult Index(Cart cart) {
            ViewData["Total"] = cart.TotalValue;
            return View(cart.CartLines.Select(c => new CartIndexViewModel(c)).ToList());
        }

        [HttpPost]
        public IActionResult Add(Cart cart, int id, int quantity = 1) {
            try
            {
                Beer product = _beerRepository.GetBy(id);
                if (product != null)
                {
                    cart.AddLine(product, quantity);
                    TempData["message"] = $"{quantity} x {product.Name} was added to your cart";
                }
            }
            catch
            {
                TempData["error"] = "Sorry, something went wrong, the product could not be added to your cart...";
            }
            return RedirectToAction("Index", "Store");
        }

        [HttpPost]
        public ActionResult Remove(Cart cart, int id) {
            try
            {
                Beer product = _beerRepository.GetBy(id);
                cart.RemoveLine(product);
                TempData["message"] = $"{product.Name} was removed from your cart";
            }
            catch
            {
                TempData["error"] = "Sorry, something went wrong, the product was not removed from your cart...";
            }
            return RedirectToAction("Index");
        }

        [Authorize(Policy = "Customer")]
        public IActionResult Checkout(Cart cart) {
            if (cart.NumberOfItems == 0)
                return RedirectToAction("Index", "Store");
            IEnumerable<Location> locations = _locationRepository.GetAll().OrderBy(l => l.Name).ToList();
            return View(new CartCheckoutViewModel(locations, new ShippingViewModel()));
        }
    }
}