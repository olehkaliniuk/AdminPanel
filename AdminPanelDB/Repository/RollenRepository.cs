using AdminPanelDB.Abstractions;
using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using Microsoft.Data.SqlClient;

namespace AdminPanelDB.Repository
{
    public class RollenRepository
    {

        private readonly string _connectionString;
        private readonly ICurrentMandantProvider _mandantProvider;

        public RollenRepository(string connectionString, ICurrentMandantProvider mandantProvider)
        {
            _connectionString = connectionString;
            _mandantProvider = mandantProvider;
        }

        public int GetRollenCount(string searchTerm = "")
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                                   SELECT COUNT(*)
                                   FROM Rollen
                                   WHERE MandantId = @MandantId
                                   AND Name LIKE @SearchTerm";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim GetRollenCount.", ex);
            }
        }

        public List<Rolle> GetRollenPage(int pageNumber, int pageSize, string searchTerm = "", string sortColumn = "Id", string sortDirection = "ASC")
        {
            try
            {
                var list = new List<Rolle>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    int offset = (pageNumber - 1) * pageSize;

                    // Nur erlaubte Spalten für ORDER BY.
                    var allowedColumns = new[]
                    {
                        "Id",
                        "Name"
                    };

                    if (string.IsNullOrEmpty(sortColumn) ||
                        !allowedColumns.Contains(sortColumn))
                    {
                        sortColumn = "Id";
                    }

                    if (string.IsNullOrEmpty(sortDirection) ||
                        (sortDirection != "ASC" &&
                         sortDirection != "DESC"))
                    {
                        sortDirection = "ASC";
                    }

                    string query = $@"
                                   SELECT Id, Name, MandantId
                                   FROM Rollen
                                   WHERE MandantId = @MandantId
                                   AND Name LIKE @SearchTerm
                                   ORDER BY {sortColumn} {sortDirection}
                                   OFFSET @Offset ROWS
                                   FETCH NEXT @PageSize ROWS ONLY";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                        cmd.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

                        cmd.Parameters.AddWithValue("@Offset", offset); 

                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new Rolle
                                {
                                    Id = (int)reader["Id"],
                                    Name = reader["Name"].ToString(),
                                    MandantId = (int)reader["MandantId"]
                                });
                            }
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions(
                    "Fehler beim GetRollenPage.",
                    ex
                );
            }
        }

        public bool CreateRolle(string name)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Prüfen, ob Rolle bereits existiert.
                    string checkQuery = @"
                                        SELECT COUNT(*)
                                        FROM Rollen
                                        WHERE Name = @Name
                                        AND MandantId = @MandantId";

                    using (var checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Name", name);
                        checkCmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            return false;
                        }
                    }

                    string insertQuery = @"
                                         INSERT INTO Rollen (Name, MandantId)
                                         VALUES (@Name, @MandantId)";

                    using (var cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim CreateRolle.", ex);
            }
        }

        public bool EditRolle(int id, string newName)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Prüfen, ob Rolle existiert.
                    string checkQuery = @"
                                        SELECT COUNT(*)
                                        FROM Rollen
                                        WHERE Id = @Id
                                        AND MandantId = @MandantId";

                    using (var checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", id);
                        checkCmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId
                        );

                        int count = (int)checkCmd.ExecuteScalar();

                        if (count == 0)
                        {
                            return false;
                        }
                    }

                    // Prüfen, ob anderer Datensatz denselben Namen hat.
                    string duplicateQuery = @"
                                            SELECT COUNT(*)
                                            FROM Rollen
                                            WHERE Name = @Name
                                            AND Id <> @Id
                                            AND MandantId = @MandantId";

                    using (var duplicateCmd = new SqlCommand(duplicateQuery, conn))
                    {
                        duplicateCmd.Parameters.AddWithValue("@Name", newName);
                        duplicateCmd.Parameters.AddWithValue("@Id", id);
                        duplicateCmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                        int count = (int)duplicateCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            return false;
                        }
                    }

                    string updateQuery = @"
                                         UPDATE Rollen
                                         SET Name = @Name
                                         WHERE Id = @Id
                                         AND MandantId = @MandantId";

                    using (var cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", newName);
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim EditRolle.", ex);
            }
        }

        public bool DeleteRolle(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                                   DELETE FROM Rollen
                                   WHERE Id = @Id
                                   AND MandantId = @MandantId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@MandantId", _mandantProvider.MandantId);

                        int affectedRows = cmd.ExecuteNonQuery();

                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim DeleteRolle.", ex);
            }
        }
    }
}
