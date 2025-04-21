using COMTRADE_parser.SelectSignal;
using ComtradeParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace COMTRADE_parser.MathMetod
{
    internal class TypeOfFaultAnalizer
    {
        private readonly IDataReader _dataReader;
        private double _magnitudeI0I2;
        private double _magnitudeI2I1;
        private double _angleI2I0;
        private FourieAnalizer fourieAnalizer_I;
        private FourieAnalizer fourieAnalizer_V;
        private double[] current_A;
        private double[] current_B;
        private double[] current_C;
        private double[] voltage_A;
        private double[] voltage_B;
        private double[] voltage_C;
        private bool _k3;
        private bool _kab2;
        private bool _kbc2;
        private bool _kca2;
        private bool _ka1;
        private bool _kb1;
        private bool _kc1;
        private bool _kab11;
        private bool _kbc11;
        private bool _kca11;

        public TypeOfFaultAnalizer(IDataReader dataReader)
        {
            _k3 = false;
            _kab2 = false;
            _kbc2 = false;
            _kca2 = false;
            _ka1 = false;
            _kb1 = false;
            _kc1 = false;
            _kab11 = false;
            _kbc11 = false;
            _kca11 = false;
            _dataReader = dataReader;
            Currents currents = new Currents(_dataReader);
            Voltage voltages = new Voltage(_dataReader);
            fourieAnalizer_I = new FourieAnalizer(_dataReader.AnalogData.Count, _dataReader.Config.Rate, current_A, current_B, current_C);
            fourieAnalizer_V = new FourieAnalizer(_dataReader.AnalogData.Count, _dataReader.Config.Rate, voltage_A, voltage_B, voltage_C);
        }
    }
}
