using AdminPanelDB.Abstractions;
using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace AdminPanelDB.Repository
{
    public class ConfigRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigRepository> _logger;
        private readonly ICurrentMandantProvider _mandantProvider;
        private readonly string _systemName;
        private readonly string _projektName;
        private string _activeConnection = "DefaultConnection";

        private const int GlobalMandantId = 999;

        public ConfigRepository(IConfiguration configuration, ILogger<ConfigRepository> logger, ICurrentMandantProvider mandantProvider)
        {
            _configuration = configuration;
            _logger = logger;
            _mandantProvider = mandantProvider;
            _systemName = configuration["ConfigSettings:SystemName"];
            _projektName = configuration["ConfigSettings:ProjektName"];
        }

        public ConfigRepository WithConnection(string connectionName)
        {
            _activeConnection = connectionName;
            return this;
        }

        private string GetConnectionString(string connectionName)
        {
            try
            {
                var conn = _configuration.GetConnectionString(connectionName);
                return conn;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fehler beim Abrufen der ConnectionString '{connectionName}'.");
                throw new RepositoryExceptions($"Fehler beim Abrufen der ConnectionString '{connectionName}'.", ex);
            }
        }

        // --- Config laden (Config-Objekt). ---
        public Config LoadConfig(string connectionName = "DefaultConnection")
        {
            try
            {
                var config = new Config();

                using (var connection = new SqlConnection(GetConnectionString(connectionName)))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                                          SELECT [KeyBezeichnung], [StringValue], [IntValue], [BoolValue], [DateTimeValue]
                                          FROM [Config]
                                          WHERE [System] = @system
                                          AND [IstAktiv] = 1
                                          AND (MandantId = @MandantId OR MandantId = @GlobalMandantId)";

                        cmd.Parameters.Add("@system", SqlDbType.VarChar, 120).Value = _systemName.Trim();
                        cmd.Parameters.Add("@projektName", SqlDbType.VarChar, 120).Value = _projektName.Trim();
                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                        cmd.Parameters.AddWithValue("@GlobalMandantId", GlobalMandantId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string key = reader["KeyBezeichnung"].ToString();
                                var prop = typeof(Config).GetProperty(key);
                                if (prop != null)
                                {
                                    object value = null;
                                    switch (Type.GetTypeCode(prop.PropertyType))
                                    {
                                        case TypeCode.String:
                                            value = reader["StringValue"] != DBNull.Value ? reader["StringValue"].ToString() : null;
                                            break;
                                        case TypeCode.Int32:
                                            value = reader["IntValue"] != DBNull.Value ? (int?)Convert.ToInt32(reader["IntValue"]) : null;
                                            break;
                                        case TypeCode.Boolean:
                                            value = reader["BoolValue"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["BoolValue"]) : null;
                                            break;
                                        case TypeCode.DateTime:
                                            value = reader["DateTimeValue"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DateTimeValue"]) : null;
                                            break;
                                    }

                                    if (value != null)
                                        prop.SetValue(config, value);
                                }
                            }
                        }
                    }
                }
                return config;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden des Configs.", ex);
            }
        }

        public List<ConfigEntry> GetPagedFiltered(
          string system, string projektName, string keyBezeichnung,
          string stringValue, string intValue, string boolValue,
          string dateTimeValue, string beschreibung, string istAktiv,
          string anpassungGesperrt, string angepasstAm, string angepasstVon,
          string istSensibleAnpassung, string mandantId,
          string sortColumn, string sortDirection, int page, int pageSize, string connectionName = "DefaultConnection")
        {
            try
            {
                var result = new List<ConfigEntry>();
                using var connection = new SqlConnection(GetConnectionString(connectionName));
                connection.Open();

                using var cmd = connection.CreateCommand();

                var offset = (page - 1) * pageSize;

                var allowedSortColumns = new[] {
                    "System", "ProjektName", "KeyBezeichnung", "StringValue",
                    "IntValue", "BoolValue", "DateTimeValue", "Beschreibung",
                    "IstAktiv", "AnpassungGesperrt", "IstSensibleAnpassung",
                    "EintragAngepasstAm", "EintragAngepasstVon", "MandantId"};

                if (!allowedSortColumns.Contains(sortColumn))
                {
                    sortColumn = "Id";
                }
                if (sortDirection != "ASC" && sortDirection != "DESC")
                {
                    sortDirection = "ASC";
                }

                // "Global (für alle Mandanten)" ist nur Anzeige-Text für MandantId == 999.
                // Wenn der Suchbegriff Teil dieser Phrase ist, soll er MandantId = 999 treffen.
                const string globalLabel = "Global (für alle Mandanten)";
                bool isGlobalMatch = !string.IsNullOrWhiteSpace(mandantId) &&
                                      globalLabel.IndexOf(mandantId, StringComparison.OrdinalIgnoreCase) >= 0;

                cmd.CommandText = $@"
                                  SELECT *
                                  FROM Config
                                  WHERE
                                  (MandantId = @MandantId OR MandantId = @GlobalMandantId) AND
                                  (@System IS NULL OR System LIKE '%' + @System + '%') AND
                                  (@ProjektName IS NULL OR ProjektName LIKE '%' + @ProjektName + '%') AND
                                  (@KeyBezeichnung IS NULL OR KeyBezeichnung LIKE '%' + @KeyBezeichnung + '%') AND
                                  (@StringValue IS NULL OR StringValue LIKE '%' + @StringValue + '%') AND
                                  (@IntValue IS NULL OR CAST(IntValue AS NVARCHAR) LIKE '%' + @IntValue + '%') AND
                                  (@BoolValue IS NULL OR CAST(BoolValue AS NVARCHAR) LIKE '%' + @BoolValue + '%') AND
                                  (@DateTimeValue IS NULL OR CONVERT(NVARCHAR, DateTimeValue, 120) LIKE '%' + @DateTimeValue + '%') AND
                                  (@Beschreibung IS NULL OR Beschreibung LIKE '%' + @Beschreibung + '%') AND
                                  (@IstAktiv IS NULL OR CAST(IstAktiv AS NVARCHAR) LIKE '%' + @IstAktiv + '%') AND
                                  (@AnpassungGesperrt IS NULL OR CAST(AnpassungGesperrt AS NVARCHAR) LIKE '%' + @AnpassungGesperrt + '%') AND
                                  (@AngepasstAm IS NULL OR CONVERT(NVARCHAR, EintragAngepasstAm, 120) LIKE '%' + @AngepasstAm + '%') AND
                                  (@AngepasstVon IS NULL OR EintragAngepasstVon LIKE '%' + @AngepasstVon + '%') AND
                                  (@IstSensibleAnpassung IS NULL OR CAST(IstSensibleAnpassung AS NVARCHAR) LIKE '%' + @IstSensibleAnpassung + '%') AND
                                  (@MandantFilter IS NULL OR
                                      CAST(MandantId AS NVARCHAR) LIKE '%' + @MandantFilter + '%' OR
                                      (@IsGlobalMatch = 1 AND MandantId = 999))
                                  ORDER BY {(sortColumn == "MandantId" ? "MandantId" : sortColumn)} {sortDirection}
                                  OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                cmd.Parameters.AddWithValue("@GlobalMandantId", GlobalMandantId);
                cmd.Parameters.AddWithValue("@System", string.IsNullOrWhiteSpace(system) ? DBNull.Value : (object)system);
                cmd.Parameters.AddWithValue("@ProjektName", string.IsNullOrWhiteSpace(projektName) ? DBNull.Value : (object)projektName);
                cmd.Parameters.AddWithValue("@KeyBezeichnung", string.IsNullOrWhiteSpace(keyBezeichnung) ? DBNull.Value : (object)keyBezeichnung);
                cmd.Parameters.AddWithValue("@StringValue", string.IsNullOrWhiteSpace(stringValue) ? DBNull.Value : (object)stringValue);
                cmd.Parameters.AddWithValue("@IntValue", string.IsNullOrWhiteSpace(intValue) ? DBNull.Value : (object)intValue);
                cmd.Parameters.AddWithValue("@BoolValue", string.IsNullOrWhiteSpace(boolValue) ? DBNull.Value : (object)boolValue);
                cmd.Parameters.AddWithValue("@DateTimeValue", string.IsNullOrWhiteSpace(dateTimeValue) ? DBNull.Value : (object)dateTimeValue);
                cmd.Parameters.AddWithValue("@Beschreibung", string.IsNullOrWhiteSpace(beschreibung) ? DBNull.Value : (object)beschreibung);
                cmd.Parameters.AddWithValue("@IstAktiv", string.IsNullOrWhiteSpace(istAktiv) ? DBNull.Value : (object)istAktiv);
                cmd.Parameters.AddWithValue("@AnpassungGesperrt", string.IsNullOrWhiteSpace(anpassungGesperrt) ? DBNull.Value : (object)anpassungGesperrt);
                cmd.Parameters.AddWithValue("@AngepasstAm", string.IsNullOrWhiteSpace(angepasstAm) ? DBNull.Value : (object)angepasstAm);
                cmd.Parameters.AddWithValue("@AngepasstVon", string.IsNullOrWhiteSpace(angepasstVon) ? DBNull.Value : (object)angepasstVon);
                cmd.Parameters.AddWithValue("@IstSensibleAnpassung", string.IsNullOrWhiteSpace(istSensibleAnpassung) ? DBNull.Value : (object)istSensibleAnpassung);
                cmd.Parameters.AddWithValue("@MandantFilter", string.IsNullOrWhiteSpace(mandantId) ? DBNull.Value : (object)mandantId);
                cmd.Parameters.AddWithValue("@IsGlobalMatch", isGlobalMatch);
                cmd.Parameters.AddWithValue("@Offset", offset);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var entry = new ConfigEntry
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        System = reader["System"] as string,
                        ProjektName = reader["ProjektName"] as string,
                        KeyBezeichnung = reader["KeyBezeichnung"] as string,
                        StringValue = reader["StringValue"] as string,
                        IntValue = reader["IntValue"] as int?,
                        BoolValue = reader["BoolValue"] as bool?,
                        DateTimeValue = reader["DateTimeValue"] as DateTime?,
                        Beschreibung = reader["Beschreibung"] as string,
                        IstAktiv = reader["IstAktiv"] as bool? ?? false,
                        AnpassungGesperrt = reader["AnpassungGesperrt"] as bool? ?? false,
                        EintragAngepasstAm = reader["EintragAngepasstAm"] as DateTime?,
                        EintragAngepasstVon = reader["EintragAngepasstVon"] as string,
                        IstSensibleAnpassung = reader["IstSensibleAnpassung"] as bool?,
                        MandantId = reader.GetInt32(reader.GetOrdinal("MandantId"))
                    };
                    result.Add(entry);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden des Configs.", ex);
            }
        }

        public int GetFilteredCount(
            string system, string projektName, string keyBezeichnung,
            string stringValue, string intValue, string boolValue,
            string dateTimeValue, string beschreibung, string istAktiv, string istSensibleAnpassung,
            string anpassungGesperrt, string angepasstAm, string angepasstVon,
            string mandantId, string connectionName = "DefaultConnection")
        {
            try
            {
                using var connection = new SqlConnection(GetConnectionString(connectionName));
                connection.Open();

                using var cmd = connection.CreateCommand();

                const string globalLabel = "Global (für alle Mandanten)";
                bool isGlobalMatch = !string.IsNullOrWhiteSpace(mandantId) &&
                                      globalLabel.IndexOf(mandantId, StringComparison.OrdinalIgnoreCase) >= 0;

                cmd.CommandText = @"
                                  SELECT COUNT(*) 
                                  FROM Config
                                  WHERE
                                  (MandantId = @MandantId OR MandantId = @GlobalMandantId) AND
                                  (@System IS NULL OR System LIKE '%' + @System + '%') AND
                                  (@ProjektName IS NULL OR ProjektName LIKE '%' + @ProjektName + '%') AND
                                  (@KeyBezeichnung IS NULL OR KeyBezeichnung LIKE '%' + @KeyBezeichnung + '%') AND
                                  (@StringValue IS NULL OR StringValue LIKE '%' + @StringValue + '%') AND
                                  (@IntValue IS NULL OR CAST(IntValue AS NVARCHAR) LIKE '%' + @IntValue + '%') AND
                                  (@BoolValue IS NULL OR CAST(BoolValue AS NVARCHAR) LIKE '%' + @BoolValue + '%') AND
                                  (@DateTimeValue IS NULL OR CONVERT(NVARCHAR, DateTimeValue, 120) LIKE '%' + @DateTimeValue + '%') AND
                                  (@Beschreibung IS NULL OR Beschreibung LIKE '%' + @Beschreibung + '%') AND
                                  (@IstAktiv IS NULL OR CAST(IstAktiv AS NVARCHAR) LIKE '%' + @IstAktiv + '%') AND
                                  (@IstSensibleAnpassung IS NULL OR CAST(IstSensibleAnpassung AS NVARCHAR) LIKE '%' + @IstSensibleAnpassung + '%') AND
                                  (@AnpassungGesperrt IS NULL OR CAST(AnpassungGesperrt AS NVARCHAR) LIKE '%' + @AnpassungGesperrt + '%') AND
                                  (@AngepasstAm IS NULL OR CONVERT(NVARCHAR, EintragAngepasstAm, 120) LIKE '%' + @AngepasstAm + '%') AND
                                  (@AngepasstVon IS NULL OR EintragAngepasstVon LIKE '%' + @AngepasstVon + '%') AND
                                  (@MandantFilter IS NULL OR
                                      CAST(MandantId AS NVARCHAR) LIKE '%' + @MandantFilter + '%' OR
                                      (@IsGlobalMatch = 1 AND MandantId = 999));";

                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                cmd.Parameters.AddWithValue("@GlobalMandantId", GlobalMandantId);
                cmd.Parameters.AddWithValue("@System", string.IsNullOrWhiteSpace(system) ? DBNull.Value : (object)system);
                cmd.Parameters.AddWithValue("@ProjektName", string.IsNullOrWhiteSpace(projektName) ? DBNull.Value : (object)projektName);
                cmd.Parameters.AddWithValue("@KeyBezeichnung", string.IsNullOrWhiteSpace(keyBezeichnung) ? DBNull.Value : (object)keyBezeichnung);
                cmd.Parameters.AddWithValue("@StringValue", string.IsNullOrWhiteSpace(stringValue) ? DBNull.Value : (object)stringValue);
                cmd.Parameters.AddWithValue("@IntValue", string.IsNullOrWhiteSpace(intValue) ? DBNull.Value : (object)intValue);
                cmd.Parameters.AddWithValue("@BoolValue", string.IsNullOrWhiteSpace(boolValue) ? DBNull.Value : (object)boolValue);
                cmd.Parameters.AddWithValue("@DateTimeValue", string.IsNullOrWhiteSpace(dateTimeValue) ? DBNull.Value : (object)dateTimeValue);
                cmd.Parameters.AddWithValue("@Beschreibung", string.IsNullOrWhiteSpace(beschreibung) ? DBNull.Value : (object)beschreibung);
                cmd.Parameters.AddWithValue("@IstAktiv", string.IsNullOrWhiteSpace(istAktiv) ? DBNull.Value : (object)istAktiv);
                cmd.Parameters.AddWithValue("@AnpassungGesperrt", string.IsNullOrWhiteSpace(anpassungGesperrt) ? DBNull.Value : (object)anpassungGesperrt);
                cmd.Parameters.AddWithValue("@AngepasstAm", string.IsNullOrWhiteSpace(angepasstAm) ? DBNull.Value : (object)angepasstAm);
                cmd.Parameters.AddWithValue("@AngepasstVon", string.IsNullOrWhiteSpace(angepasstVon) ? DBNull.Value : (object)angepasstVon);
                cmd.Parameters.AddWithValue("@IstSensibleAnpassung", string.IsNullOrWhiteSpace(istSensibleAnpassung) ? DBNull.Value : (object)istSensibleAnpassung);
                cmd.Parameters.AddWithValue("@MandantFilter", string.IsNullOrWhiteSpace(mandantId) ? DBNull.Value : (object)mandantId);
                cmd.Parameters.AddWithValue("@IsGlobalMatch", isGlobalMatch);

                return (int)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden des Configs.", ex);
            }
        }

        // CRUD.
        // Create.
        public void Create(ConfigEntry entry, string userName, string connectionName = "DefaultConnection")
        {
            try
            {
                using var connection = new SqlConnection(GetConnectionString(connectionName));
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
                                  INSERT INTO Config
                                  (System, ProjektName, KeyBezeichnung, StringValue, IntValue, BoolValue, DateTimeValue, Beschreibung, IstAktiv, AnpassungGesperrt, IstSensibleAnpassung, EintragAngepasstAm, EintragAngepasstVon, MandantId)
                                  VALUES (@System, @ProjektName, @KeyBezeichnung, @StringValue, @IntValue, @BoolValue, @DateTimeValue, @Beschreibung, @IstAktiv, @AnpassungGesperrt, @IstSensibleAnpassung, GETDATE(), @EintragAngepasstVon, @MandantId) SELECT CAST(SCOPE_IDENTITY() AS INT);";


                var targetMandantId = entry.IstGlobal ? GlobalMandantId : _mandantProvider.MandantId;

                cmd.Parameters.AddWithValue("@System", (object)entry.System ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProjektName", (object)entry.ProjektName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KeyBezeichnung", (object)entry.KeyBezeichnung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StringValue", (object)entry.StringValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IntValue", (object)entry.IntValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BoolValue", (object)entry.BoolValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateTimeValue", (object)entry.DateTimeValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Beschreibung", (object)entry.Beschreibung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IstAktiv", (object)entry.IstAktiv ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AnpassungGesperrt", (object)entry.AnpassungGesperrt ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EintragAngepasstVon", userName ?? "Unknown");
                cmd.Parameters.AddWithValue("@IstSensibleAnpassung", (object)entry.IstSensibleAnpassung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MandantId", targetMandantId);

                entry.Id = (int)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Erstellen des Config-Eintrags", ex);
            }
        }

        // --- Nach Id lesen. ---
        public ConfigEntry GetById(int id, string connectionName = "DefaultConnection")
        {
            try
            {
                ConfigEntry entry = null;
                using var connection = new SqlConnection(GetConnectionString(connectionName));
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = "SELECT * FROM Config WHERE Id = @id AND (MandantId = @MandantId OR MandantId = @GlobalMandantId)";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);
                cmd.Parameters.AddWithValue("@GlobalMandantId", GlobalMandantId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    entry = new ConfigEntry
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        System = reader["System"].ToString(),
                        ProjektName = reader["ProjektName"].ToString(),
                        KeyBezeichnung = reader["KeyBezeichnung"].ToString(),
                        StringValue = reader["StringValue"] != DBNull.Value ? reader["StringValue"].ToString() : null,
                        IntValue = reader["IntValue"] != DBNull.Value ? (int?)reader["IntValue"] : null,
                        BoolValue = reader["BoolValue"] != DBNull.Value ? (bool?)reader["BoolValue"] : null,
                        DateTimeValue = reader["DateTimeValue"] != DBNull.Value ? (DateTime?)reader["DateTimeValue"] : null,
                        Beschreibung = reader["Beschreibung"] != DBNull.Value ? reader["Beschreibung"].ToString() : null,
                        IstAktiv = reader["IstAktiv"] != DBNull.Value ? Convert.ToBoolean(reader["IstAktiv"]) : false,
                        AnpassungGesperrt = reader["AnpassungGesperrt"] != DBNull.Value ? Convert.ToBoolean(reader["AnpassungGesperrt"]) : false,

                        EintragAngepasstAm = reader["EintragAngepasstAm"] != DBNull.Value ? (DateTime?)reader["EintragAngepasstAm"] : null,
                        EintragAngepasstVon = reader["EintragAngepasstVon"] != DBNull.Value ? reader["EintragAngepasstVon"].ToString() : null,

                        IstSensibleAnpassung = reader["IstSensibleAnpassung"] != DBNull.Value ? Convert.ToBoolean(reader["IstSensibleAnpassung"]) : false,


                        IstGlobal = Convert.ToInt32(reader["MandantId"]) == GlobalMandantId,
                    };
                }

                return entry;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden des Config-Eintrags", ex);
            }
        }

        //  --- Update. ---
        // Hinweis: bewusst NUR eigene MandantId, nicht 999 - sonst könnte jeder Mandant
        // globale Einträge verändern und damit alle anderen Mandanten beeinflussen.
        public void Update(ConfigEntry entry, string userName, string connectionName = "DefaultConnection", bool wasGlobal = false)
        {
            try
            {
                using var connection = new SqlConnection(GetConnectionString(connectionName));
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
                                  UPDATE Config
                                  SET System=@System, ProjektName=@ProjektName, KeyBezeichnung=@KeyBezeichnung,
                                  StringValue=@StringValue, IntValue=@IntValue, BoolValue=@BoolValue, DateTimeValue=@DateTimeValue,
                                  Beschreibung=@Beschreibung, IstAktiv=@IstAktiv, AnpassungGesperrt=@AnpassungGesperrt,
                                  EintragAngepasstAm=GETDATE(),
                                  IstSensibleAnpassung=@IstSensibleAnpassung,
                                  EintragAngepasstVon=@EintragAngepasstVon,
                                  MandantId=@TargetMandantId
                                  WHERE Id=@Id AND MandantId=@CurrentMandantId";

                cmd.Parameters.AddWithValue("@Id", entry.Id);
                cmd.Parameters.AddWithValue("@CurrentMandantId", wasGlobal ? GlobalMandantId : _mandantProvider.MandantId);
                cmd.Parameters.AddWithValue("@TargetMandantId", entry.IstGlobal ? GlobalMandantId : _mandantProvider.MandantId);
                cmd.Parameters.AddWithValue("@System", (object)entry.System ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProjektName", (object)entry.ProjektName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KeyBezeichnung", (object)entry.KeyBezeichnung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StringValue", (object)entry.StringValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IntValue", (object)entry.IntValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BoolValue", (object)entry.BoolValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateTimeValue", (object)entry.DateTimeValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Beschreibung", (object)entry.Beschreibung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IstAktiv", (object)entry.IstAktiv ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AnpassungGesperrt", (object)entry.AnpassungGesperrt ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EintragAngepasstVon", userName ?? "Unknown");
                cmd.Parameters.AddWithValue("@IstSensibleAnpassung", (object)entry.IstSensibleAnpassung ?? DBNull.Value);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler bei der Aktualisierung des Config-Eintrags", ex);
            }
        }

        // --- Delete. ---
        // Eigene MandantId und globale Einträge (999) dürfen gelöscht werden, aber nie gesperrte (AnpassungGesperrt).
        public void Delete(int id, bool isGlobal = false, string connectionName = "DefaultConnection")
        {
            try
            {
                using var connection = new SqlConnection(GetConnectionString(connectionName));
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
                                  DELETE FROM Config
                                  WHERE Id=@id
                                  AND MandantId=@MandantId
                                  AND AnpassungGesperrt = 0";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@MandantId", isGlobal ? GlobalMandantId : _mandantProvider.MandantId);

                // Nichts gelöscht - Fehler statt falscher Erfolgsmeldung.
                if (cmd.ExecuteNonQuery() == 0)
                {
                    throw new RepositoryExceptions("Eintrag wurde nicht gelöscht (nicht gefunden oder gesperrt).", null);
                }
            }
            catch (RepositoryExceptions)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Löschen des Config-Eintrags", ex);
            }
        }

        // --- Sperre setzen. ---
        public void SetLock(int id, bool locked, string userName, bool isGlobal, string connectionName = "DefaultConnection")
        {
            try
            {
                using var connection = new SqlConnection(GetConnectionString(connectionName));
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                                  UPDATE Config
                                  SET AnpassungGesperrt=@Locked, EintragAngepasstAm=GETDATE(), EintragAngepasstVon=@User
                                  WHERE Id=@Id AND MandantId=@MandantId";


                cmd.Parameters.AddWithValue("@Locked", locked);
                cmd.Parameters.AddWithValue("@User", userName ?? "Unknown");
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@MandantId", isGlobal ? GlobalMandantId : _mandantProvider.MandantId);

                if (cmd.ExecuteNonQuery() == 0)
                    throw new RepositoryExceptions("Eintrag nicht gefunden.", null);
            }
            catch (RepositoryExceptions) { throw; }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Ändern der Sperre", ex);
            }
        }
    }
}