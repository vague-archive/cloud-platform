namespace Void.Platform.Lib;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public static class Crypto
{
  //-----------------------------------------------------------------------------------------------

  public static string UTF8Encode(byte[] bytes)
  {
    return Encoding.UTF8.GetString(bytes);
  }

  public static byte[] UTF8Decode(string value)
  {
    return Encoding.UTF8.GetBytes(value);
  }

  //-----------------------------------------------------------------------------------------------

  public static string Base64Encode(string value)
  {
    return Base64Encode(UTF8Decode(value));
  }

  public static string Base64Encode(byte[] bytes)
  {
    return Convert.ToBase64String(bytes);
  }

  public static byte[] Base64Decode(string value)
  {
    return Convert.FromBase64String(value);
  }

  //-----------------------------------------------------------------------------------------------

  public static uint Crc32(string identifier)
  {
    byte[] data = UTF8Decode(identifier);
    return System.IO.Hashing.Crc32.HashToUInt32(data);
  }

  //-----------------------------------------------------------------------------------------------

  public static byte[] Sha256(string value)
  {
    return Sha256(UTF8Decode(value));
  }

  public static byte[] Sha256(byte[] bytes)
  {
    using (var sha256 = SHA256.Create())
    {
      return sha256.ComputeHash(bytes);
    }
  }

  //-----------------------------------------------------------------------------------------------

  public static byte[] HmacSha256(string value, string key)
  {
    return HmacSha256(UTF8Decode(value), key);
  }

  public static byte[] HmacSha256(byte[] bytes, string key)
  {
    using var hmac = new HMACSHA256(UTF8Decode(key));
    return hmac.ComputeHash(bytes);
  }

  //-----------------------------------------------------------------------------------------------

  public static byte[] MD5(string value)
  {
    return MD5(UTF8Decode(value));
  }

  public static byte[] MD5(byte[] bytes)
  {
    using var md5 = System.Security.Cryptography.MD5.Create();
    return md5.ComputeHash(bytes);
  }

  //-----------------------------------------------------------------------------------------------

  public static byte[] Blake3(string value)
  {
    return Blake3(UTF8Decode(value));
  }

  public static byte[] Blake3(byte[] bytes)
  {
    return global::Blake3.Hasher.Hash(bytes).AsSpan().ToArray();
  }

  //-----------------------------------------------------------------------------------------------

  public static byte[] RandomBytes(int size)
  {
    return RandomNumberGenerator.GetBytes(size);
  }

  public static string HexString(byte[] bytes)
  {
    return Convert.ToHexString(bytes).ToLower();
  }

  //-----------------------------------------------------------------------------------------------

  public static string GenerateToken(string? value = null, int size = 32 /* 256 bits */)
  {
    if (value is not null)
      return EncodeToken(UTF8Decode(value));
    else
      return EncodeToken(RandomBytes(size));
  }

  public static string HashToken(string token)
  {
    return HexString(Sha256(DecodeToken(token)));
  }

  public static bool VerifyToken(string token, string digest)
  {
    return digest == HashToken(token);
  }

  private static string EncodeToken(byte[] bytes)
  {
    // in order for tokens to be URL safe, they need to be b64 encoded PLUS...
    return Base64Encode(bytes)
      .Replace("+", "-") // replace '+' with '-'
      .Replace("/", "_") // replace '/' with '_'
      .TrimEnd('=');     // remove trailing '='
  }

  private static byte[] DecodeToken(string value) // reverse EncodeToken
  {
    var b64 = value
      .Replace("_", "/")
      .Replace("-", "+");
    switch (b64.Length % 4)
    {
      case 2: b64 += "=="; break;
      case 3: b64 += "="; break;
      case 0: break;
    }
    return Base64Decode(b64);
  }

  //-----------------------------------------------------------------------------------------------

  public static (string, string) GeneratePkceVerifier(int size = 64 /* 512 bytes */)
  {
    var verifier = PkceEncode(RandomBytes(size));
    var challenge = PkceEncode(Sha256(UTF8Decode(verifier)));
    return (verifier, challenge);
  }

  public static string PkceEncode(byte[] bytes)
  {
    return Base64Encode(bytes) // base64 encoding, but also...
      .Replace("+", "-") // replace '+' with '-'
      .Replace("/", "_") // replace '/' with '_'
      .TrimEnd('=');     // remove trailing '='
  }

  //-----------------------------------------------------------------------------------------------

  public class PasswordHasher
  {
    private readonly int Iterations = 100000;
    private readonly int SaltSize = 16; // 128-bit salt
    private readonly int KeySize = 32;  // 256-bit subkey

    public string Hash(string password)
    {
      // generate a random salt
      var salt = RandomBytes(SaltSize);

      // generate the hash
      var hash = pbkdf2(password, salt);

      // combine the salt and hash
      var hashBytes = new byte[SaltSize + KeySize];
      Buffer.BlockCopy(salt, 0, hashBytes, 0, SaltSize);
      Buffer.BlockCopy(hash, 0, hashBytes, SaltSize, KeySize);

      // base64 encoded the result
      return Base64Encode(hashBytes);
    }

    public bool Verify(string password, string storedHash)
    {
      var bytes = Base64Decode(storedHash);

      // extract the stored salt
      var salt = new byte[SaltSize];
      Buffer.BlockCopy(bytes, 0, salt, 0, SaltSize);

      // extract the stored password hash
      var passwordHash = new byte[KeySize];
      Buffer.BlockCopy(bytes, SaltSize, passwordHash, 0, KeySize);

      // hash the input password with the same salt
      var computedHash = pbkdf2(password, salt);

      // compare the computed hash with the stored hash
      return CryptographicOperations.FixedTimeEquals(computedHash, passwordHash);
    }

    private byte[] pbkdf2(string password, byte[] salt)
    {
      using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA512);
      return pbkdf2.GetBytes(KeySize);
    }
  }

  //-----------------------------------------------------------------------------------------------

  public class Encryptor
  {
    public const int KeyLength = 32; // 256-bit encryption key
    public const int TagLength = 16; // 128-bit authentication tag
    public const int IvLength = 12; // 96-bit IV/nonce

    //---------------------------------------------------------------------------------------------

    private AesGcm aes;

    public Encryptor(string encryptionKey)
    {
      aes = new AesGcm(Base64Decode(encryptionKey), TagLength);
    }

    //---------------------------------------------------------------------------------------------

    public static string GenerateKey(int length = KeyLength)
    {
      return Base64Encode(RandomBytes(length));
    }

    //---------------------------------------------------------------------------------------------

    public string? Encrypt(string? plainText)
    {
      if (plainText is null)
      {
        return null;
      }
      else
      {
        return EncryptV1(plainText);
      }
    }

    public string? Decrypt(string? encryptedText)
    {
      if (encryptedText is null)
      {
        return null;
      }
      else if (encryptedText.StartsWith("v1::"))
      {
        return DecryptV1(encryptedText);
      }
      else if (encryptedText.Contains("::"))
      {
        // our legacy application base64 encoded the ciphertext and iv before concatenating them with a "::" separator
        return DecryptLegacy(encryptedText);
      }
      else
      {
        throw new Exception($"unexpected encrypted value {encryptedText}");
      }
    }

    //---------------------------------------------------------------------------------------------

    private string EncryptV1(string plainText)
    {
      byte[] iv = RandomNumberGenerator.GetBytes(IvLength);
      byte[] tag = new byte[TagLength];
      byte[] plaintextBytes = UTF8Decode(plainText);
      byte[] ciphertext = new byte[plaintextBytes.Length];

      aes.Encrypt(iv, plaintextBytes, ciphertext, tag);

      byte[] combined = new byte[ciphertext.Length + tag.Length + iv.Length];
      ciphertext.CopyTo(combined, 0);
      tag.CopyTo(combined, ciphertext.Length);
      iv.CopyTo(combined, ciphertext.Length + tag.Length);

      return $"v1::{Base64Encode(combined)}";
    }

    private string DecryptV1(string encryptedText)
    {
      RuntimeAssert.True(encryptedText.StartsWith("v1::"));

      byte[] encryptedBytes = Base64Decode(encryptedText.Substring(4));

      int ciphertextLength = encryptedBytes.Length - TagLength - IvLength;
      if (ciphertextLength < 0)
        throw new ArgumentException("Invalid encrypted data length");

      byte[] ciphertext = new byte[ciphertextLength];
      byte[] tag = new byte[TagLength];
      byte[] iv = new byte[IvLength];

      Array.Copy(encryptedBytes, 0, ciphertext, 0, ciphertextLength);
      Array.Copy(encryptedBytes, ciphertextLength, tag, 0, TagLength);
      Array.Copy(encryptedBytes, ciphertextLength + TagLength, iv, 0, IvLength);

      byte[] plaintextBytes = new byte[ciphertextLength];

      aes.Decrypt(iv, ciphertext, tag, plaintextBytes);

      return UTF8Encode(plaintextBytes);
    }

    //---------------------------------------------------------------------------------------------

    private string DecryptLegacy(string encryptedText)
    {
      string[] parts = encryptedText.Split("::");
      RuntimeAssert.True(parts.Length == 2);
      string ciphertext = parts[0];
      string iv = parts[1];
      return DecryptLegacy(ciphertext, iv);
    }

    private string DecryptLegacy(string ciphertext, string iv)
    {
      byte[] ciphertextBytes = Base64Decode(ciphertext);
      byte[] ivBytes = Base64Decode(iv);
      byte[] combined = new byte[ciphertextBytes.Length + ivBytes.Length];
      ciphertextBytes.CopyTo(combined, 0);
      ivBytes.CopyTo(combined, ciphertextBytes.Length);
      return Decrypt($"v1::{Base64Encode(combined)}")!;
    }
  }

  //-----------------------------------------------------------------------------------------------

  public class JwtGenerator
  {
    static JwtGenerator()
    {
      JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
    }

    public const string Issuer = "https://void.dev";
    public const string Audience = "platform";

    private IClock Clock { get; init; }
    private SymmetricSecurityKey SigningKey { get; init; }
    private SigningCredentials SigningCreds { get; init; }
    private TokenValidationParameters ValidationCreds { get; init; }

    public JwtGenerator(string signingKey, IClock clock)
    {
      Clock = clock;
      SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
      SigningCreds = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256);
      ValidationCreds = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = SigningKey,
        ClockSkew = TimeSpan.FromSeconds(30),
      };
    }

    public string Create(ClaimsPrincipal principal)
    {
      return Create(principal.Claims, out var token);
    }

    public string Create(ClaimsPrincipal principal, out JwtSecurityToken token)
    {
      return Create(principal.Claims, out token);
    }

    public string Create(IEnumerable<Claim> claims)
    {
      return Create(claims, out var token);
    }

    public string Create(IEnumerable<Claim> claims, out JwtSecurityToken token)
    {
      var now = Clock.Now;
      var expires = now.Plus(Duration.FromHours(24));
      token = new JwtSecurityToken(
        issuer: Issuer,
        audience: Audience,
        notBefore: now.ToDateTimeUtc(),
        expires: expires.ToDateTimeUtc(),
        claims: claims,
        signingCredentials: SigningCreds
      );
      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public List<Claim>? Verify(string jwt)
    {
      try
      {
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(jwt, ValidationCreds, out var securityToken);
        return principal.Claims.ToList();
      }
      catch (SecurityTokenSignatureKeyNotFoundException)
      {
        return null;
      }
      catch (SecurityTokenExpiredException)
      {
        return null;
      }
    }
  }

  public static bool LooksLikeJwt(string value)
  {
    var parts = value.Split(".");
    return (parts.Length == 3 &&
      parts[0].Length > 1 &&
      parts[1].Length > 1 &&
      parts[2].Length > 1);
  }

  //-----------------------------------------------------------------------------------------------
}