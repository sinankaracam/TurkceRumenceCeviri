using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using TurkceRumenceCeviri.Utilities;
using TurkceRumenceCeviri.Services.Implementations;

namespace TurkceRumenceCeviri.Views
{
    public partial class RomanianListeningWindow : Window
    {
        private readonly string _speechKey;
        private readonly string _speechRegion;
        private CancellationTokenSource? _cts;
        private bool _isListening;
        private SpeechRecognizer? _recognizer;
        private string _committedText = string.Empty;
        private string _lastPartial = string.Empty;

        public RomanianListeningWindow(string speechKey, string speechRegion)
        {
            InitializeComponent();
            _speechKey = speechKey ?? string.Empty;
            _speechRegion = string.IsNullOrWhiteSpace(speechRegion) ? "westeurope" : speechRegion;

            if (string.IsNullOrWhiteSpace(_speechKey))
            {
                UpdateStatus("Speech key bulunamadý. Ayarlardan anahtarý girin.");
                StartButton.IsEnabled = false;
            }
            else
            {
                UpdateStatus("Hazýr");
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isListening)
                return;

            if (string.IsNullOrWhiteSpace(_speechKey))
            {
                UpdateStatus("Speech key girilmemiþ.");
                return;
            }

            _cts = new CancellationTokenSource();
            _isListening = true;
            UpdateButtons();
            _ = ListenLoopAsync(_cts.Token);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopListening();
        }

        private void StopListening()
        {
            try { _cts?.Cancel(); } catch { }
            _cts?.Dispose();
            _cts = null;

            if (_recognizer != null)
            {
                try { _recognizer.StopContinuousRecognitionAsync().Wait(500); } catch { }
                try { _recognizer.Dispose(); } catch { }
                _recognizer = null;
            }

            _isListening = false;
            UpdateButtons();
            UpdateStatus("Durduruldu");
        }

        private void UpdateButtons()
        {
            StartButton.IsEnabled = !_isListening && !string.IsNullOrWhiteSpace(_speechKey);
            StopButton.IsEnabled = _isListening;
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            try
            {
                var config = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
                config.SpeechRecognitionLanguage = "ro-RO";
                config.SetProfanity(ProfanityOption.Raw);

                _committedText = string.Empty;
                _lastPartial = string.Empty;

                _recognizer = new SpeechRecognizer(config, AudioConfig.FromDefaultMicrophoneInput());

                _recognizer.Recognizing += async (s, e) =>
                {
                    if (token.IsCancellationRequested) return;
                    var partial = e.Result.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(partial)) return;
                    if (!IsRomanianStrict(partial, true))
                    {
                        await Dispatcher.InvokeAsync(() => StatusText.Text = "Yabanci dil algilandi, atlandi.");
                        return;
                    }

                    _lastPartial = partial;
                    var combined = string.IsNullOrWhiteSpace(_committedText) ? partial : $"{_committedText} {partial}";
                    await Dispatcher.InvokeAsync(() =>
                    {
                        RomanianTextBox.Text = combined;
                        PhoneticTextBox.Text = RomanianPhoneticConverter.Convert(combined);
                        StatusText.Text = "Dinleniyor (Rumence ? Fonetik)";
                    });
                };

                _recognizer.Recognized += async (s, e) =>
                {
                    if (token.IsCancellationRequested) return;
                    var text = e.Result.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(text)) return;
                    if (!IsRomanianStrict(text, false))
                    {
                        await Dispatcher.InvokeAsync(() => StatusText.Text = "Yabanci dil algilandi, atlandi.");
                        return;
                    }

                    _committedText = string.IsNullOrWhiteSpace(_committedText) ? text : $"{_committedText} {text}";
                    _lastPartial = string.Empty;

                    await Dispatcher.InvokeAsync(() =>
                    {
                        RomanianTextBox.Text = _committedText;
                        PhoneticTextBox.Text = RomanianPhoneticConverter.Convert(_committedText);
                        StatusText.Text = "Dinleniyor (Rumence ? Fonetik)";
                    });
                };

                _recognizer.Canceled += async (s, e) =>
                {
                    await Dispatcher.InvokeAsync(() => StatusText.Text = $"Ýptal: {e.Reason} {e.ErrorCode}");
                };

                await _recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(200, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    StatusText.Text = $"Hata: {ex.Message}";
                });
            }
            finally
            {
                if (_recognizer != null)
                {
                    try { await _recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false); } catch { }
                    try { _recognizer.Dispose(); } catch { }
                    _recognizer = null;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    _isListening = false;
                    UpdateButtons();
                });
            }
        }

        private void CopyPhoneticButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(PhoneticTextBox.Text))
            {
                Clipboard.SetText(PhoneticTextBox.Text);
                MessageBox.Show("Fonetik metin panoya kopyalandý.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            RomanianTextBox.Text = string.Empty;
            PhoneticTextBox.Text = string.Empty;
            _committedText = string.Empty;
            _lastPartial = string.Empty;
            UpdateStatus("Temizlendi");
        }

        private bool IsRomanianStrict(string text, bool isPartial)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var roChars = new HashSet<char>(new[] { '?','?','â','Â','î','Î','?','?','?','?' });
            foreach (var ch in text)
            {
                if (roChars.Contains(ch)) return true; // diacritics -> kesin Rumence
            }

            var roWords = new[]
            {
                "si","?i","este","sunt","in","în","din","cu","la","de","pe","care",
                "nu","da","foarte","mai","cel","cea","acesta","aceasta","acestia","ace?tia","acelea",
                "merge","vine","vorbeste","vorbe?te","fac","facem","se","intampla","întâmpl?","va","fie"
            };

            var lower = text.ToLowerInvariant();
            int hits = 0;
            foreach (var w in roWords)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(lower, $"\\b{System.Text.RegularExpressions.Regex.Escape(w.ToLowerInvariant())}\\b"))
                {
                    hits++;
                    if (hits >= 2) return true; // en az iki Rumence kelime
                }
            }

            var tokens = System.Text.RegularExpressions.Regex.Matches(lower, "\\p{L}+");
            int tokenCount = tokens.Count;

            if (!isPartial && hits >= 1 && tokenCount >= 2) return true; // final sonuçta bir kelime bile yeterli
            if (isPartial && tokenCount >= 2 && hits >= 1) return true; // erken parça ama 2+ kelime ve 1 hit

            // Eðer ilk parça ise ve 2+ kelime varsa, Türkçe benzemiyorsa kabul et
            if (isPartial && string.IsNullOrWhiteSpace(_committedText) && tokenCount >= 2 && !RomanianNormalizer.IsLikelyTurkish(text))
                return true;

            return false;
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        protected override void OnClosed(EventArgs e)
        {
            StopListening();
            base.OnClosed(e);
        }
    }
}
