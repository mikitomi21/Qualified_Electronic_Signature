using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Shapes;

namespace XAdES_App
{
    public static class EncryptionUtils
    {
        public static RSA DecryptRSA(byte[] encryptedRSA, int pin)
        {
            byte[] pinBytes = BitConverter.GetBytes(pin);
            byte[] key = new byte[32];
            byte[] iv = new byte[16];
            Array.Copy(pinBytes, key, pinBytes.Length);
            Array.Copy(pinBytes, iv, pinBytes.Length);
            byte[] decryptedRSA = AesCfbDecrypt(encryptedRSA, key, iv);
            char[] charArr = Encoding.ASCII.GetChars(decryptedRSA);
            RSA rsa = RSA.Create();
            rsa.ImportFromPem(charArr);
            //Console.WriteLine(Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()));
            return rsa;
        }

        private static byte[] AesCfbDecrypt(byte[] encryptedArr, byte[] key, byte[] iv, int blockSize = 128)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;

                //Probably unnecessary block
                aes.IV = iv;
                aes.BlockSize = blockSize;
                aes.Mode = CipherMode.CFB;
                //

                int paddedArraySize = ((encryptedArr.Length / blockSize) + 1) * blockSize;
                byte[] encryptedPaddedArr = new byte[paddedArraySize];
                byte[] decryptedPaddedArr = new byte[paddedArraySize];
                Array.Copy(encryptedArr, encryptedPaddedArr, encryptedArr.Length);
                int bytesWritten = 0;
                aes.TryDecryptCfb(encryptedPaddedArr, iv, decryptedPaddedArr, out bytesWritten, PaddingMode.None, blockSize);
                if (bytesWritten == paddedArraySize)
                {
                    byte[] decryptedArr = new byte[encryptedArr.Length];
                    Array.Copy(decryptedPaddedArr, decryptedArr, decryptedArr.Length);
                    return decryptedArr;
                }
                else
                {
                    throw new Exception("Could not decrypt entire array.");
                }
            }
        }

        /// <returns>Path to an encrypted file</returns>
        public static string RSAEncryptWithPublicKey(FileInfo fileInfo, byte[] publicKey, bool deleteOriginalFile = false)
        {
            byte[] fileBytes = File.ReadAllBytes(fileInfo.FullName);
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportRSAPublicKey(publicKey, out _);
                byte[] encryptedData = rsa.Encrypt(fileBytes, true);
                string filePath = $"{fileInfo.FullName}.enc";
                File.WriteAllBytes(filePath, encryptedData);
                if (deleteOriginalFile) File.Delete(fileInfo.FullName);
                return filePath;
            }

        }

        public static string RSADecryptWithPrivateKey(FileInfo encryptedFileInfo, byte[] privateKey, bool deleteEncryptedFile = false)
        {
            byte[] encryptedFileBytes = File.ReadAllBytes(encryptedFileInfo.FullName);
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportRSAPrivateKey(privateKey, out _);
                byte[] decryptedData = rsa.Decrypt(encryptedFileBytes, true);
                string filePath = System.IO.Path.ChangeExtension(encryptedFileInfo.FullName, null);
                File.WriteAllBytes(filePath, decryptedData);
                if (deleteEncryptedFile) File.Delete(encryptedFileInfo.FullName);
                return filePath;
            }
        }

    }
}
