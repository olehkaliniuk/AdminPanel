using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using AdminPanelDB.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AdminPanelDB.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminRepository _repo;


        public AdminController(IConfiguration configuration)
        {
            string connString = configuration.GetConnectionString("DefaultConnection");
            _repo = new AdminRepository(connString);
   
        }

        [HttpGet]

        // Страница Abteilungen & Referate
        public IActionResult AbRef()
        {
            var model = new AdminPanelViewModel
            {
                Abteilungen = _repo.GetAbteilungReferate()
            };
            return View(model);
        }

        // Страница Personen
        public IActionResult Personen()
        {
            var model = new AdminPanelViewModel
            {
                Personen = _repo.GetAllPersonen()
        
            };  

            // Для дропдаунов
            ViewBag.Abteilungen = _repo.GetAllAbteilungen();


            return View(model);
        }


        public IActionResult Config()
        {
            return View();
        }


        // ---------------- Abteilung ----------------
        [HttpPost]
        public IActionResult CreateAbteilung(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                bool success = _repo.CreateAbteilung(name);

                if (!success)
                {
                    TempData["Error"] = "Abteilung mit diesem Namen existiert bereits!";
                }
            }

            return RedirectToAction("AbRef");
        }


        [HttpPost]
        public IActionResult EditAbteilung(int id, string newName)
        {
            if (!string.IsNullOrWhiteSpace(newName))
            {
                bool success = _repo.EditAbteilung(id, newName);
                if (!success)
                {
                    TempData["Error"] = "Abteilung mit diesem Namen existiert bereits!";
                }
            }

            return RedirectToAction("AbRef");
        }


        [HttpPost]
        public IActionResult DeleteAbteilung(int id)
        {
            bool deleted = _repo.DeleteAbteilung(id);
            if (!deleted)
            {
                TempData["Error"] = "Невозможно удалить Abteilung, пока есть связанные Referate.";
            }
            return RedirectToAction("AbRef");
        }


        // ---------------- Referat ----------------
        [HttpPost]
        public IActionResult CreateReferat(int abteilungId, string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                _repo.CreateReferat(abteilungId, name);

            return RedirectToAction("AbRef");
        }

        [HttpPost]
        public IActionResult EditReferat(int id, string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                _repo.EditReferat(id, name);

            return RedirectToAction("AbRef");
        }

        [HttpPost]
        public IActionResult DeleteReferat(int id)
        {
            _repo.DeleteReferat(id);
            return RedirectToAction("AbRef");
        }






        //user 

        [HttpGet]
        public IActionResult CreateUser()
        {
            ViewBag.Abteilungen = _repo.GetAllAbteilungen(); // список всех Abteilungen
            return View(new Personen());
        }

        [HttpPost]
        public IActionResult CreateUser(Personen person)
        {
            if (ModelState.IsValid)
            {
                _repo.CreatePerson(person);
                return RedirectToAction("Personen");
            }

            ViewBag.Abteilungen = _repo.GetAllAbteilungen();
            return View(person);
        }

        // Ajax для Referat
        [HttpGet]
        public JsonResult GetReferateByAbteilung(int abteilungId)
        {
            var referate = _repo.GetReferateByAbteilungId(abteilungId);
            return Json(referate);
        }



        // GET: EditUser
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var person = _repo.GetAllPersonen().FirstOrDefault(p => p.Id == id);
            if (person == null) return NotFound();

            ViewBag.Abteilungen = _repo.GetAllAbteilungen();

            // Если у пользователя уже есть Abteilung, подгрузим Referate
            if (!string.IsNullOrEmpty(person.Abteilung))
            {
                var abt = _repo.GetAllAbteilungen().FirstOrDefault(a => a.Name == person.Abteilung);
                if (abt != null)
                {
                    ViewBag.Referate = _repo.GetReferateByAbteilungId(abt.Id);
                }
            }

            return View(person);
        }

        // POST: EditUser
        [HttpPost]
        public IActionResult EditUser(Personen person)
        {
            if (ModelState.IsValid)
            {
                // Сохраняем изменения
                _repo.UpdatePerson(person);
                return RedirectToAction("Personen");
            }

            ViewBag.Abteilungen = _repo.GetAllAbteilungen();

            if (!string.IsNullOrEmpty(person.Abteilung))
            {
                var abt = _repo.GetAllAbteilungen().FirstOrDefault(a => a.Name == person.Abteilung);
                if (abt != null)
                {
                    ViewBag.Referate = _repo.GetReferateByAbteilungId(abt.Id);
                }
            }

            return View(person);
        }


        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            if (id > 0)
            {
                _repo.DeletePerson(id); // вызываем метод в репозитории
            }
            return RedirectToAction("Personen");
        }


    }
}
