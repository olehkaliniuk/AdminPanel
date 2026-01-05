using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using Microsoft.Data.SqlClient;

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
                Personen user = null;

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var query = @"
                                SELECT u.Id, u.Titel, u.Name, u.Vorname, u.Email, u.Uid, u.Abteilung, u.Referat, u.Stelle, u.Kennwort, u.IstAdmin, u.Rolle
                                FROM [zsPersonen] u
                                WHERE u.Email = @Email";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new Personen
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
                                    IstAdmin = (bool)reader["IstAdmin"],
                                    Rolle = reader["Rolle"] as string
                                };
                            }
                        }
                    }
                }
                return user;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Daten.", ex);
            }
        }
    }
}
