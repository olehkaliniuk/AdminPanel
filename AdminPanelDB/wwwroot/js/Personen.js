document.addEventListener('DOMContentLoaded', () => {

    const personenForm = document.getElementById('personenForm');
    const pageInput = document.getElementById('pageNumberInput');
    const pageSizeSelect = document.querySelector('#personenForm select[name="pageSize"]');

    // Seite auf 1 zurücksetzen, wenn Filter (außer Seite und pageSize) geändert werden.
    document.querySelectorAll('#personenForm input:not(#pageNumberInput), #personenForm select:not([name="pageSize"])')
        .forEach(el => {
            el.addEventListener('change', () => {
                pageInput.value = 1;
            });
        });

    // Bei Änderung von pageSize: Seite zurücksetzen und Formular absenden.
    pageSizeSelect.addEventListener('input', () => {
        pageInput.value = 1;
        personenForm.submit();
    });

    // Manuelle Seiteneingabe — Formular absenden.
    pageInput.addEventListener('change', () => {
        personenForm.submit();
    });

    // Suchbutton — Seite auf 1 zurücksetzen und absenden.
    document.querySelector('#personenForm button[type="submit"]').addEventListener('click', () => {
        pageInput.value = 1;
        personenForm.submit();
    });

    // Manuelle Filter-Submission von anderen Buttons möglich.
    function submitFilter() {
        pageInput.value = 1;
        personenForm.submit();
    }

});


// Benutzer löschen.
async function deleteUser(id) {
    const customConfirm = await confirm("Bist du sicher, dass du diese Person löschen willst?");
    if (!customConfirm) {
        return;
    }

    try {
        const response = await fetch(`/Admin/DeleteUser/${id}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": getCsrfToken()
            },
            body: JSON.stringify({ id })
        });

        if (response.ok) {
            const data = await response.json();
            const errorBox = document.getElementById('error-box');

            if (data.success) {
                // Zeile entfernen.
                const row = document.getElementById(`row-${id}`);
                if (row)
                {
                    row.remove();
                }

                if (errorBox) {
                    errorBox.style.color = 'green';
                    errorBox.textContent = data.message;
                }

                // Prüfen, ob noch Zeilen vorhanden sind.
                const rows = document.querySelectorAll("#personenTable tbody tr");
                const dataRows = Array.from(rows).filter(r => !r.textContent.includes("Keine Einträge"));

                if (dataRows.length === 0) {
                    // Wenn letzte Zeile gelöscht und es vorherige Seite gibt
                    const currentPageInput = document.getElementById("pageNumberInput");
                    const currentPage = parseInt(currentPageInput.value);
                    if (currentPage > 1) {
                        currentPageInput.value = currentPage - 1;
                    }
                }

                // Formular absenden, um die Tabelle zu aktualisieren
                setTimeout(() => {
                    document.getElementById("personenForm").submit();
                }, 1500); // 1.5 секунды

            } else {
                // Fehler beim Löschen → Nachricht zeigen, остаться на той же странице
                if (errorBox) {
                    errorBox.style.color = 'red';
                    errorBox.textContent = data.message;
                }
            }

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
