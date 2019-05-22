using Panacea.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Panacea.Windows
{
    /// <summary>
    /// Interaction logic for DevConsole.xaml
    /// </summary>
    public partial class DevConsole : Window
    {
        public ObservableCollection<LogModel> Logs { get; }
        public DevConsole(ILogger logger)
        {
            Logs = new ObservableCollection<LogModel>();
            logger.OnLog += Logger_OnLog;
            InitializeComponent();
        }

        private void Logger_OnLog(object sender, Log e)
        {
            var log = Logs.FirstOrDefault(l => l.Name == e.Sender);
            if (log == null)
            {
                log = new LogModel() { Name = e.Sender };
                Logs.Add(log);
            }

            log.Items.Add(new LogModel() { Name =e.Message });
            
        }
    }

    public class LogModel:INotifyPropertyChanged
    {
        public LogModel()
        {
            Items = new ObservableCollection<LogModel>();
        }

        public string Name { get; set; }

        public ObservableCollection<LogModel> Items { get; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
