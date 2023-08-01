using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PeterO.Cbor;

namespace Framework.Caspar
{
    public static partial class Api
    {
        public static class PasskeyHelper
        {
            public static void ParseAttestationObject(byte[] attestationObject)
            {
                var cborObject = CBORObject.DecodeFromBytes(attestationObject);


                //   var fff1 = cborObject["authData"]["attestedCredentialData"];

                var bytes = cborObject["authData"].GetByteString();
                var rpIdHash = bytes[0..32];
                var flags = bytes[32];
                var signCount = BitConverter.ToUInt32(bytes[33..37]);
                var attestedCredentialData = bytes[37..];

                var aaguid = attestedCredentialData[0..16];

                var bigEndianBytes = attestedCredentialData[16..18];
                Array.Reverse(bigEndianBytes);
                var len = BitConverter.ToUInt16(bigEndianBytes);
                var credentialId = attestedCredentialData[18..(18 + len)];
                var credentialPublicKey = attestedCredentialData[(18 + len)..];


                var fffffff = new ReadOnlySpan<byte>(credentialPublicKey);

                var ffffffff = fffffff.Slice(22);

                string publicKeyPem = "-----BEGIN PUBLIC KEY-----\n" +
                      "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEzMODOVZ5cR1t9MA2DD2uVQB6nRu" +
                      "16aICz1MQ3+Sz88JWpXHpvOXztZ2CZ6/rtgA4zpNdxHcJGsicSWkj2kgc1Q==" +
                      "\n-----END PUBLIC KEY-----";

                byte[] publicKeyBytes = Encoding.ASCII.GetBytes(publicKeyPem);

                //           MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAETHfi8foQF4UtSNVxSFxeu7W+gMxdSGElhdo7825SD3Lyb+Sqh4G6Kra0ro1BdrM6Qx+hsUx4Qwdby7QY0pzxyA=
                var pk = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEeFBNETXOk1dGYfcd89DdNn11s3sOXx4f2mUmAJxUPCIuTa55ajvtA0RHhXtRjpiFCmFpsC75thJdSHwpdS0tQg==";
                //var pk = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEnfChCacbQqUqZr8PRo4C0O5YyT92OdXNFIp8FoTyuswvzP5i6KaR8peWhN4iV9MflYFEON0L/MHs5hnZZpvC7A==";

                var prvKey = ECDsa.Create();
                var fff = prvKey.ExportPkcs8PrivateKey().ToBase64String();
                var dddd = prvKey.ExportSubjectPublicKeyInfo().ToBase64String();
                prvKey.ImportSubjectPublicKeyInfo(dddd.FromBase64ToBytes(), out _);


                var ecdsa = System.Security.Cryptography.ECDsaCng.Create(ECCurve.NamedCurves.nistP256);
                ecdsa.ImportSubjectPublicKeyInfo(pk.FromBase64ToBytes(), out _);

                var credential = new { challenge = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=", signature = "MEUCIE62qhZh69oomSHr0a25S0BB7fICt9IewKkxsSLYknTWAiEA7OlWqM7Gi7th/FkM+7JynoKdEPXb06fG5dKtdwleKn8=", publicKey = "o2NmbXRmcGFja2VkZ2F0dFN0bXSiY2FsZyZjc2lnWEgwRgIhAJCDCy4mX9boquVCf4fU4p6jbyA04wBftfzinHJ5MSaRAiEAqee6gyYNO9FWTJ6l+ASU3V3w07O6mTzBlhJemh9OW5VoYXV0aERhdGFYpEmWDeWIDoxodDQXD2R2YFuP5K65ooYyx5lc87qDHZdjRQAAAACtzgACNbzGCmSLCyXx8FUDACC2J3NsyaPtCXL1Pf5cOv5ebc5u4HNwLM3uAOBSFkJ1IKUBAgMmIAEhWCC8eT8H9JX3X8B3ZKX7G1GRy9KEoDA4ra8NwUJRS9FSBiJYIA==" };
                var clientData = "SZYN5YgOjGh0NBcPZHZgW4/krrmihjLHmVzzuoMdl2MFAAAAADehUq9laWCJTXsyzILYHd5/dTQgzPlYPjovmEhqnjSz";

                byte[] challengeBytes = Convert.FromBase64String(credential.challenge.Replace('-', '+').Replace('_', '/'));
                byte[] signatureBytes = Convert.FromBase64String(credential.signature.Replace('-', '+').Replace('_', '/'));
                byte[] data = Convert.FromBase64String(clientData.Replace('-', '+').Replace('_', '/'));


                bool isSignatureValid = ecdsa.VerifyData(data, signatureBytes, HashAlgorithmName.SHA256);



                ECDsaCng ecsdKey = new ECDsaCng(CngKey.Import(Convert.FromBase64String(pk), CngKeyBlobFormat.EccPublicBlob));

                var rsa = new System.Security.Cryptography.RSACryptoServiceProvider(2048);
                rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyPem), out _);

