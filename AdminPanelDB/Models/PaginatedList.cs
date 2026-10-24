using System;
using System.Collections.Generic;

namespace AdminPanelDB.Models
{
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; } // Aktuelle Seite.
        public int TotalPages { get; private set; } // Gesamtzahl der Seiten.
        public int PageSize { get; private set; } // Anzahl der Elemente pro Seite.

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageSize = pageSize; // Seitengröße setzen.

            // Immer mindestens eine Seite.
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            if (TotalPages == 0)
            {
                TotalPages = 1;
            }

            // Wenn eine Seite größer als TotalPages angefragt wird — auf 1 zurücksetzen.
            PageIndex = pageIndex > TotalPages ? 1 : pageIndex;

            // Elemente hinzufügen, auch wenn die Liste leer ist.
            this.AddRange(items ?? new List<T>());
        }

        public bool HasPreviousPage => PageIndex > 1; // Gibt es eine vorherige Seite.
        public bool HasNextPage => PageIndex < TotalPages; // Gibt es eine nächste Seite.

        // Synchroner Fabrik-Methode für Repositories.
        public static PaginatedList<T> Create(List<T> items, int count, int pageIndex, int pageSize)
        {
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
