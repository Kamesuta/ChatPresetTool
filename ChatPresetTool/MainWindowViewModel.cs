using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ChatPresetTool
{
    public class MainWindowViewModel : BasePropertyChanged
    {
        public class Message : BasePropertyChanged
        {
            public string Text
            {
                get => _text;
                set => SetValue(value, ref this._text);
            }
            private string _text = "";
        }

        public ObservableCollection<Message> TextCollection
        {
            get => _textCollection;
            set => SetValue(value, ref this._textCollection);
        }
        private ObservableCollection<Message> _textCollection = new ObservableCollection<Message>();

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetValue(value, ref this._selectedIndex);
        }
        private int _selectedIndex = 0;

        public DelegateCommand OnFocusCommand { get; }

        public DelegateCommand CopyCommand { get; } = new DelegateCommand()
        {
            ExecuteHandler = parameter =>
            {
                Clipboard.SetText((string) parameter);
            }
        };

        public DelegateCommand OnEnterCommand { get; }

        public MainWindowViewModel()
        {
            OnFocusCommand = new DelegateCommand()
            {
                ExecuteHandler = parameter =>
                {
                    SelectedIndex = (int)parameter;
                }
            };

            OnEnterCommand = new DelegateCommand()
            {
                ExecuteHandler = parameter =>
                {
                }
            };

            TextCollection.Add(new Message() { Text = "aaaaa" });
            TextCollection.Add(new Message() { Text = "bbbb" });
        }
    }

    public class NextElementConverter : IMultiValueConverter
    {
        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var listView = (ListView) values[0];
            var textBox = (TextBox) values[1];
            return null;
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
