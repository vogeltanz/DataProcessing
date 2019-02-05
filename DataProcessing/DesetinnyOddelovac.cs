using System;
using System.Collections.Generic;
using System.Text;

namespace DataProcessing
{
    /// <summary>
    /// Oddělovač hodnot desetinného čísla. Buď se jedná o čárku (1,5) nebo o tečku (1.5)
    /// </summary>
    public enum DesetinnyOddelovac
    {
        //Jak je možné vidět u výčtového typu lze vložit i znak (char), protože se reálně jedná o 16 bitovou hodnotu. Této možnosti lze později využít v kódu pomocí přetypování.
        čárka = ',',
        tečka = '.'
    }
}
