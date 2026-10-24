//using AdminPanelDB.Abstractions;
//using AdminPanelDB.Exeptions;
//using AdminPanelDB.Models;
//using AdminPanelDB.ViewModels;
//using Microsoft.Data.SqlClient;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;

//namespace AdminPanelDB.Repository
//{
//    public class KaGruppenRepository
//    {
//        private readonly string _connectionString;
//        private readonly ICurrentMandantProvider _mandantProvider;

//        public KaGruppenRepository(string connectionString, ICurrentMandantProvider mandantProvider)
//        {
//            _connectionString = connectionString;
//            _mandantProvider = mandantProvider;
//        }

//        public int GetHauptGruppenCount(string hauptgruppeSearchTerm = "", string nebengruppeSearchTerm = "", int? kategorieNummer = null)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    string cmdText;

//                    if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
//                    {
//                        cmdText = @"
//                                  SELECT COUNT(DISTINCT a.Id)
//                                  FROM Hauptgruppe a
//                                  INNER JOIN Nebengruppe r ON r.GehoertZuHauptgruppeNummer = a.HauptgruppeNummer
//                                  WHERE a.Bezeichnung LIKE @hauptSearch
//                                  AND r.Bezeichnung LIKE @nebenSearch
//                                  AND a.MandantId = @MandantId
//                                  AND r.MandantId = @MandantId";
//                    }
//                    else
//                    {
//                        cmdText = @"
//                                  SELECT COUNT(DISTINCT a.Id)
//                                  FROM Hauptgruppe a
//                                  LEFT JOIN Nebengruppe r ON r.GehoertZuHauptgruppeNummer = a.Id
//                                  WHERE a.Bezeichnung LIKE @hauptSearch
//                                  AND a.MandantId = @MandantId";
//                    }

//                    if (kategorieNummer.HasValue)
//                    {
//                        cmdText += " AND a.GehoertZuKategorieNummer = @kategorieNummer";
//                    }

//                    using (var cmd = new SqlCommand(cmdText, conn))
//                    {
//                        cmd.Parameters.AddWithValue("@hauptSearch", $"%{hauptgruppeSearchTerm}%");
//                        if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
//                        {
//                            cmd.Parameters.AddWithValue("@nebenSearch", $"%{nebengruppeSearchTerm}%");
//                        }
//                        if (kategorieNummer.HasValue)
//                        {
//                            cmd.Parameters.AddWithValue("@kategorieNummer", kategorieNummer.Value);
//                        }
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                        return (int)cmd.ExecuteScalar();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetHauptGruppenCount.", ex);
//            }
//        }

//        public List<KaGruppenViewModel> GetHauptGruppenPage(int pageNumber, int pageSize, string hauptgruppeSearchTerm = "", string nebengruppeSearchTerm = "", string sortColumn = null, string sortDirection = null, int? kategorieNummer = null)
//        {
//            try
//            {
//                var list = new List<KaGruppenViewModel>();


//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    int offset = (pageNumber - 1) * pageSize;
//                    var allowedColumns = new[] { "Bezeichnung", "Id", "HauptgruppeNummer" };
//                    if (string.IsNullOrEmpty(sortColumn) || !allowedColumns.Contains(sortColumn))
//                    {
//                        sortColumn = "HauptgruppeNummer";
//                    }
//                    if (string.IsNullOrEmpty(sortDirection) || (sortDirection != "ASC" && sortDirection != "DESC"))
//                    {
//                        sortDirection = "ASC";
//                    }

//                    string cmdText;

//                    // Hauptgruppe.
//                    if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
//                    {
//                        cmdText = $@"
//                                  SELECT DISTINCT a.Id, a.ObjektId, a.GehoertZuKategorieNummer, a.Bezeichnung, a.Kuerzel, a.Beschreibung, a.HauptgruppeNummer, a.LaufenderAktenzeichenZaehler, a.IstAktiv, a.IstTestGruppe
//                                  FROM Hauptgruppe a
//                                  INNER JOIN Nebengruppe r ON r.GehoertZuHauptgruppeNummer = a.HauptgruppeNummer
//                                  WHERE a.Bezeichnung LIKE @hauptSearch
//                                  AND r.Bezeichnung LIKE @nebenSearch
//                                  AND a.MandantId = @MandantId
//                                  AND r.MandantId = @MandantId";
//                    }
//                    else
//                    {
//                        cmdText = $@"
//                                  SELECT DISTINCT a.Id, a.ObjektId, a.GehoertZuKategorieNummer, a.Bezeichnung, a.Kuerzel, a.Beschreibung, a.HauptgruppeNummer, a.LaufenderAktenzeichenZaehler, a.IstAktiv, a.IstTestGruppe
//                                  FROM Hauptgruppe a
//                                  LEFT JOIN Nebengruppe r ON r.GehoertZuHauptgruppeNummer = a.HauptgruppeNummer
//                                  WHERE a.Bezeichnung LIKE @hauptSearch
//                                  AND a.MandantId = @MandantId";
//                    }

//                    if (kategorieNummer.HasValue)
//                    {
//                        cmdText += " AND a.GehoertZuKategorieNummer = @kategorieNummer";
//                    }

//                    cmdText += $" ORDER BY {sortColumn} {sortDirection} OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

//                    using (var cmd = new SqlCommand(cmdText, conn))
//                    {
//                        cmd.Parameters.AddWithValue("@hauptSearch", $"%{hauptgruppeSearchTerm}%");
//                        if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
//                        {
//                            cmd.Parameters.AddWithValue("@nebenSearch", $"%{nebengruppeSearchTerm}%");
//                        }
//                        if (kategorieNummer.HasValue)
//                        {
//                            cmd.Parameters.AddWithValue("@kategorieNummer", kategorieNummer.Value);
//                        }
//                        cmd.Parameters.AddWithValue("@offset", offset);
//                        cmd.Parameters.AddWithValue("@pageSize", pageSize);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                        using (var reader = cmd.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                list.Add(new KaGruppenViewModel
//                                {
//                                    Hauptgruppen = new List<Hauptgruppe>
//                                    {
//                                        new Hauptgruppe
//                                        {
//                                            Id = (int)reader["Id"],
//                                            ObjektId = (Guid)reader["ObjektId"],
//                                            GehoertZuKategorie = (int)reader["GehoertZuKategorieNummer"],
//                                            Bezeichnung = reader["Bezeichnung"].ToString()!,
//                                            Kuerzel = reader["Kuerzel"].ToString()!,
//                                            Beschreibung = reader["Beschreibung"].ToString()!,
//                                            HauptgruppeNummer = (int)reader["HauptgruppeNummer"],
//                                            LaufenderAktenzeichenZaehler = (int)reader["LaufenderAktenzeichenZaehler"],
//                                            IstAktiv = (bool)reader["IstAktiv"],
//                                            IstTestGruppe = (bool)reader["IstTestGruppe"]
//                                        }
//                                    },
//                                    Nebengruppen = new List<Nebengruppe>()
//                                });
//                            }
//                        }
//                    }

