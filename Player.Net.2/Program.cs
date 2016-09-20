namespace Player.Net._2
{
    using System;
    using System.Deployment.Application;
    using System.Windows.Forms;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using DJPad.Db;

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            IoCContainer.AddMapping<IMediaCorpus, SqlCeMediaCorpus>(true);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                if (ApplicationDeployment.CurrentDeployment.IsFirstRun)
                {
                    Application.Run(new ReleaseNotes());
                }
            }

            Application.Run(new Player());
        }
    }
}
