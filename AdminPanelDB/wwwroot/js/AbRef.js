
// Entfernt die aktuelle Seite bei Änderung der Seitengröße für AbRef und Personen und KaGruppen.
function onPageSizeChange() {
    // Setzt die Seite immer auf 1 zurück.
    const form = document.getElementById('mainForm');
    const pageInput = document.getElementById('pageNumberInput');

    // Aktualisiert das Eingabefeld für die Seitenzahl auf 1.
    pageInput.value = 1;

    // Sendet das Formular ab.
    form.submit();
}


// Lokaler Filter für Referate.
function filterReferateLocal(el) {
    // Liest den Filterwert aus dem Eingabefeld und wandelt ihn in Kleinbuchstaben um.
    const filter = (el.value || "").toLowerCase();

    // Findet die übergeordnete Tabellenzelle oder das Elternelement.
    const td = el.closest('td') || el.parentElement;
    if (!td)
    {
        return
    };

    // Sucht den Container mit der Referate-Liste innerhalb der Zelle.
    const container = td.querySelector('.referate-list');
    if (!container) 
    {
        return
    };

    // Holt alle Referat-Items im Container.
    const items = container.getElementsByClassName('referat-item');

    // Iteriert über alle Items und blendet diejenigen aus, die den Filtertext nicht enthalten.
    for (let i = 0; i < items.length; i++) {
        const item = items[i];

        // Holt den Text aus einem Input-Feld oder aus dem Textinhalt des Items.
        const input = item.querySelector('input[type="text"]');
        const txt = input ? input.value.toLowerCase() : (item.textContent || "").toLowerCase();

        // Zeigt das Item an, wenn es den Filtertext enthält, ansonsten blendet es aus.
        item.style.display = txt.includes(filter) ? "" : "none";
    }
}



// Referate umkehren.
let referateAscending = true;

function sortReferateGlobal(trigger) {

    //  Holen Alle Container Mit Referaten In Der Gesamten Tabelle.
    const containers = document.querySelectorAll(".referate-list");

    containers.forEach(container => {
        const items = Array.from(container.children);

        items.sort((a, b) => {
            const textA = a.querySelector("input")?.value?.toLowerCase() ?? "";
            const textB = b.querySelector("input")?.value?.toLowerCase() ?? "";
            return referateAscending ? textA.localeCompare(textB) : textB.localeCompare(textA);
        });

        items.forEach(x => container.appendChild(x));
    });

    // Aktualisierung referateSortArrow.
    const arrow = document.getElementById("referateSortArrow");
    arrow.textContent = referateAscending ? "▲" : "▼";

    referateAscending = !referateAscending;
}




// Abteilung löschen ohne Seiten-Update.
async function deleteAbteilung(id) {

    const customConfirm = await confirm("Bist du sicher, dass du diese Abteilun löschen willst?");
    if (!customConfirm) {
        return;
    }


    try {
        const response = await fetch(`/Admin/DeleteAbteilung/${id}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": getCsrfToken()
            },
            body: JSON.stringify({ id })
        });

        const data = await response.json();
        const errorBox = document.getElementById('error-box');

        if (!data.success) {
            // Fehler anzeigen.
            errorBox.style.color = 'red';
            errorBox.textContent = data.message;
            return;
        }

        // Zeile entfernen.
        const row = document.getElementById(`row-${id}`);
        if (row)
        {
            row.remove();
        }

        // Prüfen, ob noch Datenzeilen vorhanden sind.
        const rows = document.querySelectorAll("#abteilungenTable tbody tr");
        const dataRows = Array.from(rows).filter(r => !r.textContent.includes("Keine Einträge"));

        if (dataRows.length === 0) {
            // Wenn Seite leer, zur vorherigen Seite zurückspringen.
            const currentPageInput = document.getElementById("pageNumberInput");
            const currentPage = parseInt(currentPageInput.value);
            if (currentPage > 1) {
                currentPageInput.value = currentPage - 1;
            }
        }

        // Formular erneut absenden, Filter neu zeichnen.
        document.getElementById("mainForm").submit();

    } catch (error) {
        console.error("Fehler:", error);
    }
}




// Referat löschen ohne Seiten-Update.
async function deleteReferat(id) {
    const customConfirm = await confirm("Bist du sicher, dass du dieses Referat löschen willst?");
    if (!customConfirm)
    {
        return;
    }

    try {
        const response = await fetch(`/Admin/DeleteReferat/${id}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": getCsrfToken()
            },
            body: JSON.stringify({ id })
        });

        if (response.ok) {
            const refDiv = document.querySelector(`[data-referat-id='${id}']`);
            const errorBox = document.getElementById('error-box');
            if (refDiv) {
                const container = refDiv.closest('.referate-list');
                refDiv.remove();

                errorBox.style.color = 'green';
                errorBox.textContent = "Referat erfolgreich gelöscht!"

                const remainingItems = container.querySelectorAll('.referat-item');
                const controlsWrapper = container.previousElementSibling; // Div mit Filter und Sortierung

                if (remainingItems.length === 0) {
                    // Keine Referate mehr anzeigen.
                    container.innerHTML = "<div>Keine Referate</div>";

                }
            }
        } else {
            const text = await response.text();
            console.error("Fehler:", text);
        }
    } catch (error) {
        console.error("Fehler Referat:", error);
    }
}




