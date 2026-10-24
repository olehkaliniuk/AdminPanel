using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AdminPanelDB.Repository
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // UserPage.
        public Personen GetUserByEmail(string email)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                            SELECT 
                                p.Id, p.Titel, p.Name, p.Vorname, p.Email, p.UId, p.Stelle, p.Kennwort,
                                pr.Rolle, pr.Abteilung, pr.Referat, pr.GueltigAb, pr.GueltigBis
                            FROM [Personen] p
                            LEFT JOIN [PersonRollen] pr ON pr.PersonId = p.Id
                                AND pr.GueltigAb <= GETDATE()
                                AND (pr.GueltigBis IS NULL OR pr.GueltigBis >= GETDATE())
                            WHERE p.Email = @Email";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@Email", SqlDbType.NVarChar, 500).Value = email;
                        using (var reader = command.ExecuteReader())
                        {
                            Personen? person = null;

                            while (reader.Read())
                            {
                                if (person == null)
                                {
                                    person = new Personen
                                    {
                                        Id = (int)reader["Id"],
                                        Titel = reader["Titel"] as string,
                                        Name = reader["Name"] as string,
                                        Vorname = reader["Vorname"] as string,
                                        Email = reader["Email"] as string,
                                        UId = reader["UId"] as string,
                                        Abteilung = reader["Abteilung"] as string,
                                        Referat = reader["Referat"] as string,
                                        Stelle = reader["Stelle"] as string,
                                        Kennwort = reader["Kennwort"] as string,
                                    };
                                }

                                string? rolle = reader["Rolle"] as string;
                                if (!string.IsNullOrEmpty(rolle) && !person.Rollen.Any(r => r.Rolle == rolle))
                                {
                                    person.Rollen.Add(new PersonRolle
                                    {
                                        Rolle = rolle,
                                        GueltigAb = reader["GueltigAb"] as DateTime?,
                                        GueltigBis = reader["GueltigBis"] as DateTime?,
                                    });
                                }

                                string? abt = reader["Abteilung"] as string;
                                string? ref_ = reader["Referat"] as string;
                                if (!string.IsNullOrEmpty(abt) && !person.AbteilungReferate.Any(x => x.Abteilung == abt && x.Referat == ref_))
                                {
                                    person.AbteilungReferate.Add((abt, ref_ ?? ""));
                                }
                            }

                            return person;
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Daten.", ex);
            }
        }
    }
}
