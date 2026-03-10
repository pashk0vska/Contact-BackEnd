using System;

namespace Contact.API.Helpers
{
   
    public static class DateTimeHelper
    {
        
        public static DateTime NormalizeToUtc(DateTime dt)
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime(),
            };
        }

        public static DateTime NormalizeOrNow(DateTime dt)
        {
            return dt == default ? DateTime.UtcNow : NormalizeToUtc(dt);
        }
    }
}

