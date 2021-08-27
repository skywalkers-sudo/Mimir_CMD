using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Mimir_CMD
{
    class Program
    {


        static void Main()
        {
            try
            {
                {
                    string path = @"\\srvcc01\Coscom_Daten\DATEN\TEMP\";       // Wurzelverzeichis der zu ladenden XML (Workspace)
                    //string path = @"C:\Users\ni88\Desktop\";       // Wurzelverzeichis der zu ladenden XML (Testspace Home)


                    using var watcher = new FileSystemWatcher(path);

                    watcher.NotifyFilter = NotifyFilters.Attributes
                                         | NotifyFilters.CreationTime
                                         | NotifyFilters.DirectoryName
                                         | NotifyFilters.FileName
                                         | NotifyFilters.LastAccess
                                         | NotifyFilters.LastWrite
                                         | NotifyFilters.Security
                                         | NotifyFilters.Size;

                    //watcher.Changed += OnChanged;
                    watcher.Created += OnCreated;
                    //watcher.Deleted += OnDeleted;
                    //watcher.Renamed += OnRenamed;
                    //watcher.Error += OnError;

                    watcher.Filter = "*.xml";
                    //watcher.IncludeSubdirectories = true;
                    watcher.EnableRaisingEvents = true;

                    Console.WriteLine (
                        "\n download code @ https://github.com/skywalkers-sudo/Mimir_CMD " +
                        "\n" +
                        "\n ***********   Watcher looks @ " + path + "   ***********" +
                        "\n" + " log:" +
                        "\n"
                        );

                    Console.ReadLine();
                }
            }
            catch (Exception u)
            {
                Console.WriteLine("" + u);
            }
        }

       


        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            try
            {

                // Settings BEGIN
                string ROOTXML = @"\\srvcc01\Coscom_Daten\DATEN\TEMP\";                        // Wurzelverzeichis der zu ladenden XML
                string TARGETXML = @"C:\Users\Public\Documents\OPEN MIND\tooldb\sync\";        // Zielverzeichnis der zu schreibenden XML

                //string ROOTXML = @"C:\Users\ni88\Desktop\";                                    // Wurzelverzeichis der zu ladenden XML (Testspace home)
                //string TARGETXML = @"C:\Users\ni88\Desktop\custom\";                           // Zielverzeichnis der zu schreibenden XML (Testspace home)


                bool STATUSNC = true;           // Status vor NC Name schreiben
                bool toNCNr = true;             // 000 and NC Nummer schreiben (ungeprüftes WKZ)
                bool refpoint = true;           // Refpoint umschreiben aktivieren (nur Bohrer "S2" zu "1")
                bool altfolder = true;          // alternative Ordnerbenennung (wie in Coscom)
                bool folderstatus = true;       // Werkzeuge nach Status in Ordnern strukturieren (noch nicht implementiert)
                bool shaftmodepara = true;      // Wenn NominalØ gleich SchaftØ setze Schaftmodus auf parametric (bei Schaftfräser, Radienfräser, Kugelfräser und Bohrer)
                bool bugcheck = true;           // überprüfe XML auf mögliche Fehler (aktuell: Z-Vorschub <9999?; Shaft Flag gesetzt?)
                // Settings ENDE



                // =================================================================================HEAD==============================================================================================
                string[] xmlListEXIST = Directory.GetFiles(ROOTXML, "*.xml");
                int anzahlxml = xmlListEXIST.GetLength(0);


                while (anzahlxml != 0)

                {

                    // array der ganzen .xml erstellen
                    string[] xmlList = Directory.GetFiles(ROOTXML, "*.xml");

                    // Pfadangabe löschen, es bleibt nur die Datei
                    string filename = null;
                    filename = System.IO.Path.GetFileName(xmlList[0]);

                    // Extension löschen (.xml)
                    char[] MyChar = { 'x', 'm', 'l', '.' };
                    string filnamewithoutExtension = filename.TrimEnd(MyChar);

                    // Erstelle neue NC Nummer für XML eintrag
                    string path1 = @ROOTXML + filename;
                    string newNCNUMBER = filnamewithoutExtension + "000";

                    // stringbuilder für Info
                    StringBuilder sb = new();

                    string datetime = DateTime.Now.ToString();
                    string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                    _ = sb.Append("\n========= Neues Wkz gefunden " + filename + " /// Date: " + datetime + " /// Übergeben mit Version: " + version+ " =========");


                    // ================================================================================FEATURE 1 CHECK (Werkzeugstatus vor NC-Namen hinzufügen) ====================================================================
                    if (STATUSNC == true)
                    {
                        XmlDocument xmlDoc = new();
                        xmlDoc.Load(path1);

                        XmlNode noderead1 = xmlDoc.SelectSingleNode("/omtdx/ncTools/ncTools/ncTools/ncTool/customData/param[@name='Werkzeugstatus']");

                        if (noderead1 != null)
                        {

                            var Status = noderead1.Attributes["value"].Value;

                            // Lese bestehenden Werkzeugnamen
                            XmlNode noderead2 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");

                            if (noderead2 != null)
                            {
                                var bNcname = noderead2.Attributes["name"].Value;

                                // Schreibe neuen Werkzeugnamen
                                XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                nodewrite.Attributes[2].Value = Status + " // " + bNcname;

                                _ = sb.Append("\n" + " --> ADD STATUS TO NAME: Werkzeugname geändert auf --> " + Status + " // " + bNcname);

                            }
                        }
                        xmlDoc.Save(path1);
                    }
 
                    // ================================================================================FEATURE 2 CHECK (set 000 ungeprüftes Wkz)==================================================================================== 
                    if (toNCNr == true)
                    {
                        XmlDocument xmlDoc = new();
                        xmlDoc.Load(path1);

                        XmlNode node = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");

                        if (node is not null)
                        {
                            node.Attributes[1].Value = newNCNUMBER;
                            _ = sb.Append("\n" + " --> UNGEPRÜFTES WKZ:    NC-Nummer auf:   '" + newNCNUMBER + "'   geändert");
                        }
                        else
                        {
                            _ = sb.Append("\n" + " --> UNGEPRÜFTES WKZ:    Knoten nicht vorhanden, 000 konnte nicht hinzugefügt werden");
                        }

                        xmlDoc.Save(path1);
                    }

                    // ================================================================================FEATURE 3 CHECK (alternative refpoint) ====================================================================================== 
                    if (refpoint == true)
                    {
                        XmlDocument xmlDoc = new();
                        xmlDoc.Load(path1);


                        // Lesen von Werkzeugreferenzpunkt
                        XmlNode noderead1 = xmlDoc.SelectSingleNode("/omtdx/ncTools/ncTools/ncTools/ncTool/referencePoints/referencePoint");

                        if (noderead1 != null)
                        {
                            var refpoint1 = noderead1.Attributes["name"].Value;

                            // Lesen von Werkzeugklasse
                            XmlNode noderead2 = xmlDoc.SelectSingleNode("/omtdx/tools/tools/tools/tool");
                            if (noderead2 != null)
                            {
                                var toolclass = noderead2.Attributes["type"].Value;

                                if (refpoint1 == "S2" && toolclass == "drilTool")
                                {
                                    // Schreibe neuen Werkzeugfefernznamen
                                    XmlNode nodewrite = xmlDoc.SelectSingleNode("/omtdx/ncTools/ncTools/ncTools/ncTool/referencePoints/referencePoint");
                                    nodewrite.Attributes[1].Value = "1";

                                    _ = sb.Append("\n" + " --> WKZ-REF:            Wkz-Klasse 'toolDrill' und Referenzpunkt '" + refpoint1 + "' erkannt --> Name Referenzpunkt geaendert auf '1' ");

                                }
                                else
                                {
                                    _ = sb.Append("\n" + " --> WKZ-REF:            Werkzeugreferenzname nicht gefunden und/oder Werkzeugklasse ist kein Bohrer  ");

                                }
                            }
                            else
                            {
                                _ = sb.Append("\n" + " --> WKZ-REF:            Referenzpunkte gefunden, Eintrag Werkzeugklasse nicht gefunden");
                            }
                        }
                        else
                        {
                            _ = sb.Append("\n" + " --> WKZ-REF:            Keine Referenzpunktinformationen enthalten ");
                        }

                        xmlDoc.Save(path1);
                    }

                    // ================================================================================FEATURE 4 CHECK (alternative Folder) ========================================================================================
                    if (altfolder == true)
                    {
                        XmlDocument xmlDoc = new();
                        xmlDoc.Load(path1);


                        XmlNode noderead1 = xmlDoc.SelectSingleNode("/omtdx/ncTools/ncTools");

                        if (noderead1 != null)
                        {
                            var folder = noderead1.Attributes["folder"].Value;


                            switch (folder)
                            {
                                // ------------------Fräswerkzeuge
                                case "Planfräser / Messerköpfe":
                                    noderead1.Attributes[0].Value = "7000 - Planfräser / Messerköpfe";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Schaftfräser":
                                    noderead1.Attributes[0].Value = "7003 - Schaftfräser";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Kugelfräser / Ballfräser":
                                    noderead1.Attributes[0].Value = "7004 - Kugelfräser / Ballfräser";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Formfräser / Sonderfräswerkzeuge":
                                    noderead1.Attributes[0].Value = "7006 - Formfräser / Sonderfräswerkzeuge";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Gewindefräser":
                                    noderead1.Attributes[0].Value = "7008 - Gewindefräser";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Scheibenfräser und Sägeblätter":
                                    noderead1.Attributes[0].Value = "7009 - Scheibenfräser und Sägeblätter";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Tonnen- / Linsenfräser":
                                    noderead1.Attributes[0].Value = "7011 - Tonnen- / Linsenfräser";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Radienfräser":
                                    noderead1.Attributes[0].Value = "7012 - Radienfräser";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "T-Nutenfräser":
                                    noderead1.Attributes[0].Value = "7014 - T-Nutenfräser";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                //------------------------------Bohrwerkzeuge--------------------

                                case "NC-Anbohrer":
                                    noderead1.Attributes[0].Value = "7101 - NC-Anbohrer";
                                    break;

                                case "Bohrer":
                                    noderead1.Attributes[0].Value = "7101 - Bohrer";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Reibahlen":
                                    noderead1.Attributes[0].Value = "7102 - Reibahlen";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Spindler / Ausdreher":
                                    noderead1.Attributes[0].Value = "7103 - Spindler / Ausdreher";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Zentrierbohrer":
                                    noderead1.Attributes[0].Value = "7104 - Zentrierbohrer";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Formbohrer & Reibahlen":
                                    noderead1.Attributes[0].Value = "7105 - Formbohrer & Reibahlen";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                //------------------------------Gewindebohrer-/former--------------------

                                case "Metrische-Gewinde":
                                    noderead1.Attributes[0].Value = "7200 - Metrische-Gewinde";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Zoll-Gewinde":
                                    noderead1.Attributes[0].Value = "7201 - Zoll-Gewinde";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Sonder-Gewinde":
                                    noderead1.Attributes[0].Value = "7202 - Sonder-Gewinde";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                //------------------------------Senk und Faswerkzeuge --------------------

                                case "Senk-Werkzeuge":
                                    noderead1.Attributes[0].Value = "7300 - Senk-Werkzeuge";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Entgratfräser (nur Winkel schneidend)":
                                    noderead1.Attributes[0].Value = "7303 - Entgratfräser (nur Winkel schneidend)";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Fasenfräser (mit Umfangsscheide)":
                                    noderead1.Attributes[0].Value = "7304 - Fasenfräser (mit Umfangsschneide)";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                case "Fasen- und Schriftstichel":
                                    noderead1.Attributes[0].Value = "7305 - Fasen- und Schriftstichel";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                //------------------------------Messtaster --------------------

                                case "Messtaster & Messdorne":
                                    noderead1.Attributes[0].Value = "7500 - Messtaster & Messdorne";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                //------------------------------Reinigungswkz Propeller etc --------------------

                                case "Bürsten, Propeller, usw...":
                                    noderead1.Attributes[0].Value = "7600 - Bürsten, Propeller, usw...";
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse " + noderead1.Attributes[0].Value + " gefunden -> Ordner aktualisiert");
                                    break;

                                //------------------------------Wenns mal wieder schiefläuft --------------------
                                default:
                                    _ = sb.Append("\n" + " --> Alter. ORDNERNAME:  Klasse für Ordner nicht gefunden ");
                                    break;
                            }
                        }
                        xmlDoc.Save(path1);
                    }

                    // ================================================================================FEATURE 5 CHECK (Status folder)==============================================================================================
                    if (folderstatus == true)
                    {
                        XmlDocument xmlDoc = new();
                        xmlDoc.Load(path1);             // xml laden

                        XmlNode noderead1 = xmlDoc.SelectSingleNode("/omtdx/ncTools/ncTools/ncTools/ncTool/customData/param[@name='Werkzeugstatus']");

                        string folderspezial = "SONDERWKZS (Bestand prüfen)";

                        if (noderead1 != null)
                        {
                            var Status = noderead1.Attributes["value"].Value;

                            switch (Status)
                            {
                                case "Freigegeben":
                                    _ = sb.Append("\n" + " --> STATUS-ORDNER:      Status Freigegeben gefunden -> wird nicht in Ordner '" + folderspezial + "' hinzugefügt");
                                    break;


                                case "FAVORIT":
                                    _ = sb.Append("\n" + " --> STATUS-ORDNER:      Klasse FAVOURIT gefunden -> wird nicht in Ordner '" + folderspezial + "' hinzugefügt");
                                    break;



                                default:

                                    // nodepath
                                    XmlNode root = xmlDoc.SelectSingleNode("omtdx/ncTools");

                                    //Create a deep clone.  The cloned node
                                    //includes the child nodes.
                                    XmlNode deep = root.CloneNode(true);

                                    //Add the deep clone to the document.
                                    root.InsertBefore(deep, root.FirstChild);

                                    //remove the old node
                                    root.RemoveChild(root.LastChild);

                                    //Create a new attribute.
                                    XmlNode root1 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools");
                                    string ns = root1.GetNamespaceOfPrefix("ncTools");
                                    XmlNode attr = xmlDoc.CreateNode(XmlNodeType.Attribute, "folder", ns);
                                    attr.Value = folderspezial;

                                    //Add the attribute to the document.
                                    root1.Attributes.SetNamedItem(attr);


                                    _ = sb.Append("\n" + " --> STATUS-ORDNER:      Sonderstatus gefunden -> in Ordner '" + folderspezial + "' hinzugefügt");

                                    break;

                            }

                            xmlDoc.Save(path1);
                        }
                    }

                    // ================================================================================FEATURE 6 CHECK (shaftmode para)=============================================================================================
                    if (shaftmodepara == true)
                    {
                        XmlDocument xmlDoc = new();
                        xmlDoc.Load(path1);             // xml laden

                        XmlNode wkzclass = xmlDoc.SelectSingleNode("/omtdx/tools/tools/tools/tool");
                        var toolclass = wkzclass.Attributes["type"].Value;

                        if (toolclass == "endMill" | toolclass == "radiusMill" | toolclass == "ballMill")
                        {
                            XmlNode nominalD = xmlDoc.SelectSingleNode("/omtdx/tools/tools/tools/tool/param[@name='toolDiameter']");
                            XmlNode shaftD = xmlDoc.SelectSingleNode("/omtdx/tools/tools/tools/tool/param[@name='toolShaftDiameter']");
                            XmlNode shaftm = xmlDoc.SelectSingleNode("/omtdx/tools/tools/tools/tool/param[@name='toolShaftType']");

                            if (nominalD != null && shaftD != null && shaftm != null)
                            {
                                var nominalDiameter = nominalD.Attributes["value"].Value;
                                var shaftDiameter = shaftD.Attributes["value"].Value;
                                var shaftmode = shaftm.Attributes["value"].Value;

                                if (nominalDiameter == shaftDiameter && shaftmode == "free")
                                {
                                    shaftm.Attributes[1].Value = "parametric";
                                    _ = sb.Append("\n" + " --> SCHAFTPARAMETRIK:   NominalØ = SchaftØ --> Schaft wird auf PARAMETRIK gesetzt ");
                                }
                                else
                                {
                                    _ = sb.Append("\n" + " --> SCHAFTPARAMETRIK:   NominalØ != SchaftØ --> Schaft bleibt FREE ");
                                }
                            }
                            else
                            {
                                _ = sb.Append("\n" + " --> SCHAFTPARAMETRIK:   erfolderliche Werte nicht in xml enthalten ");
                            }
                        }


                        else
                        {
                            _ = sb.Append("\n" + " --> SCHAFTPARAMETRIK:   Keine unterstützte Klasse");
                        }

                        xmlDoc.Save(path1);
                    }

                    // ================================================================================FEATURE 7 CHECK (überprüfe auf Fehler)=======================================================================================
                    if (bugcheck == true)
                    {
                        XmlDocument xmlDoc = new();
                        xmlDoc.Load(path1);             // xml laden
                        
                        XmlNode wkzclass = xmlDoc.SelectSingleNode("/omtdx/tools/tools/tools/tool");
                        XmlNode shaftflag = xmlDoc.SelectSingleNode("/omtdx/tools/tools/tools/tool/param[@name='toolShaftEnabled']");


                        var toolclass = wkzclass.Attributes["type"].Value;

                        // Schaft prüfung
                        switch (toolclass)
                        {
                            case "endMill":
                                if (shaftflag != null)
                                {
                                    var valueshaftflag = shaftflag.Attributes["value"].Value;

                                    if (valueshaftflag == "1")
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          Schaft-Check --> all is good");
                                    }
                                    else
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ATTENTION !!! Schaft nicht aktiviert");

                                        // ====================  START FÜGE HINWEIS IN XML =====================
                                        // Lese bestehenden Werkzeugnamen
                                        XmlNode noderead2 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                        XmlNode noderead3 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                        if (noderead2 != null)
                                        {
                                            var bNcname = noderead2.Attributes["name"].Value;
                                            // Schreibe neuen Werkzeugnamen
                                            XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                            nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                        }
                                        else if (noderead3 != null)  // (notwedig falls in Feature Statusordner ein Ordner hinzugefügt wurde)
                                        {
                                            var bNcname = noderead3.Attributes["name"].Value;
                                            // Schreibe neuen Werkzeugnamen
                                            XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                            nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                        }
                                        else
                                        {
                                            _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ERROR !!! Schreiben in XML fehlgeschlagen - Knoten nicht vorhanden");
                                        }
                                        // ==================== ENDE FÜGE HINWEIS IN XML =====================
                                    }
                                }
                                else
                                {
                                    _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ATTENTION !!! Schaftknoten in XML fehlt");

                                    // ====================  START FÜGE HINWEIS IN XML =====================
                                    // Lese bestehenden Werkzeugnamen
                                    XmlNode noderead2 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                    XmlNode noderead3 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                    if (noderead2 != null)
                                    {
                                        var bNcname = noderead2.Attributes["name"].Value;
                                        // Schreibe neuen Werkzeugnamen
                                        XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                        nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                    }
                                    else if (noderead3 != null)  // (notwedig falls in Feature Statusordner ein Ordner hinzugefügt wurde)
                                    {
                                        var bNcname = noderead3.Attributes["name"].Value;
                                        // Schreibe neuen Werkzeugnamen
                                        XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                        nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                    }
                                    else
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ERROR !!! Schreiben in XML fehlgeschlagen - Knoten nicht vorhanden");
                                    }
                                    // ==================== ENDE FÜGE HINWEIS IN XML =====================
                                }
                                break;


                            case "radiusMill":
                                if (shaftflag != null)
                                {
                                    var valueshaftflag = shaftflag.Attributes["value"].Value;

                                    if (valueshaftflag == "1")
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          Schaft-Check --> all is good");
                                    }
                                    else
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ATTENTION !!! Schaft nicht aktiviert");

                                        // ====================  START FÜGE HINWEIS IN XML =====================
                                        // Lese bestehenden Werkzeugnamen
                                        XmlNode noderead2 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                        XmlNode noderead3 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                        if (noderead2 != null)
                                        {
                                            var bNcname = noderead2.Attributes["name"].Value;
                                            // Schreibe neuen Werkzeugnamen
                                            XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                            nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                        }
                                        else if (noderead3 != null)  // (notwedig falls in Feature Statusordner ein Ordner hinzugefügt wurde)
                                        {
                                            var bNcname = noderead3.Attributes["name"].Value;
                                            // Schreibe neuen Werkzeugnamen
                                            XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                            nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                        }
                                        else
                                        {
                                            _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ERROR !!! Schreiben in XML fehlgeschlagen - Knoten nicht vorhanden");
                                        }
                                        // ==================== ENDE FÜGE HINWEIS IN XML =====================
                                    }
                                }
                                else
                                {
                                    _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ATTENTION !!! Schaftknoten in XML fehlt");

                                    // ====================  START FÜGE HINWEIS IN XML =====================
                                    // Lese bestehenden Werkzeugnamen
                                    XmlNode noderead2 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                    XmlNode noderead3 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                    if (noderead2 != null)
                                    {
                                        var bNcname = noderead2.Attributes["name"].Value;
                                        // Schreibe neuen Werkzeugnamen
                                        XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                        nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                    }
                                    else if (noderead3 != null)  // (notwedig falls in Feature Statusordner ein Ordner hinzugefügt wurde)
                                    {
                                        var bNcname = noderead3.Attributes["name"].Value;
                                        // Schreibe neuen Werkzeugnamen
                                        XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                        nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                    }
                                    else
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ERROR !!! Schreiben in XML fehlgeschlagen - Knoten nicht vorhanden");
                                    }
                                    // ==================== ENDE FÜGE HINWEIS IN XML =====================
                                }
                                break;


                            case "ballMill":
                                if (shaftflag != null)
                                {
                                    var valueshaftflag = shaftflag.Attributes["value"].Value;

                                    if (valueshaftflag == "1")
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          Schaft-Check --> all is good");
                                    }
                                    else
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ATTENTION !!! Schaft nicht aktiviert");

                                        // ====================  START FÜGE HINWEIS IN XML =====================
                                        // Lese bestehenden Werkzeugnamen
                                        XmlNode noderead2 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                        XmlNode noderead3 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                        if (noderead2 != null)
                                        {
                                            var bNcname = noderead2.Attributes["name"].Value;
                                            // Schreibe neuen Werkzeugnamen
                                            XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                            nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                        }
                                        else if (noderead3 != null)  // (notwedig falls in Feature Statusordner ein Ordner hinzugefügt wurde)
                                        {
                                            var bNcname = noderead3.Attributes["name"].Value;
                                            // Schreibe neuen Werkzeugnamen
                                            XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                            nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                        }
                                        else
                                        {
                                            _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ERROR !!! Schreiben in XML fehlgeschlagen - Knoten nicht vorhanden");
                                        }
                                        // ==================== ENDE FÜGE HINWEIS IN XML =====================
                                    }
                                }
                                else
                                {
                                    _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ATTENTION !!! Schaftknoten in XML fehlt");

                                    // ====================  START FÜGE HINWEIS IN XML =====================
                                    // Lese bestehenden Werkzeugnamen
                                    XmlNode noderead2 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                    XmlNode noderead3 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                    if (noderead2 != null)
                                    {
                                        var bNcname = noderead2.Attributes["name"].Value;
                                        // Schreibe neuen Werkzeugnamen
                                        XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                        nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                    }
                                    else if (noderead3 != null)  // (notwedig falls in Feature Statusordner ein Ordner hinzugefügt wurde)
                                    {
                                        var bNcname = noderead3.Attributes["name"].Value;
                                        // Schreibe neuen Werkzeugnamen
                                        XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                        nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                    }
                                    else
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ERROR !!! Schreiben in XML fehlgeschlagen - Knoten nicht vorhanden");
                                    }
                                    // ==================== ENDE FÜGE HINWEIS IN XML =====================
                                }
                                break;


                            case "drilTool":
                                if (shaftflag != null)
                                {
                                    var valueshaftflag = shaftflag.Attributes["value"].Value;

                                    if (valueshaftflag == "1")
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          Schaft-Check --> all is good");
                                    }
                                    else
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ATTENTION !!! Schaft nicht aktiviert");

                                        // ====================  START FÜGE HINWEIS IN XML =====================
                                        // Lese bestehenden Werkzeugnamen
                                        XmlNode noderead2 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                        XmlNode noderead3 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                        if (noderead2 != null)
                                        {
                                            var bNcname = noderead2.Attributes["name"].Value;
                                            // Schreibe neuen Werkzeugnamen
                                            XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                            nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                        }
                                        else if (noderead3 != null)  // (notwedig falls in Feature Statusordner ein Ordner hinzugefügt wurde)
                                        {
                                            var bNcname = noderead3.Attributes["name"].Value;
                                            // Schreibe neuen Werkzeugnamen
                                            XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                            nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                        }
                                        else
                                        {
                                            _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ERROR !!! Schreiben in XML fehlgeschlagen - Knoten nicht vorhanden");
                                        }
                                        // ==================== ENDE FÜGE HINWEIS IN XML =====================
                                    }
                                }
                                else
                                {
                                    _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ATTENTION !!! Schaftknoten in XML fehlt");

                                    // ====================  START FÜGE HINWEIS IN XML =====================
                                    // Lese bestehenden Werkzeugnamen
                                    XmlNode noderead2 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                    XmlNode noderead3 = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                    if (noderead2 != null)
                                    {
                                        var bNcname = noderead2.Attributes["name"].Value;
                                        // Schreibe neuen Werkzeugnamen
                                        XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTool");
                                        nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                    }
                                    else if (noderead3 != null)  // (notwedig falls in Feature Statusordner ein Ordner hinzugefügt wurde)
                                    {
                                        var bNcname = noderead3.Attributes["name"].Value;
                                        // Schreibe neuen Werkzeugnamen
                                        XmlNode nodewrite = xmlDoc.SelectSingleNode("omtdx/ncTools/ncTools/ncTools/ncTools/ncTool");
                                        nodewrite.Attributes[2].Value = "!! ATTENTION !!! Schaft FEHLT" + " // " + bNcname;
                                    }
                                    else
                                    {
                                        _ = sb.Append("\n" + " --> BUG-CHECK:          !!! ERROR !!! Schreiben in XML fehlgeschlagen - Knoten nicht vorhanden");
                                    }
                                    // ==================== ENDE FÜGE HINWEIS IN XML =====================
                                }
                                break;

                            default:

                                break;
                        }

                        // Axialen Vorschub prüfen

                        // fehlt noch 


                        xmlDoc.Save(path1);
                    }

                    // ================================================================================FEATURE  verschiebe Datei==================================================================================================
                    if (System.IO.Directory.Exists(TARGETXML))
                    {
                        File.Move(xmlList[0], @TARGETXML + filename);
                        _ = sb.Append("\n" + " --> File moved to: " + @TARGETXML);
                    }
                    else
                    {
                        Directory.CreateDirectory(@TARGETXML);
                        File.Move(xmlList[0], @TARGETXML + filename);
                        _ = sb.Append("\n" + " --> Created Directory " + @TARGETXML + " and moved File");
                    }

                    // ================================================================================FEATURE  schreibe Infos in Ausgabefenster   ============================================================================
                    _ = sb.Append("\n" + "================================================  Fini " + filename + "  =============================================== \n"); 
                    Console.WriteLine(sb);

                    // ================================================================================FEATURE  INFOS in LOG schreiben      ===============================================================================

                    // if log directory exists
                    if (Directory.Exists(TARGETXML + "log_sync_xml/"))
                    {
                        // if logfile exist append content
                        if (File.Exists(TARGETXML + "log_sync_xml/" + filnamewithoutExtension + ".log"))
                        {
                            using StreamWriter myWriter = new(TARGETXML + "log_sync_xml/" + filnamewithoutExtension + ".log", append: true);
                            myWriter.WriteLineAsync(sb.ToString());
                            myWriter.Close();

                        }
                        // if logfile not exists create new .log
                        else
                        {
                            StreamWriter myWriter = File.CreateText(TARGETXML + "log_sync_xml/" + filnamewithoutExtension + ".log");
                            myWriter.WriteLine(sb.ToString());
                            myWriter.Close();
                        }
                    }
                    // if log directory not exists
                    else
                    {
                        Directory.CreateDirectory(TARGETXML + "log_sync_xml/");
                        StreamWriter myWriter = File.CreateText(TARGETXML + "log_sync_xml/" + filnamewithoutExtension + ".log");
                        myWriter.WriteLine(sb.ToString());
                        myWriter.Close();
                    }


            anzahlxml--;
                }
            }



            // =======================================================================    Fehler abfangen    =========================================================================================
            catch (Exception u)
            {
                Console.WriteLine("" + u);
            }
        }
    }
}