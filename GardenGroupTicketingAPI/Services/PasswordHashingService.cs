namespace GardenGroupTicketingAPI.Services
{
    public class PasswordHashingService : IPasswordHashingService
    {
        private readonly int _workfactor;

        public PasswordHashingService(int workfactor = 12)
        {
            _workfactor = workfactor;
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException(nameof(password), "Password cannot be null or empty!");
            }

            return BCrypt.Net.BCrypt.HashPassword(password, _workfactor);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException(nameof(password), "Password cannot be null or empty!");
            }

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new ArgumentNullException(nameof(passwordHash), "Password in the database is null or empty!");
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }
    }
}
