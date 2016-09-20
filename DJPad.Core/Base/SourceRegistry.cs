namespace DJPad.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using DJPad.Core.Interfaces;
    using DJPad.Sources;

    public static class SourceRegistry
    {
        #region Static Fields

        private static readonly Dictionary<string, IFileSource> sourceRegistry =
            new Dictionary<string, IFileSource>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Constructors and Destructors

        static SourceRegistry()
        {
            RegisterSource(new Mp3Source());
            RegisterSource(new WmaSource());
            RegisterSource(new CdSource());
            RegisterSource(new WaveSource());
        }

        #endregion

        #region Public Methods and Operators

        public static string[] GetSupportedFileTypes()
        {
            return sourceRegistry.Values.Select(s => s.GetFileType()).ToArray();
        }

        public static bool IsSupported(string filename)
        {
            return GetSupportedFileTypes().Contains(Path.GetExtension(filename), StringComparer.OrdinalIgnoreCase);
        }

        public static void RegisterSource<T>(T handler) where T : IFileSource
        {
            if (!sourceRegistry.Keys.Contains(handler.GetFileType(), StringComparer.OrdinalIgnoreCase))
            {
                sourceRegistry.Add(handler.GetFileType(), handler);
            }
        }

        public static IFileSource ResolveSource(string filePath, bool createNew = false)
        {
            IFileSource returnSource = new NullSource();

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("filename");
            }

            string extension = Path.GetExtension(filePath);

            if (!string.IsNullOrEmpty(extension))
            {
                sourceRegistry.TryGetValue(extension, out returnSource);
            }

            if (returnSource != null && createNew)
            {
                returnSource = (IFileSource)Activator.CreateInstance(returnSource.GetType());
            }

            returnSource.FileName = filePath;

            return returnSource;
        }

        #endregion
    }
}