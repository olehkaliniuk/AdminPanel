using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using Microsoft.Data.SqlClient;
using System.Net;

namespace AdminPanelDB.Repository
{
    public class AdresseRepository
    {
        private readonly string _connectionString;

        public AdresseRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        public List<Adresse> GetPagedFiltered(
            string land, string strasse, string ort, string stateOrPrefecture, string plz,
            string gebaude, string wohnung, string organisation, string name,
            string bezeichnung, string iban, string bic, string istInsolvent, string istAktiv,
            string ansprechpartner, string ansprechpartnerTel, string ansprechpartnerEmail,
            string rechnungsLand, string rechnungsStrasse, string rechnungsOrt,
            string rechnungsStateOrPrefecture, string rechnungsPlz,
            string rechnungsOrganisation, string rechnungsName,
            string gesamtadresse,
            string sortColumn, string sortDirection, int page, int pageSize)
        {
            try
            {
                var result = new List<Adresse>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var cmd = connection.CreateCommand())
                    {
                        var offset = (page - 1) * pageSize;

                        // Validierung der Sortierung, um SQL-Injection zu verhindern.
                        var allowedSortColumns = new[]{
                        "Land","Strasse","Ort","StateOrPrefecture","PLZ","Gebaude","Wohnung",
                        "Organisation","Name","Bezeichnung","Iban","Bic","IstInsolvent","IstAktiv",
                        "Ansprechpartner","AnsprechpartnerTel","AnsprechpartnerEmail",
                        "RechnungsLand","RechnungsStrasse","RechnungsOrt",
                        "RechnungsStateOrPrefecture","RechnungsPLZ",
                        "RechnungsOrganisation","RechnungsName","Gesamtadresse"};

                        if (!allowedSortColumns.Contains(sortColumn))
                        {
                            sortColumn = "Id";
                        }

                        if (sortDirection != "ASC" && sortDirection != "DESC")
                        {
                            sortDirection = "ASC";
                        }


                        cmd.CommandText = @"
                                          SELECT *
                                          FROM Adresse
                                          WHERE
                                          (@Land IS NULL OR Land LIKE '%' + @Land + '%') AND
                                          (@Strasse IS NULL OR Strasse LIKE '%' + @Strasse + '%') AND
                                          (@Ort IS NULL OR Ort LIKE '%' + @Ort + '%') AND
                                          (@StateOrPrefecture IS NULL OR StateOrPrefecture LIKE '%' + @StateOrPrefecture + '%') AND
                                          (@PLZ IS NULL OR PLZ LIKE '%' + @PLZ + '%') AND
                                          (@Gebaude IS NULL OR Gebaude LIKE '%' + @Gebaude + '%') AND
                                          (@Wohnung IS NULL OR Wohnung LIKE '%' + @Wohnung + '%') AND
                                          (@Organisation IS NULL OR Organisation LIKE '%' + @Organisation + '%') AND
                                          (@Name IS NULL OR Name LIKE '%' + @Name + '%') AND
                                          (@Bezeichnung IS NULL OR Bezeichnung LIKE '%' + @Bezeichnung + '%') AND
                                          (@Iban IS NULL OR Iban LIKE '%' + @Iban + '%') AND
                                          (@Bic IS NULL OR Bic LIKE '%' + @Bic + '%') AND
                                          (@IstInsolvent IS NULL OR CAST(IstInsolvent AS NVARCHAR) LIKE '%' + @IstInsolvent + '%') AND
                                          (@IstAktiv IS NULL OR CAST(IstAktiv AS NVARCHAR) LIKE '%' + @IstAktiv + '%') AND
                                          (@Ansprechpartner IS NULL OR Ansprechpartner LIKE '%' + @Ansprechpartner + '%') AND
                                          (@AnsprechpartnerTel IS NULL OR AnsprechpartnerTel LIKE '%' + @AnsprechpartnerTel + '%') AND
                                          (@AnsprechpartnerEmail IS NULL OR AnsprechpartnerEmail LIKE '%' + @AnsprechpartnerEmail + '%') AND
                                          (@RechnungsLand IS NULL OR RechnungsLand LIKE '%' + @RechnungsLand + '%') AND
                                          (@RechnungsStrasse IS NULL OR RechnungsStrasse LIKE '%' + @RechnungsStrasse + '%') AND
                                          (@RechnungsOrt IS NULL OR RechnungsOrt LIKE '%' + @RechnungsOrt + '%') AND
                                          (@RechnungsStateOrPrefecture IS NULL OR RechnungsStateOrPrefecture LIKE '%' + @RechnungsStateOrPrefecture + '%') AND
                                          (@RechnungsPLZ IS NULL OR RechnungsPLZ LIKE '%' + @RechnungsPLZ + '%') AND
                                          (@RechnungsOrganisation IS NULL OR RechnungsOrganisation LIKE '%' + @RechnungsOrganisation + '%') AND
                                          (@RechnungsName IS NULL OR RechnungsName LIKE '%' + @RechnungsName + '%') AND
                                          (@Gesamtadresse IS NULL OR Gesamtadresse LIKE '%' + @Gesamtadresse + '%')
                                          ORDER BY " + sortColumn + " " + sortDirection + @"
                                          OFFSET @Offset ROWS
                                          FETCH NEXT @PageSize ROWS ONLY;";

                        cmd.Parameters.AddWithValue("@Land", string.IsNullOrWhiteSpace(land) ? DBNull.Value : (object)land);
                        cmd.Parameters.AddWithValue("@Strasse", string.IsNullOrWhiteSpace(strasse) ? DBNull.Value : (object)strasse);
                        cmd.Parameters.AddWithValue("@Ort", string.IsNullOrWhiteSpace(ort) ? DBNull.Value : (object)ort);
                        cmd.Parameters.AddWithValue("@StateOrPrefecture", string.IsNullOrWhiteSpace(stateOrPrefecture) ? DBNull.Value : (object)stateOrPrefecture);
                        cmd.Parameters.AddWithValue("@PLZ", string.IsNullOrWhiteSpace(plz) ? DBNull.Value : (object)plz);
                        cmd.Parameters.AddWithValue("@Gebaude", string.IsNullOrWhiteSpace(gebaude) ? DBNull.Value : (object)gebaude);
                        cmd.Parameters.AddWithValue("@Wohnung", string.IsNullOrWhiteSpace(wohnung) ? DBNull.Value : (object)wohnung);
                        cmd.Parameters.AddWithValue("@Organisation", string.IsNullOrWhiteSpace(organisation) ? DBNull.Value : (object)organisation);
                        cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(name) ? DBNull.Value : (object)name);
                        cmd.Parameters.AddWithValue("@Bezeichnung", string.IsNullOrWhiteSpace(bezeichnung) ? DBNull.Value : (object)bezeichnung);
                        cmd.Parameters.AddWithValue("@Iban", string.IsNullOrWhiteSpace(iban) ? DBNull.Value : (object)iban);
                        cmd.Parameters.AddWithValue("@Bic", string.IsNullOrWhiteSpace(bic) ? DBNull.Value : (object)bic);
                        cmd.Parameters.AddWithValue("@IstInsolvent", string.IsNullOrWhiteSpace(istInsolvent) ? DBNull.Value : (object)istInsolvent);
                        cmd.Parameters.AddWithValue("@IstAktiv", string.IsNullOrWhiteSpace(istAktiv) ? DBNull.Value : (object)istAktiv);
                        cmd.Parameters.AddWithValue("@Ansprechpartner", string.IsNullOrWhiteSpace(ansprechpartner) ? DBNull.Value : (object)ansprechpartner);
                        cmd.Parameters.AddWithValue("@AnsprechpartnerTel", string.IsNullOrWhiteSpace(ansprechpartnerTel) ? DBNull.Value : (object)ansprechpartnerTel);
                        cmd.Parameters.AddWithValue("@AnsprechpartnerEmail", string.IsNullOrWhiteSpace(ansprechpartnerEmail) ? DBNull.Value : (object)ansprechpartnerEmail);
                        cmd.Parameters.AddWithValue("@RechnungsLand", string.IsNullOrWhiteSpace(rechnungsLand) ? DBNull.Value : (object)rechnungsLand);
                        cmd.Parameters.AddWithValue("@RechnungsStrasse", string.IsNullOrWhiteSpace(rechnungsStrasse) ? DBNull.Value : (object)rechnungsStrasse);
                        cmd.Parameters.AddWithValue("@RechnungsOrt", string.IsNullOrWhiteSpace(rechnungsOrt) ? DBNull.Value : (object)rechnungsOrt);
                        cmd.Parameters.AddWithValue("@RechnungsStateOrPrefecture", string.IsNullOrWhiteSpace(rechnungsStateOrPrefecture) ? DBNull.Value : (object)rechnungsStateOrPrefecture);
                        cmd.Parameters.AddWithValue("@RechnungsPLZ", string.IsNullOrWhiteSpace(rechnungsPlz) ? DBNull.Value : (object)rechnungsPlz);
                        cmd.Parameters.AddWithValue("@RechnungsOrganisation", string.IsNullOrWhiteSpace(rechnungsOrganisation) ? DBNull.Value : (object)rechnungsOrganisation);
                        cmd.Parameters.AddWithValue("@RechnungsName", string.IsNullOrWhiteSpace(rechnungsName) ? DBNull.Value : (object)rechnungsName);
                        cmd.Parameters.AddWithValue("@Gesamtadresse", string.IsNullOrWhiteSpace(gesamtadresse) ? DBNull.Value : (object)gesamtadresse);
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var entry = new Adresse
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Land = reader["Land"] as string,
                                    Strasse = reader["Strasse"] as string,
                                    Ort = reader["Ort"] as string,
                                    StateOrPrefecture = reader["StateOrPrefecture"] as string,
                                    PLZ = reader["PLZ"] as string,
                                    Gebaude = reader["Gebaude"] as string,
                                    Wohnung = reader["Wohnung"] as string,
                                    Organisation = reader["Organisation"] as string,
                                    Name = reader["Name"] as string,
                                    Bezeichnung = reader["Bezeichnung"] as string,
                                    Iban = reader["Iban"] as string,
                                    Bic = reader["Bic"] as string,
                                    IstInsolvent = reader.GetBoolean(reader.GetOrdinal("IstInsolvent")),
                                    IstAktiv = reader.GetBoolean(reader.GetOrdinal("IstAktiv")),
                                    Ansprechpartner = reader["Ansprechpartner"] as string,
                                    AnsprechpartnerTel = reader["AnsprechpartnerTel"] as string,
                                    AnsprechpartnerEmail = reader["AnsprechpartnerEmail"] as string,
                                    RechnungsLand = reader["RechnungsLand"] as string,
                                    RechnungsStrasse = reader["RechnungsStrasse"] as string,
                                    RechnungsOrt = reader["RechnungsOrt"] as string,
                                    RechnungsStateOrPrefecture = reader["RechnungsStateOrPrefecture"] as string,
                                    RechnungsPLZ = reader["RechnungsPLZ"] as string,
                                    RechnungsOrganisation = reader["RechnungsOrganisation"] as string,
                                    RechnungsName = reader["RechnungsName"] as string,
                                    Gesamtadresse = reader["Gesamtadresse"] as string
                                };

