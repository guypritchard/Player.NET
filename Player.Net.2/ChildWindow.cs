namespace Player.Net._2
{
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using DJPad.UI.GdiPlus;
    using UserInterface;

    using DJPad.Player;
    using DJPad.UI;

    public enum RelativePosition {  Left, Right, Top, Bottom };

    public sealed class ChildWindow : GdiPlusChromelessForm
    {
        private readonly IUserInterface<Bitmap> userInterface;
        private PlayerState playerState;
        private readonly WindowState windowState;
        private readonly WindowState parentState;

        public ChildWindow(IUserInterface<Bitmap> ui, RelativePosition pos, PlayerState playerState, WindowState parentState) : base(true)
        {
            this.userInterface = ui;
            this.playerState = playerState;
            this.parentState = parentState;

            this.DragEnter += (sender, args) =>
            {
                args.Effect = args.Data.GetDataPresent(DataFormats.FileDrop)
                            ? DragDropEffects.Copy
                            : DragDropEffects.None;
            };

            this.DragDrop += (sender, args) =>
            {
                var fileNames = args.Data.GetData(DataFormats.FileDrop) as string[];
                if (fileNames == null || !fileNames.Any())
                {
                    return;
                }
            };

            this.windowState = new WindowState
                {
                    Close = () => this.Hide(),
                    Repaint = () => this.Invalidate(),
                    Location = () => this.Location,
                };

            this.playerState.PlayStateChanged += (i,s) => this.Invalidate();
            this.CreateUi();

            this.parentState.Form.LocationChanged += (s, e) =>
            {
                if (pos == RelativePosition.Left)
                {
                    this.Location = new Point(this.parentState.Form.Location.X - this.parentState.Form.Size.Width, this.parentState.Form.Location.Y);
                }
                else
                {
                    this.Location = new Point(this.parentState.Form.Location.X + this.parentState.Form.Size.Width, this.parentState.Form.Location.Y);
                }
            };
        }

        private void CreateUi()
        {
            var layout = userInterface.GenerateUI(this.playerState, this.windowState);
            this.LightControlCollection.Clear();
            this.LightControlCollection.AddRange(layout);
            this.Text = userInterface.Name;
            this.ClientSize = userInterface.Size;
        }
    }
}
