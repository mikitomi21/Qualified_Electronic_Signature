using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;


namespace XAdES_App
{
    public partial class EnterPinWindow : Window
    {
        private string _pin = "";
        private bool _wasPinSubmitted = false;
        public string Pin
        {
            get
            {
                return _pin;
            }
        }
        public bool WasPinSubmitted
        {
            get
            {
                return _wasPinSubmitted;
            }
        }
        public EnterPinWindow()
        {
            InitializeComponent();
        }

        public EnterPinWindow(Window hostWindow)
        {
            InitializeComponent();
            Owner = hostWindow;
        }

        private void SubmitPin()
        {
            _pin = PinText.Text;
            _wasPinSubmitted = true;
        }

        private void CancelPin()
        {
            _pin = "";
            _wasPinSubmitted = false;
        }

        private void SubmitPinButton(object sender, RoutedEventArgs e)
        {
            SubmitPin();
            Close();
        }

        private void CancelPinButton(object sender, RoutedEventArgs e)
        {
            CancelPin();
            Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int prevCaretIndex = PinText.CaretIndex;
            PinText.Text = Regex.Replace(PinText.Text, @"[^\d]", "");
            PinText.CaretIndex = prevCaretIndex;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_wasPinSubmitted) CancelPin();
        }
    }
}
