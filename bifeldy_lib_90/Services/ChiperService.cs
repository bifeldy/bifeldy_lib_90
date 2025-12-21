using bifeldy_lib_90.Extensions;
using bifeldy_lib_90.Libraries;
using bifeldy_lib_90.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace bifeldy_lib_90.Services {

    public interface IChiperService {
        string EncryptText(string plainText, string passPhrase = null);
        string DecryptText(string cipherText, string passPhrase = null, Encoding encoding = null);
        string CalculateMD5File(string filePath);
        string CalculateCRC32File(string filePath);
        string CalculateSHA1File(string filePath);
        string GetMimeFile(string filePath);
        string HashByte(byte[] data);
        string HashText(string textMessage);
        string EncodeJWT(IEnumerable<Claim> claims, ulong expiredNextMilliSeconds = 60 * 60 * 1000 * 1);
        IEnumerable<Claim> DecodeJWT(string token);
        Task<string> SignFile(string filePath);
        Task<string> SignByte(byte[] data);
        Task<string> SignText(string textMessage);
        Task<bool> VerifyFile(string signature, string filePath);
        Task<bool> VerifyByte(string signature, byte[] data);
        Task<bool> VerifyText(string signature, string textMessage);
    }

    public sealed class CChiperService : IChiperService {

        private readonly EnvVar _envVar;
        private readonly IApplicationService _app;

        private readonly FileExtensionContentTypeProvider _mimeProvider = new();

        // This constant is used to determine the keysize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int KEY_SIZE = 128;
        private const int BLOCK_SIZE = 128;

        // This constant determines the number of iterations for the password bytes generation function.
        // Normal app encryption    100,000 – 300,000
        // High-security            500,000+
        // Mobile / low CPU         50,000 – 100,000
        private const int DERIVATION_ITERATIONS = 1000;

        private string pubKeyPath { get; }
        private string privKeyPath { get; }

        public CChiperService(
            IOptions<EnvVar> envVar,
            IApplicationService app
        ) {
            this._envVar = envVar.Value;
            this._app = app;
            //
            this.pubKeyPath = Path.Combine(this._app.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "public.key");
            this.privKeyPath = Path.Combine(this._app.AppLocation, Bifeldy.DEFAULT_DATA_FOLDER, "private.key");
        }

        private byte[] Generate128BitsOfRandomEntropy() {
            byte[] randomBytes = new byte[16]; // 16 Bytes will give us 128 bits.
            using (var rngCsp = RandomNumberGenerator.Create()) {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }

            return randomBytes;
        }

        public string EncryptText(string plainText, string passPhrase = null) {
            if (string.IsNullOrEmpty(passPhrase) || passPhrase?.Length < 8) {
                passPhrase = this.HashText(this._app.AppName);
            }
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            byte[] saltStringBytes = this.Generate128BitsOfRandomEntropy();
            byte[] ivStringBytes = this.Generate128BitsOfRandomEntropy();
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DERIVATION_ITERATIONS, HashAlgorithmName.SHA256)) {
                byte[] keyBytes = password.GetBytes(KEY_SIZE / 8);
                using (var symmetricKey = Aes.Create()) {
                    symmetricKey.BlockSize = BLOCK_SIZE;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes)) {
                        using (var memoryStream = new MemoryStream()) {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write)) {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                byte[] cipherTextBytes = saltStringBytes;
                                cipherTextBytes = [.. cipherTextBytes, .. ivStringBytes];
                                cipherTextBytes = [.. cipherTextBytes, .. memoryStream.ToArray()];
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public string DecryptText(string cipherText, string passPhrase = null, Encoding encoding = null) {
            if (string.IsNullOrEmpty(passPhrase) || passPhrase?.Length < 8) {
                passPhrase = this.HashText(this._app.AppName);
            }
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            byte[] saltStringBytes = [.. cipherTextBytesWithSaltAndIv.Take(KEY_SIZE / 8)];
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            byte[] ivStringBytes = [.. cipherTextBytesWithSaltAndIv.Skip(KEY_SIZE / 8).Take(KEY_SIZE / 8)];
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            byte[] cipherTextBytes = [.. cipherTextBytesWithSaltAndIv.Skip(KEY_SIZE / 8 * 2).Take(cipherTextBytesWithSaltAndIv.Length - (KEY_SIZE / 8 * 2))];
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DERIVATION_ITERATIONS, HashAlgorithmName.SHA256)) {
                byte[] keyBytes = password.GetBytes(KEY_SIZE / 8);
                using (var symmetricKey = Aes.Create()) {
                    symmetricKey.BlockSize = BLOCK_SIZE;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes)) {
                        using (var memoryStream = new MemoryStream(cipherTextBytes)) {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read)) {
                                using (var streamReader = new StreamReader(cryptoStream, encoding ?? Encoding.UTF8, encoding == null)) {
                                    return streamReader.ReadToEnd();
                                }
                            }
                        }
                    }
                }
            }
        }

        public string CalculateMD5File(string filePath) {
            using (var md5 = MD5.Create()) {
                using (FileStream stream = File.OpenRead(filePath)) {
                    return md5.ComputeHash(stream).ToStringHex();
                }
            }
        }

        public string CalculateCRC32File(string filePath) {
            using (FileStream stream = File.OpenRead(filePath)) {
                return new Crc32().Compute(stream).ToString("x8");
            }
        }

        public string CalculateSHA1File(string filePath) {
            using (var sha1 = SHA1.Create()) {
                using (FileStream stream = File.OpenRead(filePath)) {
                    return sha1.ComputeHash(stream).ToStringHex();
                }
            }
        }

        public string GetMimeFile(string filePath) {
            if (this._mimeProvider.TryGetContentType(filePath, out string mime)) {
                return mime;
            }

            return MediaTypeNames.Application.Octet;
        }

        public string HashByte(byte[] data) {
            using (var sha1 = SHA1.Create()) {
                byte[] hash = sha1.ComputeHash(data);
                return hash.ToStringHex();
            }
        }

        public string HashText(string textMessage) {
            byte[] data = Encoding.UTF8.GetBytes(textMessage);
            return this.HashByte(data);
        }

        public string EncodeJWT(IEnumerable<Claim> claims, ulong expiredNextMilliSeconds = 60 * 60 * 1000 * 1) {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.HashText(this._envVar.JWT_SECRET)));
            var credetial = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                this._app.AppName,
                this._envVar.JWT_AUDIENCE,
                claims,
                expires: DateTime.Now.AddMilliseconds(expiredNextMilliSeconds),
                signingCredentials: credetial
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public IEnumerable<Claim> DecodeJWT(string token) {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.HashText(this._envVar.JWT_SECRET)));
            var tokenHandler = new JwtSecurityTokenHandler();
            _ = tokenHandler.ValidateToken(token, new TokenValidationParameters() {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = this._app.AppName,
                ValidAudience = this._envVar.JWT_AUDIENCE
            }, out SecurityToken validateToken);

            var jwtToken = (JwtSecurityToken) validateToken;
            return jwtToken.Claims;
        }

        private async Task<RSA> GenerateAndLoadRsa() {
            var rsa = RSA.Create();
            rsa.KeySize = 4096;

            if (!File.Exists(this.privKeyPath)) {
                string privateKey = rsa.ToXmlString(true);
                await File.WriteAllTextAsync(this.privKeyPath, privateKey);

                string publicKey = rsa.ToXmlString(false);
                await File.WriteAllTextAsync(this.pubKeyPath, publicKey);

                return rsa;
            }

            string privateKeyString = await File.ReadAllTextAsync(this.privKeyPath);
            rsa.FromXmlString(privateKeyString);

            return rsa;
        }

        private async Task<string> RsaSign(Func<SHA256, RSAPKCS1SignatureFormatter, Task<string>> callback) {
            using (var alg = SHA256.Create()) {
                using (RSA rsa = await this.GenerateAndLoadRsa()) {
                    var rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
                    rsaFormatter.SetHashAlgorithm(nameof(SHA256));
                    return await callback(alg, rsaFormatter);
                }
            }
        }

        public async Task<string> SignFile(string filePath) {
            return await this.RsaSign(async (alg, rsaFormatter) => {
                using (FileStream stream = File.OpenRead(filePath)) {
                    byte[] hash = await alg.ComputeHashAsync(stream);
                    byte[] signHash = rsaFormatter.CreateSignature(hash);
                    return signHash.ToStringHex();
                }
            });
        }

        public async Task<string> SignByte(byte[] data) {
            return await this.RsaSign(async (alg, rsaFormatter) => {
                byte[] hash = alg.ComputeHash(data);
                byte[] signHash = rsaFormatter.CreateSignature(hash);
                string signedHash = signHash.ToStringHex();
                return await Task.FromResult(signedHash);
            });
        }

        public async Task<string> SignText(string textMessage) {
            byte[] data = Encoding.UTF8.GetBytes(textMessage);
            return await this.SignByte(data);
        }

        private async Task<bool> RsaVerify(Func<SHA256, RSAPKCS1SignatureDeformatter, Task<bool>> callback) {
            using (var alg = SHA256.Create()) {
                using (RSA rsa = await this.GenerateAndLoadRsa()) {
                    var rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                    rsaDeformatter.SetHashAlgorithm(nameof(SHA256));
                    return await callback(alg, rsaDeformatter);
                }
            }
        }

        public async Task<bool> VerifyFile(string signature, string filePath) {
            return await this.RsaVerify(async (alg, rsaDeformatter) => {
                using (FileStream stream = File.OpenRead(filePath)) {
                    byte[] hash = await alg.ComputeHashAsync(stream);
                    byte[] signHash = signature.ParseHexTextToByte();
                    return rsaDeformatter.VerifySignature(hash, signHash);
                }
            });
        }

        public async Task<bool> VerifyByte(string signature, byte[] data) {
            return await this.RsaVerify(async (alg, rsaDeformatter) => {
                byte[] hash = alg.ComputeHash(data);
                byte[] signHash = signature.ParseHexTextToByte();
                bool isVerified = rsaDeformatter.VerifySignature(hash, signHash);
                return await Task.FromResult(isVerified);
            });
        }

        public async Task<bool> VerifyText(string signature, string textMessage) {
            byte[] data = Encoding.UTF8.GetBytes(textMessage);
            return await this.VerifyByte(signature, data);
        }

    }

}