//                    // Nebengruppen.
//                    foreach (var item in list)
//                    {
//                        string query = @"
//                                       SELECT Id, GehoertZuKategorieNummer, Bezeichnung, Kuerzel, Beschreibung, NebengruppeNummer, 
//                                       IstAktiv, IstTestGruppe, GehoertZuHauptgruppeNummer
//                                       FROM Nebengruppe
//                                       WHERE GehoertZuHauptgruppeNummer = @HauptgruppeNummer
//                                       AND MandantId = @MandantId";

//                        if (kategorieNummer.HasValue)
//                        {
//                            query += " AND GehoertZuKategorieNummer = @kategorieNummer";
//                        }

//                        if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
//                        {
//                            query += " AND Bezeichnung LIKE @nebenSearch";
//                        }

//                        // Sortierung nach NebengruppeNummer.
//                        query += " ORDER BY NebengruppeNummer ASC";

//                        using (var cmd = new SqlCommand(query, conn))
//                        {

//                            cmd.Parameters.AddWithValue("@HauptgruppeNummer", item.Hauptgruppen!.First().HauptgruppeNummer);

//                            if (!string.IsNullOrEmpty(nebengruppeSearchTerm))
//                            {
//                                cmd.Parameters.AddWithValue("@nebenSearch", $"%{nebengruppeSearchTerm}%");
//                            }

//                            if (kategorieNummer.HasValue)
//                            {
//                                cmd.Parameters.AddWithValue("@kategorieNummer", kategorieNummer.Value);
//                            }

//                            cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                            using (var reader = cmd.ExecuteReader())
//                            {
//                                while (reader.Read())
//                                {
//                                    item.Nebengruppen!.Add(new Nebengruppe
//                                    {
//                                        Id = (int)reader["Id"],
//                                        GehoertZuKategorie = (int)reader["GehoertZuKategorieNummer"],
//                                        Bezeichnung = reader["Bezeichnung"].ToString()!,
//                                        Kuerzel = reader["Kuerzel"].ToString()!,
//                                        NebengruppeNummer = (int)reader["NebengruppeNummer"],
//                                        IstAktiv = (bool)reader["IstAktiv"],
//                                        IstTestGruppe = (bool)reader["IstTestGruppe"],
//                                        GehoertZuHauptgruppe = (int)reader["GehoertZuHauptgruppeNummer"]
//                                    });
//                                }
//                            }
//                        }
//                    }
//                    return list;

//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetHauptGruppenPage.", ex);
//            }
//        }


//        // Kategorie.
//        public bool KategorieNummerExists(int kategorieNummer)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();

//                    string query = "SELECT COUNT(*) FROM Kategorie WHERE KategorieNummer = @KategorieNummer AND MandantId = @MandantId";
//                    using (var cmd = new SqlCommand(query, conn))
//                    {
//                        cmd.Parameters.AddWithValue("@KategorieNummer", kategorieNummer);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        int count = (int)cmd.ExecuteScalar();
//                        return count > 0;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim KategorieNummerExists.", ex);
//            }
//        }

//        public List<Kategorie> GetAllKategorien()
//        {
//            try
//            {
//                var kategorien = new List<Kategorie>();
//                using (var connection = new SqlConnection(_connectionString))
//                {
//                    connection.Open();

//                    using (var command = new SqlCommand(@"
//                                                SELECT Id, ObjektId, Bezeichnung, Kuerzel, Beschreibung, KategorieNummer, IstAktiv, IstTestKategorie
//                                                FROM Kategorie
//                                                WHERE MandantId = @MandantId
//                                                ORDER BY KategorieNummer ASC", connection))
//                    {
//                        command.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        using (var reader = command.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                kategorien.Add(new Kategorie
//                                {
//                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
//                                    ObjektId = reader["ObjektId"] != DBNull.Value ? reader.GetGuid(reader.GetOrdinal("ObjektId")) : Guid.Empty,
//                                    Bezeichnung = reader.GetString(reader.GetOrdinal("Bezeichnung")),
//                                    Kuerzel = reader["Kuerzel"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Kuerzel")) : "",
//                                    Beschreibung = reader["Beschreibung"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Beschreibung")) : "",
//                                    KategorieNummer = reader["KategorieNummer"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("KategorieNummer")) : 0,
//                                    IstAktiv = reader["IstAktiv"] != DBNull.Value && (bool)reader["IstAktiv"],
//                                    IstTestKategorie = reader["IstTestKategorie"] != DBNull.Value && (bool)reader["IstTestKategorie"]
//                                });
//                            }
//                        }
//                    }
//                }
//                return kategorien;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetAllKategorien.", ex);
//            }
//        }

//        public int CreateKategorieRep(Kategorie kategorie)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                {
//                    if (kategorie.ObjektId == Guid.Empty)
//                    {
//                        kategorie.ObjektId = Guid.NewGuid();
//                    }

//                    string query = @"
//                           INSERT INTO Kategorie (Bezeichnung, Kuerzel, Beschreibung, KategorieNummer, IstAktiv, IstTestKategorie, ObjektId, MandantId)
//                           OUTPUT INSERTED.Id
//                           VALUES (@Bezeichnung, @Kuerzel, @Beschreibung, @KategorieNummer, @IstAktiv, @IstTestKategorie, @ObjektId, @MandantId);";

//                    connection.Open();
//                    using (var command = new SqlCommand(query, connection))
//                    {
//                        command.Parameters.AddWithValue("@Bezeichnung", kategorie.Bezeichnung ?? (object)DBNull.Value);
//                        command.Parameters.AddWithValue("@Kuerzel", kategorie.Kuerzel ?? (object)DBNull.Value);
//                        command.Parameters.AddWithValue("@Beschreibung", kategorie.Beschreibung ?? (object)DBNull.Value);
//                        command.Parameters.AddWithValue("@KategorieNummer", kategorie.KategorieNummer);
//                        command.Parameters.AddWithValue("@IstAktiv", kategorie.IstAktiv);
//                        command.Parameters.AddWithValue("@IstTestKategorie", kategorie.IstTestKategorie);
//                        command.Parameters.AddWithValue("@ObjektId", kategorie.ObjektId);
//                        command.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        return (int)command.ExecuteScalar();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim CreateKategorieRep.", ex);
//            }
//        }

//        public int GetKategorieNummerById(int id)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                {
//                    connection.Open();
//                    using (var cmd = new SqlCommand("SELECT KategorieNummer FROM Kategorie WHERE Id = @Id AND MandantId = @MandantId", connection))
//                    {
//                        cmd.Parameters.AddWithValue("@Id", id);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        object result = cmd.ExecuteScalar();
//                        if (result == null || result == DBNull.Value)
//                        {
//                            throw new Exception("KategorieNummer konnte nicht gefunden werden.");
//                        }

//                        return Convert.ToInt32(result);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetKategorieNummerById.", ex);
//            }
//        }

//        public Kategorie GetKategorieById(int id)
//        {
//            try
//            {
//                Kategorie kategorie = null;
//                using var connection = new SqlConnection(_connectionString);
//                connection.Open();
//                using var cmd = connection.CreateCommand();

//                cmd.CommandText = "SELECT * FROM Kategorie WHERE Id = @id AND MandantId = @MandantId";
//                cmd.Parameters.AddWithValue("@id", id);
//                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                using var reader = cmd.ExecuteReader();
//                if (reader.Read())
//                {
//                    kategorie = new Kategorie
//                    {
//                        Id = Convert.ToInt32(reader["Id"]),
//                        ObjektId = reader["ObjektId"] != DBNull.Value ? (Guid)reader["ObjektId"] : Guid.Empty,
//                        Bezeichnung = reader["Bezeichnung"] != DBNull.Value ? reader["Bezeichnung"].ToString() : null,
//                        Kuerzel = reader["Kuerzel"] != DBNull.Value ? reader["Kuerzel"].ToString() : null,
//                        Beschreibung = reader["Beschreibung"] != DBNull.Value ? reader["Beschreibung"].ToString() : null,
//                        KategorieNummer = reader["KategorieNummer"] != DBNull.Value ? Convert.ToInt32(reader["KategorieNummer"]) : 0,
//                        IstAktiv = reader["IstAktiv"] != DBNull.Value ? Convert.ToBoolean(reader["IstAktiv"]) : false,
//                        IstTestKategorie = reader["IstTestKategorie"] != DBNull.Value ? Convert.ToBoolean(reader["IstTestKategorie"]) : false,
//                    };
//                }

//                return kategorie;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetById.", ex);
//            }
//        }

//        public Kategorie GetKategorieByKategroieNummer(int KategorieNummer)
//        {
//            try
//            {
//                Kategorie kategorie = null;
//                using var connection = new SqlConnection(_connectionString);
//                connection.Open();
//                using var cmd = connection.CreateCommand();
//                cmd.CommandText = "SELECT * FROM Kategorie WHERE KategorieNummer = @KategorieNummer AND MandantId = @MandantId";
//                cmd.Parameters.AddWithValue("@KategorieNummer", KategorieNummer);
//                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                using var reader = cmd.ExecuteReader();
//                if (reader.Read())
//                {
//                    kategorie = new Kategorie
//                    {
//                        Id = Convert.ToInt32(reader["Id"]),
//                        ObjektId = reader["ObjektId"] != DBNull.Value ? (Guid)reader["ObjektId"] : Guid.Empty,
//                        Bezeichnung = reader["Bezeichnung"] != DBNull.Value ? reader["Bezeichnung"].ToString() : null,
//                        Kuerzel = reader["Kuerzel"] != DBNull.Value ? reader["Kuerzel"].ToString() : null,
//                        Beschreibung = reader["Beschreibung"] != DBNull.Value ? reader["Beschreibung"].ToString() : null,
//                        KategorieNummer = reader["KategorieNummer"] != DBNull.Value ? Convert.ToInt32(reader["KategorieNummer"]) : 0,
//                        IstAktiv = reader["IstAktiv"] != DBNull.Value ? Convert.ToBoolean(reader["IstAktiv"]) : false,
//                        IstTestKategorie = reader["IstTestKategorie"] != DBNull.Value ? Convert.ToBoolean(reader["IstTestKategorie"]) : false,
//                    };
//                }
//                return kategorie;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetKategorieByKategroieNummer.", ex);
//            }
//        }

//        public void UpdateKategorie(Kategorie kategorie, int oldKategorieNummer, bool applyToGroups)
//        {
//            try
//            {
//                using var connection = new SqlConnection(_connectionString);
//                connection.Open();

//                using var transaction = connection.BeginTransaction();

//                try
//                {
//                    // 1. Kategorie aktualisieren.
//                    using (var cmd = connection.CreateCommand())
//                    {
//                        cmd.Transaction = transaction;
//                        cmd.CommandText = @"
//                                  UPDATE Kategorie
//                                  SET Bezeichnung=@Bezeichnung, Kuerzel=@Kuerzel, Beschreibung=@Beschreibung,
//                                  KategorieNummer=@KategorieNummer, IstAktiv=@IstAktiv, IstTestKategorie=@IstTestKategorie
//                                  WHERE Id=@Id AND MandantId=@MandantId";

//                        cmd.Parameters.AddWithValue("@Id", kategorie.Id);
//                        cmd.Parameters.AddWithValue("@Bezeichnung", (object)kategorie.Bezeichnung ?? DBNull.Value);
//                        cmd.Parameters.AddWithValue("@Kuerzel", (object)kategorie.Kuerzel ?? DBNull.Value);
//                        cmd.Parameters.AddWithValue("@Beschreibung", (object)kategorie.Beschreibung ?? DBNull.Value);
//                        cmd.Parameters.AddWithValue("@KategorieNummer", kategorie.KategorieNummer);
//                        cmd.Parameters.AddWithValue("@IstAktiv", kategorie.IstAktiv);
//                        cmd.Parameters.AddWithValue("@IstTestKategorie", kategorie.IstTestKategorie);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                        int affected = cmd.ExecuteNonQuery();
//                        if (affected == 0)
//                        {
//                            // Id existiert nicht (mehr) in diesem Mandanten - Abbruch statt stillem Weiterlaufen.
//                            throw new Exception("Kategorie wurde nicht gefunden oder gehört nicht zum aktuellen Mandanten.");
//                        }
//                    }

//                    // 2. Wenn KategorieNummer sich geändert hat, Hauptgruppe & Nebengruppe anpassen.
//                    if (oldKategorieNummer != kategorie.KategorieNummer)
//                    {
//                        using (var cmd = connection.CreateCommand())
//                        {
//                            cmd.Transaction = transaction;

