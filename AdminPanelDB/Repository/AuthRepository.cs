using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace AdminPanelDB.Repository
{
    public class AuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository(string connectionString)
        {
            _connectionString = connectionString;
        }



        public (bool Success, string Message) TryLogin(string email, string kennwort)
        {
            try
            {

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(kennwort))
                {
                    return (false, "Email oder Passwort darf nicht leer sein.");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Prüfen, ob die Email existiert.
                    string checkEmailQuery = "SELECT COUNT(*) FROM [zsPersonen] WHERE Email = @Email";
                    using (var cmd = new SqlCommand(checkEmailQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        int userExists = (int)cmd.ExecuteScalar();
                        if (userExists == 0)
                        {
                            return (false, "Ein Konto mit dieser E-Mail-Adresse existiert nicht.");
                        }

                    }

                    // Aktuellen Stand der Versuche abrufen.
                    string selectQuery = @"
                                         SELECT FailedLoginAttempts, LastFailedAttempt
                                         FROM [zsPersonen]
                                         WHERE Email = @Email";

                    int failedAttempts = 0;
                    DateTime? lastAttempt = null;

                    using (var cmd = new SqlCommand(selectQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                failedAttempts = reader["FailedLoginAttempts"] != DBNull.Value ? Convert.ToInt32(reader["FailedLoginAttempts"]) : 0;
                                lastAttempt = reader["LastFailedAttempt"] != DBNull.Value ? Convert.ToDateTime(reader["LastFailedAttempt"]) : (DateTime?)null;
                            }
                        }
                    }

                    // Automatischer Reset nach 2 Minuten Inaktivität.
                    if (failedAttempts >= 3 && lastAttempt.HasValue && lastAttempt.Value.AddMinutes(2) <= DateTime.UtcNow)
                    {
                        ResetLoginAttempts(email, connection);
                        failedAttempts = 0;
                        lastAttempt = null;
                    }

                    // Prüfen, ob Sperre noch aktiv ist (2 Minuten noch nicht vorbei).
                    if (failedAttempts >= 3 && lastAttempt.HasValue && lastAttempt.Value.AddMinutes(2) > DateTime.UtcNow)
                    {
                        var remaining = lastAttempt.Value.AddMinutes(2) - DateTime.UtcNow;
                        int minutes = remaining.Minutes;
                        int seconds = remaining.Seconds;

                        string minuteText = minutes == 0 ? "" : minutes == 1 ? "1 Minute" : $"{minutes} Minuten";
                        string secondText = seconds == 0 ? "" : seconds == 1 ? "1 Sekunde" : $"{seconds} Sekunden";

                        // Teile korrekt zusammenfügen.
                        string timeText;
                        if (minutes > 0 && seconds > 0)
                        {
                            timeText = $"{minuteText} {secondText}";
                        }
                        else if (minutes > 0)
                        {
                            timeText = minuteText;
                        }
                        else if (seconds > 0)
                        {
                            timeText = secondText;
                        }
                        else
                        {
                            timeText = "sofort";
                        }


                        return (false, $"Ihr Konto ist vorübergehend gesperrt. Bitte versuchen Sie es erneut in {timeText}.");
                    }


                    // Passwort prüfen.
                    string hashedPassword = HashPassword(kennwort);
                    string loginQuery = @"
                                        SELECT COUNT(*) 
                                        FROM [zsPersonen]
                                        WHERE Email = @Email
                                        AND (Kennwort = @Hash
                                        OR (TKennwort = @Hash AND TempPasswordExpiry > GETUTCDATE()))";
                    using (var cmd = new SqlCommand(loginQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Hash", hashedPassword);
                        int count = (int)cmd.ExecuteScalar();

                        if (count > 0)
                        {
                            // Erfolgreicher Login, Zähler zurücksetzen.
                            ResetLoginAttempts(email, connection);
                            return (true, "Anmeldung erfolgreich.");
                        }
                        else
                        {
                            // Fehler beim Login, Zähler erhöhen.
                            failedAttempts++;
                            UpdateLoginAttempts(email, failedAttempts, connection);

                            int remainingAttempts = Math.Max(0, 3 - failedAttempts);
                            if (remainingAttempts == 0)
                            {
                                return (false, "Drei false Versuche. Das Konto wurde für 2 Minuten gesperrt.");
                            }
                            else
                            {
                                return (false, $"Falsches Passwort. Verbleibende Versuche: {remainingAttempts}.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Login.", ex);
            }
        }



        private void UpdateLoginAttempts(string email, int failedAttempts, SqlConnection connection)
        {
            string updateQuery = @"
                                 UPDATE [zsPersonen]
                                 SET FailedLoginAttempts = @Count, LastFailedAttempt = GETUTCDATE()
                                 WHERE Email = @Email";

            using (var cmd = new SqlCommand(updateQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Count", failedAttempts);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.ExecuteNonQuery();
            }
        }



        private void ResetLoginAttempts(string email, SqlConnection connection)
        {
            string resetQuery = @"
                                UPDATE [zsPersonen]
                                SET FailedLoginAttempts = 0, LastFailedAttempt = NULL
                                WHERE Email = @Email";

            using (var cmd = new SqlCommand(resetQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.ExecuteNonQuery();
            }
        }



        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }


        public (int userId, bool isAdmin, string rolle, string fullName) GetUserByEmail(string email)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var query = "SELECT Id, IstAdmin, Rolle, Vorname, Name FROM [zsPersonen] WHERE Email = @Email";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@Email", SqlDbType.NVarChar, 500).Value = email;

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = (int)reader["Id"];
                                bool isAdmin = (bool)reader["IstAdmin"];
                                string rolle = reader["Rolle"] as string ?? "";
                                string vorname = reader["Vorname"] as string ?? "";
                                string name = reader["Name"] as string ?? "";
                                string fullName = $"{vorname} {name}".Trim();

                                return (userId, isAdmin, rolle, fullName);
                            }
                            else
                            {
                                return (0, false, null, null);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Benutzerdaten.", ex);
            }
        }




        // API.
        public Personen GetUserByEmailAPI(string email)
        {
            Personen user = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var query = @"
                            SELECT u.Id, u.Titel, u.Name, u.Vorname, u.Email, u.Abteilung, u.Referat, u.Stelle, u.Kennwort
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