                                result.Add(entry);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Adressdaten.", ex);
            }
        }


        public int GetFilteredCount(
            string land, string strasse, string ort, string stateOrPrefecture, string plz,
            string gebaude, string wohnung, string organisation, string name,
            string bezeichnung, string iban, string bic, string istInsolvent,
            string istAktiv, string ansprechpartner, string ansprechpartnerTel,
            string ansprechpartnerEmail,
            string rechnungsLand, string rechnungsStrasse, string rechnungsOrt,
            string rechnungsStateOrPrefecture, string rechnungsPlz,
            string rechnungsOrganisation, string rechnungsName,
            string gesamtadresse)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                                  SELECT COUNT(*) 
                                  FROM Adresse
                                  WHERE
                                  (@Land IS NULL OR Land LIKE '%' + @Land + '%') AND
                                  (@Strasse IS NULL OR Strasse LIKE '%' + @Strasse + '%') AND
                                  (@Ort IS NULL OR Ort LIKE '%' + @Ort + '%') AND
                                  (@StateOrPrefecture IS NULL OR StateOrPrefecture LIKE '%' + @StateOrPrefecture + '%') AND
                                  (@PLZ IS NULL OR PLZ LIKE '%' + @PLZ + '%') AND
                                  (@Gebaude IS NULL OR Gebaude LIKE '%' + @Gebaude + '%') AND
                                  (@Wohnung IS NULL OR Wohnung LIKE '%' + @Wohnung + '%') AND
                                  (@Organisation IS NULL OR Organisation LIKE '%' + @Organisation + '%') AND
                                  (@Name IS NULL OR Name LIKE '%' + @Name + '%') AND
                                  (@Bezeichnung IS NULL OR Bezeichnung LIKE '%' + @Bezeichnung + '%') AND
                                  (@Iban IS NULL OR Iban LIKE '%' + @Iban + '%') AND
                                  (@Bic IS NULL OR Bic LIKE '%' + @Bic + '%') AND
                                  (@IstInsolvent IS NULL OR CAST(IstInsolvent AS NVARCHAR) LIKE '%' + @IstInsolvent + '%') AND
                                  (@IstAktiv IS NULL OR CAST(IstAktiv AS NVARCHAR) LIKE '%' + @IstAktiv + '%') AND
                                  (@Ansprechpartner IS NULL OR Ansprechpartner LIKE '%' + @Ansprechpartner + '%') AND
                                  (@AnsprechpartnerTel IS NULL OR AnsprechpartnerTel LIKE '%' + @AnsprechpartnerTel + '%') AND
                                  (@AnsprechpartnerEmail IS NULL OR AnsprechpartnerEmail LIKE '%' + @AnsprechpartnerEmail + '%') AND
                                  (@RechnungsLand IS NULL OR RechnungsLand LIKE '%' + @RechnungsLand + '%') AND
                                  (@RechnungsStrasse IS NULL OR RechnungsStrasse LIKE '%' + @RechnungsStrasse + '%') AND
                                  (@RechnungsOrt IS NULL OR RechnungsOrt LIKE '%' + @RechnungsOrt + '%') AND
                                  (@RechnungsStateOrPrefecture IS NULL OR RechnungsStateOrPrefecture LIKE '%' + @RechnungsStateOrPrefecture + '%') AND
                                  (@RechnungsPLZ IS NULL OR RechnungsPLZ LIKE '%' + @RechnungsPLZ + '%') AND
                                  (@RechnungsOrganisation IS NULL OR RechnungsOrganisation LIKE '%' + @RechnungsOrganisation + '%') AND
                                  (@RechnungsName IS NULL OR RechnungsName LIKE '%' + @RechnungsName + '%') AND
                                  (@Gesamtadresse IS NULL OR Gesamtadresse LIKE '%' + @Gesamtadresse + '%');";

                cmd.Parameters.AddWithValue("@Land", string.IsNullOrWhiteSpace(land) ? DBNull.Value : (object)land);
                cmd.Parameters.AddWithValue("@Strasse", string.IsNullOrWhiteSpace(strasse) ? DBNull.Value : (object)strasse);
                cmd.Parameters.AddWithValue("@Ort", string.IsNullOrWhiteSpace(ort) ? DBNull.Value : (object)ort);
                cmd.Parameters.AddWithValue("@StateOrPrefecture", string.IsNullOrWhiteSpace(stateOrPrefecture) ? DBNull.Value : (object)stateOrPrefecture);
                cmd.Parameters.AddWithValue("@PLZ", string.IsNullOrWhiteSpace(plz) ? DBNull.Value : (object)plz);
                cmd.Parameters.AddWithValue("@Gebaude", string.IsNullOrWhiteSpace(gebaude) ? DBNull.Value : (object)gebaude);
                cmd.Parameters.AddWithValue("@Wohnung", string.IsNullOrWhiteSpace(wohnung) ? DBNull.Value : (object)wohnung);
                cmd.Parameters.AddWithValue("@Organisation", string.IsNullOrWhiteSpace(organisation) ? DBNull.Value : (object)organisation);
                cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(name) ? DBNull.Value : (object)name);
                cmd.Parameters.AddWithValue("@Bezeichnung", string.IsNullOrWhiteSpace(bezeichnung) ? DBNull.Value : (object)bezeichnung);
                cmd.Parameters.AddWithValue("@Iban", string.IsNullOrWhiteSpace(iban) ? DBNull.Value : (object)iban);
                cmd.Parameters.AddWithValue("@Bic", string.IsNullOrWhiteSpace(bic) ? DBNull.Value : (object)bic);
                cmd.Parameters.AddWithValue("@IstInsolvent", string.IsNullOrWhiteSpace(istInsolvent) ? DBNull.Value : (object)istInsolvent);
                cmd.Parameters.AddWithValue("@IstAktiv", string.IsNullOrWhiteSpace(istAktiv) ? DBNull.Value : (object)istAktiv);
                cmd.Parameters.AddWithValue("@Ansprechpartner", string.IsNullOrWhiteSpace(ansprechpartner) ? DBNull.Value : (object)ansprechpartner);
                cmd.Parameters.AddWithValue("@AnsprechpartnerTel", string.IsNullOrWhiteSpace(ansprechpartnerTel) ? DBNull.Value : (object)ansprechpartnerTel);
                cmd.Parameters.AddWithValue("@AnsprechpartnerEmail", string.IsNullOrWhiteSpace(ansprechpartnerEmail) ? DBNull.Value : (object)ansprechpartnerEmail);
                cmd.Parameters.AddWithValue("@RechnungsLand", string.IsNullOrWhiteSpace(rechnungsLand) ? DBNull.Value : (object)rechnungsLand);
                cmd.Parameters.AddWithValue("@RechnungsStrasse", string.IsNullOrWhiteSpace(rechnungsStrasse) ? DBNull.Value : (object)rechnungsStrasse);
                cmd.Parameters.AddWithValue("@RechnungsOrt", string.IsNullOrWhiteSpace(rechnungsOrt) ? DBNull.Value : (object)rechnungsOrt);
                cmd.Parameters.AddWithValue("@RechnungsStateOrPrefecture", string.IsNullOrWhiteSpace(rechnungsStateOrPrefecture) ? DBNull.Value : (object)rechnungsStateOrPrefecture);
                cmd.Parameters.AddWithValue("@RechnungsPLZ", string.IsNullOrWhiteSpace(rechnungsPlz) ? DBNull.Value : (object)rechnungsPlz);
                cmd.Parameters.AddWithValue("@RechnungsOrganisation", string.IsNullOrWhiteSpace(rechnungsOrganisation) ? DBNull.Value : (object)rechnungsOrganisation);
                cmd.Parameters.AddWithValue("@RechnungsName", string.IsNullOrWhiteSpace(rechnungsName) ? DBNull.Value : (object)rechnungsName);
                cmd.Parameters.AddWithValue("@Gesamtadresse", string.IsNullOrWhiteSpace(gesamtadresse) ? DBNull.Value : (object)gesamtadresse);

                return (int)cmd.ExecuteScalar();

            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Adressdaten.", ex);
            }
        }

        // Erhalten die Liste der Felder anhand des Ländercodes.
        public async Task<List<string>> GetFieldsByCountryCodeAsync(string countryCode)
        {
            try
            {
                var fields = new List<string>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = new SqlCommand("SELECT field FROM AddressFields WHERE country_code = @code ORDER BY Id", connection);
                    command.Parameters.AddWithValue("@code", countryCode);

                    var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        fields.Add(reader.GetString(0));
                    }
                }

                return fields;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Felder.", ex);
            }
        }

        // Adresse speichern.
        public void Create(Adresse adresse)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                                          INSERT INTO Adresse
                                          (Land, Strasse, Ort, PLZ, StateOrPrefecture, Gebaude, Wohnung, Organisation, Name, Bezeichnung,
                                          Iban, Bic, IstInsolvent, IstAktiv, Ansprechpartner, AnsprechpartnerTel, AnsprechpartnerEmail,
                                          RechnungsStrasse, Rechnungsort, RechnungsPLZ, Rechnungsland, RechnungsStateOrPrefecture,
                                          RechnungsOrganisation, RechnungsName, Gesamtadresse)
                                          VALUES
                                          (@Land, @Strasse, @Ort, @PLZ, @StateOrPrefecture, @Gebaude, @Wohnung, @Organisation, @Name, @Bezeichnung,
                                          @Iban, @Bic, @IstInsolvent, @IstAktiv, @Ansprechpartner, @AnsprechpartnerTel, @AnsprechpartnerEmail,
                                          @RechnungsStrasse, @Rechnungsort, @RechnungsPLZ, @Rechnungsland, @RechnungsStateOrPrefecture,
                                          @RechnungsOrganisation, @RechnungsName, @Gesamtadresse)";

                        cmd.Parameters.AddWithValue("@Land", adresse.Land ?? "");
                        cmd.Parameters.AddWithValue("@Strasse", adresse.Strasse ?? "");
                        cmd.Parameters.AddWithValue("@Ort", adresse.Ort ?? "");
                        cmd.Parameters.AddWithValue("@PLZ", adresse.PLZ ?? "");
                        cmd.Parameters.AddWithValue("@StateOrPrefecture", adresse.StateOrPrefecture ?? "");
                        cmd.Parameters.AddWithValue("@Gebaude", adresse.Gebaude ?? "");
                        cmd.Parameters.AddWithValue("@Wohnung", adresse.Wohnung ?? "");
                        cmd.Parameters.AddWithValue("@Organisation", adresse.Organisation ?? "");
                        cmd.Parameters.AddWithValue("@Name", adresse.Name ?? "");
                        cmd.Parameters.AddWithValue("@Bezeichnung", adresse.Bezeichnung ?? "");
                        cmd.Parameters.AddWithValue("@IBAN", adresse.Iban ?? "");
                        cmd.Parameters.AddWithValue("@BIC", adresse.Bic ?? "");
                        cmd.Parameters.AddWithValue("@IstInsolvent", adresse.IstInsolvent);
                        cmd.Parameters.AddWithValue("@IstAktiv", adresse.IstAktiv);
                        cmd.Parameters.AddWithValue("@Ansprechpartner", adresse.Ansprechpartner ?? "");
                        cmd.Parameters.AddWithValue("@AnsprechpartnerTel", adresse.AnsprechpartnerTel ?? "");
                        cmd.Parameters.AddWithValue("@AnsprechpartnerEmail", adresse.AnsprechpartnerEmail ?? "");
                        cmd.Parameters.AddWithValue("@RechnungsStrasse", adresse.RechnungsStrasse ?? "");
                        cmd.Parameters.AddWithValue("@Rechnungsort", adresse.RechnungsOrt ?? "");
                        cmd.Parameters.AddWithValue("@RechnungsPLZ", adresse.RechnungsPLZ ?? "");
                        cmd.Parameters.AddWithValue("@Rechnungsland", adresse.RechnungsLand ?? "");
                        cmd.Parameters.AddWithValue("@RechnungsStateOrPrefecture", adresse.RechnungsStateOrPrefecture ?? "");
                        cmd.Parameters.AddWithValue("@RechnungsOrganisation", adresse.RechnungsOrganisation ?? "");
                        cmd.Parameters.AddWithValue("@RechnungsName", adresse.RechnungsName ?? "");
                        cmd.Parameters.AddWithValue("@Gesamtadresse", adresse.Gesamtadresse ?? "");

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Create.", ex);
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

                cmd.CommandText = "DELETE FROM Adresse WHERE Id=@id";
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Delete.", ex);
            }
        }


        // get by ID.
        public Adresse GetById(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                                          SELECT 
                                          Id,
                                          Land, Strasse, Ort, PLZ, StateOrPrefecture,
                                          Gebaude, Wohnung, Organisation, Name, Bezeichnung,
                                          Iban, Bic,
                                          IstInsolvent, IstAktiv,
                                          Ansprechpartner, AnsprechpartnerTel, AnsprechpartnerEmail,
                                          RechnungsStrasse, RechnungsOrt, RechnungsPLZ, RechnungsLand,
                                          RechnungsStateOrPrefecture, RechnungsOrganisation, RechnungsName,
                                          Gesamtadresse
                                          FROM Adresse
                                          WHERE Id = @Id";

                        cmd.Parameters.AddWithValue("@Id", id);

                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                var adresse = new Adresse();

                                adresse.Id = r.GetInt32(r.GetOrdinal("Id"));

                                adresse.Land = r["Land"].ToString();
                                adresse.Strasse = r["Strasse"].ToString();
                                adresse.Ort = r["Ort"].ToString();
                                adresse.PLZ = r["PLZ"].ToString();
                                adresse.StateOrPrefecture = r["StateOrPrefecture"].ToString();

                                adresse.Gebaude = r["Gebaude"].ToString();
                                adresse.Wohnung = r["Wohnung"].ToString();
                                adresse.Organisation = r["Organisation"].ToString();
                                adresse.Name = r["Name"].ToString();
                                adresse.Bezeichnung = r["Bezeichnung"].ToString();

                                adresse.Iban = r["Iban"].ToString();
                                adresse.Bic = r["Bic"].ToString();

                                adresse.IstInsolvent = r["IstInsolvent"] != DBNull.Value
                                                 && Convert.ToBoolean(r["IstInsolvent"]);

                                adresse.IstAktiv = r["IstAktiv"] != DBNull.Value
                                             && Convert.ToBoolean(r["IstAktiv"]);

                                adresse.Ansprechpartner = r["Ansprechpartner"].ToString();
                                adresse.AnsprechpartnerTel = r["AnsprechpartnerTel"].ToString();
                                adresse.AnsprechpartnerEmail = r["AnsprechpartnerEmail"].ToString();

                                adresse.RechnungsStrasse = r["RechnungsStrasse"].ToString();
                                adresse.RechnungsOrt = r["RechnungsOrt"].ToString();
                                adresse.RechnungsPLZ = r["RechnungsPLZ"].ToString();
                                adresse.RechnungsLand = r["RechnungsLand"].ToString();
                                adresse.RechnungsStateOrPrefecture = r["RechnungsStateOrPrefecture"].ToString();
                                adresse.RechnungsOrganisation = r["RechnungsOrganisation"].ToString();
                                adresse.RechnungsName = r["RechnungsName"].ToString();

                                adresse.Gesamtadresse = r["Gesamtadresse"].ToString();

                                return adresse;
                            }
                        }
                    }
                }

                return null;

            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Felder.", ex);
            }
        }



        // Adresse aktualisieren.
        public void Update(Adresse adresse)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                                          UPDATE Adresse SET
                                          Land = @Land,
                                          Strasse = @Strasse,
                                          Ort = @Ort,
                                          PLZ = @PLZ,
                                          StateOrPrefecture = @StateOrPrefecture,
                                          Gebaude = @Gebaude,
                                          Wohnung = @Wohnung,
                                          Organisation = @Organisation,
                                          Name = @Name,
                                          Bezeichnung = @Bezeichnung,
                                          Iban = @Iban,
                                          Bic = @Bic,
                                          IstInsolvent = @IstInsolvent,
                                          IstAktiv = @IstAktiv,
                                          Ansprechpartner = @Ansprechpartner,
                                          AnsprechpartnerTel = @AnsprechpartnerTel,
                                          AnsprechpartnerEmail = @AnsprechpartnerEmail,
                                          RechnungsStrasse = @RechnungsStrasse,
                                          Rechnungsort = @Rechnungsort,
                                          RechnungsPLZ = @RechnungsPLZ,
                                          Rechnungsland = @Rechnungsland,
                                          RechnungsStateOrPrefecture = @RechnungsStateOrPrefecture,
                                          RechnungsOrganisation = @RechnungsOrganisation,
                                          RechnungsName = @RechnungsName,
                                          Gesamtadresse = @Gesamtadresse
                                          WHERE Id = @Id";

                        cmd.Parameters.AddWithValue("@Id", adresse.Id);
                        cmd.Parameters.AddWithValue("@Land", adresse.Land ?? "");
                        cmd.Parameters.AddWithValue("@Strasse", adresse.Strasse ?? "");
                        cmd.Parameters.AddWithValue("@Ort", adresse.Ort ?? "");
                        cmd.Parameters.AddWithValue("@PLZ", adresse.PLZ ?? "");
                        cmd.Parameters.AddWithValue("@StateOrPrefecture", adresse.StateOrPrefecture ?? "");
                        cmd.Parameters.AddWithValue("@Gebaude", adresse.Gebaude ?? "");
                        cmd.Parameters.AddWithValue("@Wohnung", adresse.Wohnung ?? "");
                        cmd.Parameters.AddWithValue("@Organisation", adresse.Organisation ?? "");
                        cmd.Parameters.AddWithValue("@Name", adresse.Name ?? "");
                        cmd.Parameters.AddWithValue("@Bezeichnung", adresse.Bezeichnung ?? "");
                        cmd.Parameters.AddWithValue("@Iban", adresse.Iban ?? "");
                        cmd.Parameters.AddWithValue("@Bic", adresse.Bic ?? "");
                        cmd.Parameters.AddWithValue("@IstInsolvent", adresse.IstInsolvent);
                        cmd.Parameters.AddWithValue("@IstAktiv", adresse.IstAktiv);
                        cmd.Parameters.AddWithValue("@Ansprechpartner", adresse.Ansprechpartner ?? "");
                        cmd.Parameters.AddWithValue("@AnsprechpartnerTel", adresse.AnsprechpartnerTel ?? "");
                        cmd.Parameters.AddWithValue("@AnsprechpartnerEmail", adresse.AnsprechpartnerEmail ?? "");
                        cmd.Parameters.AddWithValue("@RechnungsStrasse", adresse.RechnungsStrasse ?? "");
                        cmd.Parameters.AddWithValue("@Rechnungsort", adresse.RechnungsOrt ?? "");
                        cmd.Parameters.AddWithValue("@RechnungsPLZ", adresse.RechnungsPLZ ?? "");
                        cmd.Parameters.AddWithValue("@Rechnungsland", adresse.RechnungsLand ?? "");
                        cmd.Parameters.AddWithValue("@RechnungsStateOrPrefecture", adresse.RechnungsStateOrPrefecture ?? "");
                        cmd.Parameters.AddWithValue("@RechnungsOrganisation", adresse.RechnungsOrganisation ?? "");
                        cmd.Parameters.AddWithValue("@RechnungsName", adresse.RechnungsName ?? "");
                        cmd.Parameters.AddWithValue("@Gesamtadresse", adresse.Gesamtadresse ?? "");

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Update.", ex);
            }
        }


    }
}
