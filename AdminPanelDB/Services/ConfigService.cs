using AdminPanelDB.Repository;
using AdminPanelDB.Models;

namespace AdminPanelDB.Services
{
    public class ConfigService
    {
        public readonly ConfigRepository _configRep;

        private Config _currentConfig;

        public ConfigService(ConfigRepository configRep)
        {
            _configRep = configRep;

        }

        // Get current config.
        public Config CurrentConfig
        {
            get
            {
                if (_currentConfig == null)
                {
                    RefreshConfig();
                }
                return _currentConfig;
            }
        }

        // Config aktualisieren.
        public void RefreshConfig() 
        {
            _currentConfig = _configRep.LoadConfig();
        }

    }
}
