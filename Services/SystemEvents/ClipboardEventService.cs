using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Project_Nook.Services.SystemEvents
{
    /// <summary>
    /// Watches the Windows clipboard and reports new text.
    ///
    /// Windows delivers clipboard changes as a window message, so this needs a real HWND to listen on;
    /// IslandWindow hands over its own handle once it has one.
    /// </summary>
    public class ClipboardEventService : IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        private HwndSource _source;
        private IntPtr _hwnd = IntPtr.Zero;

        /// <summary>
        /// Text this app just put on the clipboard itself. Copying an entry back out of the history would
        /// otherwise be captured as a brand new clip and the history would chase its own tail.
        /// </summary>
        private string _selfCopiedText;

        public event EventHandler<string> TextCaptured;

        public void Attach(IntPtr hwnd)
        {
            if (_hwnd != IntPtr.Zero) return;
            if (hwnd == IntPtr.Zero) return;

            _hwnd = hwnd;
            _source = HwndSource.FromHwnd(hwnd);
            if (_source == null) return;

            _source.AddHook(WndProc);
            AddClipboardFormatListener(hwnd);
        }

        /// <summary>Call immediately before writing to the clipboard, so the echo is ignored.</summary>
        public void SuppressNext(string text) => _selfCopiedText = text;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE) CaptureText();
            return IntPtr.Zero;
        }

        private void CaptureText()
        {
            string text = ReadClipboardText();
            if (string.IsNullOrWhiteSpace(text)) return;

            if (text == _selfCopiedText)
            {
                _selfCopiedText = null;
                return;
            }

            TextCaptured?.Invoke(this, text);
        }

        /// <summary>
        /// The clipboard is a shared, lockable resource: whichever app just wrote to it may still hold it
        /// open. A couple of retries turns a routine race into a non-event.
        /// </summary>
        private static string ReadClipboardText()
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (ShouldIgnoreClipboard()) return null;
                    return Clipboard.ContainsText() ? Clipboard.GetText() : null;
                }
                catch
                {
                    System.Threading.Thread.Sleep(30);
                }
            }
            return null;
        }

        /// <summary>
        /// Honours the opt-out formats password managers and other sensitive apps place on the clipboard
        /// alongside their payload. Without this, copying a password out of a vault would write it to the
        /// history database in plain text and leave it there.
        /// </summary>
        private static bool ShouldIgnoreClipboard()
        {
            // "Clipboard Viewer Ignore" is the long-standing convention (KeePass, 1Password, Bitwarden and
            // others set it); the two Cloud/History formats are what Windows itself checks before syncing.
            string[] optOutFormats =
            {
                "Clipboard Viewer Ignore",
                "ExcludeClipboardContentFromMonitorProcessing",
                "CanIncludeInClipboardHistory",
                "CanUploadToCloudClipboard"
            };

            try
            {
                var data = Clipboard.GetDataObject();
                if (data == null) return false;

                foreach (var format in optOutFormats)
                {
                    if (!data.GetDataPresent(format)) continue;

                    // The two Can* formats carry a DWORD: 0 means "do not record".
                    if (format.StartsWith("Can", StringComparison.Ordinal))
                    {
                        if (IsZeroFlag(data.GetData(format))) return true;
                        continue;
                    }

                    return true;
                }
            }
            catch { }

            return false;
        }

        private static bool IsZeroFlag(object value)
        {
            try
            {
                if (value is int i) return i == 0;
                if (value is System.IO.MemoryStream stream && stream.Length >= 4)
                {
                    var buffer = new byte[4];
                    stream.Position = 0;
                    stream.ReadExactly(buffer);
                    return BitConverter.ToInt32(buffer, 0) == 0;
                }
            }
            catch { }

            return false;
        }

        public void Dispose()
        {
            if (_hwnd != IntPtr.Zero)
            {
                try { RemoveClipboardFormatListener(_hwnd); } catch { }
                _hwnd = IntPtr.Zero;
            }

            if (_source != null)
            {
                _source.RemoveHook(WndProc);
                _source = null;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    }
}