//                            cmd.CommandText = @"
//                                      UPDATE Hauptgruppe
//                                      SET GehoertZuKategorieNummer=@NewKategorieNummer
//                                      WHERE GehoertZuKategorieNummer=@OldKategorieNummer AND MandantId=@MandantId";

//                            cmd.Parameters.AddWithValue("@NewKategorieNummer", kategorie.KategorieNummer);
//                            cmd.Parameters.AddWithValue("@OldKategorieNummer", oldKategorieNummer);
//                            cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                            cmd.ExecuteNonQuery();
//                        }

//                        using (var cmd = connection.CreateCommand())
//                        {
//                            cmd.Transaction = transaction;

//                            cmd.CommandText = @"
//                                      UPDATE Nebengruppe
//                                      SET GehoertZuKategorieNummer=@NewKategorieNummer
//                                      WHERE GehoertZuKategorieNummer=@OldKategorieNummer AND MandantId=@MandantId";

//                            cmd.Parameters.AddWithValue("@NewKategorieNummer", kategorie.KategorieNummer);
//                            cmd.Parameters.AddWithValue("@OldKategorieNummer", oldKategorieNummer);
//                            cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                            cmd.ExecuteNonQuery();
//                        }
//                    }

//                    // 3. Flags IstAktiv und IstTestKategorie auf Gruppen anwenden, falls gewünscht.
//                    if (applyToGroups)
//                    {
//                        using (var cmd = connection.CreateCommand())
//                        {
//                            cmd.Transaction = transaction;

//                            cmd.CommandText = @"
//                                      UPDATE Hauptgruppe
//                                      SET IstAktiv=@IstAktiv, IstTestGruppe=@IstTestKategorie
//                                      WHERE GehoertZuKategorieNummer=@KategorieNummer AND MandantId=@MandantId";

//                            cmd.Parameters.AddWithValue("@IstAktiv", kategorie.IstAktiv);
//                            cmd.Parameters.AddWithValue("@IstTestKategorie", kategorie.IstTestKategorie);
//                            cmd.Parameters.AddWithValue("@KategorieNummer", kategorie.KategorieNummer);
//                            cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                            cmd.ExecuteNonQuery();
//                        }

//                        using (var cmd = connection.CreateCommand())
//                        {
//                            cmd.Transaction = transaction;

//                            cmd.CommandText = @"
//                                      UPDATE Nebengruppe
//                                      SET IstAktiv=@IstAktiv, IstTestGruppe=@IstTestKategorie
//                                      WHERE GehoertZuKategorieNummer=@KategorieNummer AND MandantId=@MandantId";

//                            cmd.Parameters.AddWithValue("@IstAktiv", kategorie.IstAktiv);
//                            cmd.Parameters.AddWithValue("@IstTestKategorie", kategorie.IstTestKategorie);
//                            cmd.Parameters.AddWithValue("@KategorieNummer", kategorie.KategorieNummer);
//                            cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                            cmd.ExecuteNonQuery();
//                        }
//                    }


//                    transaction.Commit();
//                }
//                catch
//                {
//                    transaction.Rollback();
//                    throw;
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim UpdateKategorie.", ex);
//            }
//        }

//        public bool DeleteKategorieRep(int id, int kategorieNummer)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                {
//                    connection.Open();

//                    int aktuelleNummer = 0;
//                    using (var cmd = new SqlCommand("SELECT KategorieNummer FROM Kategorie WHERE Id = @Id AND MandantId = @MandantId", connection))
//                    {
//                        cmd.Parameters.AddWithValue("@Id", id);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        var result = cmd.ExecuteScalar();
//                        if (result == null || result == DBNull.Value)
//                            return false;

//                        aktuelleNummer = Convert.ToInt32(result);
//                    }

//                    // Prüfen, ob es Hauptgruppen gibt, die mit dieser KategorieNummer verknüpft sind.
//                    using (var checkCmd = new SqlCommand(@"
//                                                 SELECT COUNT(*) 
//                                                 FROM Hauptgruppe 
//                                                 WHERE GehoertZuKategorieNummer = @KategorieNummer AND MandantId = @MandantId", connection))
//                    {
//                        checkCmd.Parameters.AddWithValue("@KategorieNummer", aktuelleNummer);
//                        checkCmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        int count = (int)checkCmd.ExecuteScalar();
//                        if (count > 0)
//                            return false;
//                    }

//                    using (var deleteCmd = new SqlCommand("DELETE FROM Kategorie WHERE Id = @Id AND MandantId = @MandantId", connection))
//                    {
//                        deleteCmd.Parameters.AddWithValue("@Id", id);
//                        deleteCmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        deleteCmd.ExecuteNonQuery();
//                    }

//                    return true;
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim DeleteKategorieRep.", ex);
//            }
//        }

//        public string GetKategorieBezeichnungByNummer(int kategorieNummer)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                {

//                    connection.Open();

//                    string query = "SELECT Bezeichnung FROM Kategorie WHERE KategorieNummer = @KategorieNummer AND MandantId = @MandantId";

//                    using (var command = new SqlCommand(query, connection))
//                    {

//                        command.Parameters.AddWithValue("@KategorieNummer", kategorieNummer);
//                        command.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                        var result = command.ExecuteScalar();

//                        // Wenn nichts gefunden wurde, leeren String zurückgeben.
//                        return result != null ? result.ToString() : string.Empty;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetKategorieBezeichnungByNummer.", ex);
//            }
//        }


//        // Hauptgruppe.
//        public int CreateHauptgruppeRep(Hauptgruppe hauptgruppe)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                {

//                    // ObjektId generieren falls noch nicht gesetzt.
//                    if (hauptgruppe.ObjektId == Guid.Empty)
//                    {
//                        hauptgruppe.ObjektId = Guid.NewGuid();
//                    }

//                    connection.Open();

//                    string query = @"
//                                   INSERT INTO Hauptgruppe 
//                                   (GehoertZuKategorieNummer, Bezeichnung, Kuerzel, Beschreibung, HauptgruppeNummer, LaufenderAktenzeichenZaehler, IstAktiv, IstTestGruppe, ObjektId, MandantId)
//                                   OUTPUT INSERTED.Id
//                                   VALUES 
//                                   (@GehoertZuKategorieNummer, @Bezeichnung, @Kuerzel, @Beschreibung, @HauptgruppeNummer, @LaufenderAktenzeichenZaehler, @IstAktiv, @IstTestGruppe, @ObjektId, @MandantId);";

//                    using (var command = new SqlCommand(query, connection))
//                    {

