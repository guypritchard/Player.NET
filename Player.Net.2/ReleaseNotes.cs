using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
namespace Player.Net._2
{
    using System.Reflection;
    using System.Windows.Forms;

    public partial class ReleaseNotes : Form
    {
        public ReleaseNotes()
        {
            InitializeComponent();
            const string resourceName = "Player.Net._2.ReleaseNotes.txt";
            var assembly = Assembly.GetExecutingAssembly();

            using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
            {
                this.textBox1.Text = reader.ReadToEnd();
            }
        }
    }
}
