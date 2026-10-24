// Rolle bearbeiten.
async function saveRolle(id) {

    const input = document.getElementById(`rolleName-${id}`);

    if (!input) {
        return;
    }

    const newName = input.value.trim();

    if (!newName) {
        showToast("Name darf nicht leer sein.", "error");
        return;
    }

    const token = document.querySelector(`#row-${id} input[name="__RequestVerificationToken"]`)?.value;

    try {

        const response = await fetch("/Rollen/EditRolle", {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded"
            },
            body: new URLSearchParams({
                id: id,
                newName: newName,
                __RequestVerificationToken: token ?? ""
            })
        });

        const result = await response.json();

        if (result.success) {

            input.value = newName;
            input.dataset.original = newName;

            showToast(result.message, "success");

        } else {

            // Alten Wert wiederherstellen.
            input.value = input.dataset.original;

            showToast(result.message, "error");
        }

    }
    catch (error) {

        console.error(error);

        showToast("Fehler beim Speichern der Rolle.", "error");
    }
}

// Rolle löschen.
async function deleteRolle(id) {

    if (!confirm("Möchten Sie diese Rolle wirklich löschen?")) {
        return;
    }

    const row = document.getElementById(`row-${id}`);

    const token = row?.querySelector('input[name="__RequestVerificationToken"]')?.value;

    try {

        const response = await fetch("/Rollen/DeleteRolle", {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded"
            },
            body: new URLSearchParams({
                id: id,
                __RequestVerificationToken: token ?? ""
            })
        });

        const result = await response.json();

        if (result.success) {

            row?.remove();

            showToast(result.message, "success");

        } else {

            showToast(result.message, "error");
        }

    }
    catch (error) {

        console.error(error);

        showToast("Fehler beim Löschen der Rolle.", "error");
    }
}

// Seitegröße ändern.
function onPageSizeChange() {

    const pageInput = document.getElementById("pageNumberInput");

    if (pageInput) {
        pageInput.value = 1;
    }

    document.getElementById("mainForm").submit();
}