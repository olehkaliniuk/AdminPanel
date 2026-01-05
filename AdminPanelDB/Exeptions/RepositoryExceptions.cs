using System;

namespace AdminPanelDB.Exeptions
{
    public class RepositoryExceptions : Exception
    {
        public RepositoryExceptions(string message, Exception inner)
            : base(message, inner) 
        { 

        }
    }
}