//                        command.Parameters.AddWithValue("@GehoertZuKategorieNummer", hauptgruppe.GehoertZuKategorie);
//                        command.Parameters.AddWithValue("@Bezeichnung", (object)hauptgruppe.Bezeichnung ?? DBNull.Value);
//                        command.Parameters.AddWithValue("@Kuerzel", (object)hauptgruppe.Kuerzel ?? DBNull.Value);
//                        command.Parameters.AddWithValue("@Beschreibung", (object)hauptgruppe.Beschreibung ?? DBNull.Value);
//                        command.Parameters.AddWithValue("@HauptgruppeNummer", hauptgruppe.HauptgruppeNummer);
//                        command.Parameters.AddWithValue("@LaufenderAktenzeichenZaehler", hauptgruppe.LaufenderAktenzeichenZaehler);
//                        command.Parameters.AddWithValue("@IstAktiv", hauptgruppe.IstAktiv);
//                        command.Parameters.AddWithValue("@IstTestGruppe", hauptgruppe.IstTestGruppe);
//                        command.Parameters.AddWithValue("@ObjektId", hauptgruppe.ObjektId);
//                        command.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                        // Führt den Befehl aus und gibt die neue ID zurück.
//                        int newId = (int)command.ExecuteScalar();
//                        return newId;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim CreateHauptgruppeRep.", ex);
//            }
//        }

//        public bool HauptgruppeNummerExists(int kategorieNummer, int hauptgruppeNummer, int? excludeId = null)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    string query = @"
//                           SELECT COUNT(*) 
//                           FROM Hauptgruppe 
//                           WHERE GehoertZuKategorieNummer = @KategorieNummer
//                           AND HauptgruppeNummer = @HauptgruppeNummer
//                           AND MandantId = @MandantId";

//                    if (excludeId.HasValue)
//                        query += " AND Id <> @ExcludeId";

//                    using (var cmd = new SqlCommand(query, conn))
//                    {
//                        cmd.Parameters.AddWithValue("@KategorieNummer", kategorieNummer);
//                        cmd.Parameters.AddWithValue("@HauptgruppeNummer", hauptgruppeNummer);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        if (excludeId.HasValue)
//                            cmd.Parameters.AddWithValue("@ExcludeId", excludeId.Value);

//                        int count = (int)cmd.ExecuteScalar();
//                        return count > 0;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim HauptgruppeNummerExists.", ex);
//            }
//        }

//        public int GetHauptgruppePosition(int kategorieNummer, int hauptgruppeNummer)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    string query = @"
//                                   SELECT COUNT(*) 
//                                   FROM Hauptgruppe
//                                   WHERE GehoertZuKategorieNummer = @kategorieNummer
//                                   AND HauptgruppeNummer <= @hauptgruppeNummer
//                                   AND MandantId = @MandantId"; // Alle kleineren oder gleiche Nummern zählen.

//                    using (var cmd = new SqlCommand(query, conn))
//                    {
//                        cmd.Parameters.AddWithValue("@kategorieNummer", kategorieNummer);
//                        cmd.Parameters.AddWithValue("@hauptgruppeNummer", hauptgruppeNummer);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                        return (int)cmd.ExecuteScalar(); // Position der Hauptgruppe in der sortierten Liste.
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetHauptgruppePosition.", ex);
//            }
//        }

//        public string GetHauptgruppeBezeichnungByNummer(int hauptgruppeNummer)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                using (var cmd = new SqlCommand("SELECT Bezeichnung FROM Hauptgruppe WHERE HauptgruppeNummer = @Nummer AND MandantId = @MandantId", conn))
//                {
//                    cmd.Parameters.AddWithValue("@Nummer", hauptgruppeNummer);
//                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                    conn.Open();
//                    var result = cmd.ExecuteScalar();
//                    return result != null ? result.ToString() : "";
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetHauptgruppeBezeichnungByNummer.", ex);
//            }
//        }

//        public bool DeleteHauptgruppeRep(int id)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                {
//                    connection.Open();

//                    // Holt die HauptgruppeNummer anhand der Id.
//                    var cmdGetNummer = new SqlCommand("SELECT HauptgruppeNummer FROM Hauptgruppe WHERE Id = @Id AND MandantId = @MandantId", connection);
//                    cmdGetNummer.Parameters.AddWithValue("@Id", id);
//                    cmdGetNummer.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                    object hauptgruppeNummerObj = cmdGetNummer.ExecuteScalar();

//                    if (hauptgruppeNummerObj == null || hauptgruppeNummerObj == DBNull.Value)
//                    {
//                        // Wenn keine Hauptgruppe gefunden wurde, wird false zurückgegeben.
//                        return false;
//                    }

//                    int hauptgruppeNummer = Convert.ToInt32(hauptgruppeNummerObj);

//                    // Prüft, ob Nebengruppen mit derselben HauptgruppeNummer existieren.
//                    var cmdCheck = new SqlCommand("SELECT COUNT(*) FROM Nebengruppe WHERE GehoertZuHauptgruppeNummer = @Nummer AND MandantId = @MandantId", connection
//                    );
//                    cmdCheck.Parameters.AddWithValue("@Nummer", hauptgruppeNummer);
//                    cmdCheck.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                    int nebengruppeCount = (int)cmdCheck.ExecuteScalar();

//                    if (nebengruppeCount > 0)
//                    {
//                        // Wenn Nebengruppen vorhanden sind, darf die Hauptgruppe nicht gelöscht werden.
//                        return false;
//                    }

//                    // Löscht die Hauptgruppe, wenn keine Nebengruppen damit verknüpft sind.
//                    var cmdDelete = new SqlCommand("DELETE FROM Hauptgruppe WHERE Id = @Id AND MandantId = @MandantId", connection);
//                    cmdDelete.Parameters.AddWithValue("@Id", id);
//                    cmdDelete.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                    cmdDelete.ExecuteNonQuery();

//                    return true;
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim DeleteHauptgruppeRep.", ex);
//            }
//        }

//        public Hauptgruppe? GetHauptgruppeById(int id)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                using (var cmd = new SqlCommand("SELECT * FROM Hauptgruppe WHERE Id = @Id AND MandantId = @MandantId", conn))
//                {
//                    cmd.Parameters.AddWithValue("@Id", id);
//                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                    conn.Open();
//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        if (reader.Read())
//                        {
//                            return new Hauptgruppe
//                            {
//                                Id = (int)reader["Id"],
//                                GehoertZuKategorie = (int)reader["GehoertZuKategorieNummer"],
//                                Bezeichnung = reader["Bezeichnung"].ToString(),
//                                Kuerzel = reader["Kuerzel"].ToString(),
//                                Beschreibung = reader["Beschreibung"].ToString(),
//                                HauptgruppeNummer = (int)reader["HauptgruppeNummer"],
//                                LaufenderAktenzeichenZaehler = (int)reader["LaufenderAktenzeichenZaehler"],
//                                IstAktiv = (bool)reader["IstAktiv"],
//                                IstTestGruppe = (bool)reader["IstTestGruppe"],
//                                ObjektId = reader["ObjektId"] != DBNull.Value ? (Guid)reader["ObjektId"] : Guid.Empty
//                            };
//                        }
//                    }
//                }
//                return null;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetHauptgruppeById.", ex);
//            }
//        }

