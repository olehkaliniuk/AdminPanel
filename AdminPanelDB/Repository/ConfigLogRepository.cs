using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace AdminPanelDB.Repository
{
    public class ConfigLogRepository
    {
        private readonly string _connectionString;

        public ConfigLogRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Loggt die Löschung eines Config-Eintrags.
        public void LogDelete(int configEntryId, ConfigEntry oldEntry, string currentUser, string tabelleName)
        {
            try
            {
                // ConfigLog Model.
                var logEntry = new ConfigLog
                {
                    TabelleKey = configEntryId.ToString(),
                    TabelleName = tabelleName,
                    Aktion = "Löschen",
                    AlterWert = JsonSerializer.Serialize(oldEntry),
                    NeuerWert = null,
                    GeaendertVon = currentUser,
                    GeaendertAm = DateTime.UtcNow
                };

                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
                              INSERT INTO ConfigLog
                              (TabelleKey, TabelleName, Aktion, AlterWert, NeuerWert, GeaendertVon, GeaendertAm)
                              VALUES
                              (@TabelleKey,@TabelleName, @Aktion, @AlterWert, @NeuerWert, @GeaendertVon, @GeaendertAm)";

                cmd.Parameters.AddWithValue("@TabelleKey", logEntry.TabelleKey);
                cmd.Parameters.AddWithValue("@TabelleName", logEntry.TabelleName);
                cmd.Parameters.AddWithValue("@Aktion", logEntry.Aktion);
                cmd.Parameters.AddWithValue("@AlterWert", logEntry.AlterWert);
                cmd.Parameters.AddWithValue("@NeuerWert", (object)logEntry.NeuerWert ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GeaendertVon", logEntry.GeaendertVon);
                cmd.Parameters.AddWithValue("@GeaendertAm", logEntry.GeaendertAm);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim LogDelete Config-Logs.", ex);
            }
        }


        public void LogUpdate(ConfigEntry oldEntry, ConfigEntry newEntry, string currentUser, string tabelleName)
        {
            try
            {
                var oldJson = JsonSerializer.Serialize(oldEntry);
                var newJson = JsonSerializer.Serialize(newEntry);

                var logEntry = new ConfigLog
                {
                    TabelleKey = oldEntry.Id.ToString(),
                    TabelleName = tabelleName,
                    Aktion = "Aktualisieren",
                    AlterWert = oldJson,
                    NeuerWert = newJson,
                    GeaendertVon = currentUser,
                    GeaendertAm = DateTime.UtcNow
                };

                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
                              INSERT INTO ConfigLog
                              (TabelleKey, TabelleName, Aktion, AlterWert, NeuerWert, GeaendertVon, GeaendertAm)
                              VALUES
                              (@TabelleKey, @TabelleName, @Aktion, @AlterWert, @NeuerWert, @GeaendertVon, @GeaendertAm)";

                cmd.Parameters.AddWithValue("@TabelleKey", logEntry.TabelleKey);
                cmd.Parameters.AddWithValue("@TabelleName", logEntry.TabelleName);
                cmd.Parameters.AddWithValue("@Aktion", logEntry.Aktion);
                cmd.Parameters.AddWithValue("@AlterWert", logEntry.AlterWert);
                cmd.Parameters.AddWithValue("@NeuerWert", logEntry.NeuerWert ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GeaendertVon", logEntry.GeaendertVon);
                cmd.Parameters.AddWithValue("@GeaendertAm", logEntry.GeaendertAm);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim LogUpdate Config-Logs.", ex);
            }
        }


        public void LogCreate(ConfigEntry newEntry, string currentUser, string tabelleName)
        {
            try
            {
                var newJson = JsonSerializer.Serialize(newEntry);

                var logEntry = new ConfigLog
                {
                    TabelleKey = newEntry.Id.ToString(),
                    TabelleName = tabelleName,
                    Aktion = "Erstellen",
                    AlterWert = null,
                    NeuerWert = newJson,
                    GeaendertVon = currentUser,
                    GeaendertAm = DateTime.UtcNow
                };

                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
                              INSERT INTO ConfigLog
                              (TabelleKey, TabelleName, Aktion, AlterWert, NeuerWert, GeaendertVon, GeaendertAm)
                              VALUES
                              (@TabelleKey, @TabelleName, @Aktion, @AlterWert, @NeuerWert, @GeaendertVon, @GeaendertAm)";

                cmd.Parameters.AddWithValue("@TabelleKey", logEntry.TabelleKey);
                cmd.Parameters.AddWithValue("@TabelleName", logEntry.TabelleName);
                cmd.Parameters.AddWithValue("@Aktion", logEntry.Aktion);
                cmd.Parameters.AddWithValue("@AlterWert", (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NeuerWert", logEntry.NeuerWert ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GeaendertVon", logEntry.GeaendertVon);
                cmd.Parameters.AddWithValue("@GeaendertAm", logEntry.GeaendertAm);

                cmd.ExecuteNonQuery();
            }

            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim LogUpdate Config-Logs.", ex);
            }
        }






        // Alles anzeigen.
        public List<ConfigLog> GetPagedFiltered(
            string tabelleKey, string tabelleName, string aktion, string alterWert, string neuerWert, string geaendertVon,
            string geaendertAm,
            string sortColumn, string sortDirection,
            int page, int pageSize)
        {
            try
            {
                var result = new List<ConfigLog>();
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var cmd = connection.CreateCommand();

                int offset = (page - 1) * pageSize;

                var allowedSortColumns = new[] { "Id", "TabelleKey", "TabelleName", "Aktion", "AlterWert", "NeuerWert", "GeaendertVon", "GeaendertAm" };
                if (!allowedSortColumns.Contains(sortColumn)) sortColumn = "Id";
                if (sortDirection != "ASC" && sortDirection != "DESC") sortDirection = "ASC";

                cmd.CommandText = $@"
                                  SELECT *
                                  FROM ConfigLog
                                  WHERE
                                  (@TabelleKey IS NULL OR TabelleKey = @TabelleKey) AND
                                  (@TabelleName IS NULL OR TabelleName = @TabelleName) AND
                                  (@Aktion IS NULL OR Aktion LIKE '%' + @Aktion + '%') AND
                                  (@AlterWert IS NULL OR AlterWert LIKE '%' + @AlterWert + '%') AND
                                  (@NeuerWert IS NULL OR NeuerWert LIKE '%' + @NeuerWert + '%') AND
                                  (@GeaendertVon IS NULL OR GeaendertVon LIKE '%' + @GeaendertVon + '%') AND
                                  (@GeaendertAm IS NULL OR GeaendertAm LIKE '%' + @GeaendertAm + '%') 
                                  ORDER BY {sortColumn} {sortDirection}
                                  OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                cmd.Parameters.AddWithValue("@TabelleKey", string.IsNullOrWhiteSpace(tabelleKey) ? DBNull.Value : (object)tabelleKey);
                cmd.Parameters.AddWithValue("@TabelleName", string.IsNullOrWhiteSpace(tabelleName) ? DBNull.Value : (object)tabelleName);
                cmd.Parameters.AddWithValue("@Aktion", string.IsNullOrWhiteSpace(aktion) ? DBNull.Value : (object)aktion);
                cmd.Parameters.AddWithValue("@AlterWert", string.IsNullOrWhiteSpace(alterWert) ? DBNull.Value : (object)alterWert);
                cmd.Parameters.AddWithValue("@NeuerWert", string.IsNullOrWhiteSpace(neuerWert) ? DBNull.Value : (object)neuerWert);
                cmd.Parameters.AddWithValue("@GeaendertVon", string.IsNullOrWhiteSpace(geaendertVon) ? DBNull.Value : (object)geaendertVon);
                cmd.Parameters.AddWithValue("@GeaendertAm", string.IsNullOrWhiteSpace(geaendertAm) ? DBNull.Value : (object)geaendertAm);
                cmd.Parameters.AddWithValue("@Offset", offset);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var log = new ConfigLog
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        TabelleKey = reader["TabelleKey"] as string,
                        TabelleName = reader["TabelleName"] as string,
                        Aktion = reader["Aktion"] as string,
                        AlterWert = reader["AlterWert"] as string,
                        NeuerWert = reader["NeuerWert"] as string,
                        GeaendertVon = reader["GeaendertVon"] as string,
                        GeaendertAm = reader["GeaendertAm"] as DateTime? ?? DateTime.MinValue
                    };
                    result.Add(log);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Config-Logs.", ex);
            }
        }


        public int GetFilteredCount(string tabelleKey, string tabelleName, string aktion, string alterWert, string neuerWert, string geaendertVon, string geaendertAm)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
                                  SELECT COUNT(*) 
                                  FROM ConfigLog
                                  WHERE
                                  (@TabelleKey IS NULL OR TabelleKey = @TabelleKey) AND
                                  (@TabelleName IS NULL OR TabelleName = @TabelleName) AND
                                  (@Aktion IS NULL OR Aktion LIKE '%' + @Aktion + '%') AND
                                  (@AlterWert IS NULL OR AlterWert LIKE '%' + @AlterWert + '%') AND
                                  (@NeuerWert IS NULL OR NeuerWert LIKE '%' + @NeuerWert + '%') AND
                                  (@GeaendertVon IS NULL OR GeaendertVon LIKE '%' + @GeaendertVon + '%') AND
                                  (@GeaendertAm IS NULL OR GeaendertAm LIKE '%' + @GeaendertAm + '%')";


                cmd.Parameters.AddWithValue("@TabelleKey", string.IsNullOrWhiteSpace(tabelleKey) ? DBNull.Value : (object)tabelleKey);
                cmd.Parameters.AddWithValue("@TabelleName", string.IsNullOrWhiteSpace(tabelleName) ? DBNull.Value : (object)tabelleName);
                cmd.Parameters.AddWithValue("@Aktion", string.IsNullOrWhiteSpace(aktion) ? DBNull.Value : (object)aktion);
                cmd.Parameters.AddWithValue("@AlterWert", string.IsNullOrWhiteSpace(alterWert) ? DBNull.Value : (object)alterWert);
                cmd.Parameters.AddWithValue("@NeuerWert", string.IsNullOrWhiteSpace(neuerWert) ? DBNull.Value : (object)neuerWert);
                cmd.Parameters.AddWithValue("@GeaendertVon", string.IsNullOrWhiteSpace(geaendertVon) ? DBNull.Value : (object)geaendertVon);
                cmd.Parameters.AddWithValue("@GeaendertAm", string.IsNullOrWhiteSpace(geaendertAm) ? DBNull.Value : (object)geaendertAm);

                return (int)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Config-Logs.", ex);
            }
        }




    }
}

