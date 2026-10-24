using AdminPanelDB.Abstractions;
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
        private readonly ICurrentMandantProvider _mandantProvider;

        public AdminRepository(string connectionString, ICurrentMandantProvider mandantProvider)
        {
            _connectionString = connectionString;
            _mandantProvider = mandantProvider;
        }


        // Abteilungen und Referate.
        public int GetAbteilungenCount(string abteilungSearchTerm = "", string referatSearchTerm = "")
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string cmdText;

                    if (!string.IsNullOrEmpty(referatSearchTerm))
                    {
                        cmdText = @"
                          SELECT COUNT(DISTINCT a.Id)
                          FROM Abteilung a
                          INNER JOIN Referat r ON r.AbteilungId = a.Id
                          WHERE a.Name LIKE @abtSearch
                          AND r.Name LIKE @refSearch
                          AND a.MandantId = @MandantId
                          AND r.MandantId = @MandantId";
                    }
                    else
                    {
                        cmdText = @"
                          SELECT COUNT(DISTINCT a.Id)
                          FROM Abteilung a
                          LEFT JOIN Referat r ON r.AbteilungId = a.Id
                          WHERE a.Name LIKE @abtSearch
                          AND a.MandantId = @MandantId";
                    }

                    using (var cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.AddWithValue("@abtSearch", $"%{abteilungSearchTerm}%");
                        if (!string.IsNullOrEmpty(referatSearchTerm))
                        {
                            cmd.Parameters.AddWithValue("@refSearch", $"%{referatSearchTerm}%");
                        }
                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetAbteilungenCount.", ex);
            }
        }

        public List<AbteilungReferateViewModel> GetAbteilungenPage(int pageNumber, int pageSize, string abteilungSearchTerm = "", string referatSearchTerm = "", string sortColumn = "Name", string sortDirection = "ASC")
        {
            try
            {
                var list = new List<AbteilungReferateViewModel>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    int offset = (pageNumber - 1) * pageSize;

                    var allowedColumns = new[] { "Name", "Id" };
                    if (string.IsNullOrEmpty(sortColumn) || !allowedColumns.Contains(sortColumn))
                    {
                        sortColumn = "Id";
                    }

                    if (string.IsNullOrEmpty(sortDirection) || (sortDirection != "ASC" && sortDirection != "DESC"))
                    {
                        sortDirection = "ASC";
                    }

                    string cmdText;

                    if (!string.IsNullOrEmpty(referatSearchTerm))
                    {
                        cmdText = $@"
                          SELECT DISTINCT a.Id, a.Name
                          FROM Abteilung a
                          INNER JOIN Referat r ON r.AbteilungId = a.Id
                          WHERE a.Name LIKE @abtSearch
                          AND r.Name LIKE @refSearch
                          AND a.MandantId = @MandantId
                          AND r.MandantId = @MandantId
                          ORDER BY {sortColumn} {sortDirection}
                          OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
                    }
                    else
                    {
                        cmdText = $@"
                          SELECT DISTINCT a.Id, a.Name
                          FROM Abteilung a
                          LEFT JOIN Referat r ON r.AbteilungId = a.Id
                          WHERE a.Name LIKE @abtSearch
                          AND a.MandantId = @MandantId
                          ORDER BY {sortColumn} {sortDirection}
                          OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
                    }

                    using (var cmdAbt = new SqlCommand(cmdText, conn))
                    {
                        cmdAbt.Parameters.AddWithValue("@abtSearch", $"%{abteilungSearchTerm}%");
                        if (!string.IsNullOrEmpty(referatSearchTerm))
                        {
                            cmdAbt.Parameters.AddWithValue("@refSearch", $"%{referatSearchTerm}%");
                        }
                        cmdAbt.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        cmdAbt.Parameters.AddWithValue("@offset", offset);
                        cmdAbt.Parameters.AddWithValue("@pageSize", pageSize);

                        using (var reader = cmdAbt.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(
                                    new AbteilungReferateViewModel
                                    {
                                        Abteilung = new Abteilung
                                        {
                                            Id = (int)reader["Id"],
                                            Name = reader["Name"].ToString(),
                                        },
                                        Referate = new List<Referat>(),
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
                               WHERE AbteilungId = @AbtId
                               AND MandantId = @MandantId";

                        if (!string.IsNullOrEmpty(referatSearchTerm))
                        {
                            query += " AND Name LIKE @refSearch";
                        }

                        using (var cmdRef = new SqlCommand(query, conn))
                        {
                            cmdRef.Parameters.AddWithValue("@AbtId", item.Abteilung.Id);
                            cmdRef.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                            if (!string.IsNullOrEmpty(referatSearchTerm))
                            {
                                cmdRef.Parameters.AddWithValue("@refSearch", $"%{referatSearchTerm}%");
                            }

                            using (var reader = cmdRef.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    item.Referate.Add(
                                        new Referat
                                        {
                                            Id = (int)reader["Id"],
                                            Name = reader["Name"].ToString(),
                                            AbteilungId = (int)reader["AbteilungId"],
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

                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Abteilung WHERE Name = @Name AND MandantId = @MandantId", conn); // Prüfen, ob Abteilung bereits existiert.
                    checkCmd.Parameters.AddWithValue("@Name", name);
                    checkCmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        return false; // Abteilung existiert bereits, Erstellung abbrechen.
                    }

                    var cmd = new SqlCommand("INSERT INTO Abteilung (Name, MandantId) VALUES (@Name, @MandantId)", conn); // Insert-Befehl vorbereiten.
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
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
                    using (var cmdGet = new SqlCommand("SELECT Name FROM Abteilung WHERE Id=@Id AND MandantId=@MandantId", conn))
                    {
                        cmdGet.Parameters.AddWithValue("@Id", id);
                        cmdGet.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
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
                    using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Abteilung WHERE Name=@Name AND Id<>@Id AND MandantId=@MandantId", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Name", newName);
                        checkCmd.Parameters.AddWithValue("@Id", id);
                        checkCmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            return false; // Name existiert bereits.
                        }
                    }

                    // Abteilung aktualisieren.
                    using (var cmdUpdateAbt = new SqlCommand("UPDATE Abteilung SET Name=@Name WHERE Id=@Id AND MandantId=@MandantId", conn))
                    {
                        cmdUpdateAbt.Parameters.AddWithValue("@Name", newName);
                        cmdUpdateAbt.Parameters.AddWithValue("@Id", id);
                        cmdUpdateAbt.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        cmdUpdateAbt.ExecuteNonQuery();
                    }

                    // Personen-Daten aktualisieren.
                    using (var cmdUpdatePerson = new SqlCommand("UPDATE Personen SET Abteilung=@NewName WHERE Abteilung=@OldName AND MandantId=@MandantId", conn))
                    {
                        cmdUpdatePerson.Parameters.AddWithValue("@NewName", newName);
                        cmdUpdatePerson.Parameters.AddWithValue("@OldName", oldName);
                        cmdUpdatePerson.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
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
                    var cmdCheck = new SqlCommand("SELECT COUNT(*) FROM Referat WHERE AbteilungId=@Id AND MandantId=@MandantId", conn);
                    cmdCheck.Parameters.AddWithValue("@Id", id);
                    cmdCheck.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                    int referatCount = (int)cmdCheck.ExecuteScalar();

                    if (referatCount > 0)
                    {
                        // Referate vorhanden, Löschen nicht erlaubt.
                        return false;
                    }

                    // Abteilung bei Personen zurücksetzen.
                    var cmdUpdatePerson = new SqlCommand("UPDATE Personen SET Abteilung = NULL WHERE Abteilung = (SELECT Name FROM Abteilung WHERE Id=@Id AND MandantId=@MandantId) AND MandantId=@MandantId", conn);
                    cmdUpdatePerson.Parameters.AddWithValue("@Id", id);
                    cmdUpdatePerson.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                    cmdUpdatePerson.ExecuteNonQuery();

                    // Abteilung löschen.
                    var cmdAbt = new SqlCommand("DELETE FROM Abteilung WHERE Id=@Id AND MandantId=@MandantId", conn);
                    cmdAbt.Parameters.AddWithValue("@Id", id);
                    cmdAbt.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
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
                    var cmd = new SqlCommand(@"INSERT INTO Referat (Name, AbteilungId, MandantId) OUTPUT INSERTED.Id VALUES (@Name, @AbtId, @MandantId)", conn);

                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@AbtId", abteilungId);
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

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

        public Referat GetReferatById(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT Id, Name, AbteilungId FROM Referat WHERE Id=@Id AND MandantId=@MandantId", conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Referat
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                AbteilungId = (int)reader["AbteilungId"]
                            };
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetReferatById.", ex);
            }
        }

        // Prüfen, ob Referat mit demselben Namen in der Abteilung existiert (für Create).
        public bool ReferatExists(int abteilungId, string name)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT COUNT(*) FROM Referat WHERE AbteilungId = @AbtId AND Name = @Name AND MandantId = @MandantId", conn);
                    cmd.Parameters.AddWithValue("@AbtId", abteilungId);
                    cmd.Parameters.AddWithValue("@Name", name.Trim());
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                    var count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim ReferatExists.", ex);
            }
        }

        // Prüfen, ob Referat mit demselben Namen in der Abteilung existiert (für Edit, excludeId = aktuelles Referat).
        public bool ReferatExistsInAbteilung(int abteilungId, string name, int excludeId = 0)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT COUNT(*) FROM Referat WHERE AbteilungId=@AbtId AND Name=@Name AND Id<>@ExcludeId AND MandantId=@MandantId", conn);
                    cmd.Parameters.AddWithValue("@AbtId", abteilungId);
                    cmd.Parameters.AddWithValue("@Name", name.Trim());
                    cmd.Parameters.AddWithValue("@ExcludeId", excludeId);
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

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
                    using (var cmdGet = new SqlCommand("SELECT Name, AbteilungId FROM Referat WHERE Id=@Id AND MandantId=@MandantId", conn))
                    {
                        cmdGet.Parameters.AddWithValue("@Id", id);
                        cmdGet.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
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
                    using (var cmdAbt = new SqlCommand("SELECT Name FROM Abteilung WHERE Id=@AbtId AND MandantId=@MandantId", conn))
                    {
                        cmdAbt.Parameters.AddWithValue("@AbtId", abteilungId);
                        cmdAbt.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        abteilungName = cmdAbt.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(abteilungName))
                    {
                        return false;
                    }

                    // Referat aktualisieren.
                    using (var cmdUpdateRef = new SqlCommand("UPDATE Referat SET Name=@Name WHERE Id=@Id AND MandantId=@MandantId", conn))
                    {
                        cmdUpdateRef.Parameters.AddWithValue("@Name", newName.Trim());
                        cmdUpdateRef.Parameters.AddWithValue("@Id", id);
                        cmdUpdateRef.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        cmdUpdateRef.ExecuteNonQuery();
                    }

                    // Nur Personen derselben Abteilung aktualisieren.
                    using (var cmdUpdatePerson = new SqlCommand("UPDATE Personen SET Referat=@NewName WHERE Referat=@OldName AND Abteilung=@AbtName AND MandantId=@MandantId", conn))
                    {
                        cmdUpdatePerson.Parameters.AddWithValue("@NewName", newName.Trim());
                        cmdUpdatePerson.Parameters.AddWithValue("@OldName", oldName);
                        cmdUpdatePerson.Parameters.AddWithValue("@AbtName", abteilungName);
                        cmdUpdatePerson.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
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
                    using (var cmdGet = new SqlCommand("SELECT Name, AbteilungId FROM Referat WHERE Id=@Id AND MandantId=@MandantId", conn))
                    {
                        cmdGet.Parameters.AddWithValue("@Id", Id);
                        cmdGet.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
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
                    using (var cmdAbt = new SqlCommand("SELECT Name FROM Abteilung WHERE Id=@Id AND MandantId=@MandantId", conn))
                    {
                        cmdAbt.Parameters.AddWithValue("@Id", abteilungId);
                        cmdAbt.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        abteilungName = cmdAbt.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrEmpty(abteilungName))
                    {
                        return;
                    }

                    // Nur Personen mit passender Abteilung und Referat aktualisieren.
                    using (var cmdUpdate = new SqlCommand("UPDATE Personen SET Referat=NULL WHERE Referat=@ReferatName AND Abteilung=@AbteilungName AND MandantId=@MandantId", conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("@ReferatName", referatName);
                        cmdUpdate.Parameters.AddWithValue("@AbteilungName", abteilungName);
                        cmdUpdate.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        cmdUpdate.ExecuteNonQuery();
                    }

                    // Referat selbst löschen.
                    using (var cmdDelete = new SqlCommand("DELETE FROM Referat WHERE Id=@Id AND MandantId=@MandantId", conn))
                    {
                        cmdDelete.Parameters.AddWithValue("@Id", Id);
                        cmdDelete.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
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
        public List<Personen> GetPersonenPage(int pageNumber, int pageSize, PersonenFilter filter, string sortColumn = "Id", string sortDirection = "ASC")
        {
            try
            {
                var dict = new Dictionary<int, Personen>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();

                    int offset = (pageNumber - 1) * pageSize;
                    var whereClauses = new List<string> { "p.MandantId = @MandantId" };
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                    void AddFilter(string column, string param, string value)
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            whereClauses.Add($"{column} LIKE @{param}");
                            cmd.Parameters.AddWithValue($"@{param}", $"%{value}%");
                        }
                    }

                    AddFilter("p.Titel", "Titel", filter.Titel);
                    AddFilter("p.Name", "Name", filter.Name);
                    AddFilter("p.Vorname", "Vorname", filter.Vorname);
                    AddFilter("p.Email", "Email", filter.Email);
                    AddFilter("p.UId", "UId", filter.UId);
                    AddFilter("pr.Abteilung", "Abteilung", filter.Abteilung);
                    AddFilter("pr.Referat", "Referat", filter.Referat);
                    AddFilter("p.Stelle", "Stelle", filter.Stelle);

                    if (!string.IsNullOrEmpty(filter.Rolle))
                    {
                        whereClauses.Add("pr.Rolle LIKE @Rolle");
                        cmd.Parameters.AddWithValue("@Rolle", $"%{filter.Rolle}%");
                    }

                    // WHERE-Klausel zusammenbauen.
                    string where = "WHERE " + string.Join(" AND ", whereClauses);

                    // Sortierspalte validieren.
                    string sortColumnSelect = sortColumn.ToUpper() == "ID" ? "p.Id" : $"p.Id, p.{sortColumn}";

                    cmd.CommandText = $@"
                                      SELECT 
                                      p.Id, p.Titel, p.Name, p.Vorname, p.Email, p.UId,
                                      p.Stelle, p.Kennwort,
                                      pr.Rolle,
                                      pr.Abteilung,
                                      pr.Referat,
                                      pr.GueltigAb,
                                      pr.GueltigBis
                                      FROM (
                                      SELECT DISTINCT {sortColumnSelect}
                                      FROM Personen p
                                      LEFT JOIN PersonRollen pr ON pr.PersonId = p.Id
                                      {where}
                                      ORDER BY p.{sortColumn} {sortDirection}
                                      OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                                      ) AS paged
                                      JOIN Personen p ON p.Id = paged.Id
                                      LEFT JOIN PersonRollen pr ON pr.PersonId = p.Id
                                      ORDER BY p.{sortColumn} {sortDirection}";

                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        int id = (int)reader["Id"];

                        if (!dict.TryGetValue(id, out var person))
                        {
                            person = new Personen
                            {
                                Id = id,
                                Titel = reader["Titel"]?.ToString(),
                                Name = reader["Name"]?.ToString(),
                                Vorname = reader["Vorname"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                UId = reader["UId"]?.ToString(),
                                Stelle = reader["Stelle"]?.ToString(),
                                Kennwort = reader["Kennwort"]?.ToString(),
                                Rollen = new List<PersonRolle>()
                            };
                            dict[id] = person;
                        }

                        string? rolle = reader["Rolle"] as string;
                        string? abteilung = reader["Abteilung"] as string;
                        string? referat = reader["Referat"] as string;

                        // Nur hinzufügen wenn noch nicht vorhanden (Duplikate vermeiden).
                        bool exists = person.Rollen.Any(r =>
                            r.Rolle == rolle &&
                            r.Abteilung == abteilung &&
                            r.Referat == referat);

                        if (!exists && (rolle != null || abteilung != null || referat != null))
                        {
                            person.Rollen.Add(new PersonRolle
                            {
                                Rolle = rolle,
                                Abteilung = abteilung,
                                Referat = referat,
                                GueltigAb = reader["GueltigAb"] as DateTime?,
                                GueltigBis = reader["GueltigBis"] as DateTime?
                            });
                        }
                    }
                }

                return dict.Values.ToList();
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

                    var whereClauses = new List<string> { "p.MandantId = @MandantId" };
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                    void AddFilter(string column, string param, string value)
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            whereClauses.Add($"{column} LIKE @{param}");
                            cmd.Parameters.AddWithValue($"@{param}", $"%{value}%");
                        }
                    }

                    AddFilter("p.Titel", "Titel", filter.Titel);
                    AddFilter("p.Name", "Name", filter.Name);
                    AddFilter("p.Vorname", "Vorname", filter.Vorname);
                    AddFilter("p.Email", "Email", filter.Email);
                    AddFilter("p.UId", "UId", filter.UId);
                    AddFilter("pr.Abteilung", "Abteilung", filter.Abteilung);
                    AddFilter("pr.Referat", "Referat", filter.Referat);
                    AddFilter("p.Stelle", "Stelle", filter.Stelle);

                    // Rolle separat behandeln, da sie in PersonRollen liegt und mehrere Rollen pro Person möglich sind.
                    if (!string.IsNullOrEmpty(filter.Rolle))
                    {
                        whereClauses.Add("pr.Rolle LIKE @Rolle");
                        cmd.Parameters.AddWithValue("@Rolle", $"%{filter.Rolle}%");
                    }

                    string where = "WHERE " + string.Join(" AND ", whereClauses);

                    cmd.CommandText = $@"SELECT COUNT(DISTINCT p.Id) FROM Personen p LEFT JOIN PersonRollen pr ON pr.PersonId = p.Id {where}";

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

        public void CreatePerson(Personen person, List<string> abteilungen, List<(string Referat, string Abteilung)> referate)
        {
            try
            {
                if (!IsValidName(person.Name) || !IsValidName(person.Vorname))
                {
                    throw new ArgumentException("Name und Vorname dürfen nur Buchstaben enthalten.");
                }

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using var transaction = conn.BeginTransaction();

                    // 1. PERSON ERSTELLEN.
                    var cmd = new SqlCommand(@"
                                     INSERT INTO Personen
                                     (Titel, Name, Vorname, Email, UId, Stelle, Kennwort, MandantId)
                                     VALUES
                                     (@Titel, @Name, @Vorname, @Email, @UId, @Stelle, @Kennwort, @MandantId);
                                     SELECT SCOPE_IDENTITY();", conn, transaction);

                    cmd.Parameters.AddWithValue("@Titel", (object?)person.Titel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", (object?)person.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Vorname", (object?)person.Vorname ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)person.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UId", (object?)person.UId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Stelle", (object?)person.Stelle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Kennwort", HashPassword(person.Kennwort));
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                    int personId = Convert.ToInt32(cmd.ExecuteScalar());

                    // 2. ROLLEN + ABTEILUNGEN + REFERATE SPEICHERN.
                    var rollen = person.Rollen != null && person.Rollen.Count > 0
                        ? person.Rollen
                        : new List<PersonRolle> { new PersonRolle { Rolle = null } };

                    foreach (var rolle in rollen)
                    {
                        if (abteilungen != null && abteilungen.Count > 0)
                        {
                            foreach (var abt in abteilungen)
                            {
                                var refFuerAbt = referate?
                                    .Where(r => r.Abteilung == abt).ToList()
                                    ?? new();

                                if (refFuerAbt.Count > 0)
                                {
                                    foreach (var ref_ in refFuerAbt)
                                        InsertPersonRolle(conn, transaction, personId,
                                            rolle.Rolle, abt, ref_.Referat);
                                }
                                else
                                {
                                    InsertPersonRolle(conn, transaction, personId,
                                        rolle.Rolle, abt, null);
                                }
                            }
                        }
                        else
                        {
                            InsertPersonRolle(conn, transaction, personId,
                                rolle.Rolle, null, null);
                        }
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim CreatePerson.", ex);
            }
        }

        // Hilfsmethode zum Einfügen von PersonRollen, um Duplikate zu vermeiden.
        // PersonRollen hat keine eigene MandantId - Mandant wird über PersonId (Personen.MandantId) vererbt.
        private void InsertPersonRolle(SqlConnection conn, SqlTransaction transaction, int personId, string? rolle, string? abteilung, string? referat)
        {
            try
            {
                var cmd = new SqlCommand(@"
                             INSERT INTO PersonRollen
                             (PersonId, Rolle, Abteilung, Referat, AngelegtAm, GueltigAb)
                             VALUES
                             (@PersonId, @Rolle, @Abteilung, @Referat, GETDATE(), GETDATE())",
                                         conn, transaction);

                cmd.Parameters.AddWithValue("@PersonId", personId);
                cmd.Parameters.AddWithValue("@Rolle", (object?)rolle ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Abteilung", (object?)abteilung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Referat", (object?)referat ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim InsertPersonRolle.", ex);
            }
        }

        // Für Create: Prüfen, ob E-Mail bereits existiert (mandantenübergreifend, da Email global eindeutig sein muss zum Login).
        public bool UserExistsByEmail(string email)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT COUNT(*) FROM Personen WHERE Email = @Email AND MandantId = @MandantId", conn);
                    cmd.Parameters.AddWithValue("@Email", email ?? "");
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim UserExistsByEmail.", ex);
            }
        }

        // Für Edit: Prüfen, ob E-Mail bereits bei anderem Benutzer existiert.
        public bool UserExistsByEmailExceptId(string email, int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT COUNT(*) FROM Personen WHERE Email = @Email AND Id <> @Id AND MandantId = @MandantId", conn);

                    cmd.Parameters.AddWithValue("@Email", email ?? "");
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim UserExistsByEmailExceptId.", ex);
            }
        }

        // Erlaube nur Buchstaben, Leerzeichen und Bindestriche im Namen (für Name und Vorname).
        private bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            // Erlaube (äöüÄÖÜß).
            return Regex.IsMatch(name, @"^[A-Za-zÄÖÜäöüß\s\-]+$");
        }

        public void UpdatePerson(Personen person, List<string> abteilungen, List<(string Referat, string Abteilung)> referate)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using var transaction = conn.BeginTransaction();

                    // 1. Personen-Daten aktualisieren (Abteilung/Referat raus - kommen aus PersonRollen).
                    var cmd = new SqlCommand(@"
                                     UPDATE Personen SET 
                                     Titel=@Titel, Name=@Name, Vorname=@Vorname,
                                     Email=@Email, UId=@UId, Stelle=@Stelle, Kennwort=@Kennwort
                                     WHERE Id=@Id AND MandantId=@MandantId", conn, transaction);
                    cmd.Parameters.AddWithValue("@Titel", (object?)person.Titel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", (object?)person.Name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Vorname", (object?)person.Vorname ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)person.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UId", (object?)person.UId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Stelle", (object?)person.Stelle ?? DBNull.Value);
                    // Prüfen ob Kennwort geändert wurde.
                    if (!string.IsNullOrWhiteSpace(person.Kennwort))
                    {
                        cmd.Parameters.AddWithValue("@Kennwort", HashPassword(person.Kennwort));
                    }
                    else
                    {
                        // Altes Kennwort aus DB holen und unverändert speichern.
                        cmd.Parameters.AddWithValue("@Kennwort", GetCurrentKennwort(person.Id));
                    }
                    cmd.Parameters.AddWithValue("@Id", person.Id);
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                    cmd.ExecuteNonQuery();

                    // 2. Alte PersonRollen löschen.
                    var delCmd = new SqlCommand("DELETE FROM PersonRollen WHERE PersonId = @PersonId", conn, transaction);
                    delCmd.Parameters.AddWithValue("@PersonId", person.Id);
                    delCmd.ExecuteNonQuery();

                    // 3. Neue Rollen + Abteilungen + Referate speichern.
                    var rollen = person.Rollen != null && person.Rollen.Count > 0
                        ? person.Rollen
                        : new List<PersonRolle> { new PersonRolle { Rolle = null } };

                    foreach (var rolle in rollen)
                    {
                        if (abteilungen != null && abteilungen.Count > 0)
                        {
                            foreach (var abt in abteilungen)
                            {
                                var refFuerAbt = referate?
                                    .Where(r => r.Abteilung == abt).ToList()
                                    ?? new();

                                if (refFuerAbt.Count > 0)
                                {
                                    foreach (var ref_ in refFuerAbt)
                                        InsertPersonRolle(conn, transaction, person.Id,
                                            rolle.Rolle, abt, ref_.Referat);
                                }
                                else
                                {
                                    InsertPersonRolle(conn, transaction, person.Id,
                                        rolle.Rolle, abt, null);
                                }
                            }
                        }
                        else
                        {
                            InsertPersonRolle(conn, transaction, person.Id, rolle.Rolle, null, null);
                        }
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim UpdatePerson.", ex);
            }
        }

        // Hilfsmethode, um aktuelles Kennwort aus DB zu holen, wenn kein neues eingegeben wurde (Edit).
        private string GetCurrentKennwort(int personId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Kennwort FROM Personen WHERE Id = @Id AND MandantId = @MandantId", conn);
                cmd.Parameters.AddWithValue("@Id", personId);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                return cmd.ExecuteScalar()?.ToString();
            }
        }

        public Personen GetPersonById(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT Id, Titel, Name, Vorname, Email, UId, Stelle, Kennwort FROM Personen WHERE Id = @Id AND MandantId = @MandantId", conn))
                {
                    cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    cmd.Parameters.Add("@MandantId", SqlDbType.Int).Value = _mandantProvider.MandantId;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return null;
                        return new Personen
                        {
                            Id = reader.GetInt32(0),
                            Titel = reader["Titel"] as string,
                            Name = reader["Name"] as string,
                            Vorname = reader["Vorname"] as string,
                            Email = reader["Email"] as string,
                            UId = reader["UId"] as string,
                            Stelle = reader["Stelle"] as string,
                            Kennwort = reader["Kennwort"] as string
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetPersonById.", ex);
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
                                     UPDATE Personen
                                     SET TKennwort=@TempHash, TempPasswordExpiry=@Expiry
                                     WHERE Id=@Id AND MandantId=@MandantId", conn);

                    cmd.Parameters.AddWithValue("@TempHash", hashedTemp);
                    cmd.Parameters.AddWithValue("@Expiry", expiry);
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim SetTemporaryPassword.", ex);
            }
        }

        public void DeletePerson(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Sicherstellen, dass die Person zum aktuellen Mandanten gehört, bevor irgendetwas gelöscht wird.
                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Personen WHERE Id = @Id AND MandantId = @MandantId", conn);
                    checkCmd.Parameters.AddWithValue("@Id", id);
                    checkCmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists == 0)
                    {
                        return; // Person gehört nicht zum aktuellen Mandanten - nichts tun.
                    }

                    // Zuerst die Rollen-Beziehungen löschen, um FK-Verletzungen zu vermeiden.
                    var cmd1 = new SqlCommand("DELETE FROM PersonRollen WHERE PersonId = @Id", conn);
                    cmd1.Parameters.AddWithValue("@Id", id);
                    cmd1.ExecuteNonQuery();

                    var cmd2 = new SqlCommand("DELETE FROM Personen WHERE Id = @Id AND MandantId = @MandantId", conn);
                    cmd2.Parameters.AddWithValue("@Id", id);
                    cmd2.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                    cmd2.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions(ex.Message, ex);
            }
        }



        // Abteilungen, Referate und Rollen.
        public List<string> GetAllRollen()
        {
            try
            {
                var list = new List<string>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var cmd = new SqlCommand(@"
                                     SELECT DISTINCT Name
                                     FROM Rollen
                                     WHERE Name IS NOT NULL
                                     AND MandantId = @MandantId
                                     ORDER BY Name", conn);

                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                    using var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(reader["Name"]?.ToString());
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Rollen.", ex);
            }
        }

        public List<Abteilung> GetAllAbteilungen()
        {
            try
            {
                var list = new List<Abteilung>();
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT Id, Name FROM Abteilung WHERE MandantId = @MandantId", conn);
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

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
                    var cmd = new SqlCommand("SELECT Id, Name FROM Referat WHERE AbteilungId=@AbtId AND MandantId=@MandantId", conn);

                    cmd.Parameters.AddWithValue("@AbtId", abteilungId);
                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

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

        // PersonRollen hat keine eigene MandantId - Absicherung ueber JOIN auf Personen,
        // damit nur Daten von Personen des aktuellen Mandanten zurueckkommen.
        public List<PersonRolle> GetRollenByPersonId(int personId)
        {
            try
            {
                var result = new List<PersonRolle>();
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(@"
                                               SELECT DISTINCT pr.Rolle, pr.Abteilung, pr.Referat
                                               FROM PersonRollen pr
                                               JOIN Personen p ON p.Id = pr.PersonId
                                               WHERE pr.PersonId = @Id
                                               AND pr.Rolle IS NOT NULL
                                               AND p.MandantId = @MandantId", conn);

                cmd.Parameters.AddWithValue("@Id", personId);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new PersonRolle
                    {
                        Rolle = reader["Rolle"] as string,
                        Abteilung = reader["Abteilung"] as string,
                        Referat = reader["Referat"] as string
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetRollenByPersonId.", ex);
            }
        }

        public List<string> GetAbteilungenByPersonId(int personId)
        {
            try
            {
                var result = new List<string>();
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(@"
                                               SELECT DISTINCT pr.Abteilung
                                               FROM PersonRollen pr
                                               JOIN Personen p ON p.Id = pr.PersonId
                                               WHERE pr.PersonId = @Id
                                               AND pr.Abteilung IS NOT NULL
                                               AND p.MandantId = @MandantId", conn);

                cmd.Parameters.AddWithValue("@Id", personId);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add(reader["Abteilung"].ToString());
                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetAbteilungenByPersonId.", ex);
            }
        }

        public List<string> GetReferateByPersonId(int personId)
        {
            try
            {
                var result = new List<string>();
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(@"
                                               SELECT DISTINCT pr.Referat, pr.Abteilung
                                               FROM PersonRollen pr
                                               JOIN Personen p ON p.Id = pr.PersonId
                                               WHERE pr.PersonId = @Id
                                               AND pr.Referat IS NOT NULL
                                               AND p.MandantId = @MandantId", conn);

                cmd.Parameters.AddWithValue("@Id", personId);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    result.Add($"{reader["Referat"]}:{reader["Abteilung"]}");
                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetReferateByPersonId.", ex);
            }
        }


        // Update Abteilung und Referat in PersonRollen und AbteilungReferat, damit die Daten konsistent bleiben.
        public void UpdateAbteilungReferat(string? oldAbteilung, string? oldReferat, string? newAbteilung, string? newReferat)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Nur Abteilung aktualisieren.
                    if (oldAbteilung != null && oldReferat == null)
                    {
                        var sql = @"UPDATE pr SET pr.[Abteilung] = @New 
                                    FROM [PersonRollen] pr JOIN [Personen] p ON p.Id = pr.PersonId
                                    WHERE pr.[Abteilung] = @Old AND p.MandantId = @MandantId;
                                    UPDATE [AbteilungReferat] SET [Abteilung] = @New 
                                    WHERE [Abteilung] = @Old AND MandantId = @MandantId";
                        var cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@New", newAbteilung);
                        cmd.Parameters.AddWithValue("@Old", oldAbteilung);
                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        cmd.ExecuteNonQuery();
                    }
                    // Nur Referat aktualisieren.
                    else if (oldReferat != null && oldAbteilung == null)
                    {
                        var sql = @"UPDATE pr SET pr.[Referat] = @New 
                                    FROM [PersonRollen] pr JOIN [Personen] p ON p.Id = pr.PersonId
                                    WHERE pr.[Referat] = @Old AND p.MandantId = @MandantId;
                                    UPDATE [AbteilungReferat] SET [Referat] = @New 
                                    WHERE [Referat] = @Old AND MandantId = @MandantId";
                        var cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@New", newReferat);
                        cmd.Parameters.AddWithValue("@Old", oldReferat);
                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        cmd.ExecuteNonQuery();
                    }
                    // Beide aktualisieren.
                    else if (oldAbteilung != null && oldReferat != null)
                    {
                        var sql = @"UPDATE pr SET pr.[Abteilung] = @NewAbt, pr.[Referat] = @NewRef 
                                    FROM [PersonRollen] pr JOIN [Personen] p ON p.Id = pr.PersonId
                                    WHERE pr.[Abteilung] = @OldAbt AND pr.[Referat] = @OldRef AND p.MandantId = @MandantId;
                                    UPDATE [AbteilungReferat] SET [Abteilung] = @NewAbt, [Referat] = @NewRef 
                                    WHERE [Abteilung] = @OldAbt AND [Referat] = @OldRef AND MandantId = @MandantId";
                        var cmd = new SqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@NewAbt", newAbteilung);
                        cmd.Parameters.AddWithValue("@NewRef", newReferat);
                        cmd.Parameters.AddWithValue("@OldAbt", oldAbteilung);
                        cmd.Parameters.AddWithValue("@OldRef", oldReferat);
                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim UpdateAbteilungReferat.", ex);
            }
        }

        public void DeleteAbteilungReferat(string? abteilung, string? referat)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string queryPersonRollen;
                    string queryAbteilungReferat;

                    // Nur Referat angegeben  nur Referat-Spalte leeren
                    if (string.IsNullOrEmpty(abteilung) && !string.IsNullOrEmpty(referat))
                    {
                        queryPersonRollen = @"
                                            UPDATE pr SET pr.[Referat] = NULL
                                            FROM [PersonRollen] pr JOIN [Personen] p ON p.Id = pr.PersonId
                                            WHERE pr.[Referat] = @Referat AND p.MandantId = @MandantId";

                        queryAbteilungReferat = @"
                                                UPDATE [AbteilungReferat]
                                                SET [Referat] = NULL
                                                WHERE [Referat] = @Referat AND MandantId = @MandantId";

                        var cmd1 = new SqlCommand(queryPersonRollen, conn);
                        cmd1.Parameters.AddWithValue("@Referat", referat);
                        cmd1.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        cmd1.ExecuteNonQuery();

                        var cmd2 = new SqlCommand(queryAbteilungReferat, conn);
                        cmd2.Parameters.AddWithValue("@Referat", referat);
                        cmd2.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        cmd2.ExecuteNonQuery();
                    }
                    // Abteilung angegeben (mit oder ohne Referat) → ganze Zeile löschen
                    else if (!string.IsNullOrEmpty(abteilung))
                    {
                        queryPersonRollen = string.IsNullOrEmpty(referat)
                            ? @"DELETE pr FROM [PersonRollen] pr JOIN [Personen] p ON p.Id = pr.PersonId
                        WHERE pr.[Abteilung] = @Abteilung AND p.MandantId = @MandantId"
                            : @"DELETE pr FROM [PersonRollen] pr JOIN [Personen] p ON p.Id = pr.PersonId
                        WHERE pr.[Abteilung] = @Abteilung AND pr.[Referat] = @Referat AND p.MandantId = @MandantId";

                        queryAbteilungReferat = string.IsNullOrEmpty(referat)
                            ? "DELETE FROM [AbteilungReferat] WHERE [Abteilung] = @Abteilung AND MandantId = @MandantId"
                            : "DELETE FROM [AbteilungReferat] WHERE [Abteilung] = @Abteilung AND [Referat] = @Referat AND MandantId = @MandantId";

                        var cmd1 = new SqlCommand(queryPersonRollen, conn);
                        cmd1.Parameters.AddWithValue("@Abteilung", abteilung);
                        cmd1.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        if (!string.IsNullOrEmpty(referat))
                            cmd1.Parameters.AddWithValue("@Referat", referat);
                        cmd1.ExecuteNonQuery();

                        var cmd2 = new SqlCommand(queryAbteilungReferat, conn);
                        cmd2.Parameters.AddWithValue("@Abteilung", abteilung);
                        cmd2.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        if (!string.IsNullOrEmpty(referat))
                            cmd2.Parameters.AddWithValue("@Referat", referat);
                        cmd2.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim DeleteAbteilungReferat.", ex);
            }
        }


    }
}