//        public bool UpdateHauptgruppe(Hauptgruppe model, bool applyToGroups)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    using (var transaction = conn.BeginTransaction())
//                    {
//                        try
//                        {
//                            // 1) Hauptgruppe aktualisieren.
//                            using (var cmd = new SqlCommand(
//                                @"UPDATE Hauptgruppe SET 
//                                GehoertZuKategorieNummer = @GehoertZuKategorieNummer,
//                                Bezeichnung = @Bezeichnung,
//                                Kuerzel = @Kuerzel,
//                                Beschreibung = @Beschreibung,
//                                HauptgruppeNummer = @HauptgruppeNummer,
//                                LaufenderAktenzeichenZaehler = @LaufenderAktenzeichenZaehler,
//                                IstAktiv = @IstAktiv,
//                                IstTestGruppe = @IstTestGruppe
//                                WHERE Id = @Id AND MandantId = @MandantId", conn, transaction))
//                            {
//                                cmd.Parameters.AddWithValue("@Id", model.Id);
//                                cmd.Parameters.AddWithValue("@GehoertZuKategorieNummer", model.GehoertZuKategorie);
//                                cmd.Parameters.AddWithValue("@Bezeichnung", model.Bezeichnung ?? "");
//                                cmd.Parameters.AddWithValue("@Kuerzel", model.Kuerzel ?? "");
//                                cmd.Parameters.AddWithValue("@Beschreibung", model.Beschreibung ?? "");
//                                cmd.Parameters.AddWithValue("@HauptgruppeNummer", model.HauptgruppeNummer);
//                                cmd.Parameters.AddWithValue("@LaufenderAktenzeichenZaehler", model.LaufenderAktenzeichenZaehler);
//                                cmd.Parameters.AddWithValue("@IstAktiv", model.IstAktiv);
//                                cmd.Parameters.AddWithValue("@IstTestGruppe", model.IstTestGruppe);
//                                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                                cmd.ExecuteNonQuery();
//                            }

//                            // 2) Wir aktualisieren alle Nebengruppen dieser Hauptgruppe.
//                            using (var cmd = new SqlCommand(
//                                @"UPDATE Nebengruppe SET 
//                                GehoertZuKategorieNummer = @GehoertZuKategorieNummer,
//                                GehoertZuHauptgruppeNummer = @GehoertZuHauptgruppeNummer
//                                WHERE GehoertZuHauptgruppeNummer = @Id AND MandantId = @MandantId", conn, transaction))
//                            {
//                                cmd.Parameters.AddWithValue("@GehoertZuKategorieNummer", model.GehoertZuKategorie);
//                                cmd.Parameters.AddWithValue("@GehoertZuHauptgruppeNummer", model.Id);
//                                cmd.Parameters.AddWithValue("@Id", model.Id);
//                                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                                cmd.ExecuteNonQuery();
//                            }

//                            // 3. Flags IstAktiv und IstTestKategorie auf Gruppen anwenden, falls gewünscht.
//                            if (applyToGroups)
//                            {
//                                using (var cmd = conn.CreateCommand())
//                                {
//                                    cmd.Transaction = transaction;

//                                    cmd.CommandText = @"
//                                                      UPDATE Nebengruppe
//                                                      SET IstAktiv=@IstAktiv, IstTestGruppe=@IstTestGruppe
//                                                      WHERE GehoertZuHauptgruppeNummer=@HauptgruppeNummer AND MandantId=@MandantId";
//                                    cmd.Parameters.AddWithValue("@IstAktiv", model.IstAktiv);
//                                    cmd.Parameters.AddWithValue("@IstTestGruppe", model.IstTestGruppe);
//                                    cmd.Parameters.AddWithValue("@HauptgruppeNummer", model.HauptgruppeNummer);
//                                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                                    cmd.ExecuteNonQuery();
//                                }
//                            }

//                            transaction.Commit();
//                            return true;
//                        }
//                        catch
//                        {
//                            transaction.Rollback();
//                            return false;
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim UpdateHauptgruppe.", ex);
//            }
//        }

//        public List<Hauptgruppe> GetHauptgruppenByKategorie(int kategorieNummer)
//        {
//            try
//            {
//                var hauptgruppen = new List<Hauptgruppe>();

//                using (var connection = new SqlConnection(_connectionString))
//                {

//                    connection.Open();

//                    using (var command = new SqlCommand(@"
//                                                SELECT Id, Bezeichnung, Kuerzel, Beschreibung, HauptgruppeNummer, 
//                                                GehoertZuKategorieNummer, IstAktiv, IstTestGruppe
//                                                FROM Hauptgruppe
//                                                WHERE GehoertZuKategorieNummer = @KategorieNummer
//                                                AND MandantId = @MandantId
//                                                ORDER BY HauptgruppeNummer ASC", connection))
//                    {

//                        command.Parameters.AddWithValue("@KategorieNummer", kategorieNummer);
//                        command.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                        using (var reader = command.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                hauptgruppen.Add(new Hauptgruppe
//                                {
//                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
//                                    Bezeichnung = reader["Bezeichnung"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Bezeichnung")) : "",
//                                    Kuerzel = reader["Kuerzel"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Kuerzel")) : "",
//                                    Beschreibung = reader["Beschreibung"] != DBNull.Value ? reader.GetString(reader.GetOrdinal("Beschreibung")) : "",
//                                    HauptgruppeNummer = reader["HauptgruppeNummer"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("HauptgruppeNummer")) : 0,
//                                    GehoertZuKategorie = reader["GehoertZuKategorieNummer"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("GehoertZuKategorieNummer")) : 0,
//                                    IstAktiv = reader["IstAktiv"] != DBNull.Value && (bool)reader["IstAktiv"],
//                                    IstTestGruppe = reader["IstTestGruppe"] != DBNull.Value && (bool)reader["IstTestGruppe"]
//                                });
//                            }
//                        }
//                    }
//                }

//                return hauptgruppen;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetHauptgruppenByKategorie.", ex);
//            }
//        }


