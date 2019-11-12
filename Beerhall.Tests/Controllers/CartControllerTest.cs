using Beerhall.Controllers;
using Beerhall.Models.Domain;
using Beerhall.Models.ViewModels;
using Beerhall.Tests.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Beerhall.Tests.Controllers {
    public class CartControllerTest {
        private readonly CartController _controller;
        private readonly Cart _cart;
        private readonly DummyApplicationDbContext _context;
        private readonly Mock<IBeerRepository> _beerRepository;
        private readonly Mock<ILocationRepository> _locationRepository;
        private readonly Mock<ICustomerRepository> _customerRepository;
        private readonly Customer _customerJan;
        private readonly ShippingViewModel _shippingVm;

        public CartControllerTest() {
            _context = new DummyApplicationDbContext();
            _beerRepository = new Mock<IBeerRepository>();
            _beerRepository.Setup(b => b.GetAll()).Returns(_context.Beers);
            _locationRepository = new Mock<ILocationRepository>();
            _locationRepository.Setup(b => b.GetAll()).Returns(_context.Locations);
            _customerRepository = new Mock<ICustomerRepository>();
            _customerRepository.Setup(b => b.GetBy("jan@hogent.be")).Returns(_context.CustomerJan);
            _controller = new CartController(_beerRepository.Object, _locationRepository.Object, _customerRepository.Object) 
            {
                TempData = new Mock<ITempDataDictionary>().Object
            };
            _cart = _context.CartFilled;
            _customerJan = _context.CustomerJan;
            _shippingVm = new ShippingViewModel
            {
                DeliveryDate = DateTime.Today.AddDays(5).DayOfWeek == DayOfWeek.Sunday ? DateTime.Today.AddDays(6) : DateTime.Today.AddDays(5),
                Giftwrapping = true,
                PostalCode = _context.Bavikhove.PostalCode,
                Street = "Bavikhovestraat"
            };
        }

        #region Index
        [Fact]
        public void Index_EmptyCart_PassesCartToDefaultView() {
            var actionResult = _controller.Index(new Cart()) as ViewResult;
            var cartLines = Assert.IsAssignableFrom<IEnumerable<CartIndexViewModel>>(actionResult.Model);
            Assert.Empty(cartLines);
            Assert.Null(actionResult.ViewName);
        }

        [Fact]
        public void Index_NonEmptyCart_PassesCartToDefaultViewAndStoresTotalInViewData() {
            var actionResult = Assert.IsType<ViewResult>(_controller.Index(_cart));
            var cartLines = Assert.IsAssignableFrom<IEnumerable<CartIndexViewModel>>(actionResult.Model);
            Assert.Single(cartLines);
            Assert.Null(actionResult.ViewName);
            Assert.Equal(10M, actionResult.ViewData["Total"]);
        }

        #endregion

        #region Add
        [Fact]
        public void Add_RedirectsToActionIndexInStoreAndAddsProductToCart() {
            _beerRepository.Setup(b => b.GetBy(1)).Returns(_context.BavikPils);
            var actionResult = Assert.IsType<RedirectToActionResult>(_controller.Add(_cart, 1, 4));
            Assert.Equal("Index", actionResult.ActionName);
            Assert.Equal("Store", actionResult.ControllerName);
            Assert.Equal(2, _cart.NumberOfItems);
        }

        #endregion

        #region Remove
        [Fact]
        public void Remove_RedirectsToActionIndexInDefaultControllerAndRemovesProductFromCart() {
            _beerRepository.Setup(b => b.GetBy(2)).Returns(_context.Wittekerke);
            var actionResult = Assert.IsType<RedirectToActionResult>(_controller.Remove(_cart, 2));
            Assert.Equal("Index", actionResult.ActionName);
            Assert.Null(actionResult.ControllerName);
            Assert.Equal(0, _cart.NumberOfItems);
        }
        #endregion

        #region Checkout HttpGet
        [Fact]
        public void Checkout_EmptyCart_RedirectsToIndexOfStore() {
            var actionResult = Assert.IsType<RedirectToActionResult>(_controller.Checkout(new Cart()));
            Assert.Equal("Index", actionResult.ActionName);
            Assert.Equal("Store", actionResult.ControllerName);
        }

        [Fact]
        public void Checkout_NonEmptyCart_PassesACheckOutViewModelInViewResultModel() {
            var actionResult = Assert.IsType<ViewResult>(_controller.Checkout(_cart));
            var model = Assert.IsType<CartCheckoutViewModel>(actionResult.Model);
            Assert.Null(model.ShippingViewModel.DeliveryDate);
            Assert.Null(model.ShippingViewModel.PostalCode);
            Assert.Null(model.ShippingViewModel.Street);
            Assert.False(model.ShippingViewModel.Giftwrapping);
            Assert.Equal(3, model.Locations.Count());
        }
        #endregion

        #region Checkout HttpPost
        [Fact]
        public void CheckOut_EmptyCart_RedirectsToIndex() {
            var actionResult = Assert.IsType<RedirectToActionResult>(_controller.Checkout(_customerJan, new Cart(), _shippingVm));
            Assert.Equal("Index", actionResult.ActionName);
        }

        [Fact]
        public void CheckOut_NoModelErrors_RedirectsToIndexInStoreAndPlacesOrderAndClearsCart() {
            _locationRepository.Setup(l => l.GetBy(_context.Bavikhove.PostalCode)).Returns(_context.Bavikhove);
            var actionResult = Assert.IsType<RedirectToActionResult>(_controller.Checkout(_customerJan, _cart, _shippingVm));
            Assert.Equal("Index", actionResult.ActionName);
            Assert.Equal("Store", actionResult.ControllerName);
            Assert.Equal(1, _customerJan.Orders.Count);
            Assert.Equal(0, _cart.NumberOfItems);
            _customerRepository.Verify(c => c.SaveChanges(), Times.Once());
        }

            [Fact]
        public void CheckOut_ModelErrors_PassesCheckOutViewModelInViewResultModelToDefaultView() {
            _controller.ModelState.AddModelError("any key", "any error");
            var actionResult = Assert.IsType<ViewResult>(_controller.Checkout(_customerJan, _cart, _shippingVm));
            var model = Assert.IsType<CartCheckoutViewModel>(actionResult.Model);
            Assert.Equal(_shippingVm, model.ShippingViewModel);
            Assert.Equal(3, model.Locations.Count());
            Assert.True(string.IsNullOrEmpty(actionResult.ViewName));
        }

        [Fact]
        public void CheckOut_DomainOrDataLayerThrowsException_PassesCheckOutViewModelInViewResultModel() {
            _shippingVm.Street = "";
            var actionResult = Assert.IsType<ViewResult>(_controller.Checkout(_customerJan, _cart, _shippingVm));
            var model = Assert.IsType<CartCheckoutViewModel>(actionResult.Model);
            Assert.Equal(_shippingVm, model.ShippingViewModel);
            Assert.Equal(3, model.Locations.Count());
        }
        #endregion
    }
}