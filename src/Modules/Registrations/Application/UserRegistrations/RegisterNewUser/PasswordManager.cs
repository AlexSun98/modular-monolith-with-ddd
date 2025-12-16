using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace CompanyName.MyMeetings.Modules.Registrations.Application.UserRegistrations.RegisterNewUser
{
    public class PasswordManager
    {
        private const int SaltSize = 0x10; // 16 bytes
        private const int HashSize = 0x20; // 32 bytes
        private const int Iterations = 0x3e8; // 1000 iterations

        public static string HashPassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            // Generate a random salt
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // Use the static Pbkdf2 method (replaces obsolete Rfc2898DeriveBytes constructor)
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            byte[] dst = new byte[0x31];
            Buffer.BlockCopy(salt, 0, dst, 1, SaltSize);
            Buffer.BlockCopy(hash, 0, dst, 0x11, HashSize);
            return Convert.ToBase64String(dst);
        }

        public static bool VerifyHashedPassword(string hashedPassword, string password)
        {
            if (hashedPassword == null)
            {
                return false;
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            byte[] src = Convert.FromBase64String(hashedPassword);
            if ((src.Length != 0x31) || (src[0] != 0))
            {
                return false;
            }

            byte[] salt = new byte[SaltSize];
            Buffer.BlockCopy(src, 1, salt, 0, SaltSize);
            byte[] expectedHash = new byte[HashSize];
            Buffer.BlockCopy(src, 0x11, expectedHash, 0, HashSize);

            // Use the static Pbkdf2 method (replaces obsolete Rfc2898DeriveBytes constructor)
            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return ByteArraysEqual(expectedHash, actualHash);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            var areSame = true;
            for (var i = 0; i < a.Length; i++)
            {
                areSame &= a[i] == b[i];
            }

            return areSame;
        }
    }
}