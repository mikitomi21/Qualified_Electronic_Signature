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


namespace XAdES_App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoadRSAKeyFromFile("D:\\Studia\\6\\Bezpieczeństwo Systemów Komputerowych\\Projekt\\Qualified_Electronic_Signature\\private_key.pem");
            XAdESSigner.SingDocument(new FileInfo("C:\\Users\\Ewikk\\Desktop\\piotr.png"));

           
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
                string pin = GetPin(null);
                if (pin == "") throw new Exception("Could not load key");
                rsa = DecryptRSA(fileBytes, int.Parse(pin));
            }
            return rsa;
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

        private string GetPin(Action<string> callback) {
            string pin = "";
            EnterPinWindow enterPinWindow = new EnterPinWindow();
            enterPinWindow.EnqueueAction(callback);
            enterPinWindow.EnqueueAction((pinString)=>pin = pinString);
            enterPinWindow.ShowDialog();
            return pin;
            
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



    }
}