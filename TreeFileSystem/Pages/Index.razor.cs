using DevExpress.Blazor;
using System.Diagnostics;

namespace TreeFileSystem.Pages
{
    public partial class Index
    {
        IEnumerable<FileSystemItem>? items;
        string SelectedGroup = "none";
        void SelectionChanged(TreeViewNodeEventArgs e)
        {

            FileSystemItem CurrentNode= (FileSystemItem)e.NodeInfo?.DataItem;
            SelectedGroup = e.NodeInfo?.Text;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            items = FileSystemHelper.ReadFileSystem("D:\\");


        }
    }
    public class FileSystemHelper

    {
        public static IEnumerable<FileSystemItem> ReadFileSystem(string path)
        {
            var result = new List<FileSystemItem>();

            // Add the root directory item
            var rootItem = new FileSystemItem()
            {
                CreatedAt = Directory.GetCreationTime(path),
                UpdatedAt = Directory.GetLastWriteTime(path),
                Name = Path.GetFileName(path),
                Type = "directory",
                FullPath = Path.GetFullPath(path),
                Parent = null,
                IsDeleted = false
            };
            //result.Add(rootItem);

            yield return rootItem;

            // Recursively add child items
            var childItems = GetChildItems(path, rootItem.FullPath);
            result.AddRange(childItems);


            //return result;
        }

        private static readonly string[] Base32Chars = {
        "0", "1", "2", "3", "4", "5", "6", "7",
        "8", "9", "A", "B", "C", "D", "E", "F",
        "G", "H", "J", "K", "M", "N", "P", "Q",
        "R", "S", "T", "V", "W", "X", "Y", "Z"
    };

        public static string NewShortGuid()
        {
            byte[] guidBytes = Guid.NewGuid().ToByteArray();
            long longValue = BitConverter.ToInt64(guidBytes, 0);
            string base32Value = ConvertToBase32(Math.Abs(longValue));
            return base32Value.Substring(0, 8);
        }

        private static string ConvertToBase32(long value)
        {
            string result = "";
            do
            {
                int remainder = (int)(value % 32);
                result = Base32Chars[remainder] + result;
                value /= 32;
            } while (value > 0);
            return result;
        }


        public static void DeleteItem(FileSystemItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            // Rename the item with prefix "GC-"
            var newName = $"GC-{NewShortGuid()}-" + item.Name;
            var newPath = Path.Combine(item.Parent, newName);
            if (item.Type == "directory")
            {
                Directory.Move(item.FullPath, newPath);
            }
            else if (item.Type == "file")
            {
                File.Move(item.FullPath, newPath);
            }
            else
            {
                throw new NotSupportedException($"Unsupported item type: {item.Type}");
            }

            // Set the IsDeleted property to true
            item.IsDeleted = true;
            item.Name = newName;
            item.FullPath = newPath;
        }

        public static void CreateFolder(FileSystemItem parentFolder, string newFolderName)
        {
            if (parentFolder == null)
            {
                throw new ArgumentNullException(nameof(parentFolder));
            }

            if (string.IsNullOrEmpty(newFolderName))
            {
                throw new ArgumentException("New folder name cannot be null or empty", nameof(newFolderName));
            }

            var newFolderPath = Path.Combine(parentFolder.FullPath, newFolderName);
            Directory.CreateDirectory(newFolderPath);
        }
        //public static void AddFile(AddFilesUI file, FileSystemItem fileItem)
        //{
        //    using (var memorystream = new MemoryStream())
        //    {
        //        file.File.SaveToStream(memorystream);
        //        memorystream.Seek(0, SeekOrigin.Begin);
        //        using (var fs = new FileStream($@"{fileItem.FullPath}\{file.File.FileName}", FileMode.CreateNew))
        //        {
        //            fs.Write(memorystream.ToArray(), 0, memorystream.ToArray().Length);
        //            fs.Flush();
        //        }

        //    }

        //}
        public static IEnumerable<FileSystemItem> GetChildItems(string path, string parentPath)
        {
            var result = new List<FileSystemItem>();

            // Add files in current directory
            var files = Directory.GetFiles(path);
            foreach (var filePath in files)
            {
                var item = new FileSystemItem()
                {
                    CreatedAt = File.GetCreationTime(filePath),
                    UpdatedAt = File.GetLastWriteTime(filePath),
                    Name = Path.GetFileName(filePath),
                    Type = "file",
                    FullPath = Path.GetFullPath(filePath),
                    Parent = parentPath,
                    IsDeleted = false
                };
                if (item.Name.StartsWith("GC-"))
                {
                    item.IsDeleted = true;
                }
                //result.Add(item);
                yield return item;
            }

            // Add subdirectories
            var directories = Directory.GetDirectories(path);
            foreach (var directoryPath in directories)
            {
                var item = new FileSystemItem()
                {
                    CreatedAt = Directory.GetCreationTime(directoryPath),
                    UpdatedAt = Directory.GetLastWriteTime(directoryPath),
                    Name = Path.GetFileName(directoryPath),
                    Type = "directory",
                    FullPath = Path.GetFullPath(directoryPath),
                    Parent = parentPath,
                    IsDeleted = false
                };
                if (item.Name.StartsWith("GC-"))
                {
                    item.IsDeleted = true;
                }
                //result.Add(item);
                yield return item;

                // Recursively add child items
                //var childItems = GetChildItems(directoryPath, item.FullPath);
                //result.AddRange(childItems);
            }

            //return result;
        }
    }
    public class FileSystemItem 
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string FullPath { get; set; }
        public string Parent { get; set; }
        public bool IsDeleted { get; set; }

        public bool HasChildrens
        {
            get
            {
                if (this.Type == "directory")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        IEnumerable<FileSystemItem> fileSystemItems;
        public IEnumerable<FileSystemItem> FileSystemItems
        {
            get
            {
                Debug.WriteLine("Debug");
                if (this.Type == "directory")
                {
                    return FileSystemHelper.GetChildItems(this.FullPath, this.Parent);
                }
                return Enumerable.Empty<FileSystemItem>();
            }

        }
    }
    public class ChemicalElementGroup
    {
        readonly Lazy<List<ChemicalElementGroup>> _groups = new Lazy<List<ChemicalElementGroup>>();
        public ChemicalElementGroup(string name, List<ChemicalElementGroup> groups = null)
        {
            Name = name;
            if (groups != null)
                Groups.AddRange(groups);
        }
   
        public bool HasChildren
        {
            get
            {
                if(Groups!=null)
                {

                    if (Groups.Count > 0)
                        return true;
                    return false;
                }
                return false;
            }
        }
        public string Name { get; set; }
        public List<ChemicalElementGroup> Groups { get { return _groups.Value; } }
    }
    public static class ChemicalElements
    {
        private static readonly Lazy<List<ChemicalElementGroup>> chemicalElementGroups = new Lazy<List<ChemicalElementGroup>>(() => {
            List<ChemicalElementGroup> groups = new List<ChemicalElementGroup>() {
                new ChemicalElementGroup("Metals", new List<ChemicalElementGroup>() {
                    new ChemicalElementGroup("Alkali metals"),
                    new ChemicalElementGroup("Alkaline earth metals"),
                    new ChemicalElementGroup("Inner transition elements", new List<ChemicalElementGroup>() {
                        new ChemicalElementGroup("Lanthanides"),
                        new ChemicalElementGroup("Actinides")
                    }),
                    new ChemicalElementGroup("Transition elements"),
                    new ChemicalElementGroup("Other metals")
                }),
                new ChemicalElementGroup("Metalloids"),
                new ChemicalElementGroup("Nonmetals", new List<ChemicalElementGroup>() {
                    new ChemicalElementGroup("Halogens"),
                    new ChemicalElementGroup("Noble gases"),
                    new ChemicalElementGroup("Other nonmetals")
                })
            };
            return groups;
        });
        public static List<ChemicalElementGroup> Groups { get { return chemicalElementGroups.Value; } }
    }
}
