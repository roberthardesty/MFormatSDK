using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using MFORMATSLib;
using System.Threading;

namespace MFormatConfidence1
{
    public partial class Form1 : Form
    {

        private MFWriter Writer;
        private MFReader Reader;
        private MFPreview Preview;

        private CancellationTokenSource cancelSource;
        private Thread threadWorker;

        public Form1()
        {
            InitializeComponent();
        }

       private void Form1_Load(object sender, EventArgs e)
        {
            Writer = new MFWriter();
            Reader = new MFReader();
            Preview = new MFPreview();

            Preview.PreviewWindowSet("", panelPreview.Handle.ToInt32());
            Preview.PreviewEnable("", 1, 1);

            cancelSource = new CancellationTokenSource();
            threadWorker = new Thread(() => thread_DoWork(cancelSource.Token));
            threadWorker.Name = "thread_DoWork";
            threadWorker.Start();

        }

        private void thread_DoWork(CancellationTokenSource cancelSource)
        {

        }
    }
}
