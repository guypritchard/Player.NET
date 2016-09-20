namespace DJPad.Core.Interfaces
{
    using System;
    using System.Drawing;
    using DJPad.Types;

    public interface IVisualisation : ISampleConsumer
    {
        Bitmap Draw(Size size, Color backgroundColor, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null);
    }
}