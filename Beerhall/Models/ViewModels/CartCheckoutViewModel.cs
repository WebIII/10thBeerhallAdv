using Beerhall.Models.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace Beerhall.Models.ViewModels {
    public class CartCheckoutViewModel {
        public SelectList Locations { get; }
        public ShippingViewModel ShippingViewModel { get; set; }

        public CartCheckoutViewModel(IEnumerable<Location> locations, ShippingViewModel shippingViewModel) {
            Locations = new SelectList(locations,
                nameof(Location.PostalCode),
                nameof(Location.Name),
                shippingViewModel?.PostalCode);
            ShippingViewModel = shippingViewModel;
        }
    }

    public class ShippingViewModel {
        public DateTime? DeliveryDate { get; set; }
        public bool Giftwrapping { get; set; }
        public string Street { get; set; }
        public string PostalCode { get; set; }
    }
}