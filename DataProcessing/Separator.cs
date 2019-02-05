using System;
using System.Collections.Generic;
using System.Text;

namespace DataProcessing
{
    /// <summary>
    /// Separator jednotlivých hodnot v CSV souboru. Buď se jedná o středník (1,5 ; 2,7 ; 4 ; 8 ; 12) nebo čárku (1.5 , 2.7 , 4 , 8 , 12)
    /// Pokud je oddělovač desetinných čísel čárka, separátorem bude středník. Pokud je tečka, separátor bude čárka.
    /// (V kódu CSV souboru je nutné ošetřit, aby nebylo možné zvolit čárku zároveň pro separator a desetinný oddělovač!)
    /// </summary>
    public enum Separator
    {
        středník = ';',
        čárka = ','
    }
}
