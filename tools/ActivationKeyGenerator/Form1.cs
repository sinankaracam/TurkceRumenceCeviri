using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ActivationKeyGenerator
{
    public class Form1 : Form
    {
        private TextBox txtDeviceCode;
        private TextBox txtActivationKey;
        private Button btnGenerate;
        private TextBox txtSalt;

        public Form1()
        {
            Text = "Activation Key Generator";
            Width = 600;
            Height = 200;

            var lblDevice = new Label { Text = "Device Code:", Left = 10, Top = 10, Width = 80 };
            txtDeviceCode = new TextBox { Left = 100, Top = 10, Width = 460 };

            var lblSalt = new Label { Text = "Verifier Salt:", Left = 10, Top = 40, Width = 80 };
            txtSalt = new TextBox { Left = 100, Top = 40, Width = 300, Text = "VerifierSalt_v1" };

            btnGenerate = new Button { Text = "Generate", Left = 410, Top = 38, Width = 150 };
            btnGenerate.Click += BtnGenerate_Click;
            // copy to clipboard after generated
            btnGenerate.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtActivationKey.Text))
                {
                    try { Clipboard.SetText(txtActivationKey.Text); } catch { }
                }
            };

            var lblKey = new Label { Text = "Activation Key:", Left = 10, Top = 80, Width = 90 };
            txtActivationKey = new TextBox { Left = 100, Top = 80, Width = 460, ReadOnly = true };

            Controls.Add(lblDevice);
            Controls.Add(txtDeviceCode);
            Controls.Add(lblSalt);
            Controls.Add(txtSalt);
            Controls.Add(btnGenerate);
            Controls.Add(lblKey);
            Controls.Add(txtActivationKey);
        }

        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            var deviceCode = txtDeviceCode.Text?.Trim() ?? string.Empty;
            var verifierSalt = txtSalt.Text?.Trim() ?? "VerifierSalt_v1";
            if (string.IsNullOrEmpty(deviceCode))
            {
                MessageBox.Show("Please paste Device Code first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(verifierSalt));
            var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(deviceCode));
            var hex = BitConverter.ToString(sig).Replace("-", "").ToUpperInvariant();
            var key = hex.Substring(0, 32).Insert(8, "-").Insert(17, "-").Insert(26, "-");
            txtActivationKey.Text = key;
        }
    }
}
