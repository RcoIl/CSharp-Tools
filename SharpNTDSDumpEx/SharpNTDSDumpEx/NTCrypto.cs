/*
 * @Author: RcoIl
 * @Date: 2020/5/26 14:40:34
*/
using SharpNTDSDumpEx.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SharpNTDSDumpEx
{
    internal static class NTCrypto
    {

        private enum EncryptionType : ushort
        {
            PekWithRc4AndSalt = 17,
            PekWithAes = 19,
        }
        private enum PekListFlags : uint
        {
            ClearText = 0,
            Encrypted = 1,
        }

        private enum PekListVersion : uint
        {
            Windowsunk = 1,
            Windows2003 = 2,
            Windows2016 = 3,
        }

        /// <summary>
        /// Decypts hashes from a NTDS column such as dBCSPwd (LM) or unicodePwd (NT)
        /// History hashes use the same format, but with additional 16 byte hashes appended
        /// Data is excepted as follows
        /// |........|................|................|................|
        ///   ^- Header 8 bytes (Algorithm ID (2b), Flags (2b), PEK ID (4b))
        ///            ^- Salt 16 bytes
        ///                             ^- Encrypted hash 16 bytes
        ///                                               ^- Optional additional encrypted 16 byte hashes
        ///
        /// All data after the salt is first decrypted using the PEK. Each 16 byte hash is then decrypted using keys generated from the RID.
        /// </summary>
        /// <param name="pekList">The PEKs.</param>
        /// <param name="encryptedHashBlob">The encrypted 40 byte blob, consisting of header, salt, and hash. Every additional 16 bytes should be an additional hash.</param>
        /// <param name="rid">The RID of the related account.</param>
        /// <returns>The decrypted 16 byte hash. Every additional 16 bytes will be an additional hash.</returns>
        public static byte[] DecryptHashes(Dictionary<uint, byte[]> pekList, byte[] encryptedHashBlob, uint rid)
        {
            var decryptedData = DecryptSecret(pekList, encryptedHashBlob);

            var decryptedHashes = new List<byte>();
            for (var i = 0; i < decryptedData.Length; i += 16)
            {
                var key1 = new byte[] { };
                var key2 = new byte[] { };
                RidToKeys(rid, out key1, out key2);
                var decryptedHash = DecryptDataWithKeyPair(key1, key2, decryptedData.Skip(i).Take(16).ToArray());
                decryptedHashes.AddRange(decryptedHash);
            }

            return decryptedHashes.ToArray();
        }

        /// <summary>
        /// Data is excepted as follows
        /// |........|................|.....
        ///   ^- Header 8 bytes (Algorithm ID (2b), Flags (2b), PEK ID (4b))
        ///            ^- Salt 16 bytes
        ///                             ^- Encrypted data
        /// </summary>
        /// <param name="pekList">The PEKs.</param>
        /// <param name="encryptedBlob">The encrypted 40 byte blob, consisting of header, salt, and data.</param>
        /// <returns>The decrypted data</returns>
        public static byte[] DecryptSecret(Dictionary<uint, byte[]> pekList, byte[] encryptedBlob)
        {
            var algorithm = BitConverter.ToUInt16(encryptedBlob, 0);
            if (!Enum.IsDefined(typeof(EncryptionType), algorithm))
            {
                Console.WriteLine($"[!] Algorithm \"{algorithm}\" is not supported.");
            }

            var pekId = BitConverter.ToUInt32(encryptedBlob, 4);
            var pek = pekList[pekId];

            var salt = encryptedBlob.Skip(8).Take(16).ToArray();
            var encryptedData = encryptedBlob.Skip(24).ToArray();

            switch ((EncryptionType)algorithm)
            {
                case EncryptionType.PekWithRc4AndSalt:
                    return DecryptDataUsingRc4AndSalt(pek, salt, encryptedData, 1);

                case EncryptionType.PekWithAes:
                    // When using AES, data is padded and the first 4 bytes contains the actual data length
                    var length = BitConverter.ToUInt32(encryptedData, 0);
                    encryptedData = encryptedData.Skip(4).ToArray();
                    return DecryptDataUsingAes(pek, salt, encryptedData).Take((int)length).ToArray();
                default:
                    throw new ArgumentOutOfRangeException(nameof(encryptedBlob), String.Format($"Encryption type \"{(EncryptionType)algorithm}\" is not supported."));
                    //Console.WriteLine($"[!] Encryption type \"{(EncryptionType)algorithm}\" is not supported.");
            }
        }

        /// <summary>
        /// Decrypts data (such as a single password hash) using DES keys derived from the RID.
        /// Data is excepted as follows
        /// |................|
        ///   ^- Encrypted hash 16 bytes
        /// </summary>
        /// <param name="key1">The first DES key used for decryption. For password hashes, this should be generated from the account RID.</param>
        /// <param name="key2">The second DES key used for decryption. For password hashes, this should be generated from the account RID.</param>
        /// <param name="data">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        private static byte[] DecryptDataWithKeyPair(byte[] key1, byte[] key2, byte[] data)
        {
            var data1 = data.Take(data.Length / 2).ToArray();
            var data2 = data.Skip(data.Length / 2).ToArray();
            var hProv = IntPtr.Zero;
            var hKey1 = IntPtr.Zero;
            var hKey2 = IntPtr.Zero;

            var keyHeader = new NativeMethods.PUBLICKEYSTRUC
            {
                BType = NativeMethods.PLAINTEXTKEYBLOB,
                BVersion = NativeMethods.CURBLOBVERSION,
                Reserved = 0,
                AiKeyAlg = NativeMethods.CALGDES,
            };

            var keyHeaderBytes = StructureToByteArray(keyHeader);

            var keyWithHeader1 = keyHeaderBytes.Concat(BitConverter.GetBytes(data1.Length)).Concat(key1).ToArray();
            var keyWithHeader2 = keyHeaderBytes.Concat(BitConverter.GetBytes(data2.Length)).Concat(key2).ToArray();

            // Get handle to the crypto provider
            if (!NativeMethods.CryptAcquireContext(ref hProv, null, null, NativeMethods.PROVRSAFULL, NativeMethods.CRYPTVERIFYCONTEXT))
            {
                NativeMethods.CryptReleaseContext(hProv, 0);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Import key 1
            if (!NativeMethods.CryptImportKey(hProv, keyWithHeader1, keyWithHeader1.Length, IntPtr.Zero, 0, ref hKey1))
            {
                NativeMethods.CryptReleaseContext(hProv, 0);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Import key 2
            if (!NativeMethods.CryptImportKey(hProv, keyWithHeader2, keyWithHeader2.Length, IntPtr.Zero, 0, ref hKey2))
            {
                NativeMethods.CryptDestroyKey(hKey1);
                NativeMethods.CryptReleaseContext(hProv, 0);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Decrypt first part of hash
            uint pdwDataLen1 = (uint)data1.Length;
            if (!NativeMethods.CryptDecrypt(hKey1, IntPtr.Zero, 0, 0, data1, ref pdwDataLen1))
            {
                NativeMethods.CryptDestroyKey(hKey2);
                NativeMethods.CryptDestroyKey(hKey1);
                NativeMethods.CryptReleaseContext(hProv, 0);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Decrypt second part of hash
            uint pdwDataLen2 = (uint)data2.Length;
            if (!NativeMethods.CryptDecrypt(hKey2, IntPtr.Zero, 0, 0, data2, ref pdwDataLen2))
            {
                NativeMethods.CryptDestroyKey(hKey2);
                NativeMethods.CryptDestroyKey(hKey1);
                NativeMethods.CryptReleaseContext(hProv, 0);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            NativeMethods.CryptDestroyKey(hKey2);
            NativeMethods.CryptDestroyKey(hKey1);
            NativeMethods.CryptReleaseContext(hProv, 0);

            return data1.Concat(data2).ToArray();
        }

        /// <summary>
        /// The format of supplementalCredentials is a USER_PROPERTIES structure (https://msdn.microsoft.com/en-us/library/cc245674.aspx).
        /// USER_PROPERTIES structure is as follows (https://msdn.microsoft.com/en-us/library/cc245500.aspx):
        ///
        /// Reserved1 (4 bytes): This value MUST be set to zero and MUST be ignored by the recipient.
        /// Length(4 bytes): This value MUST be set to the length, in bytes, of the entire structure, starting from the Reserved4 field.
        /// Reserved2(2 bytes): This value MUST be set to zero and MUST be ignored by the recipient.
        /// Reserved3(2 bytes): This value MUST be set to zero and MUST be ignored by the recipient.
        /// Reserved4(96 bytes): This value MUST be ignored by the recipient and MAY contain arbitrary values.
        /// PropertySignature(2 bytes): This field MUST be the value 0x50, in little-endian byte order.This is an arbitrary value used to indicate whether the structure is corrupt.That is, if this value is not 0x50 on read, the structure is considered corrupt, processing MUST be aborted, and an error code MUST be returned.
        /// PropertyCount(2 bytes): The number of USER_PROPERTY elements in the UserProperties field.When there are zero USER_PROPERTY elements in the UserProperties field, this field MUST be omitted; the resultant USER_PROPERTIES structure has a constant size of 0x6F bytes.
        /// UserProperties(variable): An array of PropertyCount USER_PROPERTY elements.
        /// Reserved5(1 byte): This value SHOULD be set to zero and MUST be ignored by the recipient.
        ///
        /// USER_PROPERTY structure is as follows (https://msdn.microsoft.com/en-us/library/cc245501.aspx):
        ///
        /// NameLength (2 bytes): The number of bytes, in little-endian byte order, of PropertyName. The property name is located at an offset of zero bytes just following the Reserved field. For more information, see the message processing section for supplementalCredentials (section 3.1.1.8.11).
        /// ValueLength(2 bytes): The number of bytes contained in PropertyValue.
        /// Reserved(2 bytes): This value MUST be ignored by the recipient and MAY be set to arbitrary values on update.
        /// PropertyName(variable): The name of this property as a UTF-16 encoded string.
        /// PropertyValue(variable): The value of this property.The value MUST be hexadecimal-encoded using an 8-bit character size, and the values '0' through '9' inclusive and 'a' through 'f' inclusive(the specification of 'a' through 'f' is case-sensitive).
        /// </summary>
        /// <param name="pekList">The PEK list.</param>
        /// <param name="encryptedSupplementalCredentialsBlob">The encrypted blob.</param>
        /// <returns>Clear text passwords.</returns>
        public static Dictionary<string, byte[]> DecryptSupplementalCredentials(Dictionary<uint, byte[]> pekList, byte[] encryptedSupplementalCredentialsBlob)
        {
            var decryptedBlob = NTCrypto.DecryptSecret(pekList, encryptedSupplementalCredentialsBlob);

            var properties = new Dictionary<string, byte[]>();

            // Check the property signature is equal to 0x50, and if not assume the structure is corrupt.
            if (decryptedBlob.Length < 110 || BitConverter.ToUInt16(decryptedBlob, 108) != 0x50)
            {
                return properties;
            }

            // If there are zero USER_PROPERTY elements, the length will be 0x6F
            if (decryptedBlob.Length == 0x6F)
            {
                return properties;
            }

            var propertiesCount = BitConverter.ToUInt16(decryptedBlob, 110);
            var propertiesBlob = decryptedBlob.Skip(112).Take(decryptedBlob.Length - 113).ToArray();

            using (var reader = new BinaryReader(new MemoryStream(propertiesBlob)))
            {
                for (var i = 0; i < propertiesCount; i++)
                {
                    var nameLength = reader.ReadUInt16();
                    var valueLength = reader.ReadUInt16();
                    reader.ReadUInt16();
                    var propertyNameBlob = reader.ReadBytes(nameLength);
                    var propertyValueBlob = reader.ReadBytes(valueLength);

                    var propertyName = Encoding.Unicode.GetString(propertyNameBlob);
                    var hexEncodedPropertyValue = Encoding.ASCII.GetString(propertyValueBlob);
                    var propertyValue = Enumerable.Range(0, hexEncodedPropertyValue.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hexEncodedPropertyValue.Substring(x, 2), 16))
                     .ToArray();

                    properties[propertyName] = propertyValue;
                }
            }

            return properties;
        }


        /// <summary>
        /// 从 NTDS pekList 列解密密码加密密钥（PEK）
        /// Data is excepted as follows
        /// |........|................|....................................................|
        ///   ^- Header 8 bytes (Version (4 bytes), Flags (4 bytes))
        ///            ^- Salt 16 bytes
        ///                             ^- Encrypted PEK, varible length depending on number of keys.
        /// </summary>
        /// <param name="systemKey"> 这 16字节的系统密钥，用于解密 PEK.</param>
        /// <param name="encryptedPekListBlob">加密的76字节密码加密密钥.</param>
        /// <returns>16字节的 PEK 明文.</returns>
        public static Dictionary<uint, byte[]> DecryptPekList(byte[] systemKey, byte[] encryptedPekListBlob)
        {
            if (systemKey.Length != 16)
            {
                throw new ArgumentOutOfRangeException(nameof(systemKey));
            }

            var version = BitConverter.ToUInt32(encryptedPekListBlob, 0);
            if (!Enum.IsDefined(typeof(PekListVersion), version))
            {
                Console.WriteLine($"[!] PEK List version \"{version}\" is not supported.");
            }

            var flags = BitConverter.ToUInt32(encryptedPekListBlob, 4);
            if (!Enum.IsDefined(typeof(PekListFlags), flags))
            {
                Console.WriteLine($"[!] PEK List flags value \"{version}\" is not supported.");
            }

            var salt = encryptedPekListBlob.Skip(8).Take(16).ToArray();
            var encryptedPekList = encryptedPekListBlob.Skip(24).ToArray();
            byte[] decryptedPekList = null;

            // 解密返回的数据
            // 取决于读取的数据中的主要/次要值
            // 可能需要或可能不需要salt
            switch ((PekListFlags)flags)
            {
                case PekListFlags.ClearText:
                    decryptedPekList = encryptedPekList;
                    break;

                case PekListFlags.Encrypted:
                    // 目前对于 PEK 有两种加密方式。
                    switch ((PekListVersion)version)
                    {
                        case PekListVersion.Windowsunk:
                            Console.WriteLine("[!] unsupported version: 1");
                            break;
                        case PekListVersion.Windows2003:
                            Console.WriteLine("[*] PekListVersion: 2003");
                            decryptedPekList = DecryptDataUsingRc4AndSalt(systemKey, salt, encryptedPekList, 1000);
                            break;

                        case PekListVersion.Windows2016:
                            Console.WriteLine("[*] PekListVersion: 2016");
                            decryptedPekList = DecryptDataUsingAes(systemKey, salt, encryptedPekList).ToArray();
                            break;
                    }

                    break;
            }

            return ParsePekList(decryptedPekList);
        }

        /// <summary>
        /// Data is excepted as follows
        /// |................|........|....|....|{....|................|}
        ///   ^- Signature 16 bytes
        ///                    ^- Last generated 8 bytes
        ///                             ^- Current key 4 bytes
        ///                                  ^- Key count 4 bytes
        ///                                       ^- Key ID 4 bytes (Key and Key ID repeated for key count)
        ///                                             ^- Key 16 bytes (Key and Key ID repeated for key count)
        /// </summary>
        /// <param name="decryptedPekList">The decrypted PEK list.</param>
        /// <returns>The current PEK.</returns>
        private static Dictionary<uint, byte[]> ParsePekList(byte[] decryptedPekList)
        {
            var keys = new Dictionary<uint, byte[]>();
            for (var i = 32; i < decryptedPekList.Length; i += 24)
            {
                var id = BitConverter.ToUInt32(decryptedPekList, i);
                var key = decryptedPekList.Skip(i + 4).Take(16).ToArray();

                keys[id] = key;
            }

            return keys;
        }
        private static byte[] DecryptDataUsingRc4AndSalt(byte[] key, byte[] salt, byte[] data, int rounds)
        {
            var hProv = IntPtr.Zero;
            var hHash = IntPtr.Zero;
            var hKey = IntPtr.Zero;

            // Get handle to the crypto provider
            if (!NativeMethods.CryptAcquireContext(ref hProv, null, null, NativeMethods.PROVRSAFULL, NativeMethods.CRYPTVERIFYCONTEXT))
            {
                NativeMethods.CryptReleaseContext(hProv, 0);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Create MD5 hashing function
            if (!NativeMethods.CryptCreateHash(hProv, NativeMethods.CALGMD5, IntPtr.Zero, 0, ref hHash))
            {
                NativeMethods.CryptReleaseContext(hProv, 0);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Hash the key
            if (!NativeMethods.CryptHashData(hHash, key, (uint)key.Length, 0))
            {
                NativeMethods.CryptDestroyHash(hHash);
                NativeMethods.CryptReleaseContext(hProv, 0);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Hash the salt for the specified number of rounds
            for (var i = 0; i < rounds; i++)
            {
                if (!NativeMethods.CryptHashData(hHash, salt, (uint)salt.Length, 0))
                {
                    NativeMethods.CryptDestroyHash(hHash);
                    NativeMethods.CryptReleaseContext(hProv, 0);
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            // Derive the RC4 key
            if (!NativeMethods.CryptDeriveKey(hProv, NativeMethods.CALGRC4, hHash, 0, ref hKey))
            {
                NativeMethods.CryptDestroyHash(hHash);
                NativeMethods.CryptReleaseContext(hProv, 0);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            uint pdwDataLen = (uint)data.Length;

            // Encrypt/Decrypt
            if (!NativeMethods.CryptEncrypt(hKey, IntPtr.Zero, 1, 0, data, ref pdwDataLen, (uint)data.Length))
            {
                NativeMethods.CryptDestroyKey(hKey);
                NativeMethods.CryptDestroyHash(hHash);
                NativeMethods.CryptReleaseContext(hProv, 0);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            NativeMethods.CryptDestroyKey(hKey);
            NativeMethods.CryptDestroyHash(hHash);
            NativeMethods.CryptReleaseContext(hProv, 0);

            return data;
        }

        private static byte[] DecryptDataUsingAes(byte[] key, byte[] salt, byte[] data)
        {
            using (var aes = AesManaged.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.Zeros;
                using (var decryptor = aes.CreateDecryptor(key, salt))
                {
                    using (var cryptoStream = new CryptoStream(new MemoryStream(data, false), decryptor, CryptoStreamMode.Read))
                    using (var outputStream = new MemoryStream(data.Length))
                    {
                        cryptoStream.CopyTo(outputStream);
                        return outputStream.ToArray();
                    }
                }
            }
        }

#pragma warning disable SA1008
        private static void RidToKeys(uint rid, out byte[] ss1, out byte[] ss2)
        {
            var s1 = new char[7];
            var s2 = new char[7];

            s1[0] = (char)(rid & 0xFF);
            s1[1] = (char)((rid >> 8) & 0xFF);
            s1[2] = (char)((rid >> 16) & 0xFF);
            s1[3] = (char)((rid >> 24) & 0xFF);
            s1[4] = s1[0];
            s1[5] = s1[1];
            s1[6] = s1[2];

            s2[0] = (char)((rid >> 24) & 0xFF);
            s2[1] = (char)(rid & 0xFF);
            s2[2] = (char)((rid >> 8) & 0xFF);
            s2[3] = (char)((rid >> 16) & 0xFF);
            s2[4] = s2[0];
            s2[5] = s2[1];
            s2[6] = s2[2];

            ss1 = StrToKey(s1);
            ss2 = StrToKey(s2);
            //return (StrToKey(s1), StrToKey(s2));
        }

#pragma warning restore SA1008

        private static byte[] StrToKey(char[] str)
        {
            var key = new byte[8];

            key[0] = BitConverter.GetBytes(str[0] >> 1)[0];
            key[1] = BitConverter.GetBytes(((str[0] & 0x01) << 6) | (str[1] >> 2))[0];
            key[2] = BitConverter.GetBytes(((str[1] & 0x03) << 5) | (str[2] >> 3))[0];
            key[3] = BitConverter.GetBytes(((str[2] & 0x07) << 4) | (str[3] >> 4))[0];
            key[4] = BitConverter.GetBytes(((str[3] & 0x0F) << 3) | (str[4] >> 5))[0];
            key[5] = BitConverter.GetBytes(((str[4] & 0x1F) << 2) | (str[5] >> 6))[0];
            key[6] = BitConverter.GetBytes(((str[5] & 0x3F) << 1) | (str[6] >> 7))[0];
            key[7] = BitConverter.GetBytes(str[6] & 0x7F)[0];
            for (int i = 0; i < 8; i++)
            {
                key[i] = BitConverter.GetBytes(key[i] << 1)[0];
            }

            return key;
        }

        private static byte[] StructureToByteArray(object obj)
        {
            int len = Marshal.SizeOf(obj);

            byte[] arr = new byte[len];

            IntPtr ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);

            return arr;
        }
    }
}
