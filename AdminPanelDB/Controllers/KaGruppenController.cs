using AdminPanelDB.Exeptions;
using AdminPanelDB.Filters;
using AdminPanelDB.Logs;
using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using AdminPanelDB.Services;
using AdminPanelDB.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace AdminPanelDB.Controllers
{
    [SessionAuthorize(AdminOnly = true)]
    public class KaGruppenController : Controller
    {
        private readonly KaGruppenRepository _kaRepository;

        private readonly ConfigService _configService;

        public KaGruppenController(ConfigService configService, IConfiguration configuration)
        {
            _configService = configService;

            var connString = configuration.GetConnectionString("DefaultConnection");
            _kaRepository = new KaGruppenRepository(connString);
        }


        // --- KaGruppen. ---
        [HttpGet]
        public IActionResult KaGruppen(
            int? pageNumber,
            int pageSize = 10,
            int? kategorieNummer = null,
            string hauptgruppeSearchTerm = "",
            string nebengruppeSearchTerm = "",
            string sortColumn = null,
            string sortDirection = null)
        {
            try
            {
                // Aktuelle Seite bestimmen (Standard = 1) und sicherstellen, dass sie >= 1 ist.
                int currentPage = pageNumber.GetValueOrDefault(1);
                if (currentPage < 1)
                {
                    currentPage = 1;
                }

                ViewBag.PageNumber = currentPage;
                ViewBag.PageSize = pageSize;

                // Sortierung festlegen, Standard: HauptgruppeNummer ASC.
                var effectiveSortColumn = string.IsNullOrEmpty(sortColumn) ? "HauptgruppeNummer" : sortColumn;
                var effectiveSortDirection = string.IsNullOrEmpty(sortDirection) ? "ASC" : sortDirection;


                // Alle Kategorien laden.
                var kategorien = _kaRepository.GetAllKategorien();
                ViewBag.Kategorien = kategorien;

                // Wenn keine Kategorie gewählt wurde, nimm die erste aus der Liste.
                if (kategorieNummer == null && kategorien.Any())
                {
                    kategorieNummer = kategorien.First().KategorieNummer;
                }

                // Ausgewählte KategorieNummer speichern.
                ViewBag.SelectedKategorieNummer = kategorieNummer;

                // Gesamtanzahl der Abteilungen ermitteln.
                int totalCount = _kaRepository.GetHauptGruppenCount(hauptgruppeSearchTerm, nebengruppeSearchTerm, kategorieNummer);

                // Abteilungen-Seite abrufen.
                var items = _kaRepository.GetHauptGruppenPage(
                    currentPage,
                    pageSize,
                    hauptgruppeSearchTerm,
                    nebengruppeSearchTerm,
                    effectiveSortColumn,
                    effectiveSortDirection,
                    kategorieNummer
                );

                // Paginierte Liste erstellen.
                var pagedList = new PaginatedList<KaGruppenViewModel>(items, totalCount, currentPage, pageSize);

                // Alle Kategorien in Dropdown // kategorieNummer für Sortierung.
                ViewBag.Kategorien = _kaRepository.GetAllKategorien();
                ViewBag.SelectedKategorieNummer = kategorieNummer;

                // ViewModel füllen.
                var model = new KaGruppenPageViewModel
                {
                    HauptGruppen = pagedList,
                    HauptgruppeSearchTerm = hauptgruppeSearchTerm,
                    NebengruppeSearchTerm = nebengruppeSearchTerm,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection,
                    PageSize = pageSize
                };

                // Gesamtanzahl für ViewBag bereitstellen.
                ViewBag.TotalCount = totalCount;

                return View(model);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }


        // Kategorie.

        // Create Kategorie.
        [HttpGet]
        public IActionResult CreateKategorie()
        {
            var model = new Kategorie();

            return View(model);
        }


        [HttpPost]
        public IActionResult CreateKategorie(Kategorie kategorie)
        {

            // 1. Pflichtfelder prüfen.
            if (string.IsNullOrWhiteSpace(kategorie.Bezeichnung) ||
                string.IsNullOrWhiteSpace(kategorie.Kuerzel) ||
                string.IsNullOrWhiteSpace(kategorie.Beschreibung) ||
                kategorie.KategorieNummer == 0)
            {
                ViewBag.Error = "Bitte alle Pflichtfelder korrekt ausfüllen.";
                return View(kategorie);
            }

            // 2. KategorieNummer auf Einzigartigkeit prüfen.
            if (_kaRepository.KategorieNummerExists(kategorie.KategorieNummer))
            {
                ViewBag.Error = "Diese KategorieNummer existiert bereits.";
                return View(kategorie);
            }

            try
            {
                // Neue Kategorie speichern und ID zurückbekommen.
                int newKategorieId = _kaRepository.CreateKategorieRep(kategorie);

                // Weiterleitung zur KaGruppen-Seite mit der neuen KategorieNummer.
                TempData["Message"] = $"Kategorie '{kategorie.Bezeichnung}' erfolgreich erstellt!";
                TempData["MessageType"] = "success";
                return RedirectToAction("KaGruppen", new { kategorieNummer = kategorie.KategorieNummer });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }





        // Edit Kategorie.
        [HttpGet]
        public IActionResult EditKategorie(int id)
        {
            var kategorie = _kaRepository.GetById(id);
            if (kategorie == null) 
            {
                return NotFound();
            }
            
            return View(kategorie);
        }

        [HttpPost]
        public IActionResult EditKategorie(Kategorie kategorie, bool applyToGroups)
        {
            var kategorieExist = _kaRepository.GetById(kategorie.Id);
            if (kategorieExist == null)
            {
                return NotFound();
            }

            // 1. Pflichtfelder prüfen.
            if (string.IsNullOrWhiteSpace(kategorie.Bezeichnung) ||
                string.IsNullOrWhiteSpace(kategorie.Kuerzel) ||
                string.IsNullOrWhiteSpace(kategorie.Beschreibung) ||
                kategorie.KategorieNummer == 0)
            {
                ViewBag.Error = "Bitte alle Pflichtfelder korrekt ausfüllen.";
                return View(kategorie);
            }

            // 2. Prüfen, ob KategorieNummer eindeutig ist (außer eigene).
            if (kategorie.KategorieNummer != kategorieExist.KategorieNummer &&
                _kaRepository.KategorieNummerExists(kategorie.KategorieNummer))
            {
                ViewBag.Error = "Diese KategorieNummer existiert bereits.";
                return View(kategorie);
            }

            try
            {
                // 3. Kategorie aktualisieren, alte KategorieNummer weitergeben.
                _kaRepository.UpdateKategorie(kategorie, kategorieExist.KategorieNummer, applyToGroups);

                TempData["Message"] = $"Kategorie '{kategorie.Bezeichnung}' erfolgreich bearbeitet!";
                TempData["MessageType"] = "success";

                // Weiterleitung zur KaGruppen-Seite.
                return RedirectToAction("KaGruppen", new { kategorieNummer = kategorie.KategorieNummer });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }




        // Delete Kategorie.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteKategorie(int id, int kategorieNummer)
        {
            try
            {
                bool deleted = _kaRepository.DeleteKategorieRep(id, kategorieNummer);

                if (!deleted)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Diese Kategorie kann nicht gelöscht werden, da sie noch Hauptgruppen enthält!"
                    });
                }

                // Gesamtanzahl der Kategorien abrufen.
                var kategorien = _kaRepository.GetAllKategorien();
                int totalCount = kategorien.Count;
                int pageSize = 10;
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Falls es keine Kategorien mehr gibt - einfach success.
                if (totalCount == 0)
                {
                    return Json(new { success = true, newPageIndex = 1 });
                }

                // Nimmt die erste Kategorie, falls vorhanden.
                int? firstKategorieId = kategorien.First().Id;

                return Json(new
                {
                    success = true,
                    redirectKategorieId = firstKategorieId,
                    message = "Kategorie erfolgreich gelöscht!"
                });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }









        // Hauptgruppe.


        // --- Create Hauptgruppe. ---
        [HttpGet]
        public IActionResult CreateHauptgruppe(int kategorieNummer)
        {
            try
            {
                // Neues Modell mit aktueller Kategorie-Nummer vorbelegen.
                var model = new Hauptgruppe
                {
                    GehoertZuKategorie = kategorieNummer
                };

                // Kategorie-Bezeichnung abrufen.
                ViewBag.KategorieBezeichnung = _kaRepository.GetKategorieBezeichnungByNummer(kategorieNummer);

                return View(model);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpPost]
        public IActionResult CreateHauptgruppe(Hauptgruppe hauptgruppe)
        {
            // 1. Pflichtfelder prüfen.
            if (string.IsNullOrWhiteSpace(hauptgruppe.Bezeichnung) ||
                string.IsNullOrWhiteSpace(hauptgruppe.Kuerzel) ||
                string.IsNullOrWhiteSpace(hauptgruppe.Beschreibung) ||
                hauptgruppe.HauptgruppeNummer == 0)
            {
                ViewBag.Error = "Bitte alle Pflichtfelder korrekt ausfüllen.";
                ViewBag.KategorieBezeichnung = _kaRepository.GetKategorieBezeichnungByNummer(hauptgruppe.GehoertZuKategorie);
                return View(hauptgruppe);
            }

            // 2. Prüfen, ob HauptgruppeNummer einzigartig in dieser Kategorie ist.
            if (_kaRepository.HauptgruppeNummerExists(hauptgruppe.GehoertZuKategorie, hauptgruppe.HauptgruppeNummer))
            {
                ViewBag.Error = "Diese HauptgruppeNummer existiert bereits in dieser Kategorie.";
                ViewBag.KategorieBezeichnung = _kaRepository.GetKategorieBezeichnungByNummer(hauptgruppe.GehoertZuKategorie);
                return View(hauptgruppe);
            }

            try
            {
                // Neue Hauptgruppe speichern.
                int newId = _kaRepository.CreateHauptgruppeRep(hauptgruppe);

                // Position in der sortierten Liste nach HauptgruppeNummer ermitteln.
                int position = _kaRepository.GetHauptgruppePosition(hauptgruppe.GehoertZuKategorie, hauptgruppe.HauptgruppeNummer);

                int pageSize = 10;
                int pageNumber = (int)Math.Ceiling((double)position / pageSize);

                TempData["Message"] = $"Hauptgruppe '{hauptgruppe.Bezeichnung}' erfolgreich erstellt!";
                TempData["MessageType"] = "success";

                // Redirect auf die richtige Seite.
                return RedirectToAction("KaGruppen", new
                {
                    kategorieNummer = hauptgruppe.GehoertZuKategorie,
                    pageNumber = pageNumber
                });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteHauptgruppe(int id, int kategorieNummer, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                bool deleted = _kaRepository.DeleteHauptgruppeRep(id);

                if (!deleted)
                {
                    return Json(new { success = false, message = "Eine Hauptgruppe kann nicht gelöscht werden, solange noch Nebengruppen damit verknüpft sind!" });
                }

                int totalCount = _kaRepository.GetHauptGruppenCount("", "", kategorieNummer);
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                if (pageNumber > totalPages) pageNumber = totalPages;
                if (pageNumber < 1) pageNumber = 1;

                return Json(new { success = true, newPageIndex = pageNumber, message = "Hauptgruppe erfolgreich gelöscht!" });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }









        // GET: EditHauptgruppe.
        [HttpGet]
        public IActionResult EditHauptgruppe(int id, int pageSize = 10, int pageNumber = 1)
        {
            try
            {
                var hauptgruppe = _kaRepository.GetHauptgruppeById(id);
                if (hauptgruppe == null) return NotFound();
                // Dropdown is Enable?.
                var kaGruppen = _configService.CurrentConfig.KaGruppen;
                ViewBag.DropdownEnable = kaGruppen;

                ViewBag.Kategorien = _kaRepository.GetAllKategorien();
                ViewBag.PageSize = pageSize;
                ViewBag.PageNumber = pageNumber;

                return View(hauptgruppe);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        // POST: EditHauptgruppe.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditHauptgruppe(Hauptgruppe model, int pageSize = 10, int pageNumber = 1, bool applyToGroups = false)
        {
            //1. Pflichtfelder prüfen.
            if (string.IsNullOrWhiteSpace(model.Bezeichnung) ||
                string.IsNullOrWhiteSpace(model.Kuerzel) ||
                string.IsNullOrWhiteSpace(model.Beschreibung) ||
                model.HauptgruppeNummer == 0)
            {
                ViewBag.Error = "Bitte alle Pflichtfelder korrekt ausfüllen.";
                ViewBag.Kategorien = _kaRepository.GetAllKategorien();

                //Is Dropdown Enable?.
                var kaGruppen = _configService.CurrentConfig.KaGruppen;
                ViewBag.DropdownEnable = kaGruppen;

                ViewBag.PageSize = pageSize;
                ViewBag.PageNumber = pageNumber;
                return View(model);
            }

            //  2. Prüfen, ob HauptgruppeNummer bereits existiert (aber nicht bei sich selbst).
            if (_kaRepository.HauptgruppeNummerExists(model.GehoertZuKategorie, model.HauptgruppeNummer, model.Id))
            {
                ViewBag.Error = "Diese HauptgruppeNummer existiert bereits in dieser Kategorie.";
                ViewBag.Kategorien = _kaRepository.GetAllKategorien();

                // Is Dropdown Enable?
                var kaGruppen = _configService.CurrentConfig.KaGruppen;
                ViewBag.DropdownEnable = kaGruppen;

                ViewBag.PageSize = pageSize;
                ViewBag.PageNumber = pageNumber;
                return View(model);
            }

            try
            {
                //  3. Update durchführen .
                bool updated = _kaRepository.UpdateHauptgruppe(model, applyToGroups);
                if (!updated)
                {
                    ViewBag.Error = "Hauptgruppe konnte nicht aktualisiert werden.";
                    ViewBag.Kategorien = _kaRepository.GetAllKategorien();

                    // Dropdown is Enable?
                    var kaGruppen = _configService.CurrentConfig.KaGruppen;
                    ViewBag.DropdownEnable = kaGruppen;

                    ViewBag.PageSize = pageSize;
                    ViewBag.PageNumber = pageNumber;
                    return View(model);
                }

                // 4. Neue Position berechnen.
                int position = _kaRepository.GetHauptgruppePosition(model.GehoertZuKategorie, model.HauptgruppeNummer);
                int newPageNumber = (int)Math.Ceiling((double)position / pageSize);
                if (newPageNumber < 1)
                {
                    newPageNumber = 1;
                }


                TempData["Message"] = $"Hauptgruppe '{model.Bezeichnung}' erfolgreich bearbeitet!";
                TempData["MessageType"] = "success";
                // 5. Redirect auf richtige Seite.
                return RedirectToAction("KaGruppen", new
                {
                    kategorieNummer = model.GehoertZuKategorie,
                    pageNumber = newPageNumber,
                    pageSize = pageSize
                });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }




        // Nebengruppe.

        // --- Create Nebengruppe. ---
        [HttpGet]
        public IActionResult CreateNebengruppe(int kategorieNummer, int hauptgruppeNummer, int pageSize = 10, int pageNumber = 1)
        {
            try
            {
                var model = new Nebengruppe
                {
                    GehoertZuKategorie = kategorieNummer,
                    GehoertZuHauptgruppe = hauptgruppeNummer
                };

                ViewBag.KategorieBezeichnung = _kaRepository.GetKategorieBezeichnungByNummer(kategorieNummer);
                ViewBag.HauptgruppeBezeichnung = _kaRepository.GetHauptgruppeBezeichnungByNummer(hauptgruppeNummer);

                ViewBag.PageSize = pageSize;
                ViewBag.PageNumber = pageNumber;

                return View(model);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateNebengruppe(Nebengruppe nebengruppe, int pageSize = 10, int pageNumber = 1)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.KategorieBezeichnung = _kaRepository.GetKategorieBezeichnungByNummer(nebengruppe.GehoertZuKategorie);
                    ViewBag.HauptgruppeBezeichnung = _kaRepository.GetHauptgruppeBezeichnungByNummer(nebengruppe.GehoertZuHauptgruppe);
                    ViewBag.PageSize = pageSize;
                    ViewBag.PageNumber = pageNumber;

                    return View(nebengruppe);
                }

                // 1. Speichern.
                int newId = _kaRepository.CreateNebengruppeRep(nebengruppe);

                // 2. Position der HAUPTGRUPPE bestimmen.
                int position = _kaRepository.GetHauptgruppePosition(
                    nebengruppe.GehoertZuKategorie,
                    nebengruppe.GehoertZuHauptgruppe 
                );

                // 3. Neue Seit.
                int newPageNumber = (int)Math.Ceiling((double)position / pageSize);
                if (newPageNumber < 1)
                { 
                    newPageNumber = 1; 
                }


                TempData["Message"] = $"Nebengruppe '{nebengruppe.Bezeichnung}' erfolgreich erstellt!";
                TempData["MessageType"] = "success";
                //  4. Redirect.
                return RedirectToAction("KaGruppen", new
                {
                    kategorieNummer = nebengruppe.GehoertZuKategorie,
                    pageNumber = newPageNumber,
                    pageSize = pageSize
                });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }




        //  Edit Nebengruppe. 
        [HttpGet]
        public IActionResult EditNebengruppe(int id, int kategorieNummer, int hauptgruppeNummer,
                                       int pageSize = 10, int pageNumber = 1)
        {
            try
            {
                var nebengruppe = _kaRepository.GetNebengruppeById(id);
                if (nebengruppe == null)
                {
                    return NotFound();
                }

                if (nebengruppe.GehoertZuHauptgruppe == 0)
                {
                    nebengruppe.GehoertZuHauptgruppe = hauptgruppeNummer;
                }

                if (nebengruppe.GehoertZuKategorie == 0)
                {
                    nebengruppe.GehoertZuKategorie = kategorieNummer;
                }

                var kaGruppen = _configService.CurrentConfig.KaGruppen;
                ViewBag.DropdownEnable = kaGruppen;

                ViewBag.Kategorien = _kaRepository.GetAllKategorien(); 
                ViewBag.Hauptgruppen = _kaRepository.GetHauptgruppenByKategorie(kategorieNummer); 

                ViewBag.OriginalKategorie = kategorieNummer;
                ViewBag.OriginalHauptgruppe = hauptgruppeNummer;



                ViewBag.PageSize = pageSize;
                ViewBag.PageNumber = pageNumber;

                return View(nebengruppe);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }


        //ajax for dynamic lists.
        [HttpGet]
        public JsonResult GetHauptgruppenByKategorie(int kategorieNummer)
        {
            var list = _kaRepository.GetHauptgruppenByKategorie(kategorieNummer);
            return Json(list);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditNebengruppe(Nebengruppe model, int pageSize = 10, int pageNumber = 1)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.KategorieBezeichnung = _kaRepository.GetKategorieBezeichnungByNummer(model.GehoertZuKategorie);
                    ViewBag.HauptgruppeBezeichnung = _kaRepository.GetHauptgruppeBezeichnungByNummer(model.GehoertZuHauptgruppe);
                    ViewBag.PageSize = pageSize;
                    ViewBag.PageNumber = pageNumber;

                    return View(model);
                }

                bool updated = _kaRepository.UpdateNebengruppe(model);

                if (!updated)
                {
                    ViewBag.Error = "Nebengruppe konnte nicht aktualisiert werden.";
                    ViewBag.KategorieBezeichnung = _kaRepository.GetKategorieBezeichnungByNummer(model.GehoertZuKategorie);
                    ViewBag.HauptgruppeBezeichnung = _kaRepository.GetHauptgruppeBezeichnungByNummer(model.GehoertZuHauptgruppe);
                    ViewBag.PageSize = pageSize;
                    ViewBag.PageNumber = pageNumber;

                    return View(model);
                }

                //Berechnung der richtigen Seite anhand der Hauptgruppe.
                int position = _kaRepository.GetHauptgruppePosition(
                    model.GehoertZuKategorie,
                    model.GehoertZuHauptgruppe
                );

                int newPageNumber = (int)Math.Ceiling((double)position / pageSize);
                if (newPageNumber < 1)
                { 
                    newPageNumber = 1;
                }

                TempData["Message"] = $"Nebengruppe '{model.Bezeichnung}' erfolgreich bearbeitet!";
                TempData["MessageType"] = "success";
                // Redirect zurück zur gleichen Seite.
                return RedirectToAction("KaGruppen", new
                {
                    kategorieNummer = model.GehoertZuKategorie,
                    pageNumber = newPageNumber,
                    pageSize = pageSize
                });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }






        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteNebengruppe(int id, int pageNumber = 1)
        {
            try
            {
                bool deleted = _kaRepository.DeleteNebengruppeRep(id);

                if (!deleted)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Nebengruppe konnte nicht gelöscht werden."
                    });
                }

                //  Auf der aktuellen Seite bleiben.
                return Json(new
                {
                    success = true,
                    newPageIndex = pageNumber,
                    message = "Nebengruppe erfolgreich gelöscht!"
                });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }


    }
}
