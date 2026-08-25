using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Project_Nook.Services
{
    /// <summary>
    /// Grabs the screen to a PNG. System.Drawing is already available to this project through
    /// UseWindowsForms, so this needs no extra package.
    /// </summary>
    public class ScreenCaptureService
    {
        /// <summary>
        /// Captures every monitor as one image and writes it to a temp PNG, returning the path.
        /// The caller owns the file: copy it somewhere permanent, then delete it.
        /// </summary>
        public string CaptureAllScreens()
        {
            // VirtualScreen spans all monitors, and its origin can be negative when a secondary display
            // sits above or to the left of the primary one.
            var bounds = SystemInformation.VirtualScreen;
            if (bounds.Width <= 0 || bounds.Height <= 0) return null;

            using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }

            var fileName = $"Screenshot {DateTime.Now:yyyy-MM-dd HHmmss}.png";
            var path = Path.Combine(Path.GetTempPath(), fileName);

            bitmap.Save(path, ImageFormat.Png);
            return path;
        }

        /// <summary>
        /// Pictures\Screenshots, the folder Windows itself uses for Print Screen captures. Created if the
        /// user has never taken a screenshot before.
        /// </summary>
        public static string GetWindowsScreenshotsFolder()
        {
            var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var folder = Path.Combine(pictures, "Screenshots");

            Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>Moves a captured temp file into a folder, avoiding collisions. Returns the final path.</summary>
        public static string MoveInto(string tempPath, string targetFolder)
        {
            var name = Path.GetFileNameWithoutExtension(tempPath);
            var ext = Path.GetExtension(tempPath);
            var destination = Path.Combine(targetFolder, name + ext);

            int counter = 1;
            while (File.Exists(destination))
            {
                destination = Path.Combine(targetFolder, $"{name} ({counter}){ext}");
                counter++;
            }

            File.Move(tempPath, destination);
            return destination;
        }
    }
}
