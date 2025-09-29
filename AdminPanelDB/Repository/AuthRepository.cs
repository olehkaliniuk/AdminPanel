using AdminPanelDB.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AdminPanelDB.Repository
{
    public class AuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // login (plain password)
        public bool UserExists(string email, string kennwort)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(kennwort))
            {
                throw new ArgumentException("Email or Kennwort is null or empty");
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var query = "SELECT COUNT(*) FROM [Personen] WHERE Email = @Email AND Kennwort = @Kennwort";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Email", SqlDbType.NVarChar, 500).Value = email;
                    command.Parameters.Add("@Kennwort", SqlDbType.NVarChar, 500).Value = kennwort;

                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        // get name for user
        public string GetUserNameByEmail(string email)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var query = "SELECT Vorname, Name FROM [Personen] WHERE Email = @Email";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Email", SqlDbType.NVarChar, 500).Value = email;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string vorname = reader["Vorname"] as string ?? "";
                            string name = reader["Name"] as string ?? "";
                            return $"{vorname} {name}".Trim();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }




        //api
        public Personen GetUserByEmail(string email)
        {
            Personen user = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var query = @"
        SELECT u.Id, u.Titel, u.Name, u.Vorname, u.Email, u.Abteilung, u.Referat, u.Stelle, u.Kennwort
        FROM [Personen] u
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
                                Abteilung = reader["Abteilung"] as string,
                                Referat = reader["Referat"] as string,
                                Stelle = reader["Stelle"] as string,
                                Kennwort = reader["Kennwort"] as string
                            };
                        }
                    }
                }
            }

            return user;
        }


      


    }
}
