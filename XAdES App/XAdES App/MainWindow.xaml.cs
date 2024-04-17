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

        private string _privateKeyPath = "";
        private string _publicKeyPath = "D:\\Studia\\6\\Bezpieczeństwo Systemów Komputerowych\\Projekt\\Qualified_Electronic_Signature\\public_key.pem";
        private string _inputFilePath = "";

        public MainWindow()
        {
            InitializeComponent();
            HideEveryChild(ModePanel);
            if (null != ModePanel.Children[0]) ModePanel.Children[0].Visibility = Visibility.Visible;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RSA rsa = LoadRSAKeyFromFile(_publicKeyPath);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load("D:\\Studia\\6\\Bezpieczeństwo Systemów Komputerowych\\Projekt\\Qualified_Electronic_Signature\\.gitignore_signature.xml");
            var test = XAdESSigner.VerifySignature(rsa,
                new FileInfo("D:\\Studia\\6\\Bezpieczeństwo Systemów Komputerowych\\Projekt\\Qualified_Electronic_Signature\\.gitignore"),
                xmlDocument);
            Console.WriteLine(test);
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
            }
            catch (PinNotSubmittedException)
            {

            }
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
                rsa = EncyptionUtils.DecryptRSA(fileBytes, int.Parse(pin));
            }
            return rsa;
        }

        private string ChooseFile(string filter = "")
        {
            OpenFileDialog dialog = new();

            dialog.Multiselect = false;
            dialog.Title = "Select a folder";
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

       

        private void ChooseKeyButton(object sender, RoutedEventArgs e)
        {
            _privateKeyPath = ChooseFile("PEM files | *.pem");
            FileInfo fileInfo = new FileInfo(_privateKeyPath);
            PrivateKeyFileName.Content = fileInfo.Name;
        }

        private void ChooseFileButton(object sender, RoutedEventArgs e)
        {
            _inputFilePath = ChooseFile();
            FileInfo fileInfo = new FileInfo(_inputFilePath);
            InputFileName.Content = fileInfo.Name;
        }

        private void HideEveryChild(Panel panel)
        {
            foreach (UIElement child in panel.Children) child.Visibility = Visibility.Hidden;
        }

        private void ModeSelected(object sender, RoutedEventArgs e)
        {
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