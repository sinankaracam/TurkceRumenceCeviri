using System.Windows;
using TurkceRumenceCeviri.Utilities;

namespace TurkceRumenceCeviri.Views
{
    public partial class PhoneticResultWindow : Window
    {
        private string _originalText;

        public PhoneticResultWindow(string text, bool autoConvert = false)
        {
            InitializeComponent();
            _originalText = text ?? string.Empty;
            MainTextBox.Text = _originalText;

            if (autoConvert && !string.IsNullOrWhiteSpace(_originalText))
            {
                ApplyPhoneticConversion(_originalText);
            }
        }

        private void PhoneticBtn_Click(object sender, RoutedEventArgs e)
        {
            var currentText = MainTextBox.Text;
            if (string.IsNullOrWhiteSpace(currentText)) return;

            ApplyPhoneticConversion(currentText);
        }

        private void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(MainTextBox.Text))
            {
                Clipboard.SetText(MainTextBox.Text);
                MessageBox.Show("Metin panoya kopyalandý.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApplyPhoneticConversion(string sourceText)
        {
            var converted = RomanianPhoneticConverter.Convert(sourceText);
            MainTextBox.Text = converted;
        }
    }
}