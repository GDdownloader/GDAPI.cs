using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace GDDownloadConsole
{
    public class GDDownloader
    {
        private readonly HttpClient _httpClient;
        private const int MaxAttempts = 10; // Max attempts to find a valid link

        public GDDownloader()
        {
            _httpClient = new HttpClient();
        }

        public async Task SearchAndDownloadVersionAsync(string version, string downloadDirectory, int attempt = 1)
        {
            if (attempt > MaxAttempts)
            {
                Console.WriteLine("Max attempts reached. Could not find a valid download link.");
                return;
            }

            try
            {
                string searchUrl = $"https://www.google.com/search?q=Geometry+Dash+{version}+download";
                string downloadUrl = await GetDownloadLinkFromSearchAsync(searchUrl);

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    Console.WriteLine("Download link not found.");
                    return;
                }

                // Ensure the download path directory exists
                Directory.CreateDirectory(downloadDirectory);

                string downloadPath = Path.Combine(downloadDirectory, $"GD{version}{Path.GetExtension(downloadUrl)}");

                using (var response = await _httpClient.GetAsync(downloadUrl))
                {
                    response.EnsureSuccessStatusCode();
                    using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }

                if (Path.GetExtension(downloadPath).Equals(".apk", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("The downloaded file is an APK. Searching for an executable version.");
                    File.Delete(downloadPath); // Clean up the APK file
                    await SearchAndDownloadVersionAsync(version, downloadDirectory, attempt + 1); // Retry
                }
                else if (Path.GetExtension(downloadPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string extractedDir = ExtractZip(downloadPath, version);
                    CheckForExecutable(extractedDir);
                }
                else if (Directory.Exists(downloadPath))
                {
                    CheckForExecutable(downloadPath);
                }
                else if (Path.GetExtension(downloadPath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Download completed: {downloadPath}");
                }
                else
                {
                    Console.WriteLine("The downloaded file is not a recognized format. Aborting.");
                    File.Delete(downloadPath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        private async Task<string> GetDownloadLinkFromSearchAsync(string searchUrl)
        {
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(searchUrl);

            var link = document.DocumentNode.SelectSingleNode("//a[@href]");

            return link?.GetAttributeValue("href", string.Empty);
        }

        private string ExtractZip(string zipPath, string version)
        {
            string extractDir = Path.Combine(Directory.GetCurrentDirectory(), $"GD{version}");
            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(zipPath, extractDir);
            File.Delete(zipPath);
            Console.WriteLine($"Extracted to {extractDir}");
            return extractDir;
        }

        private void CheckForExecutable(string directory)
        {
            var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);

            if (exeFiles.Length > 0)
            {
                Console.WriteLine("Executable file(s) found:");
                foreach (var exe in exeFiles)
                {
                    Console.WriteLine(exe);
                }
            }
            else
            {
                Console.WriteLine("No executable files found in the extracted directory.");
            }
        }
    }
}
