

namespace Player.Db
{
    using DJPad.Core.Interfaces;
    using DJPad.Db;
    using System.Windows;
    using DJPad.Db.Mongo;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IMediaCorpus corpus;
        private Album Player;

        public MainWindow()
        {
            this.corpus = new MongoMediaCorpus();
            this.corpus.Initialize();
            this.Player = new Album();

            this.DataContext = this;
            InitializeComponent();

            foreach (var album in corpus.Albums())
            {
                this.AlbumList.Items.Add(album);
            }

            this.PlayerControl.DataContext = Player;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void Image_OnImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
