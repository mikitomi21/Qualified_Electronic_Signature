﻿using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Xml;
using System.Diagnostics;

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
        private const string MAX_RSA_FILE_SIZE_EXCEED_ERROR_MSG = "File size is too big, File must be <=512 B";

        private string _privateKeyPath = "";
        private string _publicKeyPath = "";
        private string _inputFilePath = "";
        private string _signatureFilePath = "";
        private string _encryptedFilePath = "";
        private string _fileToBeEncryptedPath = "";

        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            HideEveryChild(ModePanel);
            if (null != ModePanel.Children[0]) ModePanel.Children[0].Visibility = Visibility.Visible;
        }

        private bool EncryptFile(string filePath)
        {
            try
            {
                RSA rsa = LoadRSAKeyFromFile(_publicKeyPath);
                string newFilePath = EncryptionUtils.RSAEncryptWithPublicKey(new FileInfo(filePath), rsa.ExportRSAPublicKey());
                Process.Start("explorer.exe", $"/select,\"{newFilePath}\"");
                ShowResult("File was Ecnrypted", true); ;
                return true;
            }
            catch (Exception ex)
            {
                ShowResult(ex.Message);
            }

            return false;
        }
        private bool DecryptFile(string filePath)
        {
            try
            {
                RSA rsa = LoadRSAKeyFromFile(_privateKeyPath);
                string newFilePath = EncryptionUtils.RSADecryptWithPrivateKey(new FileInfo(filePath), rsa.ExportRSAPrivateKey());
                Process.Start("explorer.exe", $"/select,\"{newFilePath}\"");
                ShowResult("File was decrypted", true); ;
                return true;
            }
            catch(Exception ex)
            {
                ShowResult(ex.Message); ;
            }
            return false;
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
            catch (Exception ex)
            {
                ShowResult(ex.Message);
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
                try
                {
                    rsa = EncryptionUtils.DecryptRSA(fileBytes, int.Parse(pin));
                }
                catch { throw new Exception("Invalid private key file or PIN was incorrect"); }
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
            EnterPinWindow enterPinWindow = new EnterPinWindow(this);
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
            if (_inputFilePath.Equals(""))
            {
                InputFileName.Content = "";
                InputFileName2.Content = "";
                return;
            }
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
            if (fileInfo.Length > MAX_RSA_FILE_SIZE_IN_BYTES) SetTextAndColor(FileToEncryptLabel, MAX_RSA_FILE_SIZE_EXCEED_ERROR_MSG, Brushes.Red);
            else FileToEncryptLabel.Content = fileInfo.Name;
        }

        private void EncryptFileButton(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_fileToBeEncryptedPath))
            {
                SetTextAndColor(FileToEncryptLabel,
                    $"{_fileToBeEncryptedPath}{(FileToEncryptLabel.Content.Equals("") ? "" : " ")}does not exist!",
                    Brushes.Red);
            }
            FileInfo fileToBeEncryptedInfo = new FileInfo(_fileToBeEncryptedPath);
            if (fileToBeEncryptedInfo.Length > MAX_RSA_FILE_SIZE_IN_BYTES)
            {
                SetTextAndColor(FileToEncryptLabel,
                    MAX_RSA_FILE_SIZE_EXCEED_ERROR_MSG,
                    Brushes.Red);
            }

            if (EncryptFile(_fileToBeEncryptedPath)) ShowResult("File was successfully encrypted", true);
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
            if (fileInfo.Length > MAX_RSA_FILE_SIZE_IN_BYTES) SetTextAndColor(FileToDecryptLabel, MAX_RSA_FILE_SIZE_EXCEED_ERROR_MSG, Brushes.Red);
            FileToDecryptLabel.Content = fileInfo.Name;
        }

        private void HideEveryChild(Panel panel)
        {
            foreach (UIElement child in panel.Children) child.Visibility = Visibility.Hidden;
        }

        private void SetTextAndColor(Label element, string content, Brush brush = null)
        {
            brush = (null == brush) ? Brushes.White : brush;
            element.Content = content;
            element.Foreground = brush;
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