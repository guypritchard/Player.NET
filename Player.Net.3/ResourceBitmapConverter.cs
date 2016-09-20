using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Player.Net._3
{
    using DJPad.Core.Utils;

    public class ResourceBitmapConverter
    {
        private Dictionary<SharpDX.Direct2D1.Bitmap, int> bitmaps = new Dictionary<SharpDX.Direct2D1.Bitmap, int>();

        internal static SharpDX.Direct2D1.Bitmap Folder_White
        {
            get
            {
                if (this.bitmaps.ContainsKey(1))
                {
                    return this.bitmaps[1];
                }
                else
                {
                    Resources.Folder_White.ToD2dBitmap(d2dWindow.Target);
                }
            }
            set
            {
                value = Resources.Folder_White.ToD2dBitmap(d2dWindow.Target);
            }
        }
    }
}
