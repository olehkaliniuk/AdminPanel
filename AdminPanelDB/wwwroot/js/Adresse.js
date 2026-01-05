// Dynamic imputs.
$("#countrySelect").on("change", function () {
    let code = $(this).val();

    $.ajax({
        url: "/Adresse/GetFields",
        type: "GET",
        data: { code: code, prefix: "Adresse" },
        success: function (html) {
            $("#dynamicFields").html(html);
        }
    });
});

// Dynamic imputs Rechnung.
$("#billCountrySelect").on("change", function () {
    let code = $(this).val();

    $.ajax({
        url: "/Adresse/GetFields",
        type: "GET",
        data: { code: code, prefix: "Rechnungs" },
        success: function (html) {
            $("#billDynamicFields").html(html);
        }
    });
});


$("#copyAddress").on("click", function () {
    let firstCode = $("#countrySelect").val();
    $("#billCountrySelect").val(firstCode);

    // Laden Der Felder Der Rechnungsadresse Über AJAX.
    $.ajax({
        url: "/Adresse/GetFields",
        type: "GET",
        data: { code: firstCode, prefix: "Rechnungs" },
        success: function (html) {
            $("#billDynamicFields").html(html);

            setTimeout(function () {
                $("#dynamicFields input").each(function () {
                    let name = $(this).attr("name"); // Adresse_Name.
                    let value = $(this).val();
                    let targetName = name.replace("Adresse_", "Rechnungs_");
                    $("#billDynamicFields input[name='" + targetName + "']").val(value);
                });
            }, 50);
        }
    });
});




document.getElementById('adresseForm').addEventListener('submit', function (e) {
    e.preventDefault();

    let errors = [];

    const getValue = (name) => document.querySelector(`input[name="${name}"]`)?.value.trim() || '';
    const getCheckbox = (name) => document.querySelector(`input[name="${name}"]`)?.checked || false;

    const name = getValue("Adresse_Name");
    const ort = getValue("Adresse_Ort");
    const strasse = getValue("Adresse_Strasse");
    const plz = getValue("Adresse_PLZ");
    const email = getValue("AnsprechpartnerEmail");
    const iban = getValue("Iban");
    const bic = getValue("Bic");
    const ansprechpartner = getValue("Ansprechpartner");
    const rechnungsOrt = getValue("Rechnungs_Ort");
    const rechnungsPLZ = getValue("Rechnungs_PLZ");
    const rechnungsName = getValue("Rechnungs_Name");

    // Validierung.
    const onlyLetters = /^[A-Za-zÄäÖöÜüß\s\-]+$/;
    const onlyDigits = /^\d+$/;
    const emailRegex = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
    const ibanRegex = /^[A-Z]{2}\d{2}[A-Z0-9]{1,30}$/;
    const bicRegex = /^[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?$/;

    // Ort.
    if (!ort) errors.push("Ort darf nicht leer sein.");
    else if (!onlyLetters.test(ort)) errors.push("Ort darf nur Buchstaben enthalten.");

    // Straße.
    if (!strasse) errors.push("Straße darf nicht leer sein.");

    // PLZ.
    if (plz && !onlyDigits.test(plz)) errors.push("PLZ darf nur Ziffern enthalten.");

    // Name.
    if (!name) errors.push("Name darf nicht leer sein.");
    else if (!onlyLetters.test(name)) errors.push("Name darf nur Buchstaben enthalten.");

    // E-Mail.
    if (!email) errors.push("E-Mail darf nicht leer sein.");
    else if (!emailRegex.test(email)) errors.push("E-Mail ist ungültig.");

    // IBAN.
    if (!iban) errors.push("IBAN darf nicht leer sein.");
    else if (!ibanRegex.test(iban)) errors.push("IBAN ist ungültig.");

    // BIC.
    if (bic && !bicRegex.test(bic)) errors.push("BIC ist ungültig.");

    // Ansprechpartner.
    if (ansprechpartner && /\d/.test(ansprechpartner)) errors.push("Ansprechpartner darf keine Ziffern enthalten.");

    // Rechnungsadresse.
    if (rechnungsOrt && !onlyLetters.test(rechnungsOrt)) errors.push("Rechnungsort darf nur Buchstaben enthalten.");
    if (rechnungsPLZ && !onlyDigits.test(rechnungsPLZ)) errors.push("Rechnungs-PLZ darf nur Ziffern enthalten.");
    if (rechnungsName && !onlyLetters.test(rechnungsName)) errors.push("Rechnungsname darf nur Buchstaben enthalten.");

    // Fehler geben wir in den Razor-Container aus.
    const errorDiv = document.getElementById('validationErrors');
    if (errors.length > 0) {
        errorDiv.innerHTML = '<ul style="color:red; text-align:center; font-weight:bold;">' +
            errors.map(err => `<li style="list-style:none">${err}</li>`).join('') +
            '</ul>';
    } else {
        errorDiv.innerHTML = '';
        console.log("Keine Fehler, Formular wird gesendet");
        e.target.submit();
    }
});






// Adresse löschen.
async function deleteAdresse(id) {
    const customConfirm = await confirm("Bist du sicher, dass du diese Adresse löschen willst?");
    if (!customConfirm) {
        return;
    }

    try {
        const response = await fetch(`/Adresse/DeleteAdresse/${id}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": getCsrfToken()
            },
            body: JSON.stringify({ id })
        });

        if (response.ok) {
            const data = await response.json();
            const row = document.getElementById(`row-${id}`);
            if (row) row.remove();

            const errorBox = document.getElementById('error-box');

            if (errorBox) {
                errorBox.style.color = 'green';
                errorBox.textContent = data.message; // Nachricht anzeigen
            }

            // Prüfen, ob noch Zeilen in der Tabelle vorhanden sind.
            const rows = document.querySelectorAll("#adresseTable tbody tr");
            const dataRows = Array.from(rows).filter(r => !r.textContent.includes("Keine Einträge"));

            if (dataRows.length === 0) {
                // Wenn leer, zur vorherigen Seite wechseln.
                if (parseInt(pageInput.value) > 1) {
                    pageInput.value = parseInt(pageInput.value) - 1;
                }
            }

            // Formular absenden, um Daten neu zu laden.
            filterForm.submit();

        } else {
            const text = await response.text();
            console.error("Fehler:", text);
        }
    } catch (error) {
        console.error("Fehler:", error);
    }
}

// CSRF-Token holen.
function getCsrfToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}





///////////////////////////////full address field.
function gesamtAdresse() {
    const fullAddress = document.getElementById('fullAddress');
    fullAddress.innerHTML = '';

    const getValue = (name) => {
        const el = document.querySelector(`input[name="${name}"]`);
        return el ? el.value.trim() : '';
    };

    const parts = [];

    const organisation = getValue('Adresse_Organisation');
    const name = getValue('Adresse_Name');
    if (organisation) parts.push(organisation);
    if (name) parts.push(name);

    const strasse = getValue('Adresse_Strasse');

    let line1 = [strasse].filter(Boolean).join(' ');
    if (line1) parts.push(line1);

    const plz = getValue('Adresse_PLZ');
    const ort = getValue('Adresse_Ort');
    let line2 = [plz, ort].filter(Boolean).join(' ');
    if (line2) parts.push(line2);


    const dropdown = document.getElementById('countrySelect');
    const land = dropdown.options[dropdown.selectedIndex].textContent;



    if (land) parts.push(land);

    const fullAddressText = parts.join('\n');
    fullAddress.value = fullAddressText;
}








