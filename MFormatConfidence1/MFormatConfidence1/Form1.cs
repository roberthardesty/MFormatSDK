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
        private MFPreviewClass Preview;
        private MFLiveClass Live;

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
            Writer = new MFWriter();
            Reader = new MFReader();
            Preview = new MFPreviewClass();

            Preview.PreviewWindowSet("", panelPreview.Handle.ToInt32());
            Preview.PreviewEnable("", 1, 1);

            FillComboBox();

            cancelSource = new CancellationTokenSource();
            threadWorker = new Thread(() => thread_DoWork(cancelSource.Token));
            threadWorker.Name = "thread_DoWork";
            threadWorker.Start();

            avProps.vidProps.eVideoFormat = eMVideoFormat.eMVF_Custom;

        }

        private void thread_DoWork(CancellationToken cancelSource)
        {
            while (!cancelSource.IsCancellationRequested)
            {
                if (isWorking)
                {
                    MFFrame frame = null;
                    Live.SourceFrameConvertedGet(ref avProps, -1, out frame, "");

                    if(frame != null)
                    {
                        if (isRec)
                        {
                            btnRecord.Enabled = false;
                            btnRecord.Text = "Recording";
                            btnRecord.BackColor = Color.Red;

                            try
                            {
                                Writer.ReceiverFramePut(frame, -1, "");

                                //UpdateStatistic()
                            }
                            catch(Exception e)
                            {
                                MessageBox.Show("Error: " e.ToString());
                            }
                        }
                    }
                    else
                    {
                        btnRecord.Enabled = true;
                        btnRecord.Text = "Record";
                        btnRecord.BackColor = Color.AntiqueWhite;
                    }

                    Preview.ReceiverFramePut(frame, -1, "");

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
                Live.DeviceGetCount(eMFDeviceType.eMFDT_Video, out deviceCount);

                if(deviceCount > 0)
                {
                    for(int i =0; i<deviceCount; i++)
                    {
                        string strName;
                        int isBusy;
                        Live.DeviceGetByIndex(eMFDeviceType.eMFDT_Video, i, out strName, out isBusy);
                        comboBoxDevices.Items.Add(strName);
                    }

                    comboBoxDevices.SelectedIndex = comboBoxDevices.Items.Count -1;
                }

                btnRecord.Enabled = false;
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        private void comboBoxDevices_SelectedIndexChange(object sender, EventArgs e)
        {
            //M_VID_PROPS vidProps;

            Live.DeviceSet(eMFDeviceType.eMFDT_Video, comboBoxDevices.SelectedIndex, "");

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
            Live.DeviceSet(eMFDeviceType.eMFDT_Video, comboBoxDevices.SelectedIndex, "");
            M_VID_PROPS vidProps;
            string name;
            if()
        }

        private void InitRecord()
        {

        }
    }
}
