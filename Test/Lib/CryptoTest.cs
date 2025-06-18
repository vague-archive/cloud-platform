namespace Void.Platform.Lib;

using System.Security.Claims;

public class CryptoTest : TestCase
{
  [Fact]
  public void TestFoo()
  {
    var token = "AD5fem3V2SEwodyNficEssRLIcvnuhRSHfbZHvhm8B8=";
    var digest = Crypto.HashToken(token);
    var expectedDigest = "45848a5097fbadb8c2ee3fe21767cb7f9c30de82c1419e2f69b877cbe98b019d";
    Assert.Equal(expectedDigest, digest);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestUTF8Encoding()
  {
    Assert.Equal("hello", Crypto.UTF8Encode(new byte[] { 104, 101, 108, 108, 111 }));
    Assert.Equal("world", Crypto.UTF8Encode(new byte[] { 119, 111, 114, 108, 100 }));
    Assert.Equal(new byte[] { 104, 101, 108, 108, 111 }, Crypto.UTF8Decode("hello"));
    Assert.Equal(new byte[] { 119, 111, 114, 108, 100 }, Crypto.UTF8Decode("world"));

    Assert.Equal(new byte[] { 195, 168 }, Crypto.UTF8Decode("è"));
    Assert.Equal("è", Crypto.UTF8Encode(new byte[] { 195, 168 }));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestBase64Encoding()
  {
    Assert.Equal("aGVsbG8=", Crypto.Base64Encode(Crypto.UTF8Decode("hello")));
    Assert.Equal("d29ybGQ=", Crypto.Base64Encode(Crypto.UTF8Decode("world")));
    Assert.Equal("hello", Crypto.UTF8Encode(Crypto.Base64Decode("aGVsbG8=")));
    Assert.Equal("world", Crypto.UTF8Encode(Crypto.Base64Decode("d29ybGQ=")));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestCrc32()
  {
    Assert.Equal(2266713815U, Crypto.Crc32("jake"));
    Assert.Equal(1339991159U, Crypto.Crc32("amy"));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestSha256()
  {
    Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", Crypto.HexString(Crypto.Sha256("hello")));
    Assert.Equal("486ea46224d1bb4fb680f34f7c9ad96a8f24ec88be73ea8e5a6c65260e9cb8a7", Crypto.HexString(Crypto.Sha256("world")));
  }

  [Fact]
  public void TestHmacSha256()
  {
    Assert.Equal("734cc62f32841568f45715aeb9f4d7891324e6d948e4c6c60c0621cdac48623a", Crypto.HexString(Crypto.HmacSha256("hello world", "secret")));
    Assert.Equal("646b8eac0c4ae4178299c9bd924e2bf073dfb07e024901a8c864f65b9462bf0d", Crypto.HexString(Crypto.HmacSha256("token", "key")));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestMD5()
  {
    Assert.Equal("b10a8db164e0754105b7a99be72e3fe5", Crypto.HexString(Crypto.MD5("Hello World")));
    Assert.Equal("b0720da0c837f4ce30312682e467d08a", Crypto.HexString(Crypto.MD5("Yolo")));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestBlake3()
  {
    Assert.Equal("41f8394111eb713a22165c46c90ab8f0fd9399c92028fd6d288944b23ff5bf76", Crypto.HexString(Crypto.Blake3("Hello World")));
    Assert.Equal("5342397cf3300914b3a26595fa74115d2020cc209b489d9f4c794d7e64bd5b2b", Crypto.HexString(Crypto.Blake3("Yolo")));
    Assert.Equal("f890484173e516bfd935ef3d22b912dc9738de38743993cfedf2c9473b3216a4", Crypto.HexString(Crypto.Blake3("BLAKE3")));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestHexString()
  {
    Assert.Equal("0102030405060708090a0b0c0d0e0f1011121314", Crypto.HexString(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGenerateToken()
  {
    string token = Crypto.GenerateToken();
    string longer = Crypto.GenerateToken(size: 64);
    Assert.Equal(43, token.Length); // 32 bytes plus base64 encoding overhead
    Assert.Equal(86, longer.Length); // 64 bytes plus base64 encoding overhead
  }

  [Fact]
  public void TestTokensAreUnique()
  {
    var t1 = Crypto.GenerateToken();
    var t2 = Crypto.GenerateToken();
    var t3 = Crypto.GenerateToken();
    Assert.NotEqual(t1, t2);
    Assert.NotEqual(t1, t3);
    Assert.NotEqual(t2, t3);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestPkceVerifier()
  {
    var (verifier, challenge) = Crypto.GeneratePkceVerifier();
    Assert.True(verifier.Length > 43);
    Assert.True(verifier.Length < 128);
    Assert.DoesNotContain("+", verifier);
    Assert.DoesNotContain("/", verifier);
    Assert.DoesNotContain("=", verifier);
    Assert.DoesNotContain("+", challenge);
    Assert.DoesNotContain("/", challenge);
    Assert.DoesNotContain("=", challenge);
    Assert.Equal(challenge, Crypto.PkceEncode(Crypto.Sha256(verifier)));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestHashToken()
  {
    var t1 = Crypto.GenerateToken("first-token");
    var t2 = Crypto.GenerateToken("second-token");
    var d1 = Crypto.HashToken(t1);
    var d2 = Crypto.HashToken(t2);

    var expected1 = Crypto.HexString(Crypto.Sha256("first-token"));
    var expected2 = Crypto.HexString(Crypto.Sha256("second-token"));

    Assert.Equal(expected1, d1);
    Assert.Equal(expected2, d2);

    Assert.NotEqual(t1, d1);
    Assert.NotEqual(t2, d2);

    Assert.True(Crypto.VerifyToken(t1, d1));
    Assert.True(Crypto.VerifyToken(t2, d2));

    Assert.False(Crypto.VerifyToken(t1, d2));
    Assert.False(Crypto.VerifyToken(t2, d1));
  }

  [Fact]
  public void TestHashTokenIsCompatibleWithLegacyApplication()
  {
    var token = Crypto.GenerateToken("jake");
    var digest = Crypto.HashToken(token);
    var expected = "cdf30c6b345276278bedc7bcedd9d5582f5b8e0c1dd858f46ef4ea231f92731d";
    Assert.Equal(expected, digest);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestPasswordHasher()
  {
    var password = "Battery Horse Staple";
    var hasher = new Crypto.PasswordHasher();
    var hash = hasher.Hash(password);

    Assert.True(hasher.Verify(password, hash));
    Assert.False(hasher.Verify("", hash));
    Assert.False(hasher.Verify("password", hash));
    Assert.False(hasher.Verify("yolo", hash));
    Assert.False(hasher.Verify(password.ToLower(), hash));
    Assert.False(hasher.Verify(password.ToUpper(), hash));
    Assert.False(hasher.Verify(password + " ", hash));
    Assert.False(hasher.Verify(" " + password, hash));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestGenerateEncryptionKey()
  {
    var key = Crypto.Encryptor.GenerateKey();
    Assert.Equal(44, key.Length); // 32 byte key plus base64 encoding overhead
  }

  [Fact]
  public void TestEncryptAndDecrypt()
  {
    var key = Crypto.Encryptor.GenerateKey();
    var encryptor = new Crypto.Encryptor(key);
    var source = "Shhh, this is a secret!";
    var encrypted = encryptor.Encrypt(source);
    var target = encryptor.Decrypt(encrypted);
    Assert.Equal(source, target);
    Assert.NotEqual(source, encrypted);
    Assert.NotEqual(target, encrypted);
    Assert.StartsWith("v1::", encrypted);
    Assert.Null(encryptor.Encrypt(null));
    Assert.Null(encryptor.Decrypt(null));
  }

  [Fact]
  public void TestEncryptAndDecryptWithDefaultEncryptionKey()
  {
    var key = Web.Config.DefaultEncryptKey;
    var encryptor = new Crypto.Encryptor(key);
    var source = "This is also a secret";
    var encrypted = encryptor.Encrypt(source);
    var target = encryptor.Decrypt(encrypted);
    Assert.Equal(source, target);
    Assert.NotEqual(source, encrypted);
    Assert.NotEqual(target, encrypted);
  }

  [Fact]
  public void TestDecryptValuesFromLegacyApplication()
  {
    // these values were generated by the legacy Deno application crypto.encrypt()
    var ciphertext = "NrAgJCqhYIhpyIztagRQOAqTVIlwwfs9u3zrrSEfO75KjGNwTFqq";
    var iv = "dfAvpfsY1PgCvwSH";
    var combined = $"{ciphertext}::{iv}";

    // we need to ensure we can still decrypt them in our .NET application
    var legacyTestEncryptKey = "x+eLvtCTh0dXznoyLt3gOGtGZWrvBmlz0u1Qqd1qmMU=";
    var encryptor = new Crypto.Encryptor(legacyTestEncryptKey);
    var target = encryptor.Decrypt(combined);
    Assert.Equal("Shhh, this is a secret!", target);
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestLooksLikeJwt()
  {
    Assert.True(Crypto.LooksLikeJwt("header.payload.signature"));
    Assert.True(Crypto.LooksLikeJwt("foo.bar.baz"));
    Assert.False(Crypto.LooksLikeJwt(""));
    Assert.False(Crypto.LooksLikeJwt("foo.bar"));
    Assert.False(Crypto.LooksLikeJwt("foo.bar.baz.bonus"));
    Assert.False(Crypto.LooksLikeJwt("foo..bar"));
    Assert.False(Crypto.LooksLikeJwt("."));
    Assert.False(Crypto.LooksLikeJwt(".."));
    Assert.False(Crypto.LooksLikeJwt("..."));
    Assert.False(Crypto.LooksLikeJwt("...."));
    Assert.False(Crypto.LooksLikeJwt("abc"));
    Assert.False(Crypto.LooksLikeJwt("abcdefghijklmnopqrstuvwxyz"));
    Assert.False(Crypto.LooksLikeJwt("abc.def.ghi.jkl.mno.pqr.stu.vwx.yz"));
    Assert.False(Crypto.LooksLikeJwt(Crypto.GenerateToken()));
    Assert.False(Crypto.LooksLikeJwt(Crypto.GenerateToken()));
    Assert.False(Crypto.LooksLikeJwt(Crypto.GenerateToken()));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestJwt()
  {
    var expectedNBF = Clock.Now.ToUnixTimeSeconds();
    var expectedEXP = expectedNBF + (24 * 60 * 60);

    var signingKey = Fake.SigningKey();
    var generator = new Crypto.JwtGenerator(signingKey, Clock);
    var payload = new Claim[] {
      new Claim("sub", "123"),
      new Claim("name", "Jake Gordon"),
      new Claim("email", "jake@void.dev"),
    };

    var jwt = generator.Create(payload, out var token);
    Assert.LooksLikeJwt(jwt);

    Assert.Equal([
      "alg:HS256",
      "typ:JWT",
    ], token.Header.Select(h => $"{h.Key}:{h.Value}"));

    Assert.Equal([
      "sub:123",
      "name:Jake Gordon",
      "email:jake@void.dev",
      $"nbf:{expectedNBF}",
      $"exp:{expectedEXP}",
      "iss:https://void.dev",
      "aud:platform",
    ], token.Claims.Select(c => $"{c.Type}:{c.Value}"));

    var verifiedClaims = generator.Verify(jwt);
    Assert.Present(verifiedClaims);

    Assert.Equal([
      "sub:123",
      "name:Jake Gordon",
      "email:jake@void.dev",
      $"nbf:{expectedNBF}",
      $"exp:{expectedEXP}",
      "iss:https://void.dev",
      "aud:platform",
    ], verifiedClaims.Select(c => $"{c.Type}:{c.Value}"));

    var oldSigningKey = Fake.SigningKey();
    var oldGenerator = new Crypto.JwtGenerator(oldSigningKey, Clock);
    Assert.Absent(oldGenerator.Verify(jwt));
  }

  //-----------------------------------------------------------------------------------------------

  [Fact]
  public void TestVerifyExpiredJWT()
  {
    var aLongTimeAgo = new TestClock(Moment.From(1999, 12, 31));

    var signingKey = Fake.SigningKey();
    var generator = new Crypto.JwtGenerator(signingKey, aLongTimeAgo);
    var payload = new Claim[] {
      new Claim("sub", "123"),
      new Claim("name", "Jake Gordon"),
      new Claim("email", "jake@void.dev"),
    };

    var jwt = generator.Create(payload, out var token);
    Assert.LooksLikeJwt(jwt);

    // preconditions
    var expectedEXP = aLongTimeAgo.Now.ToUnixTimeSeconds() + (24 * 60 * 60);
    Assert.Equal($"{expectedEXP}", token.Claims.First(c => c.Type == "exp").Value);
    Assert.True(Clock.Now.ToUnixTimeSeconds() > expectedEXP);

    var verifiedClaims = generator.Verify(jwt);
    Assert.Absent(verifiedClaims);
  }

  //-----------------------------------------------------------------------------------------------
}