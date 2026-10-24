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
    if (!customConfirm) return;

    try {
        const response = await fetch(`/Admin/DeleteUser/${id}`, {
            method: "POST",
            headers: {
                "RequestVerificationToken": getCsrfToken()
            }
        });

        const text = await response.text();

        let data;
        try {
            data = JSON.parse(text);
        } catch (e) {
            console.error("Server hat kein JSON geliefert (Status " + response.status + "):", text);
            showToast("Unerwartete Antwort vom Server.", 'error');
            return;
        }

        if (!data.success) {
            showToast(data.message, 'error');
            return;
        }

        const message = data.message || "Person erfolgreich gelöscht!";
        const row = document.getElementById(`row-${id}`);

        if (row) {
            setTimeout(() => {
                row.remove();

                const rows = document.querySelectorAll("#personenTable tbody tr");
                const dataRows = Array.from(rows).filter(r => r.id.startsWith("row-"));

                if (dataRows.length === 0) {
                    const pageInput = document.getElementById("pageNumberInput");
                    const currentPage = parseInt(pageInput.value);

                    if (currentPage > 1) {
                        // Seite ist leer: zurückspringen, Meldung überlebt den Reload.
                        pageInput.value = currentPage - 1;
                        showToastAfterReload(message, 'success');
                        document.getElementById("personenForm").submit();
                    }
                }
            }, 300);
        }

        showToast(message, 'success');

    } catch (error) {
        console.error("FETCH ERROR:", error);
        showToast("Unerwarteter Fehler beim Löschen.", 'error');
    }
}