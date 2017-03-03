//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MCHROMAKEYLib;
using MFORMATSLib;
using Microsoft.Win32;
using MLCHARGENLib;
using eMFCC = MFORMATSLib.eMFCC;
using eMFrameClone = MFORMATSLib.eMFrameClone;
using eMFrameFlags = MFORMATSLib.eMFrameFlags;
using eMVideoFormat = MFORMATSLib.eMVideoFormat;
using M_AV_PROPS = MFORMATSLib.M_AV_PROPS;
using M_TIME = MFORMATSLib.M_TIME;
namespace MFormatConfidence1
{
    public partial class Playback : Form
    {
        enum eState
        {
            eST_PlayFwd,
            eST_Pause,
            eST_PlayRev,
            eST_StepFwd,
            eST_StepRev
        };
        class PlayerState
        {
            public Object stateLock = new Object();
            public eState state;
            public double dblFrameRequest;
            public double dblDuration;
            public double dblRate;
            public string strFileName;
            public bool bLoop;
        }

        private MFReaderClass objMFReader;            //MFormats Reader object
        private MFPreviewClass objPreview;            //Preview object
        private PlayerState playerState;              //Playback state
        private MFRendererClass objRenderer;          //MRenderer object
        private MFSinkClass objMFSink;                //MFSink object

        private MFFactory objFactory;			//Frames class used to create MFrame from file
        private Thread threadWorker;	//Working thread
        private Object objLock = new Object();
        private bool isWorking = false;
        private bool isThreading = false;

        private M_AV_PROPS avProps;
        private M_TIME Time;
        private string[] m_arrArgs;
        private CancellationTokenSource cancelSource;

        public Playback(string[] args)
        {
            m_arrArgs = args;
            InitializeComponent();
        }

        private void Playback_Load(object sender, EventArgs e)
        {
            objFactory = new MFFactory();
            objPreview = new MFPreviewClass();
            playerState = new PlayerState();
            objRenderer = new MFRendererClass();
            objMFSink = new MFSinkClass();

            //playerState.bLoop = checkBoxLoop.Checked;

            // First file start with pause, next file open in same state
            pause();

            //Configure preview
            objPreview.PreviewWindowSet("", panelPreview.Handle.ToInt32());
            //objPreview.PreviewEnable("", Convert.ToInt32(checkBoxAudio.Checked), Convert.ToInt32(checkBoxVideo.Checked));

            //FillVideoFormats();
            avProps.vidProps.eVideoFormat = eMVideoFormat.eMVF_Custom;


            cancelSource = new CancellationTokenSource();
            threadWorker = new Thread(() => thread_DoWork(cancelSource.Token));
            threadWorker.Name = "thread_DoWork";
            threadWorker.Start();

            if (m_arrArgs.Length > 0 && File.Exists(m_arrArgs[0]))
                OpenFile(m_arrArgs[0]);
        }

        private void thread_DoWork(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (NextFrame() == false) return;
            }
        }

        private bool NextFrame()
        {
            return true;
        }


        //button click events
        private void btnLoad_Click(object sender, EventArgs e)
        {
            // Open next file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK && openFileDialog.FileName != string.Empty)
                OpenFile(openFileDialog.FileName);
        }
    }
}
