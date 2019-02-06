using System;
using System.Collections.Generic;
using System.Text;

namespace DataProcessing
{
    public struct CSVpolozka
    {
        public string JmenoSloupce
        {
            get;
            set;
        }
        public string HodnotaSloupce
        {
            get;
            set;
        }

        public CSVpolozka(string jmenoSloupce, string hodnotaSloupce)
        {
            this.JmenoSloupce = jmenoSloupce;
            this.HodnotaSloupce = hodnotaSloupce;
        }
    }
}
