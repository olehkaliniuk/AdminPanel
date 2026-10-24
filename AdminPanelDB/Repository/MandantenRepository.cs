using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AdminPanelDB.Repository
{
    public class MandantenRepository
    {
        private readonly string _connectionString;

        public MandantenRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Mandant> GetAllMandanten()
        {
            try
            {
                var list = new List<Mandant>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = "SELECT Id, Name, Beschreibung FROM [Mandanten] ORDER BY Name";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Mandant
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"] as string ?? "",
                                Beschreibung = reader["Beschreibung"] as string
                            });
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new RepositoryExceptions("Fehler beim Laden der Mandanten.", ex);
            }
        }
    }
}