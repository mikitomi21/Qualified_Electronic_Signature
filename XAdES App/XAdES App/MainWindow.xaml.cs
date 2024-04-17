using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Security.Cryptography;
using System.IO.Packaging;
using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Diagnostics;
using System.Security.Cryptography.Xml;
using System.Reflection;

namespace XAdES_App
{
    public partial class MainWindow : Window
    {
        private class PinNotSubmittedException : Exception
        {
            public PinNotSubmittedException() { }
            public PinNotSubmittedException(string message) : base(message) { }
            public PinNotSubmittedException(string message, Exception inner) : base(message, inner) { }
        };

        private const int MAX_RSA_FILE_SIZE_IN_BYTES = 512;

        private string _privateKeyPath = "D:\\Studia\\6\\Bezpieczeństwo Systemów Komputerowych\\Projekt\\Qualified_Electronic_Signature\\private_key.pem";
        private string _publicKeyPath = "D:\\Studia\\6\\Bezpieczeństwo Systemów Komputerowych\\Projekt\\Qualified_Electronic_Signature\\public_key.pem";
        private string _inputFilePath = "";
        private string _signatureFilePath = "";
        private string _encryptedFilePath = "";
        private string _fileToBeEncryptedPath = "";

        public MainWindow()
        {
            InitializeComponent();
            HideEveryChild(ModePanel);
            if (null != ModePanel.Children[0]) ModePanel.Children[0].Visibility = Visibility.Visible;
        }

        private bool EncryptFile(string filePath)
        {
            RSA rsa = LoadRSAKeyFromFile(_publicKeyPath);
            string newFilePath = EncryptionUtils.RSAEncryptWithPublicKey(new FileInfo(filePath), rsa.ExportRSAPublicKey());
            Process.Start("explorer.exe", $"/select,\"{newFilePath}\"");
            return true;
        }
        private bool DecryptFile(string filePath)
        {
            RSA rsa = LoadRSAKeyFromFile(_privateKeyPath);
            string newFilePath = EncryptionUtils.RSADecryptWithPrivateKey(new FileInfo(filePath), rsa.ExportRSAPrivateKey());
            Process.Start("explorer.exe", $"/select,\"{newFilePath}\"");
            return true;
        }

        private void SignButton(object sender, RoutedEventArgs e)
        {
            Sign();
        }

        private void Sign()
        {
            try
            {
                RSA rsa = LoadRSAKeyFromFile(_privateKeyPath);
                string newFilePath = XAdESSigner.SingDocument(rsa, new FileInfo(_inputFilePath));
                Process.Start("explorer.exe", $"/select,\"{newFilePath}\"");
                ShowResult("File was successfully signed.", true);
            }
            catch (PinNotSubmittedException)
            {

            }
        }
        private void VerifyButton(object sender, RoutedEventArgs e)
        {
            Verify();
        }

        private void Verify()
        {
            RSA rsa = LoadRSAKeyFromFile(_publicKeyPath);
            XmlDocument signature = new XmlDocument();
            signature.Load(_signatureFilePath);
            if (XAdESSigner.VerifySignature(rsa, new FileInfo(_inputFilePath), signature)) ShowResult("Signature is valid.", true);
            else ShowResult("Signature is invalid");
        }
        private RSA LoadRSAKeyFromFile(string path)
        {
            RSA rsa = RSA.Create();
            byte[] fileBytes = File.ReadAllBytes(path);
            char[] charArr = Encoding.ASCII.GetChars(fileBytes);
            try
            {
                rsa.ImportFromPem(charArr);
            }
            catch
            {
                string pin = GetPin();
                rsa = EncryptionUtils.DecryptRSA(fileBytes, int.Parse(pin));
            }
            return rsa;
        }

        private string ChooseFile(string filter = "", string windowTitle = "Select file")
        {
            OpenFileDialog dialog = new();

            dialog.Multiselect = false;
            dialog.Title = windowTitle;
            dialog.Filter = filter;
            dialog.InitialDirectory = Environment.CurrentDirectory;

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                return dialog.FileName;
            }
            return "";
        }

        /// <summary>
        /// This method prompts the user for PIN entry.
        /// </summary>
        /// <returns>The PIN entered by the user.</returns>
        /// <exception cref="PinNotSubmittedException">Thrown when the user closes the window without entering a PIN.</exception>
        private string GetPin()
        {
            EnterPinWindow enterPinWindow = new EnterPinWindow();
            enterPinWindow.ShowDialog();
            return enterPinWindow.WasPinSubmitted ? enterPinWindow.Pin : throw new PinNotSubmittedException("Pin not submitted");
        }



        private void ChoosePrivateKeyButton(object sender, RoutedEventArgs e)
        {
            _privateKeyPath = ChooseFile("PEM files | *.pem");
            FileInfo fileInfo = new FileInfo(_privateKeyPath);
            PrivateKeyFileName.Content = fileInfo.Name;
            PrivateKeyFileName2.Content = fileInfo.Name;
        }
        private void ChoosePublicKeyButton(object sender, RoutedEventArgs e)
        {
            _publicKeyPath = ChooseFile("PEM files | *.pem");
            FileInfo fileInfo = new FileInfo(_publicKeyPath);
            PublicKeyFileName.Content = fileInfo.Name;
            PublicKeyFileName2.Content = fileInfo.Name;
        }


        private void ChooseFileButton(object sender, RoutedEventArgs e)
        {
            _inputFilePath = ChooseFile();
            if (_inputFilePath.Equals("")) return;
            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(_inputFilePath);
            }
            catch (Exception ex) { ShowResult(ex.Message); }
            InputFileName.Content = fileInfo.Name;
            InputFileName2.Content = fileInfo.Name;
        }

        private void ChooseSignatureFileButton(object sender, RoutedEventArgs e)
        {
            _signatureFilePath = ChooseFile("XML files | *.xml");
            if (_signatureFilePath.Equals("")) return;
            FileInfo fileInfo = new FileInfo(_signatureFilePath);
            SignatureFileLabel.Content = fileInfo.Name;
        }
        private void ChooseFileToEncrypt(object sender, RoutedEventArgs e)
        {
            _fileToBeEncryptedPath = ChooseFile();
            if (_fileToBeEncryptedPath.Equals("")) return;
            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(_fileToBeEncryptedPath);
            }
            catch (Exception ex) { ShowResult(ex.Message); }
            if (fileInfo.Length > MAX_RSA_FILE_SIZE_IN_BYTES) throw new NotImplementedException();
            FileToEncryptLabel.Content = fileInfo.Name;
        }

        private void EncryptFileButton(object sender, RoutedEventArgs e)
        {
            if(EncryptFile(_fileToBeEncryptedPath)) ShowResult("File was successfully encrypted", true);
            else ShowResult("An error occurred while encrypting", true);
        }

        private void DecryptFileButton(object sender, RoutedEventArgs e)
        {
            DecryptFile(_encryptedFilePath);
        }

        private void ChooseFileToDecrypt(object sender, RoutedEventArgs e)
        {
            _encryptedFilePath = ChooseFile("Encrypted files | *.enc");
            if (_encryptedFilePath.Equals("")) return;
            FileInfo fileInfo = new FileInfo(_encryptedFilePath);
            var test = Math.Floor(4096f / 8) - 11;
            if (fileInfo.Length > MAX_RSA_FILE_SIZE_IN_BYTES) throw new NotImplementedException();
            FileToDecryptLabel.Content = fileInfo.Name;
        }

        private void HideEveryChild(Panel panel)
        {
            foreach (UIElement child in panel.Children) child.Visibility = Visibility.Hidden;
        }

        private void ShowResult(string message, bool success = false)
        {
            if (null == Result) return;

            Result.Content = message;

            if (success) Result.Foreground = Brushes.Green;
            else Result.Foreground = Brushes.Red;
        }

        private void ModeSelected(object sender, RoutedEventArgs e)
        {
            ShowResult("");
            if (null == ModePanel) return;
            foreach (Panel child in ModePanel.Children)
            {
                if ($"{child.Name}Selector".Equals(((ComboBoxItem)sender).Name))
                {
                    child.Visibility = Visibility.Visible;
                }
                else child.Visibility = Visibility.Hidden;
            }
        }
    }
}