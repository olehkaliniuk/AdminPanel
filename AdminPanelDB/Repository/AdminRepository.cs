using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using AdminPanelDB.ViewModels;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AdminPanelDB.Repository
{
    public class AdminRepository
    {
        private readonly string _connectionString;

        public AdminRepository(string connectionString)
        {
            _connectionString = connectionString;
        }



        public int GetAbteilungenCount(
              string abteilungSearchTerm = "",
              string referatSearchTerm = ""
        )
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string cmdText; // SQL-Befehl speichern.

                    if (!string.IsNullOrEmpty(referatSearchTerm))
                    {
                        // Filter für Referat ist gesetzt — nur Abteilungen mit Referaten zählen.
                        cmdText = @"
                                  SELECT COUNT(DISTINCT a.Id)
                                  FROM Abteilung a
                                  INNER JOIN Referat r ON r.AbteilungId = a.Id
                                  WHERE a.Name LIKE @abtSearch
                                  AND r.Name LIKE @refSearch";
                    }
                    else
                    {
                        // Kein Referat-Filter — alle Abteilungen zählen, auch ohne Referate.
                        cmdText = @"
                                  SELECT COUNT(DISTINCT a.Id)
                                  FROM Abteilung a
                                  LEFT JOIN Referat r ON r.AbteilungId = a.Id
                                  WHERE a.Name LIKE @abtSearch";
                    }

                    using (var cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.AddWithValue("@abtSearch", $"%{abteilungSearchTerm}%"); // Parameter für Abteilungsname setzen.
                        if (!string.IsNullOrEmpty(referatSearchTerm))
                        {
                            cmd.Parameters.AddWithValue("@refSearch", $"%{referatSearchTerm}%"); // Parameter für Referatsname setzen.
                        }
                        return (int)cmd.ExecuteScalar(); // Anzahl der Abteilungen zurückgeben.
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetAbteilungenCount.", ex);
            }
        }



        public List<AbteilungReferateViewModel> GetAbteilungenPage(
              int pageNumber,
              int pageSize,
              string abteilungSearchTerm = "",
              string referatSearchTerm = "",
              string sortColumn = "Name",
              string sortDirection = "ASC"
        )
        {
            try
            {
                var list = new List<AbteilungReferateViewModel>(); // Ergebnisliste erstellen.

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open(); // Verbindung zur Datenbank öffnen.

                    int offset = (pageNumber - 1) * pageSize; // Offset für Paging berechnen.

                    var allowedColumns = new[] { "Name", "Id" }; // erlaubte Sortierspalten prüfen.
                    if (string.IsNullOrEmpty(sortColumn) || !allowedColumns.Contains(sortColumn))
                    {
                        sortColumn = "Id"; // Standardspalte setzen.
                    }

                    if (string.IsNullOrEmpty(sortDirection) || (sortDirection != "ASC" && sortDirection != "DESC"))
                    {
                        sortDirection = "ASC"; // Standardsortierung setzen.
                    }

                    string cmdText;

                    if (!string.IsNullOrEmpty(referatSearchTerm))
                    {
                        // Filter für Referat gesetzt — INNER JOIN, keine leeren Abteilungen anzeigen.
                        cmdText = $@"
                                  SELECT DISTINCT a.Id, a.Name
                                  FROM Abteilung a
                                  INNER JOIN Referat r ON r.AbteilungId = a.Id
                                  WHERE a.Name LIKE @abtSearch
                                  AND r.Name LIKE @refSearch
                                  ORDER BY {sortColumn} {sortDirection}
                                  OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
                    }
                    else
                    {
                        // Kein Referat-Filter — LEFT JOIN, auch leere Abteilungen anzeigen.
                        cmdText = $@"
                                  SELECT DISTINCT a.Id, a.Name
                                  FROM Abteilung a
                                  LEFT JOIN Referat r ON r.AbteilungId = a.Id
                                  WHERE a.Name LIKE @abtSearch
                                  ORDER BY {sortColumn} {sortDirection}
                                  OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
                    }

                    using (var cmdAbt = new SqlCommand(cmdText, conn))
                    {
                        cmdAbt.Parameters.AddWithValue("@abtSearch", $"%{abteilungSearchTerm}%"); // Abteilungsfilter setzen.
                        if (!string.IsNullOrEmpty(referatSearchTerm))
                        {
                            cmdAbt.Parameters.AddWithValue("@refSearch", $"%{referatSearchTerm}%"); // Referatsfilter setzen.
                        }
                        cmdAbt.Parameters.AddWithValue("@offset", offset); // Paging-Offset setzen.
                        cmdAbt.Parameters.AddWithValue("@pageSize", pageSize); // Seitengröße setzen.

                        using (var reader = cmdAbt.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(
                                    new AbteilungReferateViewModel
                                    {
                                        Abteilung = new Abteilung
                                        {
                                            Id = (int)reader["Id"], // Abteilungs-ID zuweisen.
                                            Name = reader["Name"].ToString(), // Abteilungsname zuweisen.
                                        },
                                        Referate = new List<Referat>(), // Leere Referatliste initialisieren.
                                    }
                                );
                            }
                        }
                    }

                    // Referate für die gefundenen Abteilungen laden.
                    foreach (var item in list)
                    {
                        string query = @"
                                       SELECT Id, Name, AbteilungId
                                       FROM Referat
                                       WHERE AbteilungId = @AbtId";

                        if (!string.IsNullOrEmpty(referatSearchTerm))
                        {
                            query += " AND Name LIKE @refSearch"; // Optionalen Filter anwenden.
                        }

                        using (var cmdRef = new SqlCommand(query, conn))
                        {
                            cmdRef.Parameters.AddWithValue("@AbtId", item.Abteilung.Id); // Parameter für Abteilung setzen.
                            if (!string.IsNullOrEmpty(referatSearchTerm))
                            {
                                cmdRef.Parameters.AddWithValue("@refSearch", $"%{referatSearchTerm}%"); // Referatsfilter setzen.
                            }

                            using (var reader = cmdRef.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    item.Referate.Add(
                                        new Referat
                                        {
                                            Id = (int)reader["Id"], // Referats-ID zuweisen.
                                            Name = reader["Name"].ToString(), // Referatsname zuweisen.
                                            AbteilungId = (int)reader["AbteilungId"], // Abteilungs-ID zuweisen.
                                        }
                                    );
                                }
                            }
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetAbteilungenPage.", ex);
            }
        }



        public bool CreateAbteilung(string name)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Abteilung WHERE Name = @Name", conn); // Prüfen, ob Abteilung bereits existiert.

                    checkCmd.Parameters.AddWithValue("@Name", name);
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        return false; // Abteilung existiert bereits, Erstellung abbrechen.
                    }

                    var cmd = new SqlCommand("INSERT INTO Abteilung (Name) VALUES (@Name)", conn); // Insert-Befehl vorbereiten.
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim CreateAbteilung.", ex);
            }
        }



        public bool EditAbteilung(int id, string newName)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Alten Namen der Abteilung abrufen.
                    string oldName;
                    using (var cmdGet = new SqlCommand("SELECT Name FROM Abteilung WHERE Id=@Id", conn))
                    {
                        cmdGet.Parameters.AddWithValue("@Id", id);
                        oldName = cmdGet.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrEmpty(oldName))
                    {
                        return false;
                    }

                    // Wenn der neue Name mit dem alten übereinstimmt, nichts tun, aber kein Fehler melden.
                    if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // Prüfen, ob bereits eine andere Abteilung mit demselben Namen existiert.
                    using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Abteilung WHERE Name=@Name AND Id<>@Id", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Name", newName);
                        checkCmd.Parameters.AddWithValue("@Id", id);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            return false; // Name existiert bereits.
                        }
                    }

                    // Abteilung aktualisieren.
                    using (var cmdUpdateAbt = new SqlCommand("UPDATE Abteilung SET Name=@Name WHERE Id=@Id", conn))
                    {
                        cmdUpdateAbt.Parameters.AddWithValue("@Name", newName);
                        cmdUpdateAbt.Parameters.AddWithValue("@Id", id);
                        cmdUpdateAbt.ExecuteNonQuery();
                    }

                    // Personen-Daten aktualisieren.
                    using (var cmdUpdatePerson = new SqlCommand("UPDATE zsPersonen SET Abteilung=@NewName WHERE Abteilung=@OldName", conn))
                    {
                        cmdUpdatePerson.Parameters.AddWithValue("@NewName", newName);
                        cmdUpdatePerson.Parameters.AddWithValue("@OldName", oldName);
                        cmdUpdatePerson.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim EditAbteilung.", ex);
            }
        }



        public bool DeleteAbteilung(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Prüfen, ob Referate zur Abteilung existieren.
                    var cmdCheck = new SqlCommand("SELECT COUNT(*) FROM Referat WHERE AbteilungId=@Id", conn);

                    cmdCheck.Parameters.AddWithValue("@Id", id);
                    int referatCount = (int)cmdCheck.ExecuteScalar();

                    if (referatCount > 0)
                    {
                        // Referate vorhanden, Löschen nicht erlaubt.
                        return false;
                    }

                    // Abteilung bei Personen zurücksetzen.
                    var cmdUpdatePerson = new SqlCommand("UPDATE zsPersonen SET Abteilung = NULL WHERE Abteilung = (SELECT Name FROM Abteilung WHERE Id=@Id)", conn);
                    cmdUpdatePerson.Parameters.AddWithValue("@Id", id);
                    cmdUpdatePerson.ExecuteNonQuery();

                    // Abteilung löschen.
                    var cmdAbt = new SqlCommand("DELETE FROM Abteilung WHERE Id=@Id", conn);
                    cmdAbt.Parameters.AddWithValue("@Id", id);
                    cmdAbt.ExecuteNonQuery();

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim DeleteAbteilung.", ex);
            }
        }



        public Referat CreateReferat(int abteilungId, string name)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"INSERT INTO Referat (Name, AbteilungId) OUTPUT INSERTED.Id VALUES (@Name, @AbtId)", conn);

                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@AbtId", abteilungId);

                    // ID abrufen.
                    int newId = (int)cmd.ExecuteScalar();

                    return new Referat
                    {
                        Id = newId,
                        Name = name,
                        AbteilungId = abteilungId,
                    };
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim DeleteAbteilung.", ex);
            }
        }



        public bool ReferatExists(int abteilungId, string name)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT COUNT(*) FROM Referat WHERE AbteilungId = @AbtId AND Name = @Name", conn);
                    cmd.Parameters.AddWithValue("@AbtId", abteilungId);
                    cmd.Parameters.AddWithValue("@Name", name.Trim());
                    var count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim ReferatExists.", ex);
            }
        }



        public bool ReferatExistsInAbteilung(int abteilungId, string name, int excludeId = 0)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT COUNT(*) FROM Referat WHERE AbteilungId=@AbtId AND Name=@Name AND Id<>@ExcludeId", conn);
                    cmd.Parameters.AddWithValue("@AbtId", abteilungId);
                    cmd.Parameters.AddWithValue("@Name", name.Trim());
                    cmd.Parameters.AddWithValue("@ExcludeId", excludeId);

                    var count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim ReferatExistsInAbteilung.", ex);
            }
        }



        public bool EditReferat(int id, string newName)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string oldName;
                    int abteilungId;

                    // Alten Namen und Abteilungs-ID abrufen.
                    using (var cmdGet = new SqlCommand("SELECT Name, AbteilungId FROM Referat WHERE Id=@Id", conn))
                    {
                        cmdGet.Parameters.AddWithValue("@Id", id);
                        using (var reader = cmdGet.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return false;
                            }


                            oldName = reader["Name"].ToString();
                            abteilungId = (int)reader["AbteilungId"];
                        }
                    }

                    // Prüfen, ob bereits vorhanden.
                    if (ReferatExistsInAbteilung(abteilungId, newName, id))
                    {
                        return false;
                    }

                    // Abteilungsnamen holen.
                    string abteilungName;
                    using (var cmdAbt = new SqlCommand("SELECT Name FROM Abteilung WHERE Id=@AbtId", conn))
                    {
                        cmdAbt.Parameters.AddWithValue("@AbtId", abteilungId);
                        abteilungName = cmdAbt.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(abteilungName))
                    {
                        return false;
                    }


                    // Referat aktualisieren.
                    using (var cmdUpdateRef = new SqlCommand("UPDATE Referat SET Name=@Name WHERE Id=@Id", conn))
                    {
                        cmdUpdateRef.Parameters.AddWithValue("@Name", newName.Trim());
                        cmdUpdateRef.Parameters.AddWithValue("@Id", id);
                        cmdUpdateRef.ExecuteNonQuery();
                    }

                    // Nur Personen derselben Abteilung aktualisieren.
                    using (var cmdUpdatePerson = new SqlCommand("UPDATE zsPersonen SET Referat=@NewName WHERE Referat=@OldName AND Abteilung=@AbtName", conn))
                    {
                        cmdUpdatePerson.Parameters.AddWithValue("@NewName", newName.Trim());
                        cmdUpdatePerson.Parameters.AddWithValue("@OldName", oldName);
                        cmdUpdatePerson.Parameters.AddWithValue("@AbtName", abteilungName);
                        cmdUpdatePerson.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim EditReferat.", ex);
            }
        }



        public void DeleteReferat(int Id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Name und AbteilungId des Referats holen.
                    string referatName;
                    int abteilungId;
                    using (
                        var cmdGet = new SqlCommand("SELECT Name, AbteilungId FROM Referat WHERE Id=@Id", conn))
                    {
                        cmdGet.Parameters.AddWithValue("@Id", Id);
                        using (var reader = cmdGet.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return;
                            }


                            referatName = reader["Name"].ToString();
                            abteilungId = (int)reader["AbteilungId"];
                        }
                    }

                    // Name der zugehörigen Abteilung holen.
                    string abteilungName;
                    using (var cmdAbt = new SqlCommand("SELECT Name FROM Abteilung WHERE Id=@Id", conn))
                    {
                        cmdAbt.Parameters.AddWithValue("@Id", abteilungId);
                        abteilungName = cmdAbt.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrEmpty(abteilungName))
                    {
                        return;
                    }


                    // Nur Personen mit passender Abteilung und Referat aktualisieren.
                    using (var cmdUpdate = new SqlCommand("UPDATE zsPersonen SET Referat=NULL WHERE Referat=@ReferatName AND Abteilung=@AbteilungName", conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("@ReferatName", referatName);
                        cmdUpdate.Parameters.AddWithValue("@AbteilungName", abteilungName);
                        cmdUpdate.ExecuteNonQuery();
                    }

                    // Referat selbst löschen.
                    using (var cmdDelete = new SqlCommand("DELETE FROM Referat WHERE Id=@Id", conn))
                    {
                        cmdDelete.Parameters.AddWithValue("@Id", Id);
                        cmdDelete.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim DeleteReferat.", ex);
            }
        }



        // Personen.
        public List<Personen> GetAllPersonen()
        {
            try
            {
                var list = new List<Personen>();
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT Id, Titel, Name, Vorname, Email, UId, Abteilung, Referat, Stelle, Kennwort, IstAdmin, Rolle FROM zsPersonen", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(
                                new Personen
                                {
                                    Id = (int)reader["Id"],
                                    Titel = reader["Titel"]?.ToString(),
                                    Name = reader["Name"]?.ToString(),
                                    Vorname = reader["Vorname"]?.ToString(),
                                    Email = reader["Email"]?.ToString(),
                                    UId = reader["UId"]?.ToString(),
                                    Abteilung = reader["Abteilung"]?.ToString(),
                                    Referat = reader["Referat"]?.ToString(),
                                    Stelle = reader["Stelle"]?.ToString(),
                                    Kennwort = reader["Kennwort"]?.ToString(),
                                    IstAdmin = (bool)reader["IstAdmin"],
                                    Rolle = reader["Rolle"]?.ToString(),
                                }
                            );
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetAllPersonen.", ex);
            }
        }



        // Pagination.
        public List<Personen> GetPersonenPage(
            int pageNumber,
            int pageSize,
            PersonenFilter filter,
            string sortColumn = "Id",
            string sortDirection = "ASC"
        )
        {
            try
            {
                var list = new List<Personen>();
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();

                    int offset = (pageNumber - 1) * pageSize;
                    var whereClauses = new List<string>();

                    void AddFilter(string column, string param, string value)
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            whereClauses.Add($"{column} LIKE @{param}");
                            cmd.Parameters.AddWithValue($"@{param}", $"%{value}%");
                        }
                    }

                    AddFilter("Titel", "Titel", filter.Titel);
                    AddFilter("Name", "Name", filter.Name);
                    AddFilter("Vorname", "Vorname", filter.Vorname);
                    AddFilter("Email", "Email", filter.Email);
                    AddFilter("UId", "UId", filter.UId);
                    AddFilter("Abteilung", "Abteilung", filter.Abteilung);
                    AddFilter("Referat", "Referat", filter.Referat);
                    AddFilter("Stelle", "Stelle", filter.Stelle);
                    AddFilter("IstAdmin", "IstAdmin", filter.IstAdmin);
                    AddFilter("Rolle", "Rolle", filter.Rolle);



                    string where =
                        whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

                    var allowedSortColumns = new[]
                    {
                    "Id",
                    "Titel",
                    "Name",
                    "Vorname",
                    "Email",
                    "UId",
                    "Abteilung",
                    "Referat",
                    "Stelle",
                    "IstAdmin",
                    "Rolle"
                };
                    if (!allowedSortColumns.Contains(sortColumn))
                    {
                        sortColumn = "Id";
                    }

                    if (sortDirection != "ASC" && sortDirection != "DESC")
                    {
                        sortDirection = "ASC";
                    }

                    cmd.CommandText = $@"
                                      SELECT Id, Titel, Name, Vorname, Email, UId, Abteilung, Referat, Stelle, Kennwort, IstAdmin, Rolle
                                      FROM zsPersonen
                                      {where}
                                      ORDER BY {sortColumn} {sortDirection}
                                      OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(
                            new Personen
                            {
                                Id = (int)reader["Id"],
                                Titel = reader["Titel"]?.ToString(),
                                Name = reader["Name"]?.ToString(),
                                Vorname = reader["Vorname"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                UId = reader["UId"]?.ToString(),
                                Abteilung = reader["Abteilung"]?.ToString(),
                                Referat = reader["Referat"]?.ToString(),
                                Stelle = reader["Stelle"]?.ToString(),
                                Kennwort = reader["Kennwort"]?.ToString(),
                                IstAdmin = (bool)reader["IstAdmin"],
                                Rolle = reader["Rolle"]?.ToString()
                            }
                        );
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetPersonenPage.", ex);
            }
        }



        public int GetPersonenCount(PersonenFilter filter)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();

                    var whereClauses = new List<string>();

                    void AddFilter(string column, string param, string value)
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            whereClauses.Add($"{column} LIKE @{param}");
                            cmd.Parameters.AddWithValue($"@{param}", $"%{value}%");
                        }
                    }

                    AddFilter("Titel", "Titel", filter.Titel);
                    AddFilter("Name", "Name", filter.Name);
                    AddFilter("Vorname", "Vorname", filter.Vorname);
                    AddFilter("Email", "Email", filter.Email);
                    AddFilter("UId", "UId", filter.UId);
                    AddFilter("Abteilung", "Abteilung", filter.Abteilung);
                    AddFilter("Referat", "Referat", filter.Referat);
                    AddFilter("Stelle", "Stelle", filter.Stelle);
                    AddFilter("IstAdmin", "IstAdmin", filter.IstAdmin);
                    AddFilter("Rolle", "Rolle", filter.Rolle);


                    string where =
                        whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

                    cmd.CommandText = $"SELECT COUNT(*) FROM zsPersonen {where}";

                    return (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetPersonenCount.", ex);
            }
        }



        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return null;
            }
                

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }



        public void CreatePerson(Personen person)
        {
            try
            {
                if (!IsValidName(person.Name) || !IsValidName(person.Vorname))
                {
                    throw new ArgumentException("Name und Vorname dürfen nur Buchstaben (inkl. ä, ö, ü, ß) enthalten.");
                }

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                                             INSERT INTO zsPersonen (Titel, Name, Vorname, Email, UId, Abteilung, Referat, Stelle, Kennwort, IstAdmin, Rolle) 
                                             VALUES (@Titel, @Name, @Vorname, @Email, @UId, @Abteilung, @Referat, @Stelle, @Kennwort, @IstAdmin, @Rolle)",conn);

                    cmd.Parameters.AddWithValue("@Titel", (object?)person.Titel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", (object?)person.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Vorname", (object?)person.Vorname ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)person.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UId", (object?)person.UId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Abteilung", (object?)person.Abteilung ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Referat", (object?)person.Referat ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Stelle", (object?)person.Stelle ?? DBNull.Value);
                    string hashedPassword = HashPassword(person.Kennwort);
                    cmd.Parameters.AddWithValue("@Kennwort", (object?)hashedPassword ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IstAdmin", (object?)person.IstAdmin ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Rolle", (object?)person.Rolle ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim CreatePerson.", ex);
            }
        }


        // For Create.
        public bool UserExistsByEmail(string email)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT COUNT(*) FROM zsPersonen WHERE Email = @Email", conn);
                    cmd.Parameters.AddWithValue("@Email", email ?? "");

                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim UserExistsByEmail.", ex);
            }
        }

        // For Edit.
        public bool UserExistsByEmailExceptId(string email, int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT COUNT(*) FROM zsPersonen WHERE Email = @Email AND Id <> @Id",conn);

                    cmd.Parameters.AddWithValue("@Email", email ?? "");
                    cmd.Parameters.AddWithValue("@Id", id);

                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim UserExistsByEmailExceptId.", ex);
            }
        }




        private bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            // Erlaube (äöüÄÖÜß).
            return Regex.IsMatch(name, @"^[A-Za-zÄÖÜäöüß\s\-]+$");
        }



        public List<Abteilung> GetAllAbteilungen()
        {
            try
            {
                var list = new List<Abteilung>();
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT Id, Name FROM Abteilung", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(
                                new Abteilung
                                {
                                    Id = (int)reader["Id"],
                                    Name = reader["Name"].ToString(),
                                }
                            );
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetAllAbteilungen.", ex);
            }
        }



        public List<Referat> GetReferateByAbteilungId(int abteilungId)
        {
            try
            {
                var list = new List<Referat>();
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT Id, Name FROM Referat WHERE AbteilungId=@AbtId", conn);

                    cmd.Parameters.AddWithValue("@AbtId", abteilungId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(
                                new Referat
                                {
                                    Id = (int)reader["Id"],
                                    Name = reader["Name"].ToString(),
                                    AbteilungId = abteilungId,
                                }
                            );
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetReferateByAbteilungId.", ex);
            }
        }



        public void UpdatePerson(Personen person)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                                             UPDATE zsPersonen SET 
                                             Titel=@Titel,
                                             Name=@Name,
                                             Vorname=@Vorname,
                                             Email=@Email,
                                             UId=@UId,
                                             Abteilung=@Abteilung,
                                             Referat=@Referat,
                                             Stelle=@Stelle,
                                             IstAdmin=@IstAdmin,
                                             Rolle=@Rolle
                                             WHERE Id=@Id", conn);

                    cmd.Parameters.AddWithValue("@Titel", (object?)person.Titel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", (object?)person.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Vorname", (object?)person.Vorname ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)person.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UId", (object?)person.UId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Abteilung", (object?)person.Abteilung ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Referat", (object?)person.Referat ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Stelle", (object?)person.Stelle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IstAdmin", (object?)person.IstAdmin ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Rolle", (object?)person.Rolle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", person.Id);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim UpdatePerson.", ex);
            }
        }

        public Personen GetPersonById(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT Id, Name, IstAdmin FROM zsPersonen WHERE Id = @Id", conn))
                {
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }


                        return new Personen
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            IstAdmin = Convert.ToInt32(reader["IstAdmin"]) == 1
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetPersonById.", ex);
            }
        }



        public void DeletePerson(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("DELETE FROM zsPersonen WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim DeletePerson.", ex);
            }
        }



        // Setze temporäres Kennwort mit Ablauf.
        public void SetTemporaryPassword(int userId, string tempPassword, int validMinutes = 120) // validMinutes = Gültigkeitsdauer in Minuten bis zur Löschung.
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tempPassword))
                {
                    throw new ArgumentException("Kennwort darf nicht leer sein");
                }

                string hashedTemp = HashPassword(tempPassword);
                DateTime expiry = DateTime.UtcNow.AddMinutes(validMinutes); // Gültigkeitsdauer des temporären Passworts.

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                                             UPDATE zsPersonen
                                             SET TKennwort=@TempHash, TempPasswordExpiry=@Expiry
                                             WHERE Id=@Id", conn);

                    cmd.Parameters.AddWithValue("@TempHash", hashedTemp);
                    cmd.Parameters.AddWithValue("@Expiry", expiry);
                    cmd.Parameters.AddWithValue("@Id", userId);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim SetTemporaryPassword.", ex);
            }
        }
    }
}