                RSA publicKey = RSA.Create();
                publicKey.ImportFromPem(publicKeyPem);

                //                         "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAErNV6M8IqjJsycVOgJQe0DzzujZ24WUtB25nCxb25dfhUnD3e2C9gMzmL21ydZlR72INOLp75x4qqe1//me3aAQ=="

                // var pk = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEzMODOVZ5cR1t9MA2DD2uVQB6nRu16aICz1MQ3+Sz88JWpXHpvOXztZ2CZ6/rtgA4zpNdxHcJGsicSWkj2kgc1Q==";
                // string publicKeyPem = "-----BEGIN PUBLIC KEY-----\n" + pk + "\n-----END PUBLIC KEY-----";

                // var ppk = Convert.FromBase64String(pk.Replace('-', '+').Replace('_', '/'));
                // publicKey.ImportSubjectPublicKeyInfo(ppk, out _);



                //   var kkk = Encoding.UTF8.GetString(credentialPublicKey);



                //  cborObject["authData"][]


                var attestation = new AttestationObject
                {
                    Format = cborObject["fmt"].AsString(),
                    AttestationStatement = new AttestationStatement
                    {
                        Format = cborObject["attStmt"]["fmt"].AsString(),
                        AttStmt = cborObject["attStmt"].EncodeToBytes()
                    },

                    AuthenticatorData = new AuthenticatorData
                    {
                        // RpIdHash = cborObject["authData"].GetByteString(0, 32),
                        // Flags = cborObject["authData"][32],
                        // SignCount = cborObject["authData"].GetUInt32(33),
                        // AttestedCredentialData = cborObject["authData"].GetByteString(37)
                    }
                };
                // Do something with the attestation object
            }
            public static void ParseAttestationObject(string base64)
            {
                // var attestationObject = Convert.FromBase64String(base64);
                // var cborObject = CBORObject.DecodeFromBytes(attestationObject);
                // var attestation = new AttestationObject
                // {
                //     Format = cborObject["fmt"].AsString(),
                //     AttestationStatement = new AttestationStatement
                //     {
                //         Format = cborObject["attStmt"]["fmt"].AsString(),
                //         AttStmt = cborObject["attStmt"].EncodeToBytes()
                //     },
                //     AuthenticatorData = new AuthenticatorData
                //     {
                //         RpIdHash = cborObject["authData"]..GetByteString(0, 32),
                //         Flags = cborObject["authData"][32],
                //         SignCount = cborObject["authData"].GetUInt32(33),
                //         AttestedCredentialData = cborObject["authData"].GetByteString(37)
                //     }
                // };
                // Do something with the attestation object
            }
        }

        public class AttestationObject
        {
            public string Format { get; set; }
            public AttestationStatement AttestationStatement { get; set; }
            public AuthenticatorData AuthenticatorData { get; set; }
        }

        public class AttestationStatement
        {
            public string Format { get; set; }
            public byte[] AttStmt { get; set; }
        }

        public class AuthenticatorData
        {
            public byte[] RpIdHash { get; set; }
            public byte Flags { get; set; }
            public uint SignCount { get; set; }
            public byte[] AttestedCredentialData { get; set; }
        }
    }
}