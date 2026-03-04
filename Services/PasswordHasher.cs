using System;
using System.Security.Cryptography;

namespace InternalControlApp.Services
{
    public class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32; // 256 bit
        private const int Iterations = 10000;
        private static readonly HashAlgorithmName _hashAlgorithmName = HashAlgorithmName.SHA256;
        private const char Delimiter = ';';

        public string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, _hashAlgorithmName, KeySize);

            return string.Join(Delimiter, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public bool Verify(string passwordHash, string inputPassword)
        {
            try
            {
                var elements = passwordHash.Split(Delimiter);
                if (elements.Length != 2)
                {
                    // Esto no es un hash válido, podría ser una contraseña antigua en texto plano.
                    return false;
                }

                var salt = Convert.FromBase64String(elements[0]);
                var hash = Convert.FromBase64String(elements[1]);

                var hashInput = Rfc2898DeriveBytes.Pbkdf2(inputPassword, salt, Iterations, _hashAlgorithmName, KeySize);

                return CryptographicOperations.FixedTimeEquals(hash, hashInput);
            }
            catch
            {
                // Si ocurre cualquier error (ej. al intentar decodificar una contraseña en texto plano),
                // la verificación falla de forma segura.
                return false;
            }
        }
    }
}