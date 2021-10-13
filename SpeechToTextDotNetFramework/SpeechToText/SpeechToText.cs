using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Grpc.Auth;
using Grpc.Core;

namespace SpeechToTextDotNetFramework.SpeechToText
{
    public class SpeechToText : IDisposable
    {
        private readonly Speech.SpeechClient _client;
        private readonly StreamingRecognitionConfig _streamingConfig;

        public SpeechToText(string googleCredential)
        {
            // 証明書を作成
            var credential = GoogleCredential.FromJson(File.ReadAllText(googleCredential));
            credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");

            // サーバに接続するためのチャンネルを作成
            ChannelBase channel = new Channel("speech.googleapis.com:443", credential.ToChannelCredentials());

            // Google Speech APIを利用するためのクライアントを作成
            _client = new Speech.SpeechClient(channel);

            // ストリーミングの設定
            _streamingConfig = new StreamingRecognitionConfig
            {
                Config = new RecognitionConfig
                {
                    SampleRateHertz = 16000,
                    Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                    LanguageCode = "ja-JP",
                },
                InterimResults = true,
                SingleUtterance = false,
            };
        }

        public event Action<string, bool> LogOutput = delegate { };
        private AsyncDuplexStreamingCall<StreamingRecognizeRequest, StreamingRecognizeResponse> _call;
        private IAudioRecorder _recorder;
        public bool Running { get; private set; }

        public async Task Start()
        {
            if (Running)
                return;
            Running = true;

            // ストリーミングを開始
            _call = _client.StreamingRecognize();
            {
                // Cloud Speech APIからレスポンスが返ってきた時の挙動を設定
                var responseReaderTask = Task.Run(async () =>
                {
                    // MoveNext１回につきレスポンス１回分のデータがくる
                    while (await _call.ResponseStream.MoveNext())
                    {
                        var note = _call.ResponseStream.Current;

                        // データなし
                        if (note.Results == null || note.Results.Count == 0)
                        {
                            continue;
                        }

                        // 候補なし
                        var result = note.Results[0];
                        if (result.Alternatives == null || result.Alternatives.Count == 0)
                        {
                            continue;
                        }

                        // データがあれば、認識結果を出力する
                        var transcript = result.Alternatives[0].Transcript;

                        LogOutput.Invoke(transcript, result.IsFinal);
                    }
                });

                // 最初の呼び出しを行う。最初は設定データだけを送る
                var initialRequest = new StreamingRecognizeRequest
                {
                    StreamingConfig = _streamingConfig,
                };
                _call.RequestStream.WriteAsync(initialRequest).Wait();

                // 録音モデルの作成
                _recorder = new RecordModel();

                // 録音モデルが音声データを吐いたら、それをすかさずサーバに送信する
                _recorder.RecordDataAvailabled += (sender, e) =>
                {
                    if (e.Length <= 0)
                    {
                        return;
                    }
                    // WriteAsyncは一度に一回しか実行できないので非同期処理の時は特に注意
                    // ここではlockをかけて処理が重ならないようにしている
                    lock (_recorder)
                    {
                        _call.RequestStream.WriteAsync(new StreamingRecognizeRequest
                        {
                            AudioContent = RecognitionAudio.FromBytes(e.Buffer, 0, e.Length).Content,
                        }).Wait();
                    }
                };

                // 録音の開始
                _recorder.Start();

                // 待機
                await responseReaderTask;
            }
            _call.Dispose();
        }

        public async Task Stop()
        {
            if (!Running)
                return;
            Running = false;

            _recorder.Stop();
            await _call.RequestStream.CompleteAsync();
        }

        public void Dispose()
        {
            _call?.Dispose();
            _recorder?.Dispose();
        }
    }
}
