using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OscilAnalyzer
{
    /// <summary>
    /// Логика взаимодействия для AnalizeOscillogramView.xaml
    /// </summary>
    public partial class AnalizeOscillogramView : UserControl
    {
        public AnalizeOscillogramView()
        {
            InitializeComponent();
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void ProgressBar_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}