// Abteilung speichern ohne Seiten-Update.
function saveAbteilung(id) {
    const input = document.getElementById(`abteilungName-${id}`);
    const newName = input.value;
    const oldValue = input.getAttribute('data-original');

    const formData = new FormData();
    formData.append('id', id);
    formData.append('newName', newName);
    formData.append('__RequestVerificationToken', getCsrfToken());

    fetch('/Admin/EditAbteilung', {
        method: 'POST',
        body: formData
    })
        .then(res => res.json())
        .then(data => {
            const errorBox = document.getElementById('error-box');

            if (data.success) {
                // Erfolgreich — Originalwert aktualisieren.
                input.setAttribute('data-original', newName);
                if (errorBox) {
                    errorBox.style.color = 'green';
                    errorBox.textContent = data.message;
                }
            } else {
                // Fehler — Wert zurücksetzen.
                input.value = oldValue;
                if (errorBox) {
                    errorBox.style.color = 'red';
                    errorBox.textContent = data.message;
                }
            }
        })
        .catch(err => console.error('Fehler:', err));
}



// Referat speichern ohne Seiten-Update.
function saveReferat(id) {
    const input = document.getElementById(`referatName-${id}`);
    const newName = input.value;
    const oldValue = input.getAttribute('data-original');

    const formData = new FormData();
    formData.append('id', id);
    formData.append('name', newName);
    formData.append('__RequestVerificationToken', getCsrfToken());

    fetch('/Admin/EditReferat', {
        method: 'POST',
        body: formData
    })
        .then(res => res.json())
        .then(data => {
            const errorBox = document.getElementById('error-box');

            if (data.success) {
                // Erfolgreich — Originalwert aktualisieren.
                input.setAttribute('data-original', newName);
                if (errorBox) {
                    errorBox.style.color = 'green';
                    errorBox.textContent = data.message;
}
            } else {
                // Fehler — Wert zurücksetzen.
                input.value = oldValue;
                if (errorBox) {
                    errorBox.style.color = 'red';
                    errorBox.textContent = data.message;
                }
            }
        })
        .catch(err => console.error('Fehler:', err));
}




// Referat erstellen ohne Seiten-Update.
function createReferat(abteilungId) {
    const input = document.getElementById(`newReferatName-${abteilungId}`);
    const name = input.value.trim();
    const errorBox = document.getElementById('error-box');

    if (!name) {
        
        errorBox.style.color = 'red';
        errorBox.textContent = "Name darf nicht leer sein!";
        return;
    }

    const formData = new FormData();
    formData.append('abteilungId', abteilungId);
    formData.append('name', name);
    formData.append('__RequestVerificationToken', getCsrfToken());

    fetch('/Admin/CreateReferat', {
        method: 'POST',
        body: formData
    })
        .then(res => res.json())
        .then(data => {


            if (data.success) {
                // Eingabefeld leeren.
                input.value = '';
                if (errorBox) errorBox.textContent = '';
                errorBox.style.color = 'green';
                errorBox.textContent = data.message;

                const abtRow = document.querySelector(`[data-abteilung-id="${abteilungId}"]`);
                const list = abtRow.querySelector('.referate-list');
                const noRefDiv = list.querySelector('div:not([data-referat-id])');

                // "Keine Referate" entfernen.
                if (noRefDiv && noRefDiv.textContent.includes("Keine Referate")) {
                    noRefDiv.remove();
                }

                // Neues Referat hinzufügen.
                const div = document.createElement('div');
                div.classList.add('referat-item');
                div.style.display = 'flex';
                div.style.alignItems = 'center';
                div.style.gap = '4px';
                div.style.opacity = '0';
                div.setAttribute('data-referat-id', data.referat.id);
                div.innerHTML = `
                    <input type="text" id="referatName-${data.referat.id}" value="${data.referat.name}" data-original="${data.referat.name}" />
                    <button type="button" onclick="saveReferat(${data.referat.id})"><i class="fas fa-save"></i></button>
                    <button type="button" onclick="deleteReferat(${data.referat.id})"><i class="fas fa-trash"></i></button>
                `;

                list.appendChild(div);

                // Einblend-Animation.
                setTimeout(() => {
                    div.style.transition = 'opacity 0.4s';
                    div.style.opacity = '1';
                }, 10);

                // Filter hinzufügen, wenn mehr als ein Referat vorhanden.
                const referateCount = list.querySelectorAll('.referat-item').length;
                let filterDiv = abtRow.querySelector('.local-referat-filter');
                if (referateCount > 1 && !filterDiv) {
                    const controlPanel = document.createElement('div');
                    controlPanel.innerHTML = `
                        <input type="text" class="local-referat-filter"
                               placeholder="Filter Referate..."
                               onkeyup="filterReferateLocal(this)" />
                        <button type="button" onclick="sortReferate(this)">▲</button>
                    `;
                    list.insertAdjacentElement('beforebegin', controlPanel);
                }
            } else {
                // Fehler anzeigen.
                if (errorBox) {
                    errorBox.style.color = 'red';
                    errorBox.textContent = data.message;
                }
            }
        })
        .catch(err => console.error('Fehler:', err));
}

// CSRF-Token holen.
function getCsrfToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}




