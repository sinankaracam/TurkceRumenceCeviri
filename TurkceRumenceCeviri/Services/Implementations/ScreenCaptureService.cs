using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace TurkceRumenceCeviri.Services.Implementations;

public static class ScreenCaptureService
{
    // Launch Windows Snipping Tool to let user select a region, then read the image from clipboard
    public static async Task<string?> CaptureSelectedRegionAsync(string outputPath, int timeoutMs = 30000)
    {
        try
        {
            // Ensure directory exists
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Clear clipboard to avoid picking previously copied content
            try
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try { Clipboard.Clear(); } catch { }
                    });
                }
                else
                {
                    try { Clipboard.Clear(); } catch { }
                }
            }
            catch { }

            // Start Windows snipping UI (preferred: explorer URI)
            var started = false;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "ms-screenclip:",
                    UseShellExecute = true
                });
                started = true;
            }
            catch { }

            if (!started)
            {
                // Fallback to SnippingTool
                try
                {
                    Process.Start(new ProcessStartInfo("SnippingTool.exe") { UseShellExecute = true, Arguments = "/clip" });
                    started = true;
                }
                catch { }
            }

            // Give UI a moment to appear before polling clipboard
            await Task.Delay(500);

            // Wait for clipboard image (use Dispatcher for STA-safe access)
            var start = DateTime.UtcNow;
            BitmapSource? bmp = null;
            while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
            {
                await Task.Delay(250);
                bmp = TryGetClipboardImage();
                if (bmp != null) break;
            }

            if (bmp == null)
                return null;

            // Save PNG
            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(fs);
            fs.Flush(true);

            // Ensure file exists and non-empty
            var startWait = DateTime.UtcNow;
            while ((DateTime.UtcNow - startWait).TotalSeconds < 3)
            {
                if (File.Exists(outputPath))
                {
                    var info = new FileInfo(outputPath);
                    if (info.Length > 0) break;
                }
                await Task.Delay(100);
            }
            return outputPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Screen capture failed: {ex.Message}");
            return null;
        }
    }

    private static BitmapSource? TryGetClipboardImage()
    {
        try
        {
            if (Application.Current != null)
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (Clipboard.ContainsImage())
                            return Clipboard.GetImage();
                    }
                    catch { }
                    return null;
                });
            }
            else
            {
                if (Clipboard.ContainsImage())
                    return Clipboard.GetImage();
            }
        }
        catch { }
        return null;
    }
}
