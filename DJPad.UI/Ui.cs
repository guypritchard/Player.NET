namespace DJPad.UI
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using DJPad.Player;
    using DJPad.UI.GdiPlus;

    public class UiWindows
    {
        public GdiPlusChromelessForm PlayList { get; set; }
    }

    public interface IUserInterface<T>
    {
        string Name { get; }
        Size Size { get; }
        IList<LightControl<T>> GenerateUI(PlayerState playerState, WindowState windowState);
    }

    public static class ControlExtensions
    {
        public static void InvokeIfRequired(this ISynchronizeInvoke obj, MethodInvoker action)
        {
            if (obj.InvokeRequired)
            {
                obj.Invoke(action, new object[0]);
            }
            else
            {
                action();
            }
        }
    }
}
