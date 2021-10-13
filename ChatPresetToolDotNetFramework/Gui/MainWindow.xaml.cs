using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using SpeechToTextDotNetFramework.SpeechToText;

namespace ChatPresetTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Regex _regex = new Regex(@"^Minecraft\*? 1\.\d+?(?:\.\d+?)?(.*)$", RegexOptions.Compiled);

        private readonly Stack<string> _stack = new Stack<string>();
        private readonly Stopwatch _timer = Stopwatch.StartNew();

        private SpeechToText _speechToText;
        private int speech_num_chars_printed = 0;
        private string speech_printed_line = "";

        public MainWindow()
        {
            InitializeComponent();

            TextBox.Text = Properties.Settings.Default.TextInput;
            AutoSend.IsChecked = Properties.Settings.Default.AutoSend;
            CommentOut.IsChecked = Properties.Settings.Default.CommentOut;

            Title += $" v{Assembly.GetExecutingAssembly().GetName().Version}";

            GlobalHook.EnableHook();
            GlobalHook.KeyDownEvent += OnKeyPress;
            GlobalHook.KeyUpEvent += OnKeyRelease;
        }

        private void InitializeSpeechToText(bool doWarning)
        {
            string path = Properties.Settings.Default.GoogleCredentialPath;
            if (path.Length > 0 && File.Exists(path))
            {
                try
                {
                    _speechToText = new SpeechToText(path);
                }
                catch (InvalidOperationException e)
                {
                }
            }

            if (_speechToText == null)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    FilterIndex = 1,
                    Filter = "Google Cloud 認証情報ファイル (.json)|*.json"
                };
                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    path = openFileDialog.FileName;
                    Properties.Settings.Default.GoogleCredentialPath = path;
                    Properties.Settings.Default.Save();
                }

                if (path.Length > 0 && File.Exists(path))
                {
                    try
                    {
                        _speechToText = new SpeechToText(path);
                    }
                    catch (InvalidOperationException e)
                    {
                        if (doWarning)
                        {
                            MessageBox.Show("認証情報の読み込みに失敗しました。", "認証情報の読み込みに失敗", MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }

                        return;
                    }
                }
            }

            if (_speechToText == null)
            {
                return;
            }

            Title += " - 音声認識有効";
            _speechToText.LogOutput += (transcript, isFinal) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var index = TextBox.Text.IndexOf(Environment.NewLine, StringComparison.Ordinal);
                    var after = TextBox.Text.Substring(index + Environment.NewLine.Length);

                    // 書き込み
                    TextBox.Text = $"{speech_printed_line}{transcript}{Environment.NewLine}{after}";

                    if (isFinal)
                    {
                        speech_printed_line += transcript;
                    }
                });
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSpeechToText(false);
            new SystemMenu(() => { InitializeSpeechToText(true); }, "SpeechToTextを有効にする")
                .CreateMenu(this);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_speechToText != null)
            {
                _speechToText.Dispose();
            }

            GlobalHook.DisableHook();

            Properties.Settings.Default.TextInput = TextBox.Text;
            Properties.Settings.Default.AutoSend = AutoSend.IsChecked;
            Properties.Settings.Default.CommentOut = CommentOut.IsChecked;
            Properties.Settings.Default.Save();
        }

        private enum Keys : byte
        {
            VK_RETURN = 0x0D,
            VK_T = 0x54,
            VK_V = 0x56,
            VK_F12 = 0x7B,
            VK_LCONTROL = 0xA2,
            VK_LMENU = 0xA4,
        }

        private async void OnKeyPress(int vkCode)
        {
            // Altが押されたら
            if (_speechToText != null && vkCode == (byte)Keys.VK_LMENU)
            {
                // 短い間隔を弾く
                if (_timer.Elapsed.Milliseconds < 30)
                {
                    return;
                }

                _timer.Restart();

                // マイクラのウィンドウか判定
                IntPtr handle = ActiveWindow.GetActiveWindow();
                string className = ActiveWindow.GetClassName(handle);
                if (className != "LWJGL" && className != "GLFW30")
                {
                    return;
                }

                string title = ActiveWindow.GetWindowTitle(handle);
                if (!_regex.IsMatch(title))
                {
                    return;
                }

                if (!_speechToText.Running)
                {
                    // 改行
                    var index = TextBox.Text.IndexOf(Environment.NewLine, StringComparison.Ordinal);
                    if (index == -1 || index > 0)
                    {
                        TextBox.Text = $"{Environment.NewLine}{TextBox.Text}";
                    }

                    // クリア
                    speech_num_chars_printed = 0;
                    speech_printed_line = "";
                    // 識別開始
                    _speechToText.Start();
                }
            }

            // Enterが押されたら
            if (vkCode == (byte)Keys.VK_F12)
            {
                // 短い間隔を弾く
                if (_timer.Elapsed.Milliseconds < 30)
                {
                    return;
                }

                _timer.Restart();

                await SendTextToMinecraft();
            }
        }

        private async Task SendTextToMinecraft()
        {
            // テキストがなかったら何もしない
            var split = SplitTextBox();

            // マイクラのウィンドウか判定
            IntPtr handle = ActiveWindow.GetActiveWindow();
            string className = ActiveWindow.GetClassName(handle);
            if (className != "LWJGL" && className != "GLFW30")
            {
                return;
            }

            string title = ActiveWindow.GetWindowTitle(handle);
            if (!_regex.IsMatch(title))
            {
                return;
            }

            // 一行目だけ取り出し
            string text = split[0];

            if (text.Length > 0)
            {
                try
                {
                    // Tを押す
                    KeySimulator.PressKey((byte)Keys.VK_T);
                    await Task.Delay(5);
                    KeySimulator.ReleaseKey((byte)Keys.VK_T);

                    await Task.Delay(10);

                    //using var backup = new ClipboardBackup();
                    Clipboard.SetText(text);

                    await Task.Delay(10);

                    KeySimulator.ReleaseKey((byte)Keys.VK_LCONTROL);
                    KeySimulator.ReleaseKey((byte)Keys.VK_V);
                    await Task.Delay(10);
                    KeySimulator.PressKey((byte)Keys.VK_LCONTROL);
                    KeySimulator.PressKey((byte)Keys.VK_V);
                    await Task.Delay(10);
                    KeySimulator.ReleaseKey((byte)Keys.VK_LCONTROL);
                    KeySimulator.ReleaseKey((byte)Keys.VK_V);

                    if (AutoSend.IsChecked)
                    {
                        KeySimulator.PressKey((byte)Keys.VK_RETURN);
                        await Task.Delay(5);
                        KeySimulator.ReleaseKey((byte)Keys.VK_RETURN);
                    }
                }
                catch (COMException)
                {
                    // コピペの競合
                }
            }

            // 行送り
            TextBox.Text = (split.Length == 1) ? "" : split[1];
            if (text.Length > 0)
            {
                _stack.Push(text);
                Previous.Text = text;
            }
        }

        private string[] SplitTextBox()
        {
            string[] split;
            {
                string textBox = TextBox.Text;
                while (true)
                {
                    var pos = textBox.IndexOf(Environment.NewLine, StringComparison.Ordinal);
                    if (pos == -1)
                    {
                        split = new[] { textBox };
                        break;
                    }

                    var previous = textBox.Substring(0, pos);
                    textBox = textBox.Substring(pos + Environment.NewLine.Length);
                    split = new[] { previous, textBox };

                    if (CommentOut.IsChecked && previous.StartsWith("#"))
                    {
                        continue;
                    }

                    if (pos != 0)
                    {
                        break;
                    }
                }
            }
            return split;
        }

        private async void OnKeyRelease(int vkCode)
        {
            // Altが離されたら
            if (_speechToText != null && vkCode == (byte)Keys.VK_LMENU)
            {
                await Task.Delay(1000);

                _speechToText.Stop();

                var index = TextBox.Text.IndexOf(Environment.NewLine, StringComparison.Ordinal);
                if (index > 0)
                {
                    await SendTextToMinecraft();
                }
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_stack.Count > 0)
            {
                string text = _stack.Pop();
                TextBox.Text = $"{text}{Environment.NewLine}{TextBox.Text}";

                string prev = _stack.Count > 0 ? _stack.Peek() : "";

                Previous.Text = prev;
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();
            TextBox.Text = Properties.Settings.Default.TextInput;
        }
    }
}