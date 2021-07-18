using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

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

        public MainWindow()
        {
            InitializeComponent();
            TextBox.Text = Properties.Settings.Default.TextInput;
            Title += $" v{Assembly.GetExecutingAssembly().GetName().Version}";

            GlobalHook.EnableHook();
            GlobalHook.KeyEvents += OnKeyPress;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            GlobalHook.DisableHook();

            Properties.Settings.Default.TextInput = TextBox.Text;
            Properties.Settings.Default.Save();
        }

        private enum Keys : byte
        {
            VK_T = 0x54,
            VK_V = 0x56,
            VK_F12 = 0x7B,
            VK_LCONTROL = 0xA2,
        }

        private async void OnKeyPress(int vkCode)
        {
            // Enterが押されたら
            if (vkCode != (byte) Keys.VK_F12)
            {
                return;
            }

            // 短い間隔を弾く
            if (_timer.Elapsed.Milliseconds < 30)
            {
                return;
            }

            _timer.Restart();

            // テキストがなかったら何もしない
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

                    split = new[] { textBox.Substring(0, pos), textBox = textBox.Substring(pos + Environment.NewLine.Length) };

                    if (pos != 0)
                    {
                        break;
                    }
                }
            }


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
                    KeySimulator.PressKey((byte) Keys.VK_T);
                    KeySimulator.ReleaseKey((byte) Keys.VK_T);

                    await Task.Delay(10);

                    //using var backup = new ClipboardBackup();
                    Clipboard.SetText(text);

                    await Task.Delay(10);

                    KeySimulator.ReleaseKey((byte) Keys.VK_LCONTROL);
                    KeySimulator.ReleaseKey((byte) Keys.VK_V);
                    await Task.Delay(10);
                    KeySimulator.PressKey((byte) Keys.VK_LCONTROL);
                    KeySimulator.PressKey((byte) Keys.VK_V);
                    await Task.Delay(10);
                    KeySimulator.ReleaseKey((byte) Keys.VK_LCONTROL);
                    KeySimulator.ReleaseKey((byte) Keys.VK_V);
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
    }
}