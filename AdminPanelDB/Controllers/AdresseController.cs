using AdminPanelDB.Exeptions;
using AdminPanelDB.Filters;
using AdminPanelDB.Logs;
using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AdminPanelDB.Controllers
{
    [SessionAuthorize(AdminOnly = true)]
    public class AdresseController : Controller
    {

        private readonly AdresseRepository _repository;

        public AdresseController(AdresseRepository repository)
        {
            _repository = repository;
        }



        [HttpGet]
        public IActionResult Adresse(
            string land, string strasse, string ort, string stateOrPrefecture, string plz, string gebaude, string wohnung, string organisation, string name,
            string bezeichnung, string iban, string bic, string istInsolvent, string istAktiv, string ansprechpartner, string ansprechpartnerTel, string ansprechpartnerEmail,
            string rechnungsLand, string rechnungsStrasse, string rechnungsOrt, string rechnungsStateOrPrefecture, string rechnungsPlz, string rechnungsOrganisation, string rechnungsName,
            string gesamtadresse,
            string sortColumn = "Id", string sortDirection = "ASC",
            int page = 1, int pageSize = 10
            )
        {
            try
            {
                // Gesamtanzahl der gefilterten Einträge ermitteln.
                var totalCount = _repository.GetFilteredCount(
                    land, strasse, ort, stateOrPrefecture, plz,
                    gebaude, wohnung, organisation, name,
                    bezeichnung, iban, bic, istInsolvent,
                    istAktiv, ansprechpartner, ansprechpartnerTel,
                    ansprechpartnerEmail,
                    rechnungsLand, rechnungsStrasse, rechnungsOrt,
                    rechnungsStateOrPrefecture, rechnungsPlz,
                    rechnungsOrganisation, rechnungsName,
                    gesamtadresse
                );

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
                    land, strasse, ort, stateOrPrefecture, plz,
                    gebaude, wohnung, organisation, name,
                    bezeichnung, iban, bic, istInsolvent,
                    istAktiv, ansprechpartner, ansprechpartnerTel,
                    ansprechpartnerEmail,
                    rechnungsLand, rechnungsStrasse, rechnungsOrt,
                    rechnungsStateOrPrefecture, rechnungsPlz,
                    rechnungsOrganisation, rechnungsName,
                    gesamtadresse,
                    sortColumn, sortDirection, page, pageSize
                );


                // Filterwerte für die View speichern.
                ViewBag.Filters = new
                {
                    land,
                    strasse,
                    ort,
                    stateOrPrefecture,
                    plz,
                    gebaude,
                    wohnung,
                    organisation,
                    name,
                    bezeichnung,
                    iban,
                    bic,
                    istInsolvent,
                    istAktiv,
                    ansprechpartner,
                    ansprechpartnerTel,
                    ansprechpartnerEmail,
                    rechnungsLand,
                    rechnungsStrasse,
                    rechnungsOrt,
                    rechnungsStateOrPrefecture,
                    rechnungsPlz,
                    rechnungsOrganisation,
                    rechnungsName,
                    gesamtadresse,
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



        [HttpGet]
        public IActionResult CreateAdresse()
        {
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/data/countries.json");
            var jsonContent = System.IO.File.ReadAllText(jsonPath);

            var countries = JsonSerializer.Deserialize<List<CountryCode>>(jsonContent);

            var model = new EditAdresseViewModel
            {

                Countries = countries,

            };

            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> GetFields(string code, string prefix)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return BadRequest("Ungültiger Ländercode.");
                }
                try
                {
                    var fields = await _repository.GetFieldsByCountryCodeAsync(code);
                    return PartialView("_DynamicFields", Tuple.Create(fields, prefix)); // Gibt die Liste der Felder für das ausgewählte Land zurück.
                }
                catch (Exception ex)
                {
                    return BadRequest($"Fehler beim Datenbankzugriff: {ex.Message}");
                }
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
        public IActionResult CreateAdresse(IFormCollection form)
        {
            try
            {
                var adresse = new Adresse
                {
                    Land = form["Land"],
                    Strasse = form["Adresse_Strasse"],
                    Ort = form["Adresse_Ort"],
                    StateOrPrefecture = GetStateOrPrefectureValue(form, "Adresse_"),
                    PLZ = form["Adresse_PLZ"],
                    Gebaude = form["Gebaude"],
                    Wohnung = form["Wohnung"],
                    Organisation = form["Adresse_Organisation"],
                    Name = form["Adresse_Name"],
                    Bezeichnung = form["Bezeichnung"],
                    Iban = form["Iban"],
                    Bic = form["Bic"],
                    IstInsolvent = form["IstInsolvent"] == "on",
                    IstAktiv = form["IstAktiv"] == "on",
                    Ansprechpartner = form["Ansprechpartner"],
                    AnsprechpartnerTel = form["AnsprechpartnerTel"],
                    AnsprechpartnerEmail = form["AnsprechpartnerEmail"],

                    // Rechnungsadresse.
                    RechnungsLand = form["RechnungsLand"],
                    RechnungsStrasse = form["Rechnungs_Strasse"],
                    RechnungsOrt = form["Rechnungs_Ort"],
                    RechnungsStateOrPrefecture = GetStateOrPrefectureValue(form, "Rechnungs_"),
                    RechnungsPLZ = form["Rechnungs_PLZ"],
                    RechnungsOrganisation = form["Rechnungs_Organisation"],
                    RechnungsName = form["Rechnungs_Name"],

                    //Gesamtadresse.
                    Gesamtadresse = form["Gesamtadresse"]
                };




                _repository.Create(adresse);

                // letzte Seite berechnen.
                int totalCount = _repository.GetFilteredCount(land: null, strasse: null, ort: null, stateOrPrefecture: null, plz: null,
                    gebaude: null, wohnung: null, organisation: null, name: null,
                    bezeichnung: null, iban: null, bic: null, istInsolvent: null,
                    istAktiv: null, ansprechpartner: null, ansprechpartnerTel: null,
                    ansprechpartnerEmail: null,
                    rechnungsLand: null, rechnungsStrasse: null, rechnungsOrt: null,
                    rechnungsStateOrPrefecture: null, rechnungsPlz: null,
                    rechnungsOrganisation: null, rechnungsName: null,
                    gesamtadresse: null
                );
                int pageSize = 10;
                int lastPage = (int)Math.Ceiling((double)totalCount / pageSize);


                TempData["Message"] = $"Adresse '{adresse.Gesamtadresse}' erfolgreich erstellt!";
                TempData["MessageType"] = "success";
                return RedirectToAction("Adresse", new { page = lastPage });

            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }



        // Adresse-Eintrag löschen.
        [HttpPost]
        public IActionResult DeleteAdresse(int id)
        {
            try
            {
                _repository.Delete(id); // Eintrag löschen.
                return Json(new
                {
                    success = true,
                    message = "Adresse erfolgreich gelöscht!"
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



        public string? GetStateOrPrefectureValue(IFormCollection form, string prefix = "")
        {
            var possibleIds = new[] {
            "Area", "County", "Department", "District", "Do_si",
            "Emirate", "Island", "Oblast", "Parish", "Prefecture", "State", "Bundesland"
        };

            foreach (var id in possibleIds)
            {
                var key = prefix + id; 
                if (!string.IsNullOrEmpty(form[key]))
                {
                    return form[key];
                }
            }
            return null;
        }



        [HttpGet]
        public async Task<IActionResult> EditAdresse(int id, int page = 1, string sortColumn = "Id", string sortDirection = "ASC", int pageSize = 10)
        {
            try
            {
                var adresse = _repository.GetById(id);
                if (adresse == null)
                {
                    return NotFound();
                }

                // Länder.
                var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/data/countries.json");
                var jsonContent = System.IO.File.ReadAllText(jsonPath);
                var countries = JsonSerializer.Deserialize<List<CountryCode>>(jsonContent);

                // Dynamische Felder Für Normale Adresse.
                var addressFields = await _repository.GetFieldsByCountryCodeAsync(adresse.Land);

                // Dynamische Felder Für Rechnungsadresse.
                var billingFields = await _repository.GetFieldsByCountryCodeAsync(adresse.RechnungsLand);

                var model = new EditAdresseViewModel
                {
                    Adresse = adresse,
                    Countries = countries,
                    AddressFields = addressFields,
                    BillingFields = billingFields
                };

                // Pagination und Sortierung setzen.
                ViewBag.Page = page;
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortDirection = sortDirection;
                ViewBag.PageSize = pageSize;

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



        // POST: Adresse bearbeiten.
        [HttpPost]
        public IActionResult EditAdresse(int id, IFormCollection form, int page = 1, string sortColumn = "Id", string sortDirection = "ASC", int pageSize = 10)
        {
            try
            {
                // Existierende Adresse laden.
                var adresse = _repository.GetById(id);
                if (adresse == null)
                {
                    return NotFound();
                }


                // Felder aktualisieren.
                adresse.Land = form["Land"];
                adresse.Strasse = form["Adresse_Strasse"];
                adresse.Ort = form["Adresse_Ort"];
                adresse.StateOrPrefecture = GetStateOrPrefectureValue(form, "Adresse_");
                adresse.PLZ = form["Adresse_PLZ"];
                adresse.Gebaude = form["Gebaude"];
                adresse.Wohnung = form["Wohnung"];
                adresse.Organisation = form["Adresse_Organisation"];
                adresse.Name = form["Adresse_Name"];
                adresse.Bezeichnung = form["Bezeichnung"];
                adresse.Iban = form["Iban"];
                adresse.Bic = form["Bic"];
                adresse.IstInsolvent = form["IstInsolvent"] == "on";
                adresse.IstAktiv = form["IstAktiv"] == "on";
                adresse.Ansprechpartner = form["Ansprechpartner"];
                adresse.AnsprechpartnerTel = form["AnsprechpartnerTel"];
                adresse.AnsprechpartnerEmail = form["AnsprechpartnerEmail"];

                // Rechnungsadresse.
                adresse.RechnungsLand = form["RechnungsLand"];
                adresse.RechnungsStrasse = form["Rechnungs_Strasse"];
                adresse.RechnungsOrt = form["Rechnungs_Ort"];
                adresse.RechnungsStateOrPrefecture = GetStateOrPrefectureValue(form, "Rechnungs_");
                adresse.RechnungsPLZ = form["Rechnungs_PLZ"];
                adresse.RechnungsOrganisation = form["Rechnungs_Organisation"];
                adresse.RechnungsName = form["Rechnungs_Name"];

                // Gesamtadresse.
                adresse.Gesamtadresse = form["Gesamtadresse"];

                // Daten speichern.
                _repository.Update(adresse);

                TempData["Message"] = $"Adresse '{adresse.Gesamtadresse}' erfolgreich bearbeitet!";
                TempData["MessageType"] = "success";
                // Redirect zurück zur selben Seite, gleiche Sortierung.
                return RedirectToAction("Adresse", new
                {
                    page = page,
                    sortColumn = sortColumn,
                    sortDirection = sortDirection,
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



        // Validierung.
        private List<string> ValidateAdresse(Adresse a)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(a.Ort))
                errors.Add("Ort darf nicht leer sein.");
            else if (!Regex.IsMatch(a.Ort, @"^[A-Za-zÄäÖöÜüß\s\-]+$"))
                errors.Add("Ort darf nur Buchstaben enthalten.");

            if (string.IsNullOrWhiteSpace(a.Strasse))
                errors.Add("Straße darf nicht leer sein.");

            if (!Regex.IsMatch(a.PLZ ?? "", @"^\d+$"))
                errors.Add("PLZ darf nur Ziffern enthalten.");

            if (string.IsNullOrWhiteSpace(a.Name))
                errors.Add("Name darf nicht leer sein.");
            else if (!Regex.IsMatch(a.Name, @"^[A-Za-zÄäÖöÜüß\s\-]+$"))
                errors.Add("Name darf nur Buchstaben enthalten.");

            if (string.IsNullOrWhiteSpace(a.AnsprechpartnerEmail))
                errors.Add("E-Mail darf nicht leer sein.");
            else if (!Regex.IsMatch(a.AnsprechpartnerEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors.Add("E-Mail ist ungültig.");

            if (string.IsNullOrWhiteSpace(a.Iban))
                errors.Add("IBAN darf nicht leer sein.");
            else if (!Regex.IsMatch(a.Iban, @"^[A-Z]{2}\d{2}[A-Z0-9]{1,30}$"))
                errors.Add("IBAN ist ungültig.");

            if (!string.IsNullOrWhiteSpace(a.Bic) && !Regex.IsMatch(a.Bic, @"^[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?$"))
                errors.Add("BIC ist ungültig.");

            if (!string.IsNullOrWhiteSpace(a.Ansprechpartner) && Regex.IsMatch(a.Ansprechpartner, @"\d"))
                errors.Add("Ansprechpartner darf keine Ziffern enthalten.");

            if (!string.IsNullOrWhiteSpace(a.RechnungsOrt) && !Regex.IsMatch(a.RechnungsOrt, @"^[A-Za-zÄäÖöÜüß\s\-]+$"))
                errors.Add("Rechnungsort darf nur Buchstaben enthalten.");

            if (!string.IsNullOrWhiteSpace(a.RechnungsPLZ) && !Regex.IsMatch(a.RechnungsPLZ, @"^\d+$"))
                errors.Add("Rechnungs-PLZ darf nur Ziffern enthalten.");

            if (!string.IsNullOrWhiteSpace(a.RechnungsName) && !Regex.IsMatch(a.RechnungsName, @"^[A-Za-zÄäÖöÜüß\s\-]+$"))
                errors.Add("Rechnungsname darf nur Buchstaben enthalten.");

            return errors;
        }
    }
}
