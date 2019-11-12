using Beerhall.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Beerhall.Controllers {
    public class StoreController : Controller {
        private readonly IBeerRepository _beerRepository;

        public StoreController(IBeerRepository beerRepository) {
            _beerRepository = beerRepository;
        }

        public IActionResult Index() {
            return View(_beerRepository.GetAll().OrderBy(b => b.Name).ToList());
        }
    }
}