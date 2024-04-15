﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace XAdES_App
{
    public static class XAdESSigner
    {

        public static void SingDocument(RSA rsa, FileInfo document)
        {
            XElement file = new XElement("File",
                new XElement("Size", document.Length),
                new XElement("Extension", document.Extension),
                new XElement("Date_of_modification", document.LastWriteTime)
                );
            var fileHash = CalculateFileHash(document.FullName, HashAlgorithm.Create("SHA256"));
            var encryptedFileHash = rsa.SignHash(fileHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            XElement hash = new XElement("Signature", Convert.ToBase64String(encryptedFileHash));
            XElement timestamp = new XElement("Timestamp", DateTime.Now.ToString());

            XElement signature = new XElement("XAdES", file, hash, timestamp);
            byte[] signatureContent = Encoding.UTF8.GetBytes(signature.ToString());

            signature.Save($"{document.DirectoryName}/{document.Name}_signature.xml");

        }

        static byte[] CalculateFileHash(string filePath, HashAlgorithm hashAlgorithm)
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
