using messaging.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace messaging.tests.Helpers
{
    internal class DatabaseHelper
    {
        public static void ResetDatabase(ApplicationDbContext applicationDbContext)
        {
            applicationDbContext.Database.ExecuteSqlRaw("DELETE FROM IJEItems;");
            applicationDbContext.Database.ExecuteSqlRaw("DELETE FROM OutgoingMessageItems;");
            applicationDbContext.Database.ExecuteSqlRaw("DELETE FROM IncomingMessageItems;");
            applicationDbContext.Database.ExecuteSqlRaw("DELETE FROM IncomingMessageLogs;");
        }
    }

}
