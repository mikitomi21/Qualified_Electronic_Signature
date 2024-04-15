using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace XAdES_App
{
    /// <summary>
    /// Interaction logic for EnterPinWindow.xaml
    /// </summary>
    public partial class EnterPinWindow : Window
    {
        private Queue<Action<string>> _actionQueue = new Queue<Action<string>>();
        public EnterPinWindow()
        {
            InitializeComponent();
        }

        public EnterPinWindow(Window hostWindow)
        {
            InitializeComponent();
            Owner = hostWindow;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

        }

        private void SubmitPin()
        {
            InvokeActions();
            Close();
        }

        private void InvokeActions()
        {
            foreach (var action in _actionQueue)
            {
                try
                {
                    action.Invoke(PinText.Text);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Could not invoke action");
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("-------------------------");
                }
            }
        }
        public void EnqueueAction(Action<string> action)
        {
            _actionQueue.Enqueue(action);
        }

        private void SubmitPin(object sender, RoutedEventArgs e)
        {
            SubmitPin();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int prevCaretIndex = PinText.CaretIndex;
            PinText.Text = Regex.Replace(PinText.Text, @"[^\d]", "");
            PinText.CaretIndex = prevCaretIndex;
        }
    }
}
