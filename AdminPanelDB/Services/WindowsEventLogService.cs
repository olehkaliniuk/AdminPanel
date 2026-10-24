using AdminPanelDB.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

namespace AdminPanelDB.Services
{
    public class WindowsEventLogService
    {
        /// <summary>
        /// Liest die letzten Fehler aus dem Application-Log (int limit = 10).
        /// </summary>
        public List<WindowsLogModel> GetApplicationErrors(int limit = 10)
        {
            var logs = new List<WindowsLogModel>();

            // Abfrage: Protokoll "Application", EventType = Fehler.
            string query = "*[System/Level=2]"; // Level=2 - Error.

            var eventLogQuery = new EventLogQuery("Application", PathType.LogName, query)
            {
                ReverseDirection = true // beginnen mit den letzten Einträgen.
            };

            using (var reader = new EventLogReader(eventLogQuery))
            {
                for (int i = 0; i < limit; i++)
                {
                    var entry = reader.ReadEvent();
                    if (entry == null) break;

                    logs.Add(new WindowsLogModel
                    {
                        Index = i,
                        Level = "Error",
                        Time = entry.TimeCreated ?? DateTime.MinValue,
                        Source = entry.ProviderName,
                        EventId = entry.Id.ToString(),
                        Message = entry.FormatDescription()
                    });
                }
            }

            return logs;
        }
    }
}
