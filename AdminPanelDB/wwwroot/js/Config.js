document.addEventListener('DOMContentLoaded', () => {
    const filterForm = document.getElementById('filterForm');
    const pageInput = document.getElementById('pageInput');
    const pageSizeSelect = document.querySelector('select[name="pageSize"]');

    // Seite auf 1 zurücksetzen, wenn Filter (außer Seite und pageSize) geändert werden.
    document.querySelectorAll('#filterForm input:not(#pageInput), #filterForm select:not([name="pageSize"])')
        .forEach(el => {
            el.addEventListener('change', () => {
                pageInput.value = 1;
            });
        });

    // Bei Änderung von pageSize: Seite zurücksetzen und Formular absenden.
    pageSizeSelect.addEventListener('input', () => {
        pageInput.value = 1;
        filterForm.submit();
    });

    // Manuelle Seiteneingabe — Formular absenden.
    pageInput.addEventListener('change', () => {
        filterForm.submit();
    });

    // Suchbutton — Seite auf 1 zurücksetzen.
    document.querySelector('#filterForm button[type="submit"]').addEventListener('click', () => {
        pageInput.value = 1;
    });

});



// Config löschen.
async function deleteConfig(id) {
    const customConfirm = await confirm("Bist du sicher, dass du diese Konfiguration löschen willst?");
    if (!customConfirm) {
        return;
    }

    try {
        const response = await fetch(`/Config/DeleteConfig/${id}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": getCsrfToken()
            },
            body: JSON.stringify({ id })
        });

        if (response.ok) {
            const data = await response.json();

            if (data.success === false) {
                showToast(data.message, 'error');
                return;
            }

            const row = document.getElementById(`row-${id}`);
            if (row) row.remove();

            // Prüfen, ob noch Zeilen in der Tabelle vorhanden sind.
            const rows = document.querySelectorAll("#configTable tbody tr");
            const dataRows = Array.from(rows).filter(r => !r.textContent.includes("Keine Einträge"));

            if (dataRows.length === 0) {
                // Wenn leer, zur vorherigen Seite wechseln.
                if (parseInt(pageInput.value) > 1) {
                    pageInput.value = parseInt(pageInput.value) - 1;
                }
            }

            // Meldung überlebt den Reload.
            showToastAfterReload(data.message || "Konfiguration erfolgreich gelöscht!", 'success');

            // Formular absenden, um Daten neu zu laden.
            filterForm.submit();

        } else {
            console.error("Fehler:", await response.text());
            showToast("Konfiguration konnte nicht gelöscht werden.", 'error');
        }
    } catch (error) {
        console.error("Fehler:", error);
        showToast("Unerwarteter Fehler beim Löschen.", 'error');
    }
}


function switchConnection(value) {
    // Connection  
    var url = new URL(window.location.href);
    url.searchParams.set('selectedConnection', value);
    url.searchParams.set('page', '1');
    window.location.href = url.toString();
}


function toggleLock(id, locked) {
    const f = document.getElementById('lockForm');
    f.querySelector('[name="id"]').value = id;
    f.querySelector('[name="locked"]').value = locked;   // "true" / "false"
    f.submit();
}