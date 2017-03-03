//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Runtime.InteropServices;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using MFORMATSLib;
//using System.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MFORMATSLib;

namespace MFormatConfidence1
{
    public partial class Form1 : Form
    {

        private MFWriterClass mWriter;
        private MFReaderClass mReader;
        private MFPreviewClass mPreview;
        private MFLiveClass mLive;

        M_AV_PROPS avProps;

        private bool isWorking;
        private bool isRec;

        private CancellationTokenSource cancelSource;
        private Thread threadWorker;

        public Form1()
        {
            InitializeComponent();
        }

       private void Form1_Load(object sender, EventArgs e)
        {
            mWriter = new MFWriterClass();
            mReader = new MFReaderClass();
            mPreview = new MFPreviewClass();
            //Preview = new MFPreviewClass();
            try
            {
                mLive = new MFLiveClass();
            }
            catch(Exception ex)
            {
                MessageBox.Show("No Live Devices Available: " + ex.ToString());
            }
            mPreview.PreviewWindowSet("", panelPreview.Handle.ToInt32());
            mPreview.PreviewEnable("", 1, 1);

            FillComboBox();

            cancelSource = new CancellationTokenSource();
            threadWorker = new Thread(() => thread_DoWork(cancelSource.Token));
            threadWorker.Name = "thread_DoWork";
            threadWorker.Start();

            avProps.vidProps.eVideoFormat = eMVideoFormat.eMVF_Custom;
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;

        }

        private void thread_DoWork(CancellationToken cancelSource)
        {
            while (!cancelSource.IsCancellationRequested)
            {
                if (isWorking)
                {
                    MFFrame frame = null;
                    mLive.SourceFrameConvertedGet(ref avProps, -1, out frame, "");

                    if(frame != null)
                    {
                        if (isRec)
                        {
                            btnRecord.Enabled = false;
                            btnRecord.Text = "Recording";
                            btnRecord.BackColor = Color.Red;

                            try
                            {
                                mWriter.ReceiverFramePut(frame, -1, "");

                                //UpdateStatistic()
                            }
                            catch(Exception e)
                            {
                                MessageBox.Show("Error: " +  e.ToString());
                            }
                        }
                    }
                    else
                    {
                        btnRecord.Enabled = true;
                        btnRecord.Text = "Record";
                        btnRecord.BackColor = Color.AntiqueWhite;
                    }

                    mPreview.ReceiverFramePut(frame, -1, "");

                    Marshal.ReleaseComObject(frame);                
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        private void FillComboBox()
        {
            comboBoxDevices.Items.Clear();

            int deviceCount;
            try
            {
                mLive.DeviceGetCount(eMFDeviceType.eMFDT_Video, out deviceCount);

                if(deviceCount > 0)
                {
                    for(int i =0; i<deviceCount; i++)
                    {
                        string strName;
                        int isBusy;
                        mLive.DeviceGetByIndex(eMFDeviceType.eMFDT_Video, i, out strName, out isBusy);
                        comboBoxDevices.Items.Add(strName);
                    }

                    comboBoxDevices.SelectedIndex = comboBoxDevices.Items.Count -1;
                }
                else
                {
                    btnRecord.Enabled = false;
                }            
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        private void comboBoxDevices_SelectedIndexChange(object sender, EventArgs e)
        {
            //M_VID_PROPS vidProps;

            mLive.DeviceSet(eMFDeviceType.eMFDT_Video, comboBoxDevices.SelectedIndex, "");

            ////RefreshProps

            //int fCount, lCount;
            //string name, options, help;

            //Live.FormatVideoGetCount(eMFormatType.eMFT_Input, out fCount);
            //if (fCount > 0)
            //{
            //    for(int i =0; i < fCount; i++)
            //    {

            //    }
            //}
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            InitLive();
            InitRecord();
        }

        private void InitLive()
        {
            try
            {
                mLive.DeviceSet(eMFDeviceType.eMFDT_Video, comboBoxDevices.SelectedIndex, "");
                M_VID_PROPS vidProps;
                string name;
                mLive.FormatVideoGetByIndex(eMFormatType.eMFT_Input, 0, out vidProps, out name);
                mLive.FormatVideoSet(eMFormatType.eMFT_Input, vidProps);
            }
            catch(Exception e)
            {
                MessageBox.Show("Could not set a device: " + e.ToString());
            }

            isWorking = true;          
        }

        private void InitRecord()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            try
            {
                string strFormat;
                IMFProps props;
                mWriter.WriterOptionGet(eMFWriterOption.eMFWO_Format, out strFormat, out props);

                string protocol = "";
                props.PropsGet("protocol", out protocol);
                string network = "";
                props.PropsGet("network", out network);

                //if(network == "true" || protocol.Contains("udp") || protocol.Contains("rtmp") || protocol.Contains("rtsp")) && textBoxURL.Text.Length > 0)
                if (sfd.ShowDialog() == DialogResult.OK)
                    mWriter.WriterSet(sfd.FileName, 1, "format = 'webm' video::codec = 'libvpx' audio::codec = 'libvorbis'");
                isRec = true;
            }
            catch(Exception e)
            {
                MessageBox.Show("Error:  " + e.ToString());
            }
       }
        
        private void btnStopRec_Click(object sender, EventArgs e)
        {
            isRec = false;
            mWriter.WriterClose(1);
        } 
    }
}
