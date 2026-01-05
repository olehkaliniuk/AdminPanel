using AdminPanelDB.Exeptions;
using AdminPanelDB.Filters;
using AdminPanelDB.Logs;
using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using AdminPanelDB.ViewModels;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace AdminPanelDB.Controllers.Admin
{
    [SessionAuthorize(AdminOnly = true)]
    public class ConfigController : Controller
    {
        private readonly ConfigRepository _repository;
        private readonly ConfigLogRepository _configLogRepository;

        protected readonly string TableName = "Config";


        private Config _currentConfig;



        public ConfigController(ConfigRepository repository, ConfigLogRepository configLog)
        {
            _repository = repository;
            _configLogRepository = configLog;

        }


        // Aktuelle Konfiguration aktualisieren.
        private void RefreshConfig()
        {
            _currentConfig = _repository.LoadConfig();
        }



        // Anzeige aller Konfigurationseinträge mit Filter, Sortierung und Pagination.
        [HttpGet]
        public IActionResult Config(
            string system, string projektName, string keyBezeichnung,
            string stringValue, string intValue, string boolValue,
            string dateTimeValue, string beschreibung, string istAktiv,
            string anpassungGesperrt, string angepasstAm, string angepasstVon,
            string sortColumn = "Id", string sortDirection = "ASC",
            int page = 1, int pageSize = 10)
        {
            try
            {
                // Gesamtanzahl der gefilterten Einträge ermitteln.
                var totalCount = _repository.GetFilteredCount(
                    system, projektName, keyBezeichnung,
                    stringValue, intValue, boolValue,
                    dateTimeValue, beschreibung, istAktiv,
                    anpassungGesperrt, angepasstAm, angepasstVon);

                // Gesamtseiten berechnen.
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                if (totalPages == 0)
                {
                    totalPages = 1;
                }

                if (page > totalPages)
                {
                    page = 1;
                }

                // Gefilterte, sortierte und paginierte Einträge abrufen.
                var entries = _repository.GetPagedFiltered(
                    system, projektName, keyBezeichnung,
                    stringValue, intValue, boolValue,
                    dateTimeValue, beschreibung, istAktiv,
                    anpassungGesperrt, angepasstAm, angepasstVon,
                    sortColumn, sortDirection, page, pageSize);

                // Filterwerte für die View speichern.
                ViewBag.Filters = new
                {
                    system,
                    projektName,
                    keyBezeichnung,
                    stringValue,
                    intValue,
                    boolValue,
                    dateTimeValue,
                    beschreibung,
                    istAktiv,
                    anpassungGesperrt,
                    angepasstAm,
                    angepasstVon,
                    sortColumn,
                    sortDirection
                };

                // Pagination-Daten für die View.
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalCount = totalCount;

                return View(entries);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
 
        }



        // GET: Neues Config-Formular anzeigen.
        [HttpGet]
        public IActionResult CreateConfig()
        {
            var model = new ConfigEntry();

            // Standardwerte setzen.
            ViewBag.DefaultSystem = "Sandbox";
            ViewBag.DefaultProject = "AdminPanel";

            return View(model);
        }



        // POST: Neuen Config-Eintrag erstellen.
        [HttpPost]
        public IActionResult CreateConfig(ConfigEntry entry)
        {
            // Pflichtfelder prüfen.
            if (string.IsNullOrWhiteSpace(entry.KeyBezeichnung))
            {
                ViewBag.Error = "KeyBezeichnung ist leer.";
                return View(entry);
            }
            if (string.IsNullOrWhiteSpace(entry.Beschreibung))
            {
                ViewBag.Error = "Beschreibung ist leer.";
                return View(entry);
            }

            if (ModelState.IsValid)
            {
                // User aus Session holen.
                var email = HttpContext.Session.GetString("UserEmail");
                var userName = HttpContext.Session.GetString("UserName") ?? email;
                var fullUserName = $"{userName} / {email}";

                try
                {
                    // Eintrag erstellen.
                    _repository.Create(entry, fullUserName);

                    // Zeit und Name aktualisieren für LogCreate.
                    entry.EintragAngepasstAm = DateTime.Now;
                    entry.EintragAngepasstVon = fullUserName;


                    // Log erstellen.
                    _configLogRepository.LogCreate(entry, fullUserName, TableName);

                    // letzte Seite berechnen.
                    int totalCount = _repository.GetFilteredCount(
                        system: null, projektName: null, keyBezeichnung: null,
                        stringValue: null, intValue: null, boolValue: null,
                        dateTimeValue: null, beschreibung: null, istAktiv: null,
                        anpassungGesperrt: null, angepasstAm: null, angepasstVon: null
                    );
                    int pageSize = 10;
                    int lastPage = (int)Math.Ceiling((double)totalCount / pageSize);

                    TempData["Message"] = $"Konfiguration '{entry.KeyBezeichnung}' erfolgreich erstellt!";
                    TempData["MessageType"] = "success";

                    // Weiterleitung zur letzten Seite.
                    return RedirectToAction("Config", new { page = lastPage });
                }
                catch(RepositoryExceptions ex)
                {
                    Logger.LogError(ex, ex.Message, 1001);

                    //ViewBag.Error = $"Fehler beim Erstellen des Config-Eintrags. '{ex}'";

                    TempData["FriendlyMessage"] = ex.Message;

                    TempData["ErrorDetails"] = ex.ToString();

                    return RedirectToAction("Index", "ExceptionsError");
                }
             
            }

            return View(entry);
        }



        // Dropdown-Optionen für Config-Formular.
        private void LoadConfigDropdowns(ConfigEntry entry)
        {
            ViewBag.SystemOptions = new List<string> { "Sandbox", "Production", "Test" };
            ViewBag.ProjectOptions = new List<string> { "AdminPanel", "UserPortal", "Reports", "Allgemein", "Verfuegungsblatt", "Vorlagenverwaltung", "VorgangsDatenSonstigeGebuehren", "DigitalerUmlauf", "EotaPortal", "Ebb" };
        }



        // GET: Config-Eintrag bearbeiten.
        [HttpGet]
        public IActionResult EditConfig(int id, int page = 1, string sortColumn = "Id", string sortDirection = "ASC", int pageSize = 10)
        {
            try
            {
                // Eintrag nach Id holen.
                var entry = _repository.GetById(id);
                if (entry == null)
                {
                    return NotFound();
                }

                // Dropdowns laden.
                LoadConfigDropdowns(entry);

                // Pagination und Sortierung setzen.
                ViewBag.Page = page;
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortDirection = sortDirection;
                ViewBag.PageSize = pageSize;

                return View(entry);
            }
            catch (RepositoryExceptions ex) {
                Logger.LogError(ex, ex.Message, 1001);

                //ViewBag.Error = $"Fehler beim Erstellen des Config-Eintrags. '{ex}'";

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }



        // POST: Config-Eintrag bearbeiten.
        [HttpPost]
        public IActionResult EditConfig(ConfigEntry entry, int page = 1, string sortColumn = "Id", string sortDirection = "ASC", int pageSize = 10)
        {
            // Existierenden Eintrag prüfen.
            var existingEntry = _repository.GetById(entry.Id);
            if (existingEntry == null)
            {
                return NotFound();
            } 

            // Validierung.
            bool hasError = false;
            string errorMessage = "";

            if (string.IsNullOrWhiteSpace(entry.KeyBezeichnung))
            {
                errorMessage += "KeyBezeichnung ist leer. ";
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(entry.Beschreibung))
            {
                errorMessage += "Beschreibung ist leer. ";
                hasError = true;
            }

            if (hasError)
            {
                // Fehler anzeigen.
                ViewBag.Error = errorMessage.Trim();
                LoadConfigDropdowns(entry);

                // Pagination und Sortierung setzen.
                ViewBag.Page = page;
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortDirection = sortDirection;
                ViewBag.PageSize = pageSize;

                return View(entry);
            }

            // Bool-Wert aus Formular verarbeiten.
            var boolValueFromForm = Request.Form["BoolValue"];
            entry.BoolValue = string.IsNullOrEmpty(boolValueFromForm) ? (bool?)null : boolValueFromForm == "true";

            // Benutzer für Änderungsprotokoll holen.
            var email = HttpContext.Session.GetString("UserEmail");
            var userName = HttpContext.Session.GetString("UserName") ?? email;
            var fullUserName = $"{userName} / {email}";

            try
            {
                // Eintrag aktualisieren und Konfiguration neu laden.
                _repository.Update(entry, fullUserName);

                // Zeit und Name aktualisieren für LogUpdate.
                entry.EintragAngepasstAm = DateTime.Now;
                entry.EintragAngepasstVon = fullUserName;

                // Log erstellen.
                _configLogRepository.LogUpdate(existingEntry, entry, fullUserName, TableName);

                RefreshConfig();

                TempData["Message"] = $"Konfiguration '{entry.KeyBezeichnung}' erfolgreich bearbeitet!";
                TempData["MessageType"] = "success";
                // Zurück zur Config-Übersicht.
                return RedirectToAction("Config", new
                {
                    page,
                    sortColumn,
                    sortDirection,
                    pageSize
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



        // Config-Eintrag löschen.
        [HttpPost]
        public IActionResult DeleteConfig(int id)
        {
            try
            {
                var currentUser = GetCurrentUser();

                // Old Config.
                var oldEntry = _repository.GetById(id);

                _repository.Delete(id);


                _configLogRepository.LogDelete(id, oldEntry, currentUser, TableName);

                RefreshConfig(); // Konfiguration neu laden.
                return Json(new
                {
                    success = true,
                    message = "Konfiguration erfolgreich gelöscht!"
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



        // Aktuelle Konfiguration zurückgeben.
        public Config GetCurrentConfig()
        {
            return _currentConfig; // Liefert die zuletzt geladene Config.
        }



        // Alle Konfigurationseinstellungen als JSON zurückgeben.
        public IActionResult ShowConfig()
        {
            try
            {
                var config = _repository.LoadConfig(); // Config aus Repository laden.
                return Json(config); // JSON-Antwort mit allen Einstellungen.
            }
            catch (RepositoryExceptions ex) {
                Logger.LogError(ex, "Fehler beim Laden des Configs.", 1001);



                TempData["FriendlyMessage"] = "Fehler beim Löschen des Configs.";

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }



        //Config Logs.
        private string Serialize(ConfigEntry entry)
        {
            return JsonSerializer.Serialize(entry, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        private string GetCurrentUser()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var email = HttpContext.Session.GetString("UserEmail");

            return $"{userName} / {email}";
        }


    }
}
