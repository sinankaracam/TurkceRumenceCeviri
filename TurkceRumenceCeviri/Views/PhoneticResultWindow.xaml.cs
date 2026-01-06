using System.Windows;
using TurkceRumenceCeviri.Utilities;

namespace TurkceRumenceCeviri.Views
{
    public partial class PhoneticResultWindow : Window
    {
        private string _originalText;

        public PhoneticResultWindow(string text)
        {
            InitializeComponent();
            _originalText = text ?? string.Empty;
            MainTextBox.Text = _originalText;
        }

        private void PhoneticBtn_Click(object sender, RoutedEventArgs e)
        {
            // Kutudaki mevcut metni al (belki kullanýcý elle düzeltti)
            string currentText = MainTextBox.Text;
            
            if (string.IsNullOrWhiteSpace(currentText)) return;

            // Dönüþtürücüyü çaðýr
            string converted = RomanianPhoneticConverter.Convert(currentText);
            
            // Metni güncelle
            MainTextBox.Text = converted;
            
            // Görsel geri bildirim (isteðe baðlý, gerekirse kaldýrýlabilir)
            // Butonu pasif yapabiliriz veya metni tekrar orijinal yapmak için toggle koyabiliriz
            // Þimdilik sadece dönüþtürüyor.
        }

        private void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(MainTextBox.Text))
            {
                Clipboard.SetText(MainTextBox.Text);
                MessageBox.Show("Metin panoya kopyalandý.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}