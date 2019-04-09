using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DataProcessing
{
    /// <summary>
    /// Jednoduchá implementace třídy pro práci s CSV souborem
    /// </summary>
    public class CSV
    {

        /// <summary>
        /// přípona souboru CSV (psáno malými písmeny)
        /// </summary>
        public const string extension = ".csv";

        /// <summary>
        /// Pokud je true, tak po dosažení hodnoty ve vlastnosti MaximalniVelikostSouboru, uzavře ukládaný soubor a začne zapisovat do nového
        /// </summary>
        public bool OddelSoubory
        {
            get;
            set;
        }

        /// <summary>
        /// Hodnota maximální velikosti souboru v bytech, který je možné zapsat
        /// </summary>
        public uint MaximalniVelikostSouboru
        {
            get;
            set;
        }

        /// <summary>
        /// Seznam pro uložení prvního řádku CSV souboru, který obsahuje hlavičku s názvy jednotlivých sloupců
        /// </summary>
        public List<String> Hlavicka
        {
            get;
            set;
        }

        /// <summary>
        /// vrací true, pokud byla načtena hlavička
        /// </summary>
        public bool JeHlavickaNactena()
        {
            if (this.Hlavicka != null || this.Hlavicka.Count > 0)
                return true;
            else
                return false;
        }


        /// <summary>
        /// data CSV souboru uložena jako seznam seznamů stringu.
        /// (V podstatě se jedná o matici, vytvořenou ze dvou dynamických polí List - tzn. je možné si to představit jako tabulku s řádky a sloupci)
        /// </summary>
        public List<List<String>> Data
        {
            get;
            set;
        }


        /// <summary>
        /// Specifikuje, jakým znakem se desetinné číslo odděluje od celého čísla
        /// </summary>
        DesetinnyOddelovac desetinnyOddelovac;
        private DesetinnyOddelovac DesetinnyOddelovac
        {
            get
            {
                return this.desetinnyOddelovac;
            }
            set
            {
                //Ošetření, aby nebylo možné zvolit čárku jako desetinný oddělovač a jako separator hodnot
                if (value == DesetinnyOddelovac.čárka)
                {
                    //Pozor! Zde je nutné se při nastavení separátoru odkazovat přímo na členskou proměnnou separator, nikoliv na vlastnost!
                    //Vzhledem k tomu, že ve vlastnosti separátoru je nastavení desetinného oddělovače, tak by došlo k zacyklení (samozřejmě došlo by k tomu jen tehdy pokud bychom u obou nastavovali vlastnost, a nikoliv přímo členskou proměnnou).
                    this.separator = Separator.středník;
                }
                else
                {
                    //Pozor! Zde je nutné se při nastavení separátoru odkazovat přímo na členskou proměnnou separator, nikoliv na vlastnost! (viz výše)
                    this.separator = Separator.čárka;
                }

                this.desetinnyOddelovac = value;
            }
        }


        /// <summary>
        /// specifikuje, jakým znakem se jednotlivé hodnoty oddělují od sebe navzájem
        /// </summary>
        Separator separator;
        public Separator Separator
        {
            get
            {
                return this.separator;
            }
            set
            {
                //Ošetření, aby nebylo možné zvolit čárku jako separator hodnot a jako desetinný oddělovač
                if (value == Separator.čárka)
                {
                    //Pozor! Zde je nutné se při nastavení desetinné tečky odkazovat přímo na členskou proměnnou desetinnyOddelovac, nikoliv na vlastnost! (viz výše ve vlastnosti Separator)
                    this.desetinnyOddelovac = DesetinnyOddelovac.tečka;
                }
                else
                {
                    //Pozor! Zde je nutné se při nastavení desetinné čárky odkazovat přímo na členskou proměnnou desetinnyOddelovac, nikoliv na vlastnost! (viz výše ve vlastnosti Separator)
                    this.desetinnyOddelovac = DesetinnyOddelovac.čárka;
                }

                this.separator = value;
            }
        }





        /// <summary>
        /// konstruktor, který provede základní nastavení dat objektu třídy
        /// </summary>
        /// <param name="separator">zvolený separator pro oddělení hodnot CSV souboru</param>
        /// <param name="maximalniVelikostSouboru">Maximální velikost souboru, pokud je nastaveno oddělování (nastaveno defaultně na 15 MB - pozn. číslo je zapsáno s oddělovačem číslic (tj. '_'), což je funkcionalita dostupná od C# 7.0).</param>
        public CSV(Separator separator = Separator.středník, bool oddelSoubory = true, uint maximalniVelikostSouboru = 15_000_000)
        {
            this.Hlavicka = null;
            this.Data = null;

            this.Separator = separator;
            this.OddelSoubory = oddelSoubory;
            this.MaximalniVelikostSouboru = maximalniVelikostSouboru;
        }





        /// <summary>
        /// načte CSV soubor dle jména nebo cesty
        /// </summary>
        /// <param name="jmenoSouboru">jméno nebo cesta k CSV souboru</param>
        /// <param name="obsahujeHlavicku">true, pokud soubor obsahuje hlavičku se jmény jednotlivých sloupců</param>
        public void NactiCSV(string jmenoSouboru, bool obsahujeHlavicku = true)
        {
            //pokud CSV soubor obsahuje hlavičku, vytvoří seznam pro jednotlivé názvy hlaviček
            if (obsahujeHlavicku)
                this.Hlavicka = new List<String>();

            //vytvoří seznam seznamů pro data
            this.Data = new List<List<string>>();

            //pokud třída implementuje interface "IDisposable", pak to znamená, že si uchovává prostředky (tzv. "unmanaged resources" - nespravované/neřízené/nezabezbečené zdroje), které je nutné ručně odstranit voláním metody Dispose().
            //Nicméně vzhledem k tomu, že v kódu se mohou objevit výjimky (Exceptions), museli bychom volání Dispose zabezpečit pro každý případ (viz metoda "UlozCSV" níže).
            //práci si můžeme ulehčit použitím příkazu using
            using (StreamReader streamReader = File.OpenText(jmenoSouboru))
            {
                string řádekCSVSouboru;
                string[] separovanýŘádekSouboru;


                //pokud soubor obsahuje (má obsahovat) hlavičku, a tok dat se ještě nedostal na konec souboru
                if (obsahujeHlavicku && streamReader.EndOfStream == false)
                {
                    //načte řádek ze souboru
                    řádekCSVSouboru = streamReader.ReadLine();
                    //rozdělí řádek na jednotlivé řetězce oddělené zadaným separátorem pomocí metody "Split"
                    separovanýŘádekSouboru = řádekCSVSouboru.Split((char)this.Separator);

                    //výsledky vloží do seznamu názvu sloupců
                    foreach (string hlavičkaSloupce in separovanýŘádekSouboru)
                    {
                        this.Hlavicka.Add(hlavičkaSloupce);
                    }
                }


                //zatímco jsme NEdorazili na konec, opakuj cyklus se čtením a zpracováním řádku
                while (streamReader.EndOfStream == false)
                {
                    //načte řádek ze souboru
                    řádekCSVSouboru = streamReader.ReadLine();
                    //rozdělí řádek na jednotlivé řetězce oddělené zadaným separátorem pomocí metody "Split"
                    //<><>toto řešení není úplně dokonalé, protože v CSV souboru může být text, ve kterém se nachází daný separátor. Text se navíc může zadávat do uvozovek, což by mělo být také ošetřeno.
                    separovanýŘádekSouboru = řádekCSVSouboru.Split((char)this.Separator);

                    //pokud je na řádku jen jeden prvek a je prazdný (tzn. na řádku nic není), pokračuje v dalším cyklu od začátku while
                    if (separovanýŘádekSouboru.Length == 1 && String.IsNullOrEmpty(separovanýŘádekSouboru[0]))
                    {
                        continue;
                    }

                    //pokud na řádku něco je, přidá se
                    if (separovanýŘádekSouboru.Length > 0)
                    {
                        //přidá načtený a separovaný prvek do seznamu stringů (tj. přidá řádek)
                        this.PřidatŘádek(separovanýŘádekSouboru);
                    }

                }


            }


        }


        /// <summary>
        /// převádí seznam stringů na seznam Nullable doublů
        /// </summary>
        /// <param name="data">seznam stringů</param>
        /// <returns>výstupní převedený seznam NullableDoublů</returns>
        public List<double?> PrevedSeznamDat(List<String> data)
        {
            List<double?> seznamNullableDoublu = new List<double?>();


            //nastaví číselný formát (pomocí třídy NumberFormatInfo) dle zvoleného oddělovače
            NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
            DesetinnyOddelovac měněnýDesetinnyOddelovac;
            if (this.desetinnyOddelovac == DesetinnyOddelovac.čárka)
            {
                měněnýDesetinnyOddelovac = DesetinnyOddelovac.tečka;
            }
            else
            {
                měněnýDesetinnyOddelovac = DesetinnyOddelovac.čárka;
            }
            string desetinnyOddelovacString = ((char)this.desetinnyOddelovac).ToString();
            numberFormatInfo.NumberDecimalSeparator = desetinnyOddelovacString;
            numberFormatInfo.PercentDecimalSeparator = desetinnyOddelovacString;


            for (int i = 0; i < data.Count; i++)
            {

                //změníme oddělovače v textu - zde využívaáme možnosti přetypovat výčtový typ (enum) na znak (char)
                //pokud už správný oddělovač v textu je, nezmění se nic. Pokud se jedná o text, tak ten se sice může změnit, ale následně nebude převeden na číslo, takže výsledek bude v pořádku
                string zmenenaHodnota = data[i].Replace((char)měněnýDesetinnyOddelovac, (char)this.desetinnyOddelovac);

                //pokusí se konvertovat řetězec na číslo dle zadaného číselného formátu
                //druhý parametr v TryParse určuje, že se jedná o číslo, které ovšem není vyjádření množství peněz (měny), nebo hexadecimálním číslem. Také učuje, že se při parsování ignorují mezery před a za možným číslem
                //třetí parametr v TryParse je námi specifikovaný číselný formát (NumberFormatInfo). Tato třída implementuje interface IFormatProvider, takže je možné ji použít (Pro nalezení takovýchto tříd je vhodné použít ve Visual Studiu v nabídce Zobrazit položku Prohlížeč objektů; poté vyhledat interface a následně vyhledat jednu z metod, kterou interface obsahuje - většinou se poté nalezne vše, co daný interface implementuje + něco navíc).
                double vystupniDesetinneCislo;
                bool hodnotaJeDesetinneCislo = Double.TryParse(zmenenaHodnota, System.Globalization.NumberStyles.Number, numberFormatInfo, out vystupniDesetinneCislo);
                if (hodnotaJeDesetinneCislo)
                {
                    //pokud se jedná o číslo, přidá jej do seznamu doublů
                    seznamNullableDoublu.Add(vystupniDesetinneCislo);
                    //a nahradí se původní hodnota stringu hodnotou změněnou
                    data[i] = zmenenaHodnota;
                }
                else
                {
                    //pokud se nejedná o číslo, přidá se null
                    seznamNullableDoublu.Add(null);
                }
            }

            return seznamNullableDoublu;
        }


        /// <summary>
        /// uloží soubor do formátu CSV
        /// </summary>
        /// <param name="jmenoSouboru">jméno nebo cesta souboru</param>
        /// <param name="zapisHlavicku">určuje, zda má dojít k zapsání hlavičky (jmen sloupců)</param>
        public void UlozCSV(string jmenoSouboru, bool zapisHlavicku = true)
        {


            if (jmenoSouboru != null)
            {

                //přidá příponu, pokud není v názvu obsažena
                string jmenoSouboruSPriponou = PridejPriponuPokudNeni(jmenoSouboru);


                //vytvoření toku dat pro zápis do souboru (vytvoří soubor a otevře ho pro zápis)
                //stejně jako u StreamReaderu i StreamWriter implementuje interface "IDisposable"
                //zde ale musíme volat metodu Dispose() ručně, protože chceme separovat soubory, tzn. že musíme nové soubory v průběhu kódu vytvářet, což by při použití příkazu "using" nebylo možné
                StreamWriter CSVfile = new System.IO.StreamWriter(jmenoSouboruSPriponou);


                //pokud se má zapsat hlavička, a ta v objektu existuje, pokusí se do souboru tuto hlavičku zapsat
                if (zapisHlavicku && this.JeHlavickaNactena() && this.Hlavicka != null && this.Hlavicka.Count > 0)
                {
                    try
                    {
                        foreach (String hlavicka in this.Hlavicka)
                        {
                            //zapíše jednotlivé hlavičky, a pokud se jedná o poslední prvek, tak za něj nepřidá separátor
                            CSVfile.Write($"{hlavicka}");
                            if (hlavicka != this.Hlavicka[this.Hlavicka.Count - 1])
                                CSVfile.Write($"{(char)this.Separator}");
                        }
                        CSVfile.WriteLine(String.Empty);
                    }
                    catch (Exception ex)
                    {
                        //pokud dojde k chybě, zavře se soubor a následně se ještě musí uvolnit prostředky
                        CSVfile.Dispose();

                        //zde vyhodíme vyjímku dále do vyšší úrovně programu, aby programátor používající knihovnu na ni mohl zareagovat vlastním způsobem
                        throw ex;
                    }
                }



                //zapíše data, pokud nějaké jsou
                if (this.Data != null && this.Data.Count > 0)
                {
                    uint cislo = 2; //číslo pro uvedení názvu souboru, pro případné rozdělení CSV souboru

                    for (int i = 0; i < this.Data.Count; i++)
                    {
                        if (this.Data[i] != null && this.Data[i].Count > 0)
                        {

                            try
                            {
                                for (int j = 0; j < this.Data[i].Count; j++)
                                {
                                    //pokud je to nutné, změní oddělovač desetinných čísel v řetězci
                                    string zmenenaHodnota = this.ZmenDesetinnyOddelovacPokudJeNutne(this.Data[i][j]);
                                    //zapíše jednotlivé sloupce
                                    CSVfile.Write($"{zmenenaHodnota}");

                                    //posledni prvek na konci řádku je bez separátoru (středníku/čárky), za ostatními hodnotami se separátor dává
                                    if (j != this.Data[i].Count - 1)
                                        CSVfile.Write($"{(char)this.Separator}");
                                }
                                //odřádkujeme
                                CSVfile.WriteLine(String.Empty);


                                //pokud má být CSV soubor po určitém množství zapsaných dat rozdělen
                                if (this.OddelSoubory == true)
                                {
                                    //zjistí aktuální délku souboru pomocí třídy FileInfo
                                    FileInfo fileInfo = new FileInfo(jmenoSouboruSPriponou);
                                    if (fileInfo.Length > this.MaximalniVelikostSouboru)
                                    {
                                        //pokud byla překročena délka souboru, poskládá nové jméno souboru
                                        jmenoSouboruSPriponou = jmenoSouboruSPriponou.Substring(0, jmenoSouboruSPriponou.Length - CSV.extension.Length) + cislo.ToString() + CSV.extension;
                                        cislo += 1;
                                        //uzavře se původní soubor a odstraní se všechny přidělené prostředky
                                        CSVfile.Dispose();
                                        //vytvoří se nový tok dat pro zápis
                                        CSVfile = new System.IO.StreamWriter(jmenoSouboruSPriponou);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //pokud dojde k chybě, zavře se soubor a následně se ještě musí uvolnit prostředky
                                CSVfile.Dispose();

                                //zde vyhodíme vyjímku dále do vyšší úrovně programu, aby programátor používající knihovnu na ni mohl zareagovat vlastním způsobem
                                throw ex;
                            }
                        }
                    }
                }

                CSVfile.Dispose();
            }
        }


        /// <summary>
        /// Zjistí, zda jméno souboru nebo cesta obsahuje příponu ".csv"
        /// </summary>
        /// <param name="jmenoSouboru">jméno souboru nebo cesta k souboru</param>
        /// <returns>Vrací true, pokud obsahuje příponu; false pokud příponu neobsahuje</returns>
        private bool MaJmenoSouboruPriponuCSV(string jmenoSouboru)
        {
            //pokud bude délka jmena souboru alespoň stejně tak dlouhá jako délka přípony, otestuje se, zda příponu obsahuje. (pozn. může zde dojít k situaci, že programátor/uživatel zadal jen příponu, což by samozřejmě vedlo k chybě při zápisu, protože nelze za normálních okolností vytvořit soubor jen se jménem přípony)
            if (jmenoSouboru != null && jmenoSouboru.Length >= CSV.extension.Length)
            {
                //pomocí metody substring vybereme jen poslední 4 znaky, které musí být totožné s příponou CSV
                string posledniCtyriZnaky = jmenoSouboru.Substring(jmenoSouboru.Length - CSV.extension.Length, CSV.extension.Length);
                //Při porovnání se rozlišují velká a malá písmena, a proto je nutné ještě převést poslední čtyři znaky na malé písmena (pokud to lze) pomocí metody ToLower
                if (posledniCtyriZnaky.ToLower() == CSV.extension)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Přidá příponu do názvu souboru nebo cesty, pokud není uvedena
        /// </summary>
        /// <param name="jmenoSouboru">jmeno nebo cesta CSV souboru</param>
        /// <returns>modifikované jméno souboru nebo cesty s přidanou příponou ".csv"</returns>
        private string PridejPriponuPokudNeni(string jmenoSouboru)
        {
            string jmenoSouboruModifikovane = jmenoSouboru;
            if (this.MaJmenoSouboruPriponuCSV(jmenoSouboru) == false)
            {
                jmenoSouboruModifikovane += CSV.extension;
            }
            return jmenoSouboruModifikovane;
        }


        /// <summary>
        /// Vrátí nejdelší délku seznamu, který vybírá ze seznamu seznamů
        /// </summary>
        /// <param name="seznamSeznamu">vstupní seznam seznamů</param>
        /// <returns>délka nejdelšího seznamu</returns>
        public int VratNejdelsiDelkuSeznamu(List<List<String>> seznamSeznamu)
        {
            int maximalniDelka = 0;
            foreach (List<String> seznam in seznamSeznamu)
            {
                if (maximalniDelka < seznam.Count)
                {
                    maximalniDelka = seznam.Count;
                }
            }
            return maximalniDelka;
        }


        /// <summary>
        /// Nalezne sloupec dle zadaného identifikátoru a vrátí jeho index.
        /// </summary>
        /// <param name="jmenoSloupce">identifikační jméno sloupce</param>
        /// <returns>Vrací index hledaného sloupce. Hodnota -1 znamená nenalezeno.</returns>
        private int NajdiIndexSloupceHlavicky(string jmenoSloupce)
        {
            int indexSloupce = -1;
            if (this.Hlavicka != null)
            {
                for (int i = 0; i < this.Hlavicka.Count; i++)
                {
                    if (jmenoSloupce == this.Hlavicka[i])
                    {
                        indexSloupce = i;
                        break;
                    }
                }
            }
            return indexSloupce;
        }

        /// <summary>
        /// zamění desetinnou tečku za čárku, nebo desetinnou čárku za tečku, pokud je to potřeba
        /// </summary>
        /// <param name="hodnota">hodnota nebo text v datovém typu String</param>
        /// <returns>řetězec se správně naformátovaným číslem, nebo původní string. Původní string vrátí pokud se nejedná o číslo, nebo jej nebylo třeba přeformátovat.</returns>
        private String ZmenDesetinnyOddelovacPokudJeNutne(String hodnota)
        {

            string hodnotaModifikovana = hodnota;
            int vystupniCeleCislo;
            bool hodnotaJeCeleCislo = Int32.TryParse(hodnotaModifikovana, out vystupniCeleCislo);
            //pokud hodnota není celé číslo, vyzkoušíme, zda se jedná o desetinné číslo
            if (hodnotaJeCeleCislo == false)
            {
                //nastaví číselný formát (pomocí třídy NumberFormatInfo) dle zvoleného oddělovače
                NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
                DesetinnyOddelovac měněnýDesetinnyOddelovac;
                string měněnýDesetinnyOddelovacString;
                if (this.desetinnyOddelovac == DesetinnyOddelovac.čárka)
                {
                    měněnýDesetinnyOddelovac = DesetinnyOddelovac.tečka;
                }
                else
                {
                    měněnýDesetinnyOddelovac = DesetinnyOddelovac.čárka;
                }
                měněnýDesetinnyOddelovacString = ((char)měněnýDesetinnyOddelovac).ToString();
                numberFormatInfo.NumberDecimalSeparator = měněnýDesetinnyOddelovacString;
                numberFormatInfo.PercentDecimalSeparator = měněnýDesetinnyOddelovacString;

                //pokusí se konvertovat řetězec na číslo dle zadaného číselného formátu
                //druhý parametr v TryParse určuje, že se jedná o číslo, které ovšem není vyjádření množství peněz (měny), nebo hexadecimálním číslem. Také učuje, že se při parsování ignorují mezery před a za možným číslem
                //třetí parametr v TryParse je námi specifikovaný číselný formát (NumberFormatInfo). Tato třída implementuje interface IFormatProvider, takže je možné ji použít (Pro nalezení takovýchto tříd je vhodné použít ve Visual Studiu v nabídce Zobrazit položku Prohlížeč objektů; poté vyhledat interface a následně vyhledat jednu z metod, kterou interface obsahuje - většinou se poté nalezne vše, co daný interface implementuje + něco navíc).
                double vystupniDesetinneCislo;
                bool hodnotaJeDesetinneCislo = Double.TryParse(hodnotaModifikovana, System.Globalization.NumberStyles.Number, numberFormatInfo, out vystupniDesetinneCislo);

                //pokud je hodnota desetinným číslem
                if (hodnotaJeDesetinneCislo == true)
                {
                    //změníme oddělovače v textu - zde využívaáme možnosti přetypovat výčtový typ (enum) na znak (char)
                    hodnotaModifikovana = hodnotaModifikovana.Replace((char)měněnýDesetinnyOddelovac, (char)this.desetinnyOddelovac);
                }
            }

            return hodnotaModifikovana;
        }


        /// <summary>
        /// Přidání řádku s využitím pomocné struktury CSV položka
        /// </summary>
        /// <param name="csvPolozky">položky zapsané jako struktury (v podstatě jen identifikátor a hodnota) v řadě za sebou</param>
        public void PřidatŘádek(params CSVpolozka[] csvPolozky)
        {

            if (this.JeHlavickaNactena())
            {
                //projdeme všechny položky a přidáme je do seřazeného seznamu (SortedList) všech přidaných položek
                //seznam se seřazuje podle klíče (v našem případě index sloupce). Obsahem je poté samotný string s hodnotou
                SortedList<int, string> pridaneSloupce = new SortedList<int, string>();
                foreach (CSVpolozka csvPolozka in csvPolozky)
                {
                    int indexSloupce = this.NajdiIndexSloupceHlavicky(csvPolozka.JmenoSloupce);
                    if (indexSloupce >= 0)
                        pridaneSloupce.Add(indexSloupce, csvPolozka.HodnotaSloupce);
                    else
                        throw new Exception("Zadaná hodnota nemohla být přidána, protože sloupec neexistuje!");
                }

                if (pridaneSloupce.Count > 0)
                    this.Data.Add(new List<string>());
                int indexPosledni = this.Data.Count - 1;

                for (int i = 0, j = 0; i < pridaneSloupce.Count; ++i, ++j)
                {
                    //zatímco jsme ještě nedošli na index správného sloupce...
                    while (j < pridaneSloupce.Keys[i])
                    {
                        //...přidáváme prázdný string do sloupců, které nebyly specifikovány
                        this.Data[indexPosledni].Add(String.Empty);
                        ++j;
                    }

                    //pokud je klíč stejný jako současný index, tak přidá hodnotu/text na konec seznamu
                    if (j == pridaneSloupce.Keys[i])
                    {
                        this.Data[indexPosledni].Add(pridaneSloupce[i]);
                    }
                }
            }
            else
            {
                throw new Exception("Metoda \"PřidatŘádek(params CSVpolozka[] csvPolozky)\" nemůže být použita, pokud nebyla vytvořena hlavička CSV souboru!");
            }

        }


        /// <summary>
        /// Přidání řádku jen pomocí hodnot. Je nutné dodržet pořadí sloupců
        /// </summary>
        /// <param name="hodnotyString">řetězce s hodnotami, zapsané ve stejném pořadí jako názvy sloupců v hlavičce</param>
        public void PřidatŘádek(params string[] hodnotyString)
        {

            if (hodnotyString.Length > 0)
                this.Data.Add(new List<string>());

            int posledniIndex = this.Data.Count - 1;
            //projdeme všechny položky a přidáme je do seznamu na posledním indexu
            for (int i = 0; i < hodnotyString.Length; i++)
            {
                //přidá položku na poslední řádek
                this.Data[posledniIndex].Add(hodnotyString[i]);
            }


            //pokud bylo zadáno méně hodnot než je aktuálně sloupců
            if (hodnotyString.Length < this.Data.Count)
            {
                //začne přidávat prázdný řetězec od 1. sloupce, který nebyl nastaven
                for (int i = hodnotyString.Length; i < this.Data.Count; i++)
                {
                    this.Data[posledniIndex].Add(String.Empty);
                }
            }

        }


    }
}
