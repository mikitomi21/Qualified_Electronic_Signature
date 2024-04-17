using System.IO;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;

namespace XAdES_App
{
    public static class XAdESSigner
    {
        private const string HASH_ALGORITHM = "SHA256";

        /// <returns>Path to created signature file</returns>
        public static string SingDocument(RSA rsa, FileInfo document)
        {
            XElement file = new XElement("File",
                new XElement("Size", document.Length),
                new XElement("Extension", document.Extension),
                new XElement("Date_of_modification", document.LastWriteTime)
                );
            //var fileHash = CalculateFileHash(document.FullName, HashAlgorithm.Create(HASH_ALGORITHM));
            //var encryptedFileHash = rsa.SignHash(fileHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var signature = rsa.SignData(File.ReadAllBytes(document.FullName), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            //XElement hash = new XElement("Signature", Convert.ToBase64String(encryptedFileHash));
            XElement hash = new XElement("Signature", Convert.ToBase64String(signature));
            XElement timestamp = new XElement("Timestamp", DateTime.Now.ToString());

            XElement root = new XElement("XAdES", file, hash, timestamp);

            string fullFileName = $"{document.DirectoryName}\\{document.Name}_signature.xml";
            root.Save(fullFileName);
            return fullFileName;
        }

        public static bool VerifySignature(RSA rsa, FileInfo document, XmlDocument signatureFile)
        {
            var list = signatureFile.GetElementsByTagName("Signature");
            if (list.Count > 1 || list.Count < 1) throw new Exception("Invalid signature file");
            byte[] signature = Convert.FromBase64String(list[0].InnerText);
            //byte[] hash = CalculateFileHash(document.FullName, HashAlgorithm.Create(HASH_ALGORITHM));
            //return rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return rsa.VerifyData(File.ReadAllBytes(document.FullName), signature, HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        }

        private static byte[] CalculateFileHash(string filePath, HashAlgorithm hashAlgorithm)
        {
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = hashAlgorithm.ComputeHash(stream);
                return hashBytes;
                //return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
