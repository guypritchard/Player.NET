namespace DJPad.Core.Interfaces
{
    using System;

    public interface IFileSource : ISampleSource, IMetadataSource
    {
        string GetFileType();

        void Load(string filename);

        void Close();

        TimeSpan Duration { get; }

        TimeSpan Position { get; set; }

        string FileName { get; set; }

        bool EndOfFile { get; }
    }
}