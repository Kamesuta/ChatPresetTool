using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechToTextDotNetFramework.SpeechToText
{
    class RecordModel : IAudioRecorder
    {
        #region 変数

        private WaveInEvent waveIn;
        private bool isStoped = false;
        private bool isDisposed = false;

        #endregion

        #region メソッド

        public void Start()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("RecordModel");
            }

            if (this.waveIn != null)
            {
                return;
            }

            this.waveIn = new WaveInEvent();
            this.waveIn.DataAvailable += this.OnDataAvailable;
            this.waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

            this.waveIn.StartRecording();
        }

        public void Stop()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("RecordModel");
            }

            if (this.isStoped)
            {
                return;
            }

            this.waveIn.StopRecording();
            this.isStoped = true;

            this.waveIn.Dispose();

            this.waveIn = null;
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("RecordModel");
            }

            this.Stop();
            GC.SuppressFinalize(this);
            this.isDisposed = true;
        }

        ~RecordModel()
        {
            this.Dispose();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.isStoped) return;
            this.RecordDataAvailabled?.Invoke(this, new RecordDataAvailabledEventArgs(e.Buffer, e.BytesRecorded));
        }

        #endregion

        #region イベント

        public event RecordDataAvailabledEventHandler RecordDataAvailabled;

        #endregion
    }
}