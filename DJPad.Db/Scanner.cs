namespace DJPad.Db
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using DJPad.Core.Utils;

    public class Scanner
    {
        /// <summary>
        /// The scan folder progress event.
        /// </summary>
        public event Action<int, int> ScanProgressEvent;

        /// <summary>
        /// The scan item event.
        /// </summary>
        public event Action<string> ScanItemEvent;

        // The event has 
        public event Action<string> ScanStartEvent;

        /// <summary>
        /// The scan folder event.
        /// </summary>
        public event Action<string, int> ScanFolderEvent;

        /// <summary>
        /// The extensions of media files.
        /// </summary>
        private readonly string[] fileTypes = { ".MP3" };

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        private string Source { get; set; }
        
        /// <summary>
        /// The folders to scan.
        /// </summary>
        private int foldersToScan;

        private ConcurrentBag<string> filesToScan = new ConcurrentBag<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Scanner"/> class.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="fileTypes"></param>
        /// <param name="foundItemCallback"></param>
        public Scanner(string source, string[] fileTypes, Action<string> scanBeginCallback, Action<string> foundItemCallback)
        {
            this.Source = source;
            this.fileTypes = fileTypes;
            this.ScanItemEvent = foundItemCallback;
            this.ScanStartEvent = scanBeginCallback;
        }

        /// <summary>
        /// The scan.
        /// </summary>
        public async Task Scan()
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            await this.ScanForFiles();
            await Task.Run(() =>
            {
                if (this.ScanStartEvent != null)
                {
                    this.ScanStartEvent(this.Source);
                }

                this.filesToScan.AsParallel().ForEach(f => this.ScanFiles(f));
            });

            s.Stop();
            Debug.WriteLine(string.Format("Scanning complete in {0}ms", s.ElapsedMilliseconds));
        }

        private async Task ScanForFiles()
        {
            await Task.Run(() =>
            {
                this.foldersToScan = this.CountFolders(this.Source) - 1;
            });
        }

        /// <summary>
        /// The number of folders to scan.
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <returns>A number of folders to scan.</returns>
        private int CountFolders(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            var foundFiles = SafeFileEnumerator.EnumerateFiles(directory, "*", SearchOption.AllDirectories).ToList();
            foundFiles.ForEach(d => filesToScan.Add(d));
            return foundFiles.Count();
        }

        private void ScanFiles(string fileName)
        {
            if (this.ScanItemEvent != null)
            {
                if (fileTypes.Contains(Path.GetExtension(fileName).ToUpper()))
                {
                    this.ScanItemEvent(fileName);
                }
            }
        }
    }
}
