using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace COMTRADE_parser
{
    internal interface ISignalAnalizer
    {
        Complex[] ProcessedSignalA { get; set; }
        Complex[] ProcessedSignalB { get; set; }
        Complex[] ProcessedSignalC { get; set; }

        //Complex[] Pramaya { get; set; }
        //Complex[] Obratnaya { get; set; }
        //Complex[] Nulevaya { get; set; }

        void RunAnalize();


    }
}
