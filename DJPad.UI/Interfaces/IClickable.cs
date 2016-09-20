using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DJPad.UI.Interfaces
{
    public interface IClickable : IControl
    {
        Action<Point> OnClick { get; set; }

        Action<Point> OnDoubleClick { get; set; }
    }

    public interface IAcceptKeys : IControl
    {
        /// <summary>
        /// OnKey
        /// </summary>
        /// <returns>
        /// An Action which returns a key, a boolean indicating whether shift is pressed.
        /// </returns>
        Action<Keys, bool, bool> OnKey { get; set; }
    }

    public interface IScrollable : IControl
    {
        Func<int, bool> OnScroll { get; set; }
    }
    
    public interface IControl
    {

    }
}
