using System;
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

        public void LoadFences()
        {
            foreach (var dir in Directory.EnumerateDirectories(basePath))
            {
                var metaFile = Path.Combine(dir, MetaFileName);
                var serializer = new XmlSerializer(typeof(FenceInfo));
                var reader = new StreamReader(metaFile);
                var fence = serializer.Deserialize(reader) as FenceInfo;
                reader.Close();

                // Remove files that no longer exist
                if (fence != null && fence.Files != null)
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

        public void UpdateFence(FenceInfo fenceInfo)
        {
            var path = GetFolderPath(fenceInfo);
            EnsureDirectoryExists(path);

            var metaFile = Path.Combine(path, MetaFileName);
            var serializer = new XmlSerializer(typeof(FenceInfo));
            var writer = new StreamWriter(metaFile);
            serializer.Serialize(writer, fenceInfo);
            writer.Close();
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
