using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace NoFences.Model
{
    public class FenceManager
    {
        public static FenceManager Instance { get; } = new FenceManager();

        private const string MetaFileName = "__fence_metadata.xml";

        private readonly string basePath;

        public FenceManager()
        {
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NoFences");
            EnsureDirectoryExists(basePath);
        }

        private FenceInfo LoadXML(string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(FenceInfo));
                using (var reader = new StreamReader(filePath))
                {
                    return serializer.Deserialize(reader) as FenceInfo;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading fence metadata from {filePath}: {ex.Message}");
                return null;
            }
        }

        public void LoadFences()
        {
            foreach (var dir in Directory.EnumerateDirectories(basePath))
            {
                var metaFile = Path.Combine(dir, MetaFileName);
                var fence = LoadXML(metaFile);
                if(fence == null)
                {
                    continue;
                }

                // Remove files that no longer exist
                if (fence.Files != null)
                {
                    var originalCount = fence.Files.Count;
                    var filesToRemove = fence.Files
                        .Where(file => !File.Exists(file) && !Directory.Exists(file))
                        .ToList();

                    foreach (var file in filesToRemove)
                    {
                        fence.Files.Remove(file);
                    }

                    // Save updated metadata if files were removed
                    if (filesToRemove.Count > 0)
                    {
                        UpdateFence(fence);
                    }
                }

                new FenceWindow(fence).Show();
            }
        }

        public void CreateFence(string name)
        {
            var fenceInfo = new FenceInfo(Guid.NewGuid())
            {
                Name = name,
                PosX = 100,
                PosY = 250,
                Height = 300,
                Width = 300
            };

            UpdateFence(fenceInfo);
            new FenceWindow(fenceInfo).Show();
        }

        public void RemoveFence(FenceInfo info)
        {
            Directory.Delete(GetFolderPath(info), true);
        }

        private void SaveToTempFile(FenceInfo fenceInfo, out string tempFilePath)
        {
            tempFilePath = Path.GetTempFileName();
            try
            {
                var serializer = new XmlSerializer(typeof(FenceInfo));
                using (var writer = new StreamWriter(tempFilePath))
                {
                    serializer.Serialize(writer, fenceInfo);
                }
                // Verify the temporary file contains valid content
                if (LoadXML(tempFilePath) == null)
                {
                    throw new InvalidOperationException("Temporary file is empty or invalid.");
                }
            }
            catch (Exception ex)
            {
                File.Delete(tempFilePath); // Clean up on failure
                Console.WriteLine($"Error saving fence metadata to {tempFilePath}: {ex.Message}");
                throw;
            }
        }

        public void UpdateFence(FenceInfo fenceInfo)
        {
            var path = GetFolderPath(fenceInfo);
            EnsureDirectoryExists(path);

            string tempFilePath;
            SaveToTempFile(fenceInfo, out tempFilePath);
            var metaFile = Path.Combine(path, MetaFileName);
            string backupPath = $"{metaFile}.{DateTime.Now:yyyyMMddHHmmss}";
            try
            {
                if (File.Exists(metaFile))
                {
                    // Create a backup of the existing file before moving it.
                    File.Copy(metaFile, backupPath);
                    File.Delete(metaFile); // Delete the original file to avoid conflicts
                }

                File.Move(tempFilePath, metaFile); // Rename the temporary file to the destination file
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving temporary file {tempFilePath} to {metaFile}: {ex.Message}");

                if (File.Exists(backupPath))
                {
                    try
                    {
                        File.Delete(metaFile);
                        File.Move(backupPath, metaFile); // Restore the backup in case of failure.
                    }
                    catch (Exception restoreEx)
                    {
                        Console.WriteLine($"Error restoring from backup: {restoreEx.Message}");
                    }
                }
            }
        }

        private void EnsureDirectoryExists(string dir)
        {
            var di = new DirectoryInfo(dir);
            if (!di.Exists)
                di.Create();
        }

        private string GetFolderPath(FenceInfo fenceInfo)
        {
            return Path.Combine(basePath, fenceInfo.Id.ToString());
        }
    }
}
