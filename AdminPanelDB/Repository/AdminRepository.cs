using AdminPanelDB.Models;
using AdminPanelDB.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AdminPanelDB.Repository
{
    public class AdminRepository
    {
        private readonly string _connectionString;
        public AdminRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<AbteilungReferateViewModel> GetAbteilungReferate()
        {
            var list = new List<AbteilungReferateViewModel>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Получаем все Abteilung
                var cmdAbt = new SqlCommand("SELECT Id, Name FROM Abteilung", conn);
                using (var reader = cmdAbt.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new AbteilungReferateViewModel
                        {
                            Abteilung = new Abteilung
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString()
                            },
                            Referate = new List<Referat>()
                        });
                    }
                }

                // Получаем Referat для каждой Abteilung
                foreach (var item in list)
                {
                    var cmdRef = new SqlCommand("SELECT Id, Name, AbteilungId FROM Referat WHERE AbteilungId=@AbtId", conn);
                    cmdRef.Parameters.AddWithValue("@AbtId", item.Abteilung.Id);
                    using (var reader = cmdRef.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            item.Referate.Add(new Referat
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                AbteilungId = (int)reader["AbteilungId"]
                            });
                        }
                    }
                }
            }
            return list;
        }

        public bool CreateAbteilung(string name)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Abteilung WHERE Name = @Name", conn);
                checkCmd.Parameters.AddWithValue("@Name", name);
                int count = (int)checkCmd.ExecuteScalar();

                if (count > 0)
                {
                    return false; // уже есть
                }

                var cmd = new SqlCommand("INSERT INTO Abteilung (Name) VALUES (@Name)", conn);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.ExecuteNonQuery();
                return true;
            }
        }



        public bool EditAbteilung(int id, string newName)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Получаем старое имя
                string oldName;
                using (var cmdGet = new SqlCommand("SELECT Name FROM Abteilung WHERE Id=@Id", conn))
                {
                    cmdGet.Parameters.AddWithValue("@Id", id);
                    oldName = cmdGet.ExecuteScalar()?.ToString();
                }

                if (string.IsNullOrEmpty(oldName))
                    return false; // такого Id нет

                // Если новое имя совпадает со старым → ничего не делаем, но и не ошибка
                if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Проверяем, существует ли другое Abteilung с таким именем
                using (var checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Abteilung WHERE Name=@Name AND Id<>@Id", conn))
                {
                    checkCmd.Parameters.AddWithValue("@Name", newName);
                    checkCmd.Parameters.AddWithValue("@Id", id);
                    int count = (int)checkCmd.ExecuteScalar();
                    if (count > 0)
                    {
                        return false; // уже есть с таким именем
                    }
                }

                // Обновляем Abteilung
                using (var cmdUpdateAbt = new SqlCommand("UPDATE Abteilung SET Name=@Name WHERE Id=@Id", conn))
                {
                    cmdUpdateAbt.Parameters.AddWithValue("@Name", newName);
                    cmdUpdateAbt.Parameters.AddWithValue("@Id", id);
                    cmdUpdateAbt.ExecuteNonQuery();
                }

                // Каскадное обновление в Personen
                using (var cmdUpdatePerson = new SqlCommand(
                    "UPDATE Personen SET Abteilung=@NewName WHERE Abteilung=@OldName", conn))
                {
                    cmdUpdatePerson.Parameters.AddWithValue("@NewName", newName);
                    cmdUpdatePerson.Parameters.AddWithValue("@OldName", oldName);
                    cmdUpdatePerson.ExecuteNonQuery();
                }

                return true;
            }
        }




        public bool DeleteAbteilung(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 1) Проверяем, есть ли Referate у этой Abteilung
                var cmdCheck = new SqlCommand("SELECT COUNT(*) FROM Referat WHERE AbteilungId=@Id", conn);
                cmdCheck.Parameters.AddWithValue("@Id", id);
                int referatCount = (int)cmdCheck.ExecuteScalar();

                if (referatCount > 0)
                {
                    // Есть связанные Referate, удалять нельзя
                    return false;
                }

                // 2) Каскадно очищаем Abteilung у Personen
                var cmdUpdatePerson = new SqlCommand("UPDATE Personen SET Abteilung = NULL WHERE Abteilung = (SELECT Name FROM Abteilung WHERE Id=@Id)", conn);
                cmdUpdatePerson.Parameters.AddWithValue("@Id", id);
                cmdUpdatePerson.ExecuteNonQuery();

                // 3) Удаляем Abteilung
                var cmdAbt = new SqlCommand("DELETE FROM Abteilung WHERE Id=@Id", conn);
                cmdAbt.Parameters.AddWithValue("@Id", id);
                cmdAbt.ExecuteNonQuery();

                return true; // Успешно удалено
            }
        }



        public void CreateReferat(int abteilungId, string name)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("INSERT INTO Referat (Name, AbteilungId) VALUES (@Name, @AbtId)", conn);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@AbtId", abteilungId);
                cmd.ExecuteNonQuery();
            }
        }

        public void EditReferat(int id, string newName)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string oldName;
                int abteilungId;

                // 1) Получаем старое имя и AbteilungId у реферата
                using (var cmdGet = new SqlCommand("SELECT Name, AbteilungId FROM Referat WHERE Id=@Id", conn))
                {
                    cmdGet.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmdGet.ExecuteReader())
                    {
                        if (!reader.Read())
                            return; // если реферата нет — выходим

                        oldName = reader["Name"].ToString();
                        abteilungId = (int)reader["AbteilungId"];
                    }
                }

                // 2) Получаем имя абтайлюнга
                string abteilungName;
                using (var cmdAbt = new SqlCommand("SELECT Name FROM Abteilung WHERE Id=@AbtId", conn))
                {
                    cmdAbt.Parameters.AddWithValue("@AbtId", abteilungId);
                    abteilungName = cmdAbt.ExecuteScalar()?.ToString();
                }

                if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(abteilungName))
                    return;

                // 3) Обновляем сам реферат
                using (var cmdUpdateRef = new SqlCommand("UPDATE Referat SET Name=@Name WHERE Id=@Id", conn))
                {
                    cmdUpdateRef.Parameters.AddWithValue("@Name", newName);
                    cmdUpdateRef.Parameters.AddWithValue("@Id", id);
                    cmdUpdateRef.ExecuteNonQuery();
                }

                // 4) Каскадно обновляем Personen только для этой абтайлюнга + старого имени
                using (var cmdUpdatePerson = new SqlCommand(
                    "UPDATE Personen SET Referat=@NewName " +
                    "WHERE Referat=@OldName AND Abteilung=@AbtName", conn))
                {
                    cmdUpdatePerson.Parameters.AddWithValue("@NewName", newName);
                    cmdUpdatePerson.Parameters.AddWithValue("@OldName", oldName);
                    cmdUpdatePerson.Parameters.AddWithValue("@AbtName", abteilungName);
                    cmdUpdatePerson.ExecuteNonQuery();
                }
            }
        }



        public void DeleteReferat(int Id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 1) Получаем название Referat и его AbteilungId
                string referatName;
                int abteilungId;
                using (var cmdGet = new SqlCommand("SELECT Name, AbteilungId FROM Referat WHERE Id=@Id", conn))
                {
                    cmdGet.Parameters.AddWithValue("@Id", Id);
                    using (var reader = cmdGet.ExecuteReader())
                    {
                        if (!reader.Read())
                            return; // такого Referat нет

                        referatName = reader["Name"].ToString();
                        abteilungId = (int)reader["AbteilungId"];
                    }
                }

                // 2) Получаем имя Abteilung
                string abteilungName;
                using (var cmdAbt = new SqlCommand("SELECT Name FROM Abteilung WHERE Id=@Id", conn))
                {
                    cmdAbt.Parameters.AddWithValue("@Id", abteilungId);
                    abteilungName = cmdAbt.ExecuteScalar()?.ToString();
                }

                if (string.IsNullOrEmpty(abteilungName))
                    return; // Abteilung не найдена

                // 3) Обновляем только те записи Personen, которые соответствуют Abteilung и Referat
                using (var cmdUpdate = new SqlCommand(
                    "UPDATE Personen SET Referat=NULL WHERE Referat=@ReferatName AND Abteilung=@AbteilungName", conn))
                {
                    cmdUpdate.Parameters.AddWithValue("@ReferatName", referatName);
                    cmdUpdate.Parameters.AddWithValue("@AbteilungName", abteilungName);
                    cmdUpdate.ExecuteNonQuery();
                }

                // 4) Удаляем сам Referat
                using (var cmdDelete = new SqlCommand("DELETE FROM Referat WHERE Id=@Id", conn))
                {
                    cmdDelete.Parameters.AddWithValue("@Id", Id);
                    cmdDelete.ExecuteNonQuery();
                }
            }
        }





        //personen

        public List<Personen> GetAllPersonen()
        {
            var list = new List<Personen>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Titel, Name, Vorname, Email, UId, Abteilung, Referat, Stelle, Kennwort FROM Personen", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Personen
                        {
                            Id = (int)reader["Id"],
                            Titel = reader["Titel"]?.ToString(),
                            Name = reader["Name"]?.ToString(),
                            Vorname = reader["Vorname"]?.ToString(),
                            Email = reader["Email"]?.ToString(),
                            UId = reader["UId"]?.ToString(),
                            Abteilung = reader["Abteilung"]?.ToString(),
                            Referat = reader["Referat"]?.ToString(),
                            Stelle = reader["Stelle"]?.ToString(),
                            Kennwort = reader["Kennwort"]?.ToString()
                        });
                    }
                }
            }
            return list;
        }

        public void CreatePerson(Personen person)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
            INSERT INTO Personen (Titel, Name, Vorname, Email, UId, Abteilung, Referat, Stelle, Kennwort) 
            VALUES (@Titel, @Name, @Vorname, @Email, @UId, @Abteilung, @Referat, @Stelle, @Kennwort)", conn);

                cmd.Parameters.AddWithValue("@Titel", (object?)person.Titel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Name", (object?)person.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Vorname", (object?)person.Vorname ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object?)person.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UId", (object?)person.UId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Abteilung", (object?)person.Abteilung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Referat", (object?)person.Referat ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Stelle", (object?)person.Stelle ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Kennwort", (object?)person.Kennwort ?? DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }


        public List<Abteilung> GetAllAbteilungen()
        {
            var list = new List<Abteilung>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Name FROM Abteilung", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Abteilung
                        {
                            Id = (int)reader["Id"],
                            Name = reader["Name"].ToString()
                        });
                    }
                }
            }
            return list;
        }


        public List<Referat> GetReferateByAbteilungId(int abteilungId)
        {
            var list = new List<Referat>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT Id, Name FROM Referat WHERE AbteilungId=@AbtId", conn);
                cmd.Parameters.AddWithValue("@AbtId", abteilungId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Referat
                        {
                            Id = (int)reader["Id"],
                            Name = reader["Name"].ToString(),
                            AbteilungId = abteilungId
                        });
                    }
                }
            }
            return list;
        }



        public void UpdatePerson(Personen person)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
            UPDATE Personen SET 
                Titel=@Titel,
                Name=@Name,
                Vorname=@Vorname,
                Email=@Email,
                UId=@UId,
                Abteilung=@Abteilung,
                Referat=@Referat,
                Stelle=@Stelle,
                Kennwort=@Kennwort
            WHERE Id=@Id", conn);

                cmd.Parameters.AddWithValue("@Titel", (object?)person.Titel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Name", (object?)person.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Vorname", (object?)person.Vorname ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object?)person.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UId", (object?)person.UId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Abteilung", (object?)person.Abteilung ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Referat", (object?)person.Referat ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Stelle", (object?)person.Stelle ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Kennwort", (object?)person.Kennwort ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Id", person.Id);

                cmd.ExecuteNonQuery();
            }
        }

        public void DeletePerson(int id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("DELETE FROM Personen WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }

    }
}
