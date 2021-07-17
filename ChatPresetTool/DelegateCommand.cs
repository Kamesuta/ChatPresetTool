using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatPresetTool
{
    public class DelegateCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        public Action<object> ExecuteHandler;
        public Func<object, bool> CanExecuteHandler;

        public bool CanExecute(object parameter)
        {
            return CanExecuteHandler?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            this.ExecuteHandler?.Invoke(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, null!);
        }
    }
}
