using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace AdminPanelDB.Repository
{
    public class ConfigRepository
    {
        private readonly string _connectionString;
        private readonly string _systemName;
        private readonly string _projektName;

        private readonly ILogger<ConfigRepository> _logger;

        public ConfigRepository(IConfiguration configuration, ILogger<ConfigRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _systemName = configuration["ConfigSettings:SystemName"];
            _projektName = configuration["ConfigSettings:ProjektName"];

            _logger = logger;
        }



        // --- Config laden (Config-Objekt). ---
        public Config LoadConfig()
        {
            try
            {
                var config = new Config();


                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                                          SELECT [KeyBezeichnung], [StringValue], [IntValue], [BoolValue], [DateTimeValue]
                                          FROM [Config]
                                          WHERE [System] = @system
                                          AND ([ProjektName] = 'Allgemein' OR [ProjektName] = @projektName)
                                          AND [IstAktiv] = 1
                                          ORDER BY CASE WHEN [ProjektName] = 'Allgemein' THEN 1 ELSE 2 END";

                        cmd.Parameters.Add("@system", SqlDbType.VarChar, 120).Value = _systemName.Trim();
                        cmd.Parameters.Add("@projektName", SqlDbType.VarChar, 120).Value = _projektName.Trim();

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



        // CRUD.
        // Create.
        public void Create(ConfigEntry entry, string userName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
                                  INSERT INTO Config
                                  (System, ProjektName, KeyBezeichnung, StringValue, IntValue, BoolValue, DateTimeValue, Beschreibung, IstAktiv, AnpassungGesperrt, EintragAngepasstAm, EintragAngepasstVon)
                                  VALUES (@System, @ProjektName, @KeyBezeichnung, @StringValue, @IntValue, @BoolValue, @DateTimeValue, @Beschreibung, @IstAktiv, @AnpassungGesperrt, GETDATE(), @EintragAngepasstVon) SELECT CAST(SCOPE_IDENTITY() AS INT);";

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

                entry.Id = (int)cmd.ExecuteScalar();
            }
            catch (Exception ex) {
                throw new RepositoryExceptions("Fehler beim Erstellen des Config-Eintrags", ex);
            }
        }



        public List<ConfigEntry> GetPagedFiltered(
              string system, string projektName, string keyBezeichnung,
              string stringValue, string intValue, string boolValue,
              string dateTimeValue, string beschreibung, string istAktiv,
              string anpassungGesperrt, string angepasstAm, string angepasstVon,
              string sortColumn, string sortDirection, int page, int pageSize)
        {
            try
            {
                var result = new List<ConfigEntry>();
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var cmd = connection.CreateCommand();

                var offset = (page - 1) * pageSize;

                var allowedSortColumns = new[] {
                "System", "ProjektName", "KeyBezeichnung", "StringValue",
                "IntValue", "BoolValue", "DateTimeValue", "Beschreibung",
                "IstAktiv", "AnpassungGesperrt", "EintragAngepasstAm", "EintragAngepasstVon"};

                if (!allowedSortColumns.Contains(sortColumn))
                {
                    sortColumn = "Id";
                }
                if (sortDirection != "ASC" && sortDirection != "DESC")
                {
                    sortDirection = "ASC";
                }

                cmd.CommandText = $@"
                                  SELECT *
                                  FROM Config
                                  WHERE
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
                                  (@AngepasstVon IS NULL OR EintragAngepasstVon LIKE '%' + @AngepasstVon + '%')
                                  ORDER BY {sortColumn} {sortDirection}
                                  OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

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
                        EintragAngepasstVon = reader["EintragAngepasstVon"] as string
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
            string dateTimeValue, string beschreibung, string istAktiv,
            string anpassungGesperrt, string angepasstAm, string angepasstVon)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                                  SELECT COUNT(*) 
                                  FROM Config
                                  WHERE
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
                                  (@AngepasstVon IS NULL OR EintragAngepasstVon LIKE '%' + @AngepasstVon + '%');";

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

                return (int)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden des Configs.", ex);
            }
        }


        // --- Nach Id lesen. ---
        public ConfigEntry GetById(int id)
        {
            try
            {
                ConfigEntry entry = null;
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = "SELECT * FROM Config WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);

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
                        EintragAngepasstVon = reader["EintragAngepasstVon"] != DBNull.Value ? reader["EintragAngepasstVon"].ToString() : null
                    };
                }

                return entry;
            }            
            catch(Exception ex) {
                throw new RepositoryExceptions("Fehler beim Laden des Config-Eintrags", ex);
            }
        }




        //  --- Update. ---
        public void Update(ConfigEntry entry, string userName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = @"
                                  UPDATE Config
                                  SET System=@System, ProjektName=@ProjektName, KeyBezeichnung=@KeyBezeichnung,
                                  StringValue=@StringValue, IntValue=@IntValue, BoolValue=@BoolValue, DateTimeValue=@DateTimeValue,
                                  Beschreibung=@Beschreibung, IstAktiv=@IstAktiv, AnpassungGesperrt=@AnpassungGesperrt,
                                  EintragAngepasstAm=GETDATE(),
                                  EintragAngepasstVon=@EintragAngepasstVon
                                  WHERE Id=@Id";

                cmd.Parameters.AddWithValue("@Id", entry.Id);
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


                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler bei der Aktualisierung des Config-Eintrags", ex);
            }
        }

        // --- Delete. ---
        public void Delete(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();

                cmd.CommandText = "DELETE FROM Config WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Löschen des Config-Eintrags", ex);
            }
        }
    }
}
