namespace Player.Net._2
{
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;
    using DJPad.UI.GdiPlus;
    using UserInterface;

    using DJPad.Player;
    using DJPad.UI;

    public sealed class Configuration : GdiPlusChromelessForm
    {
        private readonly IUserInterface<Bitmap> userInterface = new ConfigurationUi();
        private PlayerState playerState;
        private readonly WindowState windowState;
        private readonly WindowState parentState;

        public Configuration(PlayerState playerState, WindowState parentState) : base(true)
        {
            this.playerState = playerState;
            this.parentState = parentState;

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
                this.Location = new Point(this.parentState.Form.Location.X - this.parentState.Form.Size.Width, this.parentState.Form.Location.Y);
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
