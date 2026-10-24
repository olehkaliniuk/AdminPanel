using AdminPanelDB.Abstractions;
using AdminPanelDB.Exeptions;
using AdminPanelDB.Filters;
using AdminPanelDB.Logs;
using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using AdminPanelDB.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;

namespace AdminPanelDB.Controllers
{
    [SessionAuthorize(AdminOnly = true)]
    public class AdminController : Controller
    {
        private readonly AdminRepository _repo;

        public AdminController(IConfiguration configuration, ICurrentMandantProvider mandantProvider)
        {
            string connString = configuration.GetConnectionString("DefaultConnection");
            _repo = new AdminRepository(connString, mandantProvider);
        }

        // ---------------- Config. ----------------
        public IActionResult Config()
        {
            return View();
        }



        // ---------------- Abteilung & Referaten. ----------------
        [HttpGet]
        public IActionResult AbRef(int? pageNumber, int pageSize = 10, string abteilungSearchTerm = "", string referatSearchTerm = "", string sortColumn = null, string sortDirection = null)
        {
            try
            {
                // Aktuelle Seite bestimmen (Standard = 1).
                int currentPage = pageNumber ?? 1;

                // Sortierung festlegen, Standard: Id aufsteigend.
                var effectiveSortColumn = string.IsNullOrEmpty(sortColumn) ? "Id" : sortColumn;
                var effectiveSortDirection = string.IsNullOrEmpty(sortDirection) ? "ASC" : sortDirection;

                // Gesamtanzahl der Abteilungen ermitteln.
                int totalCount = _repo.GetAbteilungenCount(abteilungSearchTerm: abteilungSearchTerm, referatSearchTerm: referatSearchTerm);

                // Gesamtseiten berechnen.
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                if (totalPages == 0)
                {
                    totalPages = 1;
                }
                if (currentPage > totalPages)
                {
                    currentPage = 1;
                }

                // Abteilungen-Seite abrufen.
                var items = _repo.GetAbteilungenPage(
                    pageNumber: currentPage,
                    pageSize: pageSize,
                    abteilungSearchTerm: abteilungSearchTerm,
                    referatSearchTerm: referatSearchTerm,
                    sortColumn: effectiveSortColumn,
                    sortDirection: effectiveSortDirection
                );

                // Paginierte Liste erstellen.
                var pagedList = new PaginatedList<AbteilungReferateViewModel>(items, totalCount, currentPage, pageSize);

                // ViewModel füllen.
                var model = new AdminPanelViewModel
                {
                    Abteilungen = pagedList,
                    AbteilungSearchTerm = abteilungSearchTerm,
                    ReferatSearchTerm = referatSearchTerm,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection
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

        // ---------------- Abteilung. ----------------
        // Abteilung erstellen.
        [HttpPost]
        public IActionResult CreateAbteilung(string name)
        {
            try
            {
                // Prüfen, ob Name leer ist.
                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["Message"] = "Name darf nicht leer sein!";
                    TempData["MessageType"] = "error";
                    return RedirectToAction("AbRef");
                }

                // Abteilung erstellen.
                bool success = _repo.CreateAbteilung(name: name);

                // Prüfen, ob Erstellung erfolgreich war.
                if (!success)
                {
                    TempData["Message"] = $"Abteilung '{name}' existiert bereits!";
                    TempData["MessageType"] = "error";
                    return RedirectToAction("AbRef");
                }

                // Gesamtanzahl nach Erstellung abrufen.
                int pageSize = 10;
                int totalCount = _repo.GetAbteilungenCount();

                // Letzte Seite berechnen.
                int lastPage = (int)Math.Ceiling((double)totalCount / pageSize);

                // Zur letzten Seite weiterleiten.
                TempData["Message"] = $"Abteilung '{name}' erfolgreich erstellt!";
                TempData["MessageType"] = "success";
                return RedirectToAction("AbRef", new { pageNumber = lastPage, pageSize });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        // Abteilung bearbeiten.
        [HttpPost]
        public IActionResult EditAbteilung(int id, string newName)
        {
            try
            {
                // Prüfen, ob Name leer ist.
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return Json(new { success = false, message = "Name darf nicht leer sein" });
                }

                // Alten Namen holen um PersonRollen/AbteilungReferat zu aktualisieren.
                string oldName = _repo.GetAllAbteilungen().FirstOrDefault(a => a.Id == id)?.Name;

                // Abteilung ändern.
                bool success = _repo.EditAbteilung(id: id, newName: newName);

                // Prüfen, ob Änderung erfolgreich war.
                if (!success)
                {
                    // JSON-Fehler zurückgeben.
                    return Json(new { success = false, message = $"Abteilung '{newName}' existiert bereits!" });
                }

                // Auch in PersonRollen und AbteilungReferat aktualisieren.
                _repo.UpdateAbteilungReferat(
                    oldAbteilung: oldName,
                    oldReferat: null,
                    newAbteilung: newName,
                    newReferat: null
                );


                // Erfolgreiche Änderung zurückgeben.
                return Json(new { success = true, message = $"Abteilung '{newName}' erfolgreich bearbeitet!" });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        // Abteilung löschen.
        [HttpPost]
        public IActionResult DeleteAbteilung(int id)
        {
            try
            {
                // Alten Namen holen um PersonRollen/AbteilungReferat zu aktualisieren.
                string oldName = _repo.GetAllAbteilungen().FirstOrDefault(a => a.Id == id)?.Name;

                bool deleted = _repo.DeleteAbteilung(id: id);

                if (!deleted)
                {
                    TempData["Message"] = "Eine Abteilung kann nicht gelöscht werden, solange noch Referate damit verknüpft sind!";
                    TempData["MessageType"] = "error";
                    return Json(new { success = false, message = "Eine Abteilung kann nicht gelöscht werden, solange noch Referate damit verknüpft sind!" });
                }
                TempData["Message"] = "Abteilung erfolgreich gelöscht!";

                // Auch in PersonRollen und AbteilungReferat löschen.
                _repo.DeleteAbteilungReferat(abteilung: oldName, referat: null);

                TempData["MessageType"] = "success";
                return Json(new { success = true, message = "Abteilung erfolgreich gelöscht!" });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        // ---------------- Referat. ----------------
        // Referat erstellen.
        [HttpPost]
        public IActionResult CreateReferat(int abteilungId, string name)
        {
            try
            {
                // Prüfen, ob Name leer ist.
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Name darf nicht leer sein!" });
                }

                // Prüfen, ob Referat bereits existiert.
                if (_repo.ReferatExists(abteilungId, name))
                {
                    return Json(new { success = false, message = $"Referat '{name}' existiert bereits in dieser Abteilung!" });
                }

                // Neues Referat erstellen.
                var referat = _repo.CreateReferat(abteilungId: abteilungId, name: name.Trim());

                // Erfolgreiches Ergebnis zurückgeben.
                return Json(new
                {
                    success = true,
                    referat = new { id = referat.Id, name = referat.Name },
                    message = $"Referat '{name}' erfolgreich erstellt!"
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

        // Referat bearbeiten.
        [HttpPost]
        public IActionResult EditReferat(int id, string name)
        {
            try
            {
                // Prüfen, ob Name leer ist.
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Name darf nicht leer sein!" });
                }

                // Alten Namen holen um PersonRollen/AbteilungReferat zu aktualisieren.
                string oldName = _repo.GetReferatById(id).Name;

                // Referat ändern.
                var success = _repo.EditReferat(id: id, newName: name);

                // Prüfen, ob Änderung erfolgreich war.
                if (!success)
                {
                    return Json(new { success = false, message = $"Referat '{name}' existiert bereits in dieser Abteilung!" });
                }

                // Auch in PersonRollen und AbteilungReferat aktualisieren.
                _repo.UpdateAbteilungReferat(
                    oldAbteilung: null,
                    oldReferat: oldName,
                    newAbteilung: null,
                    newReferat: name
                );

                return Json(new { success = true, message = $"Referat '{name}' erfolgreich bearbeitet!" });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        // Referat löschen.
        [HttpPost]
        public IActionResult DeleteReferat(int id)
        {
            try
            {
                // Alten Namen holen um PersonRollen/AbteilungReferat zu aktualisieren.
                string oldName = _repo.GetReferatById(id).Name;

                _repo.DeleteReferat(Id: id);

                // Auch in PersonRollen und AbteilungReferat bereinigen.
                _repo.DeleteAbteilungReferat(abteilung: null, referat: oldName);


                return RedirectToAction("AbRef");
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }




        // -----------------  Personen. ----------------
        [HttpGet]
        public IActionResult Personen(int? pageNumber, int pageSize = 10, string sortColumn = "Id", string sortDirection = "ASC", string titel = "", string name = "", string vorname = "", string email = "", string uid = "", string abteilung = "", string referat = "", string stelle = "", string rolle = "")
        {
            try
            {
                // Aktuelle Seite bestimmen (Standard = 1).
                int currentPage = pageNumber ?? 1;

                // Filterobjekt erstellen.
                var filter = new PersonenFilter
                {
                    Titel = titel,
                    Name = name,
                    Vorname = vorname,
                    Email = email,
                    UId = uid,
                    Abteilung = abteilung,
                    Referat = referat,
                    Stelle = stelle,
                    Rolle = rolle
                };

                // Gesamtanzahl der Personen nach Filter ermitteln.
                int totalCount = _repo.GetPersonenCount(filter: filter);

                // Gesamtseiten berechnen.
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                if (totalPages == 0)
                {
                    totalPages = 1;
                }
                if (currentPage > totalPages)
                {
                    currentPage = 1;
                }

                // Aktuelle Seite abrufen.
                var items = _repo.GetPersonenPage(pageNumber: currentPage, pageSize: pageSize, filter: filter, sortColumn: sortColumn, sortDirection: sortDirection);

                // Paginierte Liste erstellen.
                var pagedList = new PaginatedList<Personen>(items, totalCount, currentPage, pageSize);

                // Rollenliste für Dropdown bereitstellen.
                var rollenListe = _repo.GetAllRollen();

                // ViewModel füllen.
                var model = new AdminPanelViewModel
                {
                    Personen = pagedList,
                    Filter = filter,
                    // Rollenliste für Dropdown in ViewModel packen.
                    RollenListe = rollenListe
                };

                // Sortierung und Gesamtanzahl für die View bereitstellen.
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortDirection = sortDirection;
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

        // Benutzer erstellen (Formular anzeigen).
        [HttpGet]
        public IActionResult CreateUser()
        {
            try
            {
                // Alle Abteilungen für Dropdown bereitstellen.
                ViewBag.Abteilungen = _repo.GetAllAbteilungen();

                // Titelliste für Dropdown bereitstellen.
                ViewBag.TitelListe = new SelectList(new List<string> { "Herr", "Frau", "Dr.", "Prof." });
                ViewBag.RolleListe = ViewBag.RollenListe = _repo.GetAllRollen();

                // Leeres Personen-Objekt an die View übergeben.
                return View(new Personen());
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        // Benutzer erstellen (Formular absenden).
        [HttpPost]
        public IActionResult CreateUser(Personen person, List<string> SelectedRollen, List<string> SelectedAbteilungen, List<string> SelectedReferate)
        {
            try
            {

                person.Rollen = SelectedRollen?.Select(r => new PersonRolle { Rolle = r }).ToList()
                ?? new List<PersonRolle>();

                // "ReferatName:AbteilungName"
                var referateParsed = SelectedReferate?
                    .Select(raw => {
                        var parts = raw.Split(':');
                        return new { ReferatName = parts[0], AbteilungName = parts.Length > 1 ? parts[1] : "" };
                    }).ToList() ?? new();


                ModelState.Clear();

                // Vorname und Nachname prüfen.
                if (!Regex.IsMatch(person.Name ?? "", @"^[A-Za-zÄÖÜäöüß\s\-]+$") ||
                    !Regex.IsMatch(person.Vorname ?? "", @"^[A-Za-zÄÖÜäöüß\s\-]+$"))
                {
                    ViewBag.Error = "Name und Vorname dürfen nur Buchstaben enthalten + Email + Kennwort.";

                    // Dropdown-Listen zurückgeben.
                    ViewBag.Abteilungen = _repo.GetAllAbteilungen();
                    ViewBag.TitelListe = new SelectList(new List<string> { "Herr", "Frau", "Dr.", "Prof." });
                    ViewBag.RolleListe = ViewBag.RollenListe = _repo.GetAllRollen();

                    return View(person);
                }

                // Email prüfen.
                if (!Regex.IsMatch(person.Email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ViewBag.Error = "Email ist nicht gültig.";
                    ViewBag.Abteilungen = _repo.GetAllAbteilungen();
                    ViewBag.TitelListe = new SelectList(new List<string> { "Herr", "Frau", "Dr.", "Prof." });
                    ViewBag.RolleListe = ViewBag.RollenListe = _repo.GetAllRollen();
                    return View(person);
                }

                // Prüfen ob Email bereits existiert.
                if (_repo.UserExistsByEmail(person.Email))
                {
                    ViewBag.Error = "Ein Benutzer mit dieser Email existiert bereits.";

                    // Dropdown-Listen zurückgeben.
                    ViewBag.Abteilungen = _repo.GetAllAbteilungen();
                    ViewBag.TitelListe = new SelectList(new List<string> { "Herr", "Frau", "Dr.", "Prof." });
                    ViewBag.RolleListe = ViewBag.RollenListe = _repo.GetAllRollen();

                    return View(person);
                }


                // Passwort prüfen.
                if (string.IsNullOrWhiteSpace(person.Kennwort))
                {
                    ViewBag.Error = "Kennwort darf nicht leer sein";

                    // Dropdown-Listen zurückgeben.
                    ViewBag.Abteilungen = _repo.GetAllAbteilungen();
                    ViewBag.TitelListe = new SelectList(new List<string> { "Herr", "Frau", "Dr.", "Prof." });
                    ViewBag.RolleListe = ViewBag.RollenListe = _repo.GetAllRollen();

                    return View(person);
                }

                // Wenn Model gültig, Person erstellen.
                if (ModelState.IsValid)
                {
                    _repo.CreatePerson(person, SelectedAbteilungen ?? new(), referateParsed.Select(r => (r.ReferatName, r.AbteilungName)).ToList()); ;

                    // Gesamtanzahl der Personen abrufen.
                    int totalCount = _repo.GetPersonenCount(new PersonenFilter());
                    int pageSize = 10;

                    // Letzte Seite berechnen.
                    int lastPage = (int)Math.Ceiling((double)totalCount / pageSize);

                    // Zur letzten Seite weiterleiten.
                    TempData["Message"] = $"Persone '{person.Name + person.Vorname}' erfolgreich erstellt!";
                    TempData["MessageType"] = "success";
                    return RedirectToAction("Personen", new { pageNumber = lastPage });
                }

                // Dropdown-Listen für fehlerhafte Eingabe zurückgeben.
                ViewBag.Abteilungen = _repo.GetAllAbteilungen();
                ViewBag.TitelListe = new SelectList(new List<string> { "Herr", "Frau", "Dr.", "Prof." });
                ViewBag.RolleListe = ViewBag.RollenListe = _repo.GetAllRollen();

                return View(person);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        // Ajax: Referate einer Abteilung abrufen.
        [HttpGet]
        public JsonResult GetReferateByAbteilung(int abteilungId)
        {
            // Referate der Abteilung vom Repository holen.
            var referate = _repo.GetReferateByAbteilungId(abteilungId: abteilungId);

            // Als JSON zurückgeben.
            return Json(referate);
        }

        // Benutzer bearbeiten (Formular anzeigen).
        [HttpGet]
        public IActionResult EditUser(int id, int pageNumber = 1, int pageSize = 10, string sortColumn = "Id", string sortDirection = "ASC")
        {
            try
            {
                // Person nach ID abrufen.
                var person = _repo.GetPersonById(id: id);
                if (person == null)
                {
                    return NotFound();
                }

                // Dropdowns und Referate für das Formular laden.
                LoadDropdowns(person);

                // Seitenzahl und Sortierung für die View bereitstellen.
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortDirection = sortDirection;

                return View(person);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        // Dropdowns für Benutzerformular laden.
        private void LoadDropdowns(Personen person)
        {
            ViewBag.Abteilungen = _repo.GetAllAbteilungen();
            ViewBag.TitelListe = new SelectList(new List<string> { "Herr", "Frau", "Dr.", "Prof." }, person.Titel);
            ViewBag.RollenListe = _repo.GetAllRollen();
            ViewBag.AktuelleRollen = _repo.GetRollenByPersonId(person.Id).Select(r => r.Rolle).Distinct().ToList();
            ViewBag.AktuelleAbteilungen = _repo.GetAbteilungenByPersonId(person.Id);
            ViewBag.AktuelleReferate = _repo.GetReferateByPersonId(person.Id); // "Referat:Abteilung"
        }

        // Benutzer bearbeiten (Formular absenden).
        [HttpPost]
        public IActionResult EditUser(Personen person, List<string> SelectedRollen, List<string> SelectedAbteilungen, List<string> SelectedReferate, int pageNumber = 1, int pageSize = 10, string sortColumn = "Id", string sortDirection = "ASC")
        {
            try
            {
                person.Rollen = SelectedRollen?.Select(r => new PersonRolle { Rolle = r }).ToList() ?? new List<PersonRolle>();

                bool hasError = false;
                string errorMessage = "";

                // Name prüfen.
                if (!Regex.IsMatch(person.Name ?? "", @"^[A-Za-zÄÖÜäöüß\s\-]+$"))
                {
                    ModelState.AddModelError(nameof(person.Name), "Name darf nur Buchstaben enthalten.");
                    errorMessage += "Name darf nur Buchstaben enthalten. ";
                    hasError = true;
                }

                // Vorname prüfen.
                if (!Regex.IsMatch(person.Vorname ?? "", @"^[A-Za-zÄÖÜäöüß\s\-]+$"))
                {
                    ModelState.AddModelError(nameof(person.Vorname), "Vorname darf nur Buchstaben enthalten.");
                    errorMessage += "Vorname darf nur Buchstaben enthalten. ";
                    hasError = true;
                }

                // Email prüfen.
                if (!Regex.IsMatch(person.Email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ModelState.AddModelError(nameof(person.Email), "Email ist nicht gültig.");
                    errorMessage += "Email ist nicht gültig. ";
                    hasError = true;
                }

                // Prüfen ob Email bereits existiert.
                if (_repo.UserExistsByEmailExceptId(person.Email, person.Id))
                {

                    ViewBag.Abteilungen = _repo.GetAllAbteilungen();

                    errorMessage += "Ein Benutzer mit dieser Email existiert bereits.";
                    hasError = true;
                }

                // Wenn Fehler vorhanden, Formular mit Fehlern erneut anzeigen.
                if (hasError)
                {
                    ViewBag.Error = errorMessage.Trim();
                    LoadDropdowns(person);
                    ViewBag.AktuelleRollen = SelectedRollen ?? new List<string>();

                    // Seitenzahl und Sortierung für View bereitstellen.
                    ViewBag.PageNumber = pageNumber;
                    ViewBag.PageSize = pageSize;
                    ViewBag.SortColumn = sortColumn;
                    ViewBag.SortDirection = sortDirection;

                    return View(person);
                }

                // Person aktualisieren.
                person.Rollen = SelectedRollen?.Select(r => new PersonRolle { Rolle = r }).ToList() ?? new List<PersonRolle>();

                var referateParsed = SelectedReferate?
                    .Select(raw => {
                        var parts = raw.Split(':');
                        return new { ReferatName = parts[0], AbteilungName = parts.Length > 1 ? parts[1] : "" };
                    }).ToList() ?? new();

                _repo.UpdatePerson(person: person, abteilungen: SelectedAbteilungen ?? new(),
                    referate: referateParsed.Select(r => (r.ReferatName, r.AbteilungName)).ToList());

                TempData["Message"] = $"Persone '{person.Name + person.Vorname}' erfolgreich bearbeitet!";
                TempData["MessageType"] = "success";

                // Zurück zur gleichen Seite.
                return RedirectToAction("Personen", new
                {
                    pageNumber,
                    pageSize,
                    sortColumn,
                    sortDirection
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

        // Benutzer löschen.
        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserId");
                var currentUserIsAdmin = HttpContext.Session.GetInt32("IstAdmin");

                if (currentUserId == null)
                {
                    return Json(new { success = false, message = "Ungültige Sitzung. Bitte erneut anmelden!" });
                }

                var targetUser = _repo.GetPersonById(id);
                if (targetUser == null)
                {
                    return Json(new { success = false, message = "Benutzer wurde nicht gefunden!" });
                }

                if (id == currentUserId)
                {
                    return Json(new { success = false, message = "Sie können Ihren eigenen Account nicht löschen!" });
                }

                if (_repo.GetRollenByPersonId(id).Any(r => r.Rolle == "Administrator"))
                {
                    return Json(new { success = false, message = "Sie können keinen Administrator löschen!" });
                }

                if (id > 0)
                {
                    _repo.DeletePerson(id: id);
                    return Json(new { success = true, message = "Benutzer erfolgreich gelöscht!" });
                }

                return Json(new { success = false, message = "Ungültige Benutzer-ID!" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }



        // ---------------- TKennwort. ----------------
        // Temporäres Passwort anzeigen (Formular).
        [HttpGet]
        public IActionResult TKennwort(int userId)
        {
            try
            {
                // Benutzer anhand der ID abrufen.
                var user = _repo.GetPersonById(id: userId);
                if (user == null)
                    return NotFound();

                return View(user);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        // Temporäres Passwort setzen (Formular absenden).
        [HttpPost]
        public IActionResult TKennwort(int Id, string TKennowort)
        {
            try
            {
                // Prüfen, ob Passwort leer ist.
                if (string.IsNullOrWhiteSpace(TKennowort))
                {
                    TempData["Error"] = "Error!";
                    return RedirectToAction("TKennwort", new { userId = Id });
                }

                try
                {
                    // Passwort setzen.
                    _repo.SetTemporaryPassword(userId: Id, tempPassword: TKennowort);
                    TempData["Success"] = "Success!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Fehler: {ex.Message}";
                }

                // Zur Personenliste zurückkehren.
                return RedirectToAction("Personen");
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
