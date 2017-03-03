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

            //playerState.bLoop = checkBoxLoop.Checked;

            // First file start with pause, next file open in same state
            pause();

            //Configure preview
            objPreview.PreviewWindowSet("", panelPlayback.Handle.ToInt32());
            objPreview.PreviewEnable("", 1, 1);

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
            // Get request pos and set pause and reverse flags
            double dblRequest = -1.0;
            string strParams = string.Empty;

            // Update player state
            lock (playerState.stateLock)
            {
                if (playerState.state == eState.eST_Pause)
                    strParams = " pause=true";
                else if (playerState.state == eState.eST_PlayRev || playerState.state == eState.eST_StepRev)
                    strParams = " reverse=true";

                // Update player state
                if (playerState.state == eState.eST_StepFwd || playerState.state == eState.eST_StepRev)
                {
                    // Pause on next iteration - because single frame was requested
                    playerState.state = eState.eST_Pause;
                }

                // Get request time and set next cycle request to next frame 
                // -1 as first parameter means "give me next frame", -5 means "give me next next 5th frame" etc,
                // this works accordingly when the reverse=true parameter is set.
                // positive values are uninterpreted as "give me frame at position"
                dblRequest = playerState.dblFrameRequest;
                playerState.dblFrameRequest = -1 * (int)(playerState.dblRate);
            }


            // Next frame cycle:
            // Get frame from reader and send to preview
            // Note: Preview keep frame according to frame time 

            MFFrame pFrame = null;
            lock (objLock) // For prevent reader replace in OpenFile() and overlay change
            {
                // Get next frame or frame by position
                // -1 as first parameter means "give me next frame", -5 means "give me next next 5th frame" etc,
                // this works accordingly when the reverse=true parameter is set.
                // positive values are uninterpreted as "give me frame at position"
                try
                {
                    if (objMFReader != null)
                        objMFReader.SourceFrameConvertedGetByTime(ref avProps, dblRequest, -1, out pFrame, strParams);

                    // Update avProps
                    if (pFrame != null)
                    {
                        int nASamples = 0;
                        pFrame.MFAVPropsGet(out avProps, out nASamples);
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Error occurs during file decoding:\n\n" + ex.Message, playerState.strFileName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }


            if (pFrame != null)
            {
                try
                {                  
                    releaseComObj(pFrame);
                }

                catch (System.Exception ex)
                {
                    MessageBox.Show("Error occurs during frame processing:\n\n" + ex.Message, playerState.strFileName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }


            // Check for the last frame
            if ((Time.eFFlags & eMFrameFlags.eMFF_Last) != 0)
            {
                lock (playerState.stateLock)
                {
                    // Pause playback at the end of the file if loop is disabled
                    if (!playerState.bLoop)
                        pause();

                    if (playerState.state == eState.eST_PlayRev)
                    {
                        // Rewind to end in case of reverse playback
                        seek(playerState.dblDuration);
                    }
                    else if (playerState.state == eState.eST_PlayFwd)
                    {
                        // Rewind to start in case of direct playback
                        seek(0);
                    }
                }
            }
            return true;
        }


        private void OpenFile(string _filename)
        {          
            // Open next file
            // Change current reader with new one
            lock (objLock) // For preview access from worker thread
            {
                try
                {
                    if (objMFReader == null)
                        objMFReader = new MFReaderClass();

                    objMFReader.ReaderOpen(_filename, "");

                    pause();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error open file:" + _filename + "\n\n" + ex.Message);
                    return;
                }

                //Get file duration
                objMFReader.ReaderDurationGet(out playerState.dblDuration);
                playerState.strFileName = _filename;

                MFFrame pFrame;
                objMFReader.SourceFrameGetByTime(-1, -1, out pFrame, "");
                
            }

            seek(0);

            GC.Collect();
        }

        private void releaseComObj(object comObj)
        {
            if (comObj != null)
                Marshal.ReleaseComObject(comObj);
        }

        private void btnLoadFile_Click(object sender, EventArgs e)
        {
            // Open next file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK && openFileDialog.FileName != string.Empty)
                OpenFile(openFileDialog.FileName);
        }

        private void play()
        {
            lock (playerState.stateLock)
            {
                // Set direct playback state
                playerState.dblRate = 1;
                playerState.state = eState.eST_PlayFwd;
            }
        }

        private void reverse()
        {
            lock (playerState.stateLock)
            {
                // Set reverse playback state
                playerState.dblRate = 1;
                playerState.state = eState.eST_PlayRev;
            }
        }

        private void pause()
        {
            lock (playerState.stateLock)
            {
                // Set pause state
                playerState.dblRate = 1;
                playerState.state = eState.eST_Pause;
            }
        }

        private void stepFwd()
        {
            lock (playerState.stateLock)
            {
                // Set single frame forward state
                playerState.state = eState.eST_StepFwd;
            }
        }

        private void stepBack()
        {
            lock (playerState.stateLock)
            {
                // Set single frame backward state
                playerState.state = eState.eST_StepRev;
            }
        }

        private void fastFwd()
        {
            lock (playerState.stateLock)
            {
                // Set needed rate and forward playback state
                if (playerState.state == eState.eST_PlayFwd)
                {
                    playerState.dblRate += 1.0;
                }
                else
                {
                    playerState.dblRate = 2.0;
                    playerState.state = eState.eST_PlayFwd;
                }
            }
        }

        private void fastBackw()
        {
            lock (playerState.stateLock)
            {
                // Set needed rate and reverse playback state
                if (playerState.state == eState.eST_PlayRev)
                {
                    playerState.dblRate += 1.0;
                }
                else
                {
                    playerState.dblRate = 2.0;
                    playerState.state = eState.eST_PlayRev;
                }
            }
        }
        private void seek(double Pos)
        {
            lock (playerState.stateLock)
            {
                // Request frame at the specified position
                playerState.dblFrameRequest = Pos;
            }

            // Start worker (e.g. if error during playback occurs)
            //m_bWork = true;
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            play();
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            pause();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            pause();
            //rewindToStart();          
        }
    }
}
