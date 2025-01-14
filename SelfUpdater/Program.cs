using System.Diagnostics;

Console.WriteLine("------ R5Reloaded Launcher Self Updater ------");

var appPath = AppDomain.CurrentDomain.BaseDirectory;
DirectoryInfo parentDir = Directory.GetParent(appPath.TrimEnd(Path.DirectorySeparatorChar));

var launcherURL = "https://cdn.r5r.org/launcher/launcher.exe";
var destinationPath = parentDir + "\\launcher.exe";

Console.WriteLine("Deleting old launcher at " + destinationPath);//

int maxAttempts = 10;  // Maximum retry attempts
int attemptCount = 0;
bool isFileReadyToDelete = false;

while (attemptCount < maxAttempts)
{
    try
    {
        // Try opening the file to check if it is in use
        using (FileStream fs = new FileStream(destinationPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            // If the file opens without exceptions, it's not in use
            isFileReadyToDelete = true;
            break;
        }
    }
    catch (IOException)
    {
        // If the file is in use, catch the IOException and retry
        Console.WriteLine("File is in use, retrying...");
        attemptCount++;
        Thread.Sleep(500); // Wait for 500 milliseconds before retrying
    }
}

if (isFileReadyToDelete)
{
    if (File.Exists(destinationPath))
    {
        File.Delete(destinationPath);
        Console.WriteLine("Old launcher deleted successfully.");
    }
}
else
{
    Console.WriteLine("Failed to delete the file after several attempts.");
}

Console.WriteLine("Downloading new launcher from " + launcherURL);
HttpClient client = new HttpClient();
var response = await client.GetAsync(launcherURL, HttpCompletionOption.ResponseHeadersRead);
response.EnsureSuccessStatusCode();

using var stream = await response.Content.ReadAsStreamAsync();
using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

Console.WriteLine("Writing new launcher to " + destinationPath);
stream.CopyTo(fileStream);
stream.Close();
fileStream.Close();

Console.WriteLine("Launching new launcher at " + destinationPath);
var startInfo = new ProcessStartInfo
{
    FileName = "cmd.exe",
    Arguments = $"/c start \"\" \"{destinationPath}\""
};

// Start the new process via cmd
Process.Start(startInfo);

// Exit the current process
Environment.Exit(0);