//        // Nebengruppe.
//        public int CreateNebengruppeRep(Nebengruppe nebengruppe)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                {
//                    connection.Open();

//                    string query = @"
//                                   INSERT INTO Nebengruppe 
//                                   (GehoertZuKategorieNummer, GehoertZuHauptgruppeNummer, Bezeichnung, Kuerzel, Beschreibung, NebengruppeNummer, IstAktiv, IstTestGruppe, MandantId)
//                                   OUTPUT INSERTED.Id
//                                   VALUES 
//                                   (@GehoertZuKategorieNummer, @GehoertZuHauptgruppeNummer, @Bezeichnung, @Kuerzel, @Beschreibung, @NebengruppeNummer, @IstAktiv, @IstTestGruppe, @MandantId);";

//                    using (var command = new SqlCommand(query, connection))
//                    {
//                        command.Parameters.AddWithValue("@GehoertZuKategorieNummer", nebengruppe.GehoertZuKategorie);
//                        command.Parameters.AddWithValue("@GehoertZuHauptgruppeNummer", nebengruppe.GehoertZuHauptgruppe);
//                        command.Parameters.AddWithValue("@Bezeichnung", (object)nebengruppe.Bezeichnung ?? DBNull.Value);
//                        command.Parameters.AddWithValue("@Kuerzel", (object)nebengruppe.Kuerzel ?? DBNull.Value);
//                        command.Parameters.AddWithValue("@Beschreibung", (object)nebengruppe.Beschreibung ?? DBNull.Value);
//                        command.Parameters.AddWithValue("@NebengruppeNummer", nebengruppe.NebengruppeNummer);
//                        command.Parameters.AddWithValue("@IstAktiv", nebengruppe.IstAktiv);
//                        command.Parameters.AddWithValue("@IstTestGruppe", nebengruppe.IstTestGruppe);
//                        command.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                        int insertedId = Convert.ToInt32(command.ExecuteScalar());
//                        return insertedId;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim CreateNebengruppeRep.", ex);
//            }
//        }

//        public Nebengruppe? GetNebengruppeById(int id)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                using (var cmd = new SqlCommand("SELECT * FROM Nebengruppe WHERE Id = @Id AND MandantId = @MandantId", conn))
//                {
//                    cmd.Parameters.AddWithValue("@Id", id);
//                    cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                    conn.Open();
//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        if (reader.Read())
//                        {
//                            return new Nebengruppe
//                            {
//                                Id = (int)reader["Id"],
//                                Bezeichnung = reader["Bezeichnung"].ToString(),
//                                Kuerzel = reader["Kuerzel"].ToString(),
//                                Beschreibung = reader["Beschreibung"].ToString(),
//                                NebengruppeNummer = (int)reader["NebengruppeNummer"],
//                                IstAktiv = (bool)reader["IstAktiv"],
//                                IstTestGruppe = (bool)reader["IstTestGruppe"]
//                            };
//                        }
//                    }
//                }
//                return null;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetNebengruppeById.", ex);
//            }
//        }

//        public bool UpdateNebengruppe(Nebengruppe nebengruppe)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                {
//                    connection.Open();

//                    string query = @"
//                                   UPDATE Nebengruppe
//                                   SET 
//                                   GehoertZuKategorieNummer = @GehoertZuKategorieNummer,
//                                   GehoertZuHauptgruppeNummer = @GehoertZuHauptgruppeNummer,
//                                   Bezeichnung = @Bezeichnung,
//                                   Kuerzel = @Kuerzel,
//                                   Beschreibung = @Beschreibung,
//                                   NebengruppeNummer = @NebengruppeNummer,
//                                   IstAktiv = @IstAktiv,
//                                   IstTestGruppe = @IstTestGruppe
//                                   WHERE Id = @Id AND MandantId = @MandantId";

//                    using (var command = new SqlCommand(query, connection))
//                    {
//                        command.Parameters.AddWithValue("@GehoertZuKategorieNummer", (object)nebengruppe.GehoertZuKategorie ?? DBNull.Value);
//                        command.Parameters.AddWithValue("@GehoertZuHauptgruppeNummer", (object)nebengruppe.GehoertZuHauptgruppe ?? DBNull.Value);
//                        command.Parameters.AddWithValue("@Bezeichnung", (object)nebengruppe.Bezeichnung ?? DBNull.Value);
//                        command.Parameters.AddWithValue("@Kuerzel", (object)nebengruppe.Kuerzel ?? DBNull.Value);
//                        command.Parameters.AddWithValue("@Beschreibung", (object)nebengruppe.Beschreibung ?? DBNull.Value);
//                        command.Parameters.AddWithValue("@NebengruppeNummer", nebengruppe.NebengruppeNummer);
//                        command.Parameters.AddWithValue("@IstAktiv", nebengruppe.IstAktiv);
//                        command.Parameters.AddWithValue("@IstTestGruppe", nebengruppe.IstTestGruppe);
//                        command.Parameters.AddWithValue("@Id", nebengruppe.Id);
//                        command.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                        int rowsAffected = command.ExecuteNonQuery();
//                        return rowsAffected > 0;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim UpdateNebengruppe.", ex);
//            }
//        }

//        public bool DeleteNebengruppeRep(int id)
//        {
//            try
//            {
//                using (var connection = new SqlConnection(_connectionString))
//                {
//                    connection.Open();

//                    // Nebengruppe löschen.
//                    var cmdAbt = new SqlCommand("DELETE FROM Nebengruppe WHERE Id = @Id AND MandantId = @MandantId", connection);
//                    cmdAbt.Parameters.AddWithValue("@Id", id);
//                    cmdAbt.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

//                    int rowsAffected = cmdAbt.ExecuteNonQuery();
//                    return rowsAffected > 0;
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim DeleteNebengruppeRep.", ex);
//            }
//        }


//        // Abteilung / Referat.
//        public List<string> GetAbteilungenByObjektId(Guid objektId)
//        {
//            try
//            {
//                var result = new List<string>();
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    using (var cmd = new SqlCommand("SELECT Abteilung FROM AbteilungReferat WHERE ObjektId = @ObjektId AND MandantId = @MandantId", conn))
//                    {
//                        cmd.Parameters.AddWithValue("@ObjektId", objektId);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        using (var reader = cmd.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                if (reader["Abteilung"] != DBNull.Value)
//                                    result.Add(reader.GetString(0));
//                            }
//                        }
//                    }
//                }
//                return result;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetAbteilungenByObjektId.", ex);
//            }
//        }

