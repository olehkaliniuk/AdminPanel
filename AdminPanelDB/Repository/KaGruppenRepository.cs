using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using AdminPanelDB.ViewModels;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AdminPanelDB.Repository
{
    public class KaGruppenRepository
    {
        private readonly string _connectionString;

        public KaGruppenRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Kategorie> GetAllKategorien()
        {
            try
            {
                var kategorien = new List<Kategorie>();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(@"
                                                        SELECT Id, Bezeichnung, Kuerzel, Beschreibung, KategorieNummer, IstAktiv, IstTestKategorie
                                                        FROM Kategorie
                                                        ORDER BY KategorieNummer ASC", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                kategorien.Add(new Kategorie
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Bezeichnung = reader.GetString(reader.GetOrdinal("Bezeichnung")),
                                    Kuerzel = reader["Kuerzel"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Kuerzel")) : "",
                                    Beschreibung = reader["Beschreibung"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Beschreibung")) : "",
                                    KategorieNummer = reader["KategorieNummer"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("KategorieNummer")) : 0,
                                    IstAktiv = reader["IstAktiv"] != DBNull.Value && (bool)reader["IstAktiv"],
                                    IstTestKategorie = reader["IstTestKategorie"] != DBNull.Value && (bool)reader["IstTestKategorie"]
                                });
                            }
                        }
                    }
                }
                return kategorien;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetAllKategorien.", ex);
            }
        }



        public int GetHauptGruppenCount(string hauptgruppeSearchTerm = "", string nebengruppeSearchTerm = "", int? kategorieNummer = null)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string cmdText;

                    if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
                    {
                        cmdText = @"
                                  SELECT COUNT(DISTINCT a.Id)
                                  FROM Hauptgruppe a
                                  INNER JOIN Nebengruppe r ON r.GehoertZuHauptgruppeNummer = a.HauptgruppeNummer
                                  WHERE a.Bezeichnung LIKE @hauptSearch
                                  AND r.Bezeichnung LIKE @nebenSearch";
                    }
                    else
                    {
                        cmdText = @"
                                  SELECT COUNT(DISTINCT a.Id)
                                  FROM Hauptgruppe a
                                  LEFT JOIN Nebengruppe r ON r.GehoertZuHauptgruppeNummer = a.Id
                                  WHERE a.Bezeichnung LIKE @hauptSearch";
                    }

                    if (kategorieNummer.HasValue)
                    {
                        cmdText += " AND a.GehoertZuKategorieNummer = @kategorieNummer";
                    }

                    using (var cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.AddWithValue("@hauptSearch", $"%{hauptgruppeSearchTerm}%");
                        if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
                        {
                            cmd.Parameters.AddWithValue("@nebenSearch", $"%{nebengruppeSearchTerm}%");
                        }
                        if (kategorieNummer.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@kategorieNummer", kategorieNummer.Value);
                        }

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetHauptGruppenCount.", ex);
            }
        }



        public List<KaGruppenViewModel> GetHauptGruppenPage(
            int pageNumber,
            int pageSize,
            string hauptgruppeSearchTerm = "",
            string nebengruppeSearchTerm = "",
            string sortColumn = null,
            string sortDirection = null,
            int? kategorieNummer = null)
        {
            try
            {
                var list = new List<KaGruppenViewModel>();


                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    int offset = (pageNumber - 1) * pageSize;
                    var allowedColumns = new[] { "Bezeichnung", "Id", "HauptgruppeNummer" };
                    if (string.IsNullOrEmpty(sortColumn) || !allowedColumns.Contains(sortColumn))
                    {
                        sortColumn = "HauptgruppeNummer";
                    }
                    if (string.IsNullOrEmpty(sortDirection) || (sortDirection != "ASC" && sortDirection != "DESC"))
                    {
                        sortDirection = "ASC";
                    }

                    string cmdText;

                    // Hauptgruppe.
                    if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
                    {
                        cmdText = $@"
                                  SELECT DISTINCT a.Id, a.GehoertZuKategorieNummer, a.Bezeichnung, a.Kuerzel, a.Beschreibung, a.HauptgruppeNummer, a.LaufenderAktenzeichenZaehler, a.IstAktiv, a.IstTestGruppe
                                  FROM Hauptgruppe a
                                  INNER JOIN Nebengruppe r ON r.GehoertZuHauptgruppeNummer = a.HauptgruppeNummer
                                  WHERE a.Bezeichnung LIKE @hauptSearch
                                  AND r.Bezeichnung LIKE @nebenSearch";
                    }
                    else
                    {
                        cmdText = $@"
                                  SELECT DISTINCT a.Id, a.GehoertZuKategorieNummer, a.Bezeichnung, a.Kuerzel, a.Beschreibung, a.HauptgruppeNummer, a.LaufenderAktenzeichenZaehler, a.IstAktiv, a.IstTestGruppe
                                  FROM Hauptgruppe a
                                  LEFT JOIN Nebengruppe r ON r.GehoertZuHauptgruppeNummer = a.HauptgruppeNummer
                                  WHERE a.Bezeichnung LIKE @hauptSearch";
                    }

                    if (kategorieNummer.HasValue)
                    {
                        cmdText += " AND a.GehoertZuKategorieNummer = @kategorieNummer";
                    }

                    cmdText += $" ORDER BY {sortColumn} {sortDirection} OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                    using (var cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.AddWithValue("@hauptSearch", $"%{hauptgruppeSearchTerm}%");
                        if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
                        {
                            cmd.Parameters.AddWithValue("@nebenSearch", $"%{nebengruppeSearchTerm}%");
                        }
                        if (kategorieNummer.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@kategorieNummer", kategorieNummer.Value);
                        }
                        cmd.Parameters.AddWithValue("@offset", offset);
                        cmd.Parameters.AddWithValue("@pageSize", pageSize);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new KaGruppenViewModel
                                {
                                    Hauptgruppen = new List<Hauptgruppe>
                        {
                            new Hauptgruppe
                            {
                                Id = (int)reader["Id"],
                                GehoertZuKategorie = (int)reader["GehoertZuKategorieNummer"],
                                Bezeichnung = reader["Bezeichnung"].ToString()!,
                                Kuerzel = reader["Kuerzel"].ToString()!,
                                Beschreibung = reader["Beschreibung"].ToString()!,
                                HauptgruppeNummer = (int)reader["HauptgruppeNummer"],
                                LaufenderAktenzeichenZaehler = (int)reader["LaufenderAktenzeichenZaehler"],
                                IstAktiv = (bool)reader["IstAktiv"],
                                IstTestGruppe = (bool)reader["IstTestGruppe"]
                            }
                        },
                                    Nebengruppen = new List<Nebengruppe>()
                                });
                            }
                        }
                    }

                    // Nebengruppen.
                    foreach (var item in list)
                    {
                        string query = @"
                                       SELECT Id, GehoertZuKategorieNummer, Bezeichnung, Kuerzel, Beschreibung, NebengruppeNummer, 
                                       IstAktiv, IstTestGruppe, GehoertZuHauptgruppeNummer
                                       FROM Nebengruppe
                                       WHERE GehoertZuHauptgruppeNummer = @HauptgruppeNummer";

                        if (kategorieNummer.HasValue)
                        {
                            query += " AND GehoertZuKategorieNummer = @kategorieNummer";
                        }

                        if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
                        {
                            query += " AND Bezeichnung LIKE @nebenSearch";
                        }

                        // Sortierung nach NebengruppeNummer.
                        query += " ORDER BY NebengruppeNummer ASC";

                        using (var cmd = new SqlCommand(query, conn))
                        {

                            cmd.Parameters.AddWithValue("@HauptgruppeNummer", item.Hauptgruppen!.First().HauptgruppeNummer);

                            if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
                            {
                                cmd.Parameters.AddWithValue("@nebenSearch", $"%{nebengruppeSearchTerm}%");
                            }

                            if (kategorieNummer.HasValue)
                            {
                                cmd.Parameters.AddWithValue("@kategorieNummer", kategorieNummer.Value);
                            }

                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    item.Nebengruppen!.Add(new Nebengruppe
                                    {
                                        Id = (int)reader["Id"],
                                        GehoertZuKategorie = (int)reader["GehoertZuKategorieNummer"],
                                        Bezeichnung = reader["Bezeichnung"].ToString()!,
                                        Kuerzel = reader["Kuerzel"].ToString()!,
                                        Beschreibung = reader["Beschreibung"].ToString()!,
                                        NebengruppeNummer = (int)reader["NebengruppeNummer"],
                                        IstAktiv = (bool)reader["IstAktiv"],
                                        IstTestGruppe = (bool)reader["IstTestGruppe"],
                                        GehoertZuHauptgruppe = (int)reader["GehoertZuHauptgruppeNummer"]
                                    });
                                }
                            }
                        }
                    }
                    return list;

                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetHauptGruppenPage.", ex);
            }
        }


        // CRUD.

        ////////// .Kategorie. //////////// 
        public bool KategorieNummerExists(int kategorieNummer)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM Kategorie WHERE KategorieNummer = @KategorieNummer";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@KategorieNummer", kategorieNummer);
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim KategorieNummerExists.", ex);
            }
        }


        // Create.
        public int CreateKategorieRep(Kategorie kategorie)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                                   INSERT INTO Kategorie (Bezeichnung, Kuerzel, Beschreibung, KategorieNummer, IstAktiv, IstTestKategorie)
                                   OUTPUT INSERTED.Id
                                   VALUES (@Bezeichnung, @Kuerzel, @Beschreibung, @KategorieNummer, @IstAktiv, @IstTestKategorie);";

                    connection.Open();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Bezeichnung", kategorie.Bezeichnung ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Kuerzel", kategorie.Kuerzel ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Beschreibung", kategorie.Beschreibung ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@KategorieNummer", kategorie.KategorieNummer);
                        command.Parameters.AddWithValue("@IstAktiv", kategorie.IstAktiv);
                        command.Parameters.AddWithValue("@IstTestKategorie", kategorie.IstTestKategorie);

                        // Return neues ID.
                        return (int)command.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim CreateKategorieRep.", ex);
            }
        }


        // Um Zur Gerade Erstellten Auswahl Zu Springen.
        public int GetKategorieNummerById(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SqlCommand("SELECT KategorieNummer FROM Kategorie WHERE Id = @Id", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            throw new Exception("KategorieNummer konnte nicht gefunden werden.");
                        }

                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetKategorieNummerById.", ex);
            }
        }




        // --- Nach Id lesen. ---
        public Kategorie GetById(int id)
        {
            try
            {
                Kategorie kategorie = null;
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = "SELECT * FROM Kategorie WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    kategorie = new Kategorie
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Bezeichnung = reader["Bezeichnung"] != DBNull.Value ? reader["Bezeichnung"].ToString() : null,
                        Kuerzel = reader["Kuerzel"] != DBNull.Value ? reader["Kuerzel"].ToString() : null,
                        Beschreibung = reader["Beschreibung"] != DBNull.Value ? reader["Beschreibung"].ToString() : null,
                        KategorieNummer = reader["KategorieNummer"] != DBNull.Value ? Convert.ToInt32(reader["KategorieNummer"]) : 0,
                        IstAktiv = reader["IstAktiv"] != DBNull.Value ? Convert.ToBoolean(reader["IstAktiv"]) : false,
                        IstTestKategorie = reader["IstTestKategorie"] != DBNull.Value ? Convert.ToBoolean(reader["IstTestKategorie"]) : false,
                    };
                }

                return kategorie;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetById.", ex);
            }
        }


        //  --- Update ---
        public void UpdateKategorie(Kategorie kategorie, int oldKategorieNummer,bool applyToGroups)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. Kategorie aktualisieren.
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"
                                          UPDATE Kategorie
                                          SET Bezeichnung=@Bezeichnung, Kuerzel=@Kuerzel, Beschreibung=@Beschreibung,
                                          KategorieNummer=@KategorieNummer, IstAktiv=@IstAktiv, IstTestKategorie=@IstTestKategorie
                                          WHERE Id=@Id";

                        cmd.Parameters.AddWithValue("@Id", kategorie.Id);
                        cmd.Parameters.AddWithValue("@Bezeichnung", (object)kategorie.Bezeichnung ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Kuerzel", (object)kategorie.Kuerzel ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Beschreibung", (object)kategorie.Beschreibung ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@KategorieNummer", kategorie.KategorieNummer);
                        cmd.Parameters.AddWithValue("@IstAktiv", kategorie.IstAktiv);
                        cmd.Parameters.AddWithValue("@IstTestKategorie", kategorie.IstTestKategorie);

                        cmd.ExecuteNonQuery();
                    }

                    // 2. Wenn KategorieNummer sich geändert hat, Hauptgruppe & Nebengruppe anpassen.
                    if (oldKategorieNummer != kategorie.KategorieNummer)
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.Transaction = transaction;

                            cmd.CommandText = @"
                                              UPDATE Hauptgruppe
                                              SET GehoertZuKategorieNummer=@NewKategorieNummer
                                              WHERE GehoertZuKategorieNummer=@OldKategorieNummer";

                            cmd.Parameters.AddWithValue("@NewKategorieNummer", kategorie.KategorieNummer);
                            cmd.Parameters.AddWithValue("@OldKategorieNummer", oldKategorieNummer);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.Transaction = transaction;

                            cmd.CommandText = @"
                                              UPDATE Nebengruppe
                                              SET GehoertZuKategorieNummer=@NewKategorieNummer
                                              WHERE GehoertZuKategorieNummer=@OldKategorieNummer";

                            cmd.Parameters.AddWithValue("@NewKategorieNummer", kategorie.KategorieNummer);
                            cmd.Parameters.AddWithValue("@OldKategorieNummer", oldKategorieNummer);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 3. Flags IstAktiv und IstTestKategorie auf Gruppen anwenden, falls gewünscht.
                    if (applyToGroups)
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.Transaction = transaction;

                            cmd.CommandText = @"
                                              UPDATE Hauptgruppe
                                              SET IstAktiv=@IstAktiv, IstTestGruppe=@IstTestKategorie
                                              WHERE GehoertZuKategorieNummer=@KategorieNummer";

                            cmd.Parameters.AddWithValue("@IstAktiv", kategorie.IstAktiv);
                            cmd.Parameters.AddWithValue("@IstTestKategorie", kategorie.IstTestKategorie);
                            cmd.Parameters.AddWithValue("@KategorieNummer", kategorie.KategorieNummer);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.Transaction = transaction;

                            cmd.CommandText = @"
                                              UPDATE Nebengruppe
                                              SET IstAktiv=@IstAktiv, IstTestGruppe=@IstTestKategorie
                                              WHERE GehoertZuKategorieNummer=@KategorieNummer";

                            cmd.Parameters.AddWithValue("@IstAktiv", kategorie.IstAktiv);
                            cmd.Parameters.AddWithValue("@IstTestKategorie", kategorie.IstTestKategorie);
                            cmd.Parameters.AddWithValue("@KategorieNummer", kategorie.KategorieNummer);
                            cmd.ExecuteNonQuery();
                        }
                    }


                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim UpdateKategorie.", ex);
            }
        }



        // --- Delete. ---

        public bool DeleteKategorieRep(int id, int kategorieNummer)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Nehmen die tatsächliche KategorieNummer aus der Tabelle.
                    int aktuelleNummer = 0;
                    using (var cmd = new SqlCommand("SELECT KategorieNummer FROM Kategorie WHERE Id = @Id", connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        var result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                            return false;

                        aktuelleNummer = Convert.ToInt32(result);
                    }

                    //Prüfen, ob es Hauptgruppen gibt, die mit dieser KategorieNummer verknüpft sind.
                    using (var checkCmd = new SqlCommand(@"
                                                         SELECT COUNT(*) 
                                                         FROM Hauptgruppe 
                                                         WHERE GehoertZuKategorieNummer = @KategorieNummer", connection))
                    {
                        checkCmd.Parameters.AddWithValue("@KategorieNummer", aktuelleNummer);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                            return false;
                    }

                    // Wenn keine verknüpften Hauptgruppen vorhanden sind, löschen.
                    using (var deleteCmd = new SqlCommand("DELETE FROM Kategorie WHERE Id = @Id", connection))
                    {
                        deleteCmd.Parameters.AddWithValue("@Id", id);
                        deleteCmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim DeleteKategorieRep.", ex);
            }
        }
















        // Hauptgruppe.
        public int CreateHauptgruppeRep(Hauptgruppe hauptgruppe)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {

                    connection.Open();

                    string query = @"
                                   INSERT INTO Hauptgruppe 
                                   (GehoertZuKategorieNummer, Bezeichnung, Kuerzel, Beschreibung, HauptgruppeNummer, LaufenderAktenzeichenZaehler, IstAktiv, IstTestGruppe)
                                   OUTPUT INSERTED.Id
                                   VALUES 
                                   (@GehoertZuKategorieNummer, @Bezeichnung, @Kuerzel, @Beschreibung, @HauptgruppeNummer, @LaufenderAktenzeichenZaehler, @IstAktiv, @IstTestGruppe);";

                    using (var command = new SqlCommand(query, connection))
                    {

                        command.Parameters.AddWithValue("@GehoertZuKategorieNummer", hauptgruppe.GehoertZuKategorie);
                        command.Parameters.AddWithValue("@Bezeichnung", (object)hauptgruppe.Bezeichnung ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Kuerzel", (object)hauptgruppe.Kuerzel ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Beschreibung", (object)hauptgruppe.Beschreibung ?? DBNull.Value);
                        command.Parameters.AddWithValue("@HauptgruppeNummer", hauptgruppe.HauptgruppeNummer);
                        command.Parameters.AddWithValue("@LaufenderAktenzeichenZaehler", hauptgruppe.LaufenderAktenzeichenZaehler);
                        command.Parameters.AddWithValue("@IstAktiv", hauptgruppe.IstAktiv);
                        command.Parameters.AddWithValue("@IstTestGruppe", hauptgruppe.IstTestGruppe);

                        // Führt den Befehl aus und gibt die neue ID zurück.
                        int newId = (int)command.ExecuteScalar();
                        return newId;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim CreateHauptgruppeRep.", ex);
            }
        }

        public bool HauptgruppeNummerExists(int kategorieNummer, int hauptgruppeNummer, int? excludeId = null)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                                   SELECT COUNT(*) 
                                   FROM Hauptgruppe 
                                   WHERE GehoertZuKategorieNummer = @KategorieNummer
                                   AND HauptgruppeNummer = @HauptgruppeNummer";

                    if (excludeId.HasValue)
                        query += " AND Id <> @ExcludeId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@KategorieNummer", kategorieNummer);
                        cmd.Parameters.AddWithValue("@HauptgruppeNummer", hauptgruppeNummer);
                        if (excludeId.HasValue)
                            cmd.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim HauptgruppeNummerExists.", ex);
            }
        }


        public int GetHauptgruppePosition(int kategorieNummer, int hauptgruppeNummer)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                                   SELECT COUNT(*) 
                                   FROM Hauptgruppe
                                   WHERE GehoertZuKategorieNummer = @kategorieNummer
                                   AND HauptgruppeNummer <= @hauptgruppeNummer"; // Alle kleineren oder gleiche Nummern zählen.

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@kategorieNummer", kategorieNummer);
                        cmd.Parameters.AddWithValue("@hauptgruppeNummer", hauptgruppeNummer);

                        return (int)cmd.ExecuteScalar(); // Position der Hauptgruppe in der sortierten Liste.
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetHauptgruppePosition.", ex);
            }
        }



        public string GetKategorieBezeichnungByNummer(int kategorieNummer)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {

                    connection.Open();

            
                    string query = "SELECT Bezeichnung FROM Kategorie WHERE KategorieNummer = @KategorieNummer";

                    using (var command = new SqlCommand(query, connection))
                    {
   
                        command.Parameters.AddWithValue("@KategorieNummer", kategorieNummer);

                   
                        var result = command.ExecuteScalar();

                        // Wenn nichts gefunden wurde, leeren String zurückgeben.
                        return result != null ? result.ToString() : string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetKategorieBezeichnungByNummer.", ex);
            }
        }


        public string GetHauptgruppeBezeichnungByNummer(int hauptgruppeNummer)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT Bezeichnung FROM Hauptgruppe WHERE HauptgruppeNummer = @Nummer", conn))
                {
                    cmd.Parameters.AddWithValue("@Nummer", hauptgruppeNummer);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    return result != null ? result.ToString() : "";
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetHauptgruppeBezeichnungByNummer.", ex);
            }
        }




        public bool DeleteHauptgruppeRep(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Holt die HauptgruppeNummer anhand der Id.
                    var cmdGetNummer = new SqlCommand("SELECT HauptgruppeNummer FROM Hauptgruppe WHERE Id = @Id", connection);
                    cmdGetNummer.Parameters.AddWithValue("@Id", id);

                    object hauptgruppeNummerObj = cmdGetNummer.ExecuteScalar();

                    if (hauptgruppeNummerObj == null || hauptgruppeNummerObj == DBNull.Value)
                    {
                        // Wenn keine Hauptgruppe gefunden wurde, wird false zurückgegeben.
                        return false;
                    }

                    int hauptgruppeNummer = Convert.ToInt32(hauptgruppeNummerObj);

                    // Prüft, ob Nebengruppen mit derselben HauptgruppeNummer existieren.
                    var cmdCheck = new SqlCommand("SELECT COUNT(*) FROM Nebengruppe WHERE GehoertZuHauptgruppeNummer = @Nummer",connection
                    );
                    cmdCheck.Parameters.AddWithValue("@Nummer", hauptgruppeNummer);

                    int nebengruppeCount = (int)cmdCheck.ExecuteScalar();

                    if (nebengruppeCount > 0)
                    {
                        // Wenn Nebengruppen vorhanden sind, darf die Hauptgruppe nicht gelöscht werden.
                        return false;
                    }

                    // Löscht die Hauptgruppe, wenn keine Nebengruppen damit verknüpft sind.
                    var cmdDelete = new SqlCommand("DELETE FROM Hauptgruppe WHERE Id = @Id", connection);
                    cmdDelete.Parameters.AddWithValue("@Id", id);
                    cmdDelete.ExecuteNonQuery();

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim DeleteHauptgruppeRep.", ex);
            }
        }





        // Get Hauptgruppe by Id.
        public Hauptgruppe? GetHauptgruppeById(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT * FROM Hauptgruppe WHERE Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Hauptgruppe
                            {
                                Id = (int)reader["Id"],
                                GehoertZuKategorie = (int)reader["GehoertZuKategorieNummer"],
                                Bezeichnung = reader["Bezeichnung"].ToString(),
                                Kuerzel = reader["Kuerzel"].ToString(),
                                Beschreibung = reader["Beschreibung"].ToString(),
                                HauptgruppeNummer = (int)reader["HauptgruppeNummer"],
                                LaufenderAktenzeichenZaehler = (int)reader["LaufenderAktenzeichenZaehler"],
                                IstAktiv = (bool)reader["IstAktiv"],
                                IstTestGruppe = (bool)reader["IstTestGruppe"]
                            };
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetHauptgruppeById.", ex);
            }
        }





        public bool UpdateHauptgruppe(Hauptgruppe model, bool applyToGroups)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1) Hauptgruppe aktualisieren.
                            using (var cmd = new SqlCommand(
                                @"UPDATE Hauptgruppe SET 
                                GehoertZuKategorieNummer = @GehoertZuKategorieNummer,
                                Bezeichnung = @Bezeichnung,
                                Kuerzel = @Kuerzel,
                                Beschreibung = @Beschreibung,
                                HauptgruppeNummer = @HauptgruppeNummer,
                                LaufenderAktenzeichenZaehler = @LaufenderAktenzeichenZaehler,
                                IstAktiv = @IstAktiv,
                                IstTestGruppe = @IstTestGruppe
                                WHERE Id = @Id", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Id", model.Id);
                                cmd.Parameters.AddWithValue("@GehoertZuKategorieNummer", model.GehoertZuKategorie);
                                cmd.Parameters.AddWithValue("@Bezeichnung", model.Bezeichnung ?? "");
                                cmd.Parameters.AddWithValue("@Kuerzel", model.Kuerzel ?? "");
                                cmd.Parameters.AddWithValue("@Beschreibung", model.Beschreibung ?? "");
                                cmd.Parameters.AddWithValue("@HauptgruppeNummer", model.HauptgruppeNummer);
                                cmd.Parameters.AddWithValue("@LaufenderAktenzeichenZaehler", model.LaufenderAktenzeichenZaehler);
                                cmd.Parameters.AddWithValue("@IstAktiv", model.IstAktiv);
                                cmd.Parameters.AddWithValue("@IstTestGruppe", model.IstTestGruppe);

                                cmd.ExecuteNonQuery();
                            }

                            // 2) Wir aktualisieren alle Nebengruppen dieser Hauptgruppe.
                            using (var cmd = new SqlCommand(
                                @"UPDATE Nebengruppe SET 
                                GehoertZuKategorieNummer = @GehoertZuKategorieNummer,
                                GehoertZuHauptgruppeNummer = @GehoertZuHauptgruppeNummer
                                WHERE GehoertZuHauptgruppeNummer = @Id", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@GehoertZuKategorieNummer", model.GehoertZuKategorie);
                                cmd.Parameters.AddWithValue("@GehoertZuHauptgruppeNummer", model.Id);
                                cmd.Parameters.AddWithValue("@Id", model.Id);

                                cmd.ExecuteNonQuery();
                            }

                            // 3. Flags IstAktiv und IstTestKategorie auf Gruppen anwenden, falls gewünscht.
                            if (applyToGroups)
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = transaction;

                                    cmd.CommandText = @"
                                                      UPDATE Nebengruppe
                                                      SET IstAktiv=@IstAktiv, IstTestGruppe=@IstTestGruppe
                                                      WHERE GehoertZuHauptgruppeNummer=@HauptgruppeNummer";
                                    cmd.Parameters.AddWithValue("@IstAktiv", model.IstAktiv);
                                    cmd.Parameters.AddWithValue("@IstTestGruppe", model.IstTestGruppe);
                                    cmd.Parameters.AddWithValue("@HauptgruppeNummer", model.HauptgruppeNummer);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim UpdateHauptgruppe.", ex);
            }
        }


     

        public int CreateNebengruppeRep(Nebengruppe nebengruppe)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {

                    connection.Open();


                    string query = @"
                                   INSERT INTO Nebengruppe 
                                   (GehoertZuKategorieNummer, GehoertZuHauptgruppeNummer, Bezeichnung, Kuerzel, Beschreibung, NebengruppeNummer, IstAktiv, IstTestGruppe)
                                   OUTPUT INSERTED.Id
                                   VALUES 
                                   (@GehoertZuKategorieNummer, @GehoertZuHauptgruppeNummer, @Bezeichnung, @Kuerzel, @Beschreibung, @NebengruppeNummer, @IstAktiv, @IstTestGruppe);";

                    using (var command = new SqlCommand(query, connection))
                    {

                        command.Parameters.AddWithValue("@GehoertZuKategorieNummer", nebengruppe.GehoertZuKategorie);
                        command.Parameters.AddWithValue("@GehoertZuHauptgruppeNummer", nebengruppe.GehoertZuHauptgruppe);
                        command.Parameters.AddWithValue("@Bezeichnung", (object)nebengruppe.Bezeichnung ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Kuerzel", (object)nebengruppe.Kuerzel ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Beschreibung", (object)nebengruppe.Beschreibung ?? DBNull.Value);
                        command.Parameters.AddWithValue("@NebengruppeNummer", nebengruppe.NebengruppeNummer);
                        command.Parameters.AddWithValue("@IstAktiv", nebengruppe.IstAktiv);
                        command.Parameters.AddWithValue("@IstTestGruppe", nebengruppe.IstTestGruppe);

                        int insertedId = Convert.ToInt32(command.ExecuteScalar());
                        return insertedId;

                    }

                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim CreateNebengruppeRep.", ex);
            }

        }




        // get Nebengruppe by Id.
        public Nebengruppe? GetNebengruppeById(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT * FROM Nebengruppe WHERE Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Nebengruppe
                            {
                                Id = (int)reader["Id"],
                                Bezeichnung = reader["Bezeichnung"].ToString(),
                                Kuerzel = reader["Kuerzel"].ToString(),
                                Beschreibung = reader["Beschreibung"].ToString(),
                                NebengruppeNummer = (int)reader["NebengruppeNummer"],
                                IstAktiv = (bool)reader["IstAktiv"],
                                IstTestGruppe = (bool)reader["IstTestGruppe"]
                            };
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetNebengruppeById.", ex);
            }
        }



        public bool UpdateNebengruppe(Nebengruppe nebengruppe)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {

                    connection.Open();


                    string query = @"
                                   UPDATE Nebengruppe
                                   SET 
                                   GehoertZuKategorieNummer = @GehoertZuKategorieNummer,
                                   GehoertZuHauptgruppeNummer = @GehoertZuHauptgruppeNummer,
                                   Bezeichnung = @Bezeichnung,
                                   Kuerzel = @Kuerzel,
                                   Beschreibung = @Beschreibung,
                                   NebengruppeNummer = @NebengruppeNummer,
                                   IstAktiv = @IstAktiv,
                                   IstTestGruppe = @IstTestGruppe
                                   WHERE Id = @Id";

                    using (var command = new SqlCommand(query, connection))
                    {

                        command.Parameters.AddWithValue("@GehoertZuKategorieNummer", (object)nebengruppe.GehoertZuKategorie ?? DBNull.Value);
                        command.Parameters.AddWithValue("@GehoertZuHauptgruppeNummer", (object)nebengruppe.GehoertZuHauptgruppe ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Bezeichnung", (object)nebengruppe.Bezeichnung ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Kuerzel", (object)nebengruppe.Kuerzel ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Beschreibung", (object)nebengruppe.Beschreibung ?? DBNull.Value);
                        command.Parameters.AddWithValue("@NebengruppeNummer", nebengruppe.NebengruppeNummer);
                        command.Parameters.AddWithValue("@IstAktiv", nebengruppe.IstAktiv);
                        command.Parameters.AddWithValue("@IstTestGruppe", nebengruppe.IstTestGruppe);
                        command.Parameters.AddWithValue("@Id", nebengruppe.Id);


                        int rowsAffected = command.ExecuteNonQuery();


                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim UpdateNebengruppe.", ex);
            }
        }


        public List<Hauptgruppe> GetHauptgruppenByKategorie(int kategorieNummer)
        {
            try
            {
                var hauptgruppen = new List<Hauptgruppe>();

                using (var connection = new SqlConnection(_connectionString))
                {

                    connection.Open();

                    using (var command = new SqlCommand(@"
                                                        SELECT Id, Bezeichnung, Kuerzel, Beschreibung, HauptgruppeNummer, 
                                                        GehoertZuKategorieNummer, IstAktiv, IstTestGruppe
                                                        FROM Hauptgruppe
                                                        WHERE GehoertZuKategorieNummer = @KategorieNummer
                                                        ORDER BY HauptgruppeNummer ASC", connection))
                    {

                        command.Parameters.AddWithValue("@KategorieNummer", kategorieNummer);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                hauptgruppen.Add(new Hauptgruppe
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Bezeichnung = reader["Bezeichnung"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Bezeichnung")) : "",
                                    Kuerzel = reader["Kuerzel"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Kuerzel")) : "",
                                    Beschreibung = reader["Beschreibung"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Beschreibung")) : "",
                                    HauptgruppeNummer = reader["HauptgruppeNummer"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("HauptgruppeNummer")) : 0,
                                    GehoertZuKategorie = reader["GehoertZuKategorieNummer"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("GehoertZuKategorieNummer")) : 0,
                                    IstAktiv = reader["IstAktiv"] != DBNull.Value && (bool)reader["IstAktiv"],
                                    IstTestGruppe = reader["IstTestGruppe"] != DBNull.Value && (bool)reader["IstTestGruppe"]
                                });
                            }
                        }
                    }
                }

                return hauptgruppen;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetHauptgruppenByKategorie.", ex);
            }
        }




        public bool DeleteNebengruppeRep(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Nebengruppe löschen.
                    var cmdAbt = new SqlCommand("DELETE FROM Nebengruppe WHERE Id=@Id", connection);
                    cmdAbt.Parameters.AddWithValue("@Id", id);
                    cmdAbt.ExecuteNonQuery();

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim DeleteNebengruppeRep.", ex);
            }
        }

    }
}
