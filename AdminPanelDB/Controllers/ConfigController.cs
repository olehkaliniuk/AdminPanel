using AdminPanelDB.Abstractions;
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

        public ConfigController(IConfiguration configuration, ILogger<ConfigRepository> logger, ICurrentMandantProvider mandantProvider, ConfigLogRepository configLog)
        {
            _repository = new ConfigRepository(configuration, logger, mandantProvider);
            _configLogRepository = configLog;
        }

        // Aktuelle Konfiguration aktualisieren.
        private void RefreshConfig()
        {
            _currentConfig = _repository.LoadConfig();
        }

        // Aktive Datenbankverbindung aus Session oder Standard zurückgeben.
        private string GetActiveConnection()
        {
            return HttpContext.Session.GetString("SelectedConnection") ?? "DefaultConnection";
        }

        // Anzeige aller Konfigurationseinträge mit Filter, Sortierung und Pagination.
        [HttpGet]
        public IActionResult Config(
            string system, string projektName, string keyBezeichnung,
            string stringValue, string intValue, string boolValue,
            string dateTimeValue, string beschreibung, string istAktiv,
            string anpassungGesperrt, string angepasstAm, string angepasstVon, string istSensibleAnpassung, string mandantId,
            string sortColumn = "Id", string sortDirection = "ASC",
            int page = 1, int pageSize = 10, string selectedConnection = null)
        {

            if (selectedConnection != null)
                HttpContext.Session.SetString("SelectedConnection", selectedConnection);

            var activeConnection = GetActiveConnection();
            ViewBag.SelectedConnection = activeConnection;

            try
            {
                // Gesamtanzahl der gefilterten Einträge ermitteln.
                var totalCount = _repository.GetFilteredCount(
                    system: system, projektName: projektName, keyBezeichnung: keyBezeichnung,
                    stringValue: stringValue, intValue: intValue, boolValue: boolValue,
                    dateTimeValue: dateTimeValue, beschreibung: beschreibung, istAktiv: istAktiv,
                    anpassungGesperrt: anpassungGesperrt, istSensibleAnpassung: istSensibleAnpassung,
                    angepasstAm: angepasstAm, angepasstVon: angepasstVon, mandantId: mandantId,
                    connectionName: GetActiveConnection());

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
                    system: system, projektName: projektName, keyBezeichnung: keyBezeichnung,
                    stringValue: stringValue, intValue: intValue, boolValue: boolValue,
                    dateTimeValue: dateTimeValue, beschreibung: beschreibung, istAktiv: istAktiv,
                    anpassungGesperrt: anpassungGesperrt, istSensibleAnpassung: istSensibleAnpassung,
                    angepasstAm: angepasstAm, angepasstVon: angepasstVon, mandantId: mandantId,
                    sortColumn: sortColumn, sortDirection: sortDirection, page: page, pageSize: pageSize,
                    connectionName: GetActiveConnection());

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
                    istSensibleAnpassung,
                    angepasstAm,
                    angepasstVon,
                    mandantId,
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



        //// GET: Neues Config-Formular anzeigen.
        [HttpGet]
        public IActionResult CreateConfig()
        {
            var model = new ConfigEntry();

            // Standardwerte setzen.
            ViewBag.DefaultSystem = "Sandbox";
            ViewBag.DefaultProject = "AdminPanel";

            // Rollen aus Session lesen.
            var rollenJson = HttpContext.Session.GetString("Rollen");
            var rollen = string.IsNullOrEmpty(rollenJson)
                ? new List<string>()
                : Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(rollenJson);

            // Sichtbarkeit für sensible Anpassung.
            ViewBag.HasDreiF = rollen.Contains("DreiF");

            return View(model);
        }

        // POST: Neuen Config-Eintrag erstellen.
        [HttpPost]
        public IActionResult CreateConfig(ConfigEntry entry)
        {
            // Rollen aus Session lesen.
            var rollenJson = HttpContext.Session.GetString("Rollen");
            var rollen = string.IsNullOrEmpty(rollenJson)
                ? new List<string>()
                : Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(rollenJson);
            bool hasDreiF = rollen.Contains("DreiF");

            // Sensible Anpassung nur erlauben, wenn Rolle vorhanden.
            entry.IstSensibleAnpassung = hasDreiF && entry.IstSensibleAnpassung == true;
            entry.IstGlobal = hasDreiF && entry.IstGlobal;   // global nur für DreiF
            entry.IstAktiv = entry.IstAktiv == true;
            entry.AnpassungGesperrt = entry.AnpassungGesperrt == true;


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
                    _repository.Create(entry: entry, userName: fullUserName, connectionName: GetActiveConnection());

                    // Zeit und Name aktualisieren für LogCreate.
                    entry.EintragAngepasstAm = DateTime.Now;
                    entry.EintragAngepasstVon = fullUserName;




                    // Log erstellen.
                    _configLogRepository.LogCreate(entry, fullUserName, GetActiveConnection());

                    // letzte Seite berechnen.
                    int totalCount = _repository.GetFilteredCount(
                        system: null, projektName: null, keyBezeichnung: null,
                        stringValue: null, intValue: null, boolValue: null,
                        dateTimeValue: null, beschreibung: null, istAktiv: null,
                        anpassungGesperrt: null, istSensibleAnpassung: null, angepasstAm: null, angepasstVon: null, mandantId: null, connectionName: GetActiveConnection()
                    );
                    int pageSize = 10;
                    int lastPage = (int)Math.Ceiling((double)totalCount / pageSize);

                    TempData["Message"] = $"Konfiguration '{entry.KeyBezeichnung}' erfolgreich erstellt!";
                    TempData["MessageType"] = "success";

                    // Weiterleitung zur letzten Seite.
                    return RedirectToAction("Config", new { page = lastPage });
                }
                catch (RepositoryExceptions ex)
                {
                    Logger.LogError(ex, ex.Message, 1001);

                    //ViewBag.Error = $"Fehler beim Erstellen des Config-Eintrags. '{ex}'";

                    TempData["FriendlyMessage"] = ex.Message;

                    TempData["ErrorDetails"] = ex.ToString();

                    return RedirectToAction("Index", "ExceptionsError");
                }

            }
            ViewBag.HasDreiF = hasDreiF;
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
                var entry = _repository.GetById(id: id, connectionName: GetActiveConnection());
                if (entry == null)
                {
                    return NotFound();
                }

                // Rollen prüfen.
                var rollenJson = HttpContext.Session.GetString("Rollen");
                var rollen = string.IsNullOrEmpty(rollenJson)
                    ? new List<string>()
                    : Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(rollenJson);
                bool hasDreiF = rollen.Contains("DreiF");

                // Gesperrt für sensible Anpassung oder globalen Eintrag, wenn Rolle fehlt.
                if ((entry.IstSensibleAnpassung == true || entry.IstGlobal) && !hasDreiF)
                {
                    return Forbid();
                }

                // Gesperrte Einträge können von niemandem bearbeitet werden.
                if (entry.AnpassungGesperrt == true)
                {
                    TempData["Message"] = $"Konfiguration '{entry.KeyBezeichnung}' ist gesperrt. Bitte zuerst entsperren.";
                    TempData["MessageType"] = "error";
                    return RedirectToAction("Config", new { page, sortColumn, sortDirection, pageSize });
                }

                ViewBag.HasDreiF = hasDreiF;

                // Dropdowns laden.
                LoadConfigDropdowns(entry);

                // Pagination und Sortierung setzen.
                ViewBag.Page = page;
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortDirection = sortDirection;
                ViewBag.PageSize = pageSize;

                ViewBag.SelectedConnection = GetActiveConnection();

                return View(entry);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        // POST: Config-Eintrag bearbeiten.
        [HttpPost]
        public IActionResult EditConfig(ConfigEntry entry, int page = 1, string sortColumn = "Id", string sortDirection = "ASC", int pageSize = 10)
        {
            // Rollen prüfen.
            var rollenJson = HttpContext.Session.GetString("Rollen");
            var rollen = string.IsNullOrEmpty(rollenJson)
                ? new List<string>()
                : Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(rollenJson);
            bool hasDreiF = rollen.Contains("DreiF");

            // Existierenden Eintrag prüfen.
            var existingEntry = _repository.GetById(id: entry.Id, connectionName: GetActiveConnection());
            if (existingEntry == null)
            {
                return NotFound();
            }

            // Gesperrt für sensible Anpassung oder globalen Eintrag, wenn Rolle fehlt.
            if ((existingEntry.IstSensibleAnpassung == true || existingEntry.IstGlobal) && !hasDreiF)
            {
                return Forbid();
            }

            // Gesperrte Einträge können von niemandem bearbeitet werden.
            if (existingEntry.AnpassungGesperrt == true)
            {
                TempData["Message"] = $"Konfiguration '{existingEntry.KeyBezeichnung}' ist gesperrt. Bitte zuerst entsperren.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Config", new { page, sortColumn, sortDirection, pageSize });
            }

            // Ohne Rolle dürfen diese Flags nicht verändert werden.
            if (!hasDreiF)
            {
                entry.IstSensibleAnpassung = existingEntry.IstSensibleAnpassung;
                entry.IstGlobal = existingEntry.IstGlobal;
            }

            // Checkbox-Werte absichern (nie null).
            entry.IstSensibleAnpassung = entry.IstSensibleAnpassung == true;
            entry.IstAktiv = entry.IstAktiv == true;

            // Sperre wird nur über ToggleLock geändert.
            entry.AnpassungGesperrt = existingEntry.AnpassungGesperrt;

            // Bool-Wert aus Formular verarbeiten.
            var boolValueFromForm = Request.Form["BoolValue"];
            entry.BoolValue = string.IsNullOrEmpty(boolValueFromForm) ? (bool?)null : boolValueFromForm == "true";

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

                ViewBag.HasDreiF = hasDreiF;
                ViewBag.SelectedConnection = GetActiveConnection();

                // Pagination und Sortierung setzen.
                ViewBag.Page = page;
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortDirection = sortDirection;
                ViewBag.PageSize = pageSize;

                return View(entry);
            }

            // Benutzer für Änderungsprotokoll holen.
            var email = HttpContext.Session.GetString("UserEmail");
            var userName = HttpContext.Session.GetString("UserName") ?? email;
            var fullUserName = $"{userName} / {email}";

            try
            {
                // Eintrag aktualisieren und Konfiguration neu laden.
                _repository.Update(entry: entry, userName: fullUserName, connectionName: GetActiveConnection(), wasGlobal: existingEntry.IstGlobal);

                // Zeit und Name aktualisieren für LogUpdate.
                entry.EintragAngepasstAm = DateTime.Now;
                entry.EintragAngepasstVon = fullUserName;

                // Log erstellen.
                _configLogRepository.LogUpdate(oldEntry: existingEntry, newEntry: entry, currentUser: fullUserName, tabelleName: GetActiveConnection());

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


        [HttpPost]
        public IActionResult ToggleLock(int id, bool locked, int page = 1, string sortColumn = "Id", string sortDirection = "ASC", int pageSize = 10)
        {
            var rollenJson = HttpContext.Session.GetString("Rollen");
            var rollen = string.IsNullOrEmpty(rollenJson)
                ? new List<string>()
                : Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(rollenJson);

            if (!rollen.Contains("DreiF")) return Forbid();

            var existing = _repository.GetById(id, GetActiveConnection());
            if (existing == null) return NotFound();

            var email = HttpContext.Session.GetString("UserEmail");
            var fullUserName = $"{HttpContext.Session.GetString("UserName") ?? email} / {email}";

            try
            {
                _repository.SetLock(id, locked, fullUserName, existing.IstGlobal, GetActiveConnection());

                var updated = _repository.GetById(id, GetActiveConnection());
                _configLogRepository.LogUpdate(oldEntry: existing, newEntry: updated, currentUser: fullUserName, tabelleName: GetActiveConnection());
                RefreshConfig();

                TempData["Message"] = locked ? "Eintrag gesperrt." : "Eintrag entsperrt.";
                TempData["MessageType"] = "success";
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }

            return RedirectToAction("Config", new { page, sortColumn, sortDirection, pageSize });
        }

        // Config-Eintrag löschen.
        [HttpPost]
        public IActionResult DeleteConfig(int id)
        {
            try
            {
                var currentUser = GetCurrentUser();

                // Old Config.
                var oldEntry = _repository.GetById(id: id, connectionName: GetActiveConnection());
                if (oldEntry == null)
                {
                    return Json(new { success = false, message = "Eintrag nicht gefunden." });
                }

                // Rollen prüfen.
                var rollenJson = HttpContext.Session.GetString("Rollen");
                var rollen = string.IsNullOrEmpty(rollenJson)
                    ? new List<string>()
                    : Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(rollenJson);
                bool hasDreiF = rollen.Contains("DreiF");

                // Gesperrte Einträge können von niemandem gelöscht werden.
                if (oldEntry.AnpassungGesperrt == true)
                {
                    return Json(new { success = false, message = "Eintrag ist gesperrt. Bitte zuerst entsperren." });
                }

                // Gesperrt für sensible Anpassung, wenn Rolle fehlt.
                if (oldEntry.IstSensibleAnpassung == true && !hasDreiF)
                {
                    return Json(new { success = false, message = "Keine Berechtigung zum Löschen dieses Eintrags." });
                }

                // Erst löschen, dann loggen (wirft Exception, wenn nichts gelöscht wurde).
                _repository.Delete(id: id, isGlobal: oldEntry.IstGlobal, connectionName: GetActiveConnection());

                _configLogRepository.LogDelete(configEntryId: id, oldEntry: oldEntry, currentUser: currentUser, tabelleName: GetActiveConnection());

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

                // Kein Redirect, da der Aufruf per fetch kommt.
                return Json(new { success = false, message = ex.Message });
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
                var config = _repository.LoadConfig(connectionName: GetActiveConnection()); // Config aus Repository laden.
                return Json(config); // JSON-Antwort mit allen Einstellungen.
            }
            catch (RepositoryExceptions ex)
            {
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