//        public List<string> GetReferateByObjektId(Guid objektId)
//        {
//            try
//            {
//                var result = new List<string>();
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    using (var cmd = new SqlCommand("SELECT Referat FROM AbteilungReferat WHERE ObjektId = @ObjektId AND MandantId = @MandantId", conn))
//                    {
//                        cmd.Parameters.AddWithValue("@ObjektId", objektId);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        using (var reader = cmd.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                if (reader["Referat"] != DBNull.Value)
//                                    result.Add(reader.GetString(0));
//                            }
//                        }
//                    }
//                }
//                return result;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetReferateByObjektId.", ex);
//            }
//        }

//        public List<Abteilung> GetAllAbteilungen()
//        {
//            try
//            {
//                var result = new List<Abteilung>();
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    using (var cmd = new SqlCommand("SELECT Id, Name FROM Abteilung WHERE MandantId = @MandantId ORDER BY Name ASC", conn))
//                    {
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        using (var reader = cmd.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                result.Add(new Abteilung
//                                {
//                                    Id = reader.GetInt32(0),
//                                    Name = reader.GetString(1)
//                                });
//                            }
//                        }
//                    }
//                }
//                return result;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetAllAbteilungen.", ex);
//            }
//        }

//        public List<Referat> GetAllReferateByAbteilungId(int abteilungId)
//        {
//            try
//            {
//                var result = new List<Referat>();
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    using (var cmd = new SqlCommand("SELECT Id, Name FROM Referat WHERE AbteilungId = @AbteilungId AND MandantId = @MandantId ORDER BY Name ASC", conn))
//                    {
//                        cmd.Parameters.AddWithValue("@AbteilungId", abteilungId);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        using (var reader = cmd.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                result.Add(new Referat
//                                {
//                                    Id = reader.GetInt32(0),
//                                    Name = reader.GetString(1)
//                                });
//                            }
//                        }
//                    }
//                }
//                return result;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetAllReferate.", ex);
//            }
//        }

//        public void AddAbteilungenToObjekt(Guid objektId, List<string> abteilungen)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    foreach (var abt in abteilungen)
//                    {
//                        using (var cmd = new SqlCommand(@"
//                                                        INSERT INTO AbteilungReferat (ObjektId, Abteilung, MandantId)
//                                                        VALUES (@ObjektId, @Abteilung, @MandantId)", conn))
//                        {
//                            cmd.Parameters.AddWithValue("@ObjektId", objektId);
//                            cmd.Parameters.AddWithValue("@Abteilung", abt);
//                            cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                            cmd.ExecuteNonQuery();
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim AddAbteilungenToObjekt.", ex);
//            }
//        }

//        public void AddReferateToObjekt(Guid objektId, List<string> referate, string abteilungName)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    foreach (var refName in referate)
//                    {
//                        using (var cmd = new SqlCommand(@"
//                                                        INSERT INTO AbteilungReferat (ObjektId, Abteilung, Referat, MandantId)
//                                                        VALUES (@ObjektId, @Abteilung, @Referat, @MandantId)", conn))
//                        {
//                            cmd.Parameters.AddWithValue("@ObjektId", objektId);
//                            cmd.Parameters.AddWithValue("@Abteilung", abteilungName);
//                            cmd.Parameters.AddWithValue("@Referat", refName);
//                            cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                            cmd.ExecuteNonQuery();
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim AddReferateToObjekt.", ex);
//            }
//        }

//        public void DeleteAbteilungenByObjektId(Guid objektId)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    using (var cmd = new SqlCommand("DELETE FROM AbteilungReferat WHERE ObjektId = @ObjektId AND Abteilung IS NOT NULL AND MandantId = @MandantId", conn))
//                    {
//                        cmd.Parameters.AddWithValue("@ObjektId", objektId);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        cmd.ExecuteNonQuery();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim DeleteAbteilungenByObjektId.", ex);
//            }
//        }

//        public void DeleteAbteilungenAndReferateByObjektId(Guid objektId)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    using (var cmd = new SqlCommand("DELETE FROM AbteilungReferat WHERE ObjektId = @ObjektId AND MandantId = @MandantId", conn))
//                    {
//                        cmd.Parameters.AddWithValue("@ObjektId", objektId);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        cmd.ExecuteNonQuery();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim DeleteAbteilungenAndReferateByObjektId.", ex);
//            }
//        }

//        public List<Abteilung> GetAbteilungIdBYName(string name)
//        {
//            try
//            {
//                var result = new List<Abteilung>();
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    using (var cmd = new SqlCommand("SELECT Id, Name FROM Abteilung WHERE Name = @Name AND MandantId = @MandantId", conn))
//                    {
//                        cmd.Parameters.AddWithValue("@Name", name);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        using (var reader = cmd.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                result.Add(new Abteilung
//                                {
//                                    Id = reader.GetInt32(0),
//                                    Name = reader.GetString(1)
//                                });
//                            }
//                        }
//                    }
//                }
//                return result;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetAbteilungIdBYName.", ex);
//            }
//        }

//        // Alle Referate für eine ObjektId löschen (vor dem neu Speichern).
//        public void DeleteReferateByObjektId(Guid objektId)
//        {
//            try
//            {
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    using (var cmd = new SqlCommand(
//                        "DELETE FROM AbteilungReferat WHERE ObjektId = @ObjektId AND MandantId = @MandantId", conn))
//                    {
//                        cmd.Parameters.AddWithValue("@ObjektId", objektId);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        cmd.ExecuteNonQuery();
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim DeleteReferateByObjektId.", ex);
//            }
//        }

//        public List<string> GetReferateByObjektIdWithAbteilung(Guid objektId)
//        {
//            try
//            {
//                var result = new List<string>();
//                using (var conn = new SqlConnection(_connectionString))
//                {
//                    conn.Open();
//                    using (var cmd = new SqlCommand(
//                        "SELECT Abteilung, Referat FROM AbteilungReferat WHERE ObjektId = @ObjektId AND MandantId = @MandantId", conn))
//                    {
//                        cmd.Parameters.AddWithValue("@ObjektId", objektId);
//                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
//                        using (var reader = cmd.ExecuteReader())
//                        {
//                            while (reader.Read())
//                            {
//                                var abt = reader["Abteilung"] != DBNull.Value ? reader.GetString(0) : "";
//                                var ref_ = reader["Referat"] != DBNull.Value ? reader.GetString(1) : "";
//                                // Format: "Abteilung - Referat" für Anzeige
//                                result.Add($"{abt} - {ref_}");
//                            }
//                        }
//                    }
//                }
//                return result;
//            }
//            catch (Exception ex)
//            {
//                throw new RepositoryExceptions("Fehler beim GetReferateByObjektId.", ex);
//            }
//        }
//    }
//}
