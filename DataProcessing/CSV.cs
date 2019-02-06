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
        /// (V podstatě se jedná o matici, vytvořenou ze dvou dynamických polí List)
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
        /// <param name="maximalniVelikostSouboru">Maximální velikost souboru, pokud je nastaveno oddělování (nastaveno defaultně na 15 MB).</param>
        public CSV(Separator separator = Separator.středník, bool oddelSoubory = true, uint maximalniVelikostSouboru = 15000000)
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
        /// <param name="obsahujeHlavicku">>true, pokud soubor obsahuje hlavičku se jmény jednotlivých sloupců</param>
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
                    separovanýŘádekSouboru = řádekCSVSouboru.Split((char)this.Separator);


                    //výsledky vloží do seznamu názvu sloupců
                    for (int i = 0; i < separovanýŘádekSouboru.Length; i++)
                    {
                        //pokud ještě nebyl vytvořen seznam se stringy na tomto indexu, vytvoř jej
                        //(v kódu nepředpokládáme, že by na různých řádcích bylo jiný počet hodnot, ale ta možnost tu je, takže to by bylo nutné ještě ošetřit a při přidávání nového seznamu během dalších řádků, doplnit předchozí chybějící hodnoty)
                        if (i >= this.Data.Count)
                            this.Data.Add(new List<string>());

                        //přidá načtený a separovaný prvek do seznamu stringu
                        this.Data[i].Add(separovanýŘádekSouboru[i]);
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
                        //pokud dojde k chybě, zavře se soubor
                        CSVfile.Close();
                        //a následně se ještě musí uvolnit prostředky
                        CSVfile.Dispose();

                        //zde vyhodíme vyjímku dále do vyšší úrovně programu, aby programátor používající knihovnu na ni mohl zareagovat vlastním způsobem
                        throw ex;
                    }
                }



                //zapíše data, pokud nějaké jsou
                if (this.Data != null && this.Data.Count > 0)
                {
                    uint cislo = 2; //číslo pro uvedení názvu souboru, pro případné rozdělení CSV souboru
                    //nalezne nejdelší délku seznamu, protože musíme procházet položky dle řádku
                    int nejdelsiDelkaSeznamu = this.VratNejdelsiDelkuSeznamu(this.Data);
                    for (int j = 0; j < nejdelsiDelkaSeznamu; j++)
                    {
                        try
                        {
                            for (int i = 0; i < this.Data.Count; i++)
                            {
                                if (this.Data[i] != null && j < this.Data[i].Count)
                                {
                                    //pokud je to nutné, změní oddělovač desetinných čísel v řetězci
                                    string zmenenaHodnota = this.ZmenDesetinnyOddelovacPokudJeNutne(this.Data[i][j]);
                                    //zapíše jednotlivé hlavičky, a pokud se jedná o poslední prvek, tak za něj nepřidá separátor
                                    CSVfile.Write($"{zmenenaHodnota}");
                                }
                                //posledni prvek na konci řádku je bez středníku
                                if (i != this.Data.Count - 1)
                                    CSVfile.Write($"{(char)this.Separator}");
                            }
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
                                    CSVfile.Close();
                                    CSVfile.Dispose();
                                    //vytvoří se nový tok dat pro zápis
                                    CSVfile = new System.IO.StreamWriter(jmenoSouboruSPriponou);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            //pokud dojde k chybě, zavře se soubor
                            CSVfile.Close();
                            //a následně se ještě musí uvolnit prostředky
                            CSVfile.Dispose();

                            //zde vyhodíme vyjímku dále do vyšší úrovně programu, aby programátor používající knihovnu na ni mohl zareagovat vlastním způsobem
                            throw ex;
                        }
                    }
                }

                CSVfile.Close();
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
        /// Vrátí seznam sloupce dat dle zadaného názvu v hlavičce
        /// </summary>
        /// <param name="jmenoSloupce">jméno sloupce v hlavičce CSV souboru</param>
        /// <returns>seznam stringů s daty daného sloupce, pokud nebyl sloupec nalezen, vrací null</returns>
        public List<String> VratSloupecDat(string jmenoSloupce)
        {
            int indexSloupce = this.NajdiIndexSloupceHlavicky(jmenoSloupce);
            if (indexSloupce >= 0 && indexSloupce < this.Data.Count)
            {
                return this.Data[indexSloupce];
            }
            return null;
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
                //projdeme všechny položky a přidáne je do seznamu seznamů všech přidaných položek
                List<List<string>> pridaneSloupce = new List<List<string>>();
                foreach (CSVpolozka csvPolozka in csvPolozky)
                {
                    List<string> sloupec = this.VratSloupecDat(csvPolozka.JmenoSloupce);
                    if (sloupec != null)
                        sloupec.Add(csvPolozka.HodnotaSloupce);
                    else
                        throw new Exception("Zadaná hodnota nemohla být přidána, protože sloupec neexistuje!");
                    pridaneSloupce.Add(sloupec);
                }


                //projdeme všechny sloupce v datech a pridáme jejich reference do seznamu seznamů nepřidaných položek
                List<List<string>> nepridaneSloupce = new List<List<string>>();
                foreach (List<String> sloupec in this.Data)
                {
                    nepridaneSloupce.Add(sloupec);
                }


                //projdeme všechny reference na sloupce přidaných položek a porovnáváme referenci na sloupce nepřidaných položek
                foreach (List<String> pridanySloupec in pridaneSloupce)
                {
                    for (int i = 0; i < nepridaneSloupce.Count; i++)
                    {
                        //pokud je reference stejná, smažeme ji v seznamu nepřidaných položek a vyskočíme ze smyčky for
                        if (pridanySloupec == nepridaneSloupce[i])
                        {
                            nepridaneSloupce.RemoveAt(i);
                            break;
                        }
                    }
                }


                //přidáme prázdný string do sloupců, kde nebylo nic přidáno
                foreach (List<string> seznamStringu in nepridaneSloupce)
                {
                    seznamStringu.Add(String.Empty);
                }


            }
            else
            {
                throw new Exception("Metoda \"PřidatŘádek(params CSVpolozka[] csvPolozky)\" nemůže být použita, pokud nebyla vytvořena hlavička CSV souboru!");
            }


        }


        /// <summary>
        /// přidání řádku jen pomocí hodnot. Je nutné dodržet pořadí sloupců
        /// </summary>
        /// <param name="hodnotyString">řetězce s hodnotami, zapsané v pořadí, v jakém se mají vkládat do sloupců</param>
        public void PřidatŘádek(params string[] hodnotyString)
        {

            //projdeme všechny položky a přidáne je do seznamu seznamů všech přidaných položek
            List<List<string>> pridaneSloupce = new List<List<string>>();
            for(int i = 0; i < hodnotyString.Length; i++)
            {
                //pokud je položka v pořadí, větším než počet sloupců,
                if (i >= this.Data.Count)
                {
                    //vytvoří hodnoty s novým sloupce (pokud se nejdná o první sloupec, tak by bylo nutné ještě doplnit metodu pro vložení všech předchozích hodnot)
                    this.Data.Add(new List<string>());
                }
                //přidá položku na konec sloupce
                this.Data[i].Add(hodnotyString[i]);
            }


            //pokud bylo zadáno méně hodnot než je aktuálně sloupců
            if (hodnotyString.Length < this.Data.Count)
            {
                //začne přidávat prázdný řetězec od 1. sloupce, který nebyl nastaven
                for (int i = hodnotyString.Length; i < this.Data.Count; i++)
                {
                    this.Data[i].Add(String.Empty);
                }
            }

        }


    }
}
