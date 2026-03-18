using System;
using System.Security.Cryptography;
using System.Text;

namespace Contact.API.Helpers
{
    /// <summary>
    /// Допоміжний клас для хешування паролів.
    /// Метод HashPassword() був ідентично продубльований 
    /// у AuthController та UsersController.
    /// Після рефакторингу — єдине місце відповідальності (DRY).
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Хешує пароль за допомогою SHA256 і повертає Base64-рядок.
        /// </summary>
        public static string Hash(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Перевіряє, чи збігається пароль з хешем.
        /// Виділений метод для підвищення читабельності (замість порівняння рядків у контролері).
        /// </summary>
        public static bool Verify(string password, string storedHash)
        {
            return Hash(password) == storedHash;
        }
    }
}
