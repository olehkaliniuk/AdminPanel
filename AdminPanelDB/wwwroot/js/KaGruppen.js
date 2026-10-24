//// Kategorie löschen ohne Seiten-Update.
//async function deleteKategorie(id) {
//    const customConfirm = await confirm("Bist du sicher, dass du diese Kategorie löschen willst?");
//    if (!customConfirm) {
//        return;
//    }

//    try {
//        const kategorieInput = document.getElementById("SelectedKategorieNummer"); // Input holen.
//        const kategorieNummer = kategorieInput ? parseInt(kategorieInput.value) : 0; // Nummer extrahieren.

//        const response = await fetch(`/KaGruppen/DeleteKategorie/${id}`, { // Anfrage senden.
//            method: "POST",
//            headers: {
//                "Content-Type": "application/json",
//                "RequestVerificationToken": getCsrfToken()
//            },
//            body: JSON.stringify({ id, kategorieNummer }) // Body.
//        });

//        const data = await response.json(); // JSON auslesen.
//        const errorBox = document.getElementById('error-box');

//        if (errorBox) {
//            errorBox.style.color = data.success ? 'green' : 'red';
//            errorBox.textContent = data.message; // Nachricht setzen und stehen lassen.
//        }


  

//        if (data.success) {
//            const categoryRow = document.getElementById(`row-${id}`);
//            if (categoryRow) categoryRow.remove(); // Zeile sofort entfernen.

//            if (data.redirectKategorieId) window.location.href = `/KaGruppen/KaGruppen?kategorieId=${data.redirectKategorieId}`; // Redirect.
//            else window.location.reload(); // Sonst reload.
//        }

//    } catch (error) {
//        console.error("Fehler:", error);
//        alert("Ein unerwarteter Fehler ist aufgetreten.");
//    }
//}

//// Entfernt die aktuelle Seite bei Änderung der Seitengröße für AbRef und Personen und KaGruppen.
//function onPageSizeChange() {
//    // Setzt die Seite immer auf 1 zurück.
//    const form = document.getElementById('mainForm');
//    const pageInput = document.getElementById('pageNumberInput');

//    // Aktualisiert das Eingabefeld für die Seitenzahl auf 1.
//    pageInput.value = 1;

//    // Sendet das Formular ab.
//    form.submit();
//}


//// Hauptgruppe löschen ohne Seiten-Update.
//async function deleteHauptgruppe(id) {
//    const customConfirm = await confirm("Bist du sicher, dass du diese Hauptgruppe löschen willst?");
//    if (!customConfirm) {
//        return;
//    }

//    try {
//        const currentPageInput = document.getElementById("pageNumberInput");
//        const pageSizeInput = document.getElementById("PageSizeInput");
//        const kategorieInput = document.getElementById("SelectedKategorieNummer");

//        const currentPage = parseInt(currentPageInput.value) || 1;
//        const pageSize = parseInt(pageSizeInput.value) || 10;
//        const kategorieNummer = kategorieInput ? parseInt(kategorieInput.value) : 0;

//        // FormData für POST.
//        const formData = new FormData();
//        formData.append("id", id);
//        formData.append("kategorieNummer", kategorieNummer);
//        formData.append("pageNumber", currentPage);
//        formData.append("pageSize", pageSize);

//        // CSRF Token.
//        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
//        if (token) formData.append("__RequestVerificationToken", token);

//        // AJAX POST delete.
//        const response = await fetch(`/KaGruppen/DeleteHauptgruppe/${id}`, {
//            method: "POST",
//            body: formData
//        });

//        const data = await response.json();
//        const errorBox = document.getElementById('error-box');

//        if (errorBox) {
//            errorBox.style.color = data.success ? 'green' : 'red';
//            errorBox.textContent = data.message; // Nachricht setzen und stehen lassen.
//        }

//        if (!data.success) {
//            errorBox.style.color = 'red';
//            errorBox.textContent = "Eine Hauptgruppe kann nicht gelöscht werden, solange noch Nebengruppen damit verknüpft sind!";
//        } else {
//            // Erfolg
//            errorBox.style.color = 'green';
//            errorBox.textContent = "Hauptgruppe erfolgreich gelöscht!";


//        // Entfernen Der Zeile Aus Dem DOM.
//        const row = document.getElementById(`row-${id}`);
//        if (row)
//        {
//           row.remove();
//        }

//        // Überprüfen, Ob Noch Weitere Zeilen Vorhanden Sind.
//        const rows = document.querySelectorAll("#kaTable tbody tr");
//        const dataRows = Array.from(rows).filter(r => !r.textContent.includes("Keine Einträge"));

//        if (dataRows.length === 0 && currentPage > 1)
//        {
//            currentPageInput.value = currentPage - 1; // Zur Vorherigen Seite Wechseln.
//        } else {
//            currentPageInput.value = currentPage; // Auf Der Aktuellen Seite Bleiben.
//        }

//        // Tabelle Über Das Absenden Des Formulars Neu Zeichnen.
//        document.getElementById("mainForm").submit();
//        }
//    } catch (err) {
//        console.error(err);
//        alert("Ein Fehler ist aufgetreten.");
//    }
//}



//// Nebengruppe löschen ohne Seiten-Update.
//async function deleteNebengruppe(id) {

//    const customConfirm = await confirm("Bist du sicher, dass du diese Nebengruppe löschen willst?");
//    if (!customConfirm) {
//        return;
//    }

//    try {

//        const currentPage = parseInt(document.getElementById("pageNumberInput").value);

//        const response = await fetch(`/KaGruppen/DeleteNebengruppe/${id}?pageNumber=${currentPage}`, {
//            method: "POST",
//            headers: {
//                "RequestVerificationToken": getCsrfToken()
//            }
//        });

//        const data = await response.json();

//        const errorBox = document.getElementById('error-box');

//        if (errorBox) {
//            errorBox.style.color = data.success ? 'green' : 'red';
//            errorBox.textContent = data.message;
//        }

//        // Fehler.
//        if (!data.success) {
//            return;
//        }

//        // =========================
//        // UI ELEMENT ENTFERNEN
//        // =========================

//        const nebengruppeElement = document.querySelector(
//            `.nebengruppe-item [onclick="deleteNebengruppe(${id})"]`
//        )?.closest('.nebengruppe-item');

//        if (nebengruppeElement) {

//            // kleine Animation
//            nebengruppeElement.style.transition = "all 0.25s ease";
//            nebengruppeElement.style.opacity = "0";
//            nebengruppeElement.style.transform = "scale(0.95)";

//            setTimeout(() => {
//                nebengruppeElement.remove();
//            }, 250);
//        }

//    } catch (error) {

//        console.error("Fehler:", error);

//        alert("Ein Fehler ist aufgetreten.");
//    }
//}




//// CSRF-Token holen.
//function getCsrfToken() {
//    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
//    return tokenElement ? tokenElement.value : '';
//}


////togle Kategorie.
//function toggleKategorie(id, btn) {

//    var element = document.getElementById(id);
//    if (!element)
//    {
//        return;
//    }

//    var icon = btn.querySelector('i');

//    if (element.style.display === 'none' || element.style.display === '') {
//        element.style.display = 'flex';
//        if (icon) {
//            icon.classList.remove('fa-eye');
//            icon.classList.add('fa-eye-slash');
//        }
//    } else {
//        element.style.display = 'none';
//        if (icon) {
//            icon.classList.remove('fa-eye-slash');
//            icon.classList.add('fa-eye');
//        }
//    }
//}


//function toggleHauptgruppe(id, btn) {
//    var element = document.getElementById(id);
//    if (!element) 
//    {
//        return;
//    }

//    var icon = btn.querySelector('i');

//    if (element.style.display === 'none' || element.style.display === '') {
//        element.style.display = 'block';
//        if (icon) {
//            icon.classList.remove('fa-eye');
//            icon.classList.add('fa-eye-slash');
//        }
//    } else {
//        element.style.display = 'none';
//        if (icon) {
//            icon.classList.remove('fa-eye-slash');
//            icon.classList.add('fa-eye');
//        }
//    }
//}


//function toggleNebengruppe(id, btn) {
//    var element = document.getElementById(id);
//    if (!element) 
//    {
//        return;
//    }

//    var icon = btn.querySelector('i');

//    if (element.style.display === 'none' || element.style.display === '') {
//        element.style.display = 'block';
//        if (icon) {
//            icon.classList.remove('fa-eye');
//            icon.classList.add('fa-eye-slash');
//        }
//    } else {
//        element.style.display = 'none';
//        if (icon) {
//            icon.classList.remove('fa-eye-slash');
//            icon.classList.add('fa-eye');
//        }
//    }
//}


//let isAscending = true;

//function sortNebengruppeGlobal() {
//    const items = Array.from(document.querySelectorAll(".nebengruppe-item"));

//    items.sort((a, b) => {
//        const numA = parseInt(a.dataset.nummer, 10);
//        const numB = parseInt(b.dataset.nummer, 10);
//        return isAscending ? numA - numB : numB - numA;
//    });

//    items.forEach(item => {
//        const parent = item.closest("#nebengruppeContainer");
//        parent.appendChild(item);
//    });

//    // UI Des Pfeils Wie Bei Der Hauptgruppe.
//    const arrow = document.getElementById("nebengruppeSortArrow");
//    arrow.textContent = isAscending ? "▲" : "▼";

//    isAscending = !isAscending;
//}


