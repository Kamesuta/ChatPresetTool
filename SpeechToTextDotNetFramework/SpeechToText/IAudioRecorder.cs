using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechToTextDotNetFramework.SpeechToText
{
    interface IAudioRecorder : IDisposable
    {
        void Start();

        void Stop();

        event RecordDataAvailabledEventHandler RecordDataAvailabled;
    }

    public delegate void RecordDataAvailabledEventHandler(object sender, RecordDataAvailabledEventArgs e);

    public class RecordDataAvailabledEventArgs : EventArgs
    {
        public byte[] Buffer { get; }
        public int Length { get; }

        public RecordDataAvailabledEventArgs(byte[] buffer, int length)
        {
            this.Buffer = buffer;
            this.Length = length;
        }
    }
}