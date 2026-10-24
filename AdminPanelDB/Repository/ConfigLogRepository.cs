using AdminPanelDB.Abstractions;
using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace AdminPanelDB.Repository
{
    public class ConfigLogRepository
    {
        private readonly string _connectionString;
        private readonly ICurrentMandantProvider _mandantProvider;
        private const int GlobalMandantId = 999;

        public ConfigLogRepository(IConfiguration configuration, ICurrentMandantProvider mandantProvider)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _mandantProvider = mandantProvider;
        }

        // Loggt die Löschung eines Config-Eintrags.
        public void LogDelete(int configEntryId, ConfigEntry oldEntry, string currentUser, string tabelleName)
        {
            try
            {
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
                                  (TabelleKey, TabelleName, Aktion, AlterWert, NeuerWert, GeaendertVon, GeaendertAm, MandantId)
                                  VALUES
                                  (@TabelleKey,@TabelleName, @Aktion, @AlterWert, @NeuerWert, @GeaendertVon, @GeaendertAm, @MandantId)";

                cmd.Parameters.AddWithValue("@TabelleKey", logEntry.TabelleKey);
                cmd.Parameters.AddWithValue("@TabelleName", logEntry.TabelleName);
                cmd.Parameters.AddWithValue("@Aktion", logEntry.Aktion);
                cmd.Parameters.AddWithValue("@AlterWert", logEntry.AlterWert);
                cmd.Parameters.AddWithValue("@NeuerWert", (object)logEntry.NeuerWert ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GeaendertVon", logEntry.GeaendertVon);
                cmd.Parameters.AddWithValue("@GeaendertAm", logEntry.GeaendertAm);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

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
                                  (TabelleKey, TabelleName, Aktion, AlterWert, NeuerWert, GeaendertVon, GeaendertAm, MandantId)
                                  VALUES
                                  (@TabelleKey, @TabelleName, @Aktion, @AlterWert, @NeuerWert, @GeaendertVon, @GeaendertAm, @MandantId)";

                cmd.Parameters.AddWithValue("@TabelleKey", logEntry.TabelleKey);
                cmd.Parameters.AddWithValue("@TabelleName", logEntry.TabelleName);
                cmd.Parameters.AddWithValue("@Aktion", logEntry.Aktion);
                cmd.Parameters.AddWithValue("@AlterWert", logEntry.AlterWert);
                cmd.Parameters.AddWithValue("@NeuerWert", logEntry.NeuerWert ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GeaendertVon", logEntry.GeaendertVon);
                cmd.Parameters.AddWithValue("@GeaendertAm", logEntry.GeaendertAm);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

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
                                  (TabelleKey, TabelleName, Aktion, AlterWert, NeuerWert, GeaendertVon, GeaendertAm, MandantId)
                                  VALUES
                                  (@TabelleKey, @TabelleName, @Aktion, @AlterWert, @NeuerWert, @GeaendertVon, @GeaendertAm, @MandantId)";

                cmd.Parameters.AddWithValue("@TabelleKey", logEntry.TabelleKey);
                cmd.Parameters.AddWithValue("@TabelleName", logEntry.TabelleName);
                cmd.Parameters.AddWithValue("@Aktion", logEntry.Aktion);
                cmd.Parameters.AddWithValue("@AlterWert", (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NeuerWert", logEntry.NeuerWert ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@GeaendertVon", logEntry.GeaendertVon);
                cmd.Parameters.AddWithValue("@GeaendertAm", logEntry.GeaendertAm);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim LogUpdate Config-Logs.", ex);
            }
        }

        public List<ConfigLog> GetPagedFiltered(
            string tabelleKey, string tabelleName, string aktion, string alterWert, string neuerWert, string geaendertVon,
            string geaendertAm, string geaendertAmMode,
            string mandantId,                     
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

                var allowedSortColumns = new[] { "Id", "TabelleKey", "TabelleName", "Aktion", "AlterWert", "NeuerWert", "GeaendertVon", "GeaendertAm", "MandantId" };
                if (!allowedSortColumns.Contains(sortColumn)) sortColumn = "Id";
                if (sortDirection != "ASC" && sortDirection != "DESC") sortDirection = "ASC";

                string dateFilter = "@GeaendertAm IS NULL";
                if (!string.IsNullOrWhiteSpace(geaendertAm) && DateTime.TryParse(geaendertAm, out _))
                {
                    dateFilter = geaendertAmMode == "before"
                        ? "GeaendertAm <= @GeaendertAm"
                        : "GeaendertAm >= @GeaendertAm";
                }

                string mandantIdFilterValue = null;
                if (!string.IsNullOrWhiteSpace(mandantId))
                {
                    const string globalLabel = "Global (für alle Mandanten)";
                    var input = mandantId.Trim();

                    if (globalLabel.Contains(input, StringComparison.OrdinalIgnoreCase))
                    {
                        mandantIdFilterValue = "999";
                    }
                    else
                    {
                        mandantIdFilterValue = input;
                    }
                }

                cmd.CommandText = $@"
                                  SELECT *
                                  FROM ConfigLog
                                  WHERE
                                  (MandantId = @MandantId OR MandantId = @GlobalMandantId) AND
                                  (@TabelleKey IS NULL OR TabelleKey = @TabelleKey) AND
                                  (@TabelleName IS NULL OR TabelleName = @TabelleName) AND
                                  (@Aktion IS NULL OR Aktion LIKE '%' + @Aktion + '%') AND
                                  (@AlterWert IS NULL OR AlterWert LIKE '%' + @AlterWert + '%') AND
                                  (@NeuerWert IS NULL OR NeuerWert LIKE '%' + @NeuerWert + '%') AND
                                  (@GeaendertVon IS NULL OR GeaendertVon LIKE '%' + @GeaendertVon + '%') AND
                                  (@MandantIdFilter IS NULL OR CAST(MandantId AS NVARCHAR(50)) LIKE '%' + @MandantIdFilter + '%') AND
                                  {dateFilter}
                                  ORDER BY {sortColumn} {sortDirection}
                                  OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                cmd.Parameters.AddWithValue("@GlobalMandantId", GlobalMandantId);
                cmd.Parameters.AddWithValue("@TabelleKey", string.IsNullOrWhiteSpace(tabelleKey) ? DBNull.Value : (object)tabelleKey);
                cmd.Parameters.AddWithValue("@TabelleName", string.IsNullOrWhiteSpace(tabelleName) ? DBNull.Value : (object)tabelleName);
                cmd.Parameters.AddWithValue("@Aktion", string.IsNullOrWhiteSpace(aktion) ? DBNull.Value : (object)aktion);
                cmd.Parameters.AddWithValue("@AlterWert", string.IsNullOrWhiteSpace(alterWert) ? DBNull.Value : (object)alterWert);
                cmd.Parameters.AddWithValue("@NeuerWert", string.IsNullOrWhiteSpace(neuerWert) ? DBNull.Value : (object)neuerWert);
                cmd.Parameters.AddWithValue("@GeaendertVon", string.IsNullOrWhiteSpace(geaendertVon) ? DBNull.Value : (object)geaendertVon);
                cmd.Parameters.AddWithValue("@GeaendertAm", DateTime.TryParse(geaendertAm, out var dt) ? (object)dt : DBNull.Value);
                cmd.Parameters.AddWithValue("@MandantIdFilter", mandantIdFilterValue == null ? DBNull.Value : (object)mandantIdFilterValue);
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
                        GeaendertAm = reader["GeaendertAm"] as DateTime? ?? DateTime.MinValue,
                        MandantId = reader.GetInt32(reader.GetOrdinal("MandantId"))   
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

        public int GetFilteredCount(
            string tabelleKey, string tabelleName, string aktion, string alterWert, string neuerWert, string geaendertVon,
            string geaendertAm, string mandantId, string geaendertAmMode = "after")
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var cmd = connection.CreateCommand();

                string dateFilter = "@GeaendertAm IS NULL";
                if (!string.IsNullOrWhiteSpace(geaendertAm) && DateTime.TryParse(geaendertAm, out _))
                {
                    dateFilter = geaendertAmMode == "before"
                        ? "GeaendertAm <= @GeaendertAm"
                        : "GeaendertAm >= @GeaendertAm";
                }

                string mandantIdFilterValue = null;
                if (!string.IsNullOrWhiteSpace(mandantId))
                {
                    const string globalLabel = "Global (für alle Mandanten)";
                    var input = mandantId.Trim();

                   
                    if (globalLabel.Contains(input, StringComparison.OrdinalIgnoreCase))
                    {
                        mandantIdFilterValue = "999";
                    }
                    else
                    {
                        mandantIdFilterValue = input;
                    }
                }

                cmd.CommandText = $@"
                                  SELECT COUNT(*) 
                                  FROM ConfigLog
                                  WHERE
                                  (MandantId = @MandantId OR MandantId = @GlobalMandantId) AND
                                  (@TabelleKey IS NULL OR TabelleKey = @TabelleKey) AND
                                  (@TabelleName IS NULL OR TabelleName = @TabelleName) AND
                                  (@Aktion IS NULL OR Aktion LIKE '%' + @Aktion + '%') AND
                                  (@AlterWert IS NULL OR AlterWert LIKE '%' + @AlterWert + '%') AND
                                  (@NeuerWert IS NULL OR NeuerWert LIKE '%' + @NeuerWert + '%') AND
                                  (@GeaendertVon IS NULL OR GeaendertVon LIKE '%' + @GeaendertVon + '%') AND
                                  (@MandantIdFilter IS NULL OR CAST(MandantId AS NVARCHAR(50)) LIKE '%' + @MandantIdFilter + '%') AND
                                  {dateFilter}";

                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                cmd.Parameters.AddWithValue("@GlobalMandantId", GlobalMandantId);
                cmd.Parameters.AddWithValue("@TabelleKey", string.IsNullOrWhiteSpace(tabelleKey) ? DBNull.Value : (object)tabelleKey);
                cmd.Parameters.AddWithValue("@TabelleName", string.IsNullOrWhiteSpace(tabelleName) ? DBNull.Value : (object)tabelleName);
                cmd.Parameters.AddWithValue("@Aktion", string.IsNullOrWhiteSpace(aktion) ? DBNull.Value : (object)aktion);
                cmd.Parameters.AddWithValue("@AlterWert", string.IsNullOrWhiteSpace(alterWert) ? DBNull.Value : (object)alterWert);
                cmd.Parameters.AddWithValue("@NeuerWert", string.IsNullOrWhiteSpace(neuerWert) ? DBNull.Value : (object)neuerWert);
                cmd.Parameters.AddWithValue("@GeaendertVon", string.IsNullOrWhiteSpace(geaendertVon) ? DBNull.Value : (object)geaendertVon);
                cmd.Parameters.AddWithValue("@GeaendertAm", DateTime.TryParse(geaendertAm, out var dt) ? (object)dt : DBNull.Value);
                cmd.Parameters.AddWithValue("@MandantIdFilter", mandantIdFilterValue == null ? DBNull.Value : (object)mandantIdFilterValue);

                return (int)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Config-Logs.", ex);
            }
        }

    }
}