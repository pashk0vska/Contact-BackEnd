namespace Contact.API.Helpers
{
    /// <summary>
    /// Допоміжний клас для хешування паролів.
    /// Рефакторинг: SHA256 замінено на BCrypt (безпечніше хешування з сіллю).
    /// Єдине місце відповідальності (DRY) — використовується в AuthController та UsersController.
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Хешує пароль за допомогою BCrypt із автоматичною сіллю.
        /// </summary>
        public static string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <summary>
        /// Перевіряє чи збігається пароль з BCrypt хешем.
        /// </summary>
        public static bool Verify(string password, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
    }
}