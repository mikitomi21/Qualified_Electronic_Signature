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
        private string _inputFilePath = "";

        public MainWindow()
        {
            InitializeComponent();
            var test = ModePanel.Children;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RSA rsa = LoadRSAKeyFromFile(_privateKeyPath);
                XAdESSigner.SingDocument(rsa, new FileInfo(_inputFilePath));
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
                rsa = DecryptRSA(fileBytes, int.Parse(pin));
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


        private RSA DecryptRSA(byte[] encryptedRSA, int pin)
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
            Console.WriteLine(Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()));
            return rsa;
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

        private byte[] AesCfbDecrypt(byte[] encryptedArr, byte[] key, byte[] iv, int blockSize = 128)
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

        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            return;
            if (null == ModePanel) return;
            foreach (UIElement child in ModePanel.Children) child.Visibility = Visibility.Hidden;
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