using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Player.Net._3
{
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            IoCContainer.AddMapping<IMediaCorpus, IMediaCorpus>();

            Application.Run(new Player());
        }
    }
}
