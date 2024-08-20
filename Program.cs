using System;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Data.SQLite;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;

namespace xmlGRE
{
    class Program
    {
        //static string rutaAct = @"c:\TRANSC_pruebas\TransCarga.db";
        static string rutaAct = Directory.GetCurrentDirectory() + @"\TransCarga.db";    // la base de datos siempre debe llamarse Transcarga.db
        public static string CadenaConexion = $"Data Source={rutaAct}";       // este app debe estar dentro del directorio del sistema Ej. c:/transcarga/xmlGRE
        
        static int Main(string[] args)
        {
            var ruta = args[0];                         // ruta donde se grabará el xml
            var ruce = args[1];                         // ruc del emisor de la guía electrónica
            var guia = args[2];                         // guia en formato <ruc>-<codGR>-<serie>-<numero>
            var ifir = args[3].ToLower();               // indicador si se debe firmar | true = si firmar, false = no firmar
            var cert = args[4];                         // ruta y nombre del certificado .pfx
            var clav = args[5];                         // clave del certificado
            var tipg = args[6];                         // tipo guía 31->Transportista || 09->Remitente

            // las tablas las crea Transcarga, el form de creación de guías lo hace
            //CreaTablaLiteGRE();                                     // llamada solo en esta ocasión
            int  retorna = 0;   // 0=fallo
            if (ruta != "" && ruce != "" && guia != "")
            {
                Console.WriteLine("Ruta xml: " + ruta);
                Console.WriteLine("Ruc: " + ruce);
                Console.WriteLine("Guía: " + guia);
                Console.WriteLine("Ruta .db: " + rutaAct);
                Console.WriteLine("Firma?: " + ifir);
                if (DatosUBLDespach(ruta, ruce, guia, ifir, cert, clav, tipg) == "Exito") retorna = 1;
            }
            return retorna;
        }

        private static string DatosUBLDespach(string Pruta, string PrucEmi, string PdocuGR, string IndFir, string RAcert, string Clacert, string tipg)      // ponemos los valores en la clase GRE_T
        {
            string retorna = "";
            using (SQLiteConnection cnx = new SQLiteConnection(CadenaConexion))
            {
                cnx.Open();
                string condet = "";
                string consulta = "";
                // 
                if (tipg == "31") condet = "select * from dt_detgre WHERE NumGuia=@gre ORDER BY NumGuia DESC LIMIT 1";  // "select * from dt_detgre where NumGuia=@gre";
                else condet = "select * from dt_detgrer where NumGuia=@gre";
                String[] detalle = new string[7];
                if (tipg == "31") consulta = "SELECT * FROM dt_cabgre WHERE NumGuia=@gre ORDER BY FecEmis,HorEmis DESC LIMIT 1";    // "select * from dt_cabgre where NumGuia=@gre";
                else consulta = "select * from dt_cabgrer where NumGuia=@gre";
                using (SQLiteCommand micon = new SQLiteCommand(condet, cnx))
                {
                    micon.Parameters.AddWithValue("@gre", PdocuGR);
                    using (SQLiteDataReader lite = micon.ExecuteReader())
                    {   // clinea,cant,codigo,peso,umed,deta1,deta2
                        if (lite.Read())
                        {
                            detalle[0] = "1";
                            detalle[1] = lite["cant"].ToString();
                            detalle[2] = lite["codigo"].ToString();
                            detalle[3] = lite["peso"].ToString();
                            detalle[4] = lite["umed"].ToString();
                            detalle[5] = lite["deta1"].ToString();
                            detalle[6] = lite["deta2"].ToString();
                        }
                    }
                }
                using (SQLiteCommand micon = new SQLiteCommand(consulta, cnx))
                {
                    micon.Parameters.AddWithValue("@gre", PdocuGR);
                    SQLiteDataReader lite = micon.ExecuteReader();
                    if (lite.HasRows == true)
                    {
                        if (lite.Read())
                        {
                            // cabecera de la guia
                            var c01 = lite["CodGuia"].ToString();
                            var c02 = lite["NomGuia"].ToString();
                            var c03 = lite["NumGuia"].ToString();
                            var c04 = lite["FecEmis"].ToString();
                            var c05 = lite["HorEmis"].ToString();
                            var c06 = int.Parse(lite["CantBul"].ToString());            // esto debe coincidir o ser la sumatoria del detalle
                            var c07 = decimal.Parse(lite["PesoTot"].ToString());        // esto debe coincidir o ser la sumatoria del detalle
                            var c08 = lite["CodUnid"].ToString();
                            var c09 = lite["FecIniT"].ToString();
                            var c10 = (lite["CargaUn"].ToString() == "true") ? true : false;
                            // documentos relacionados
                            var d01 = lite["DocRelnu1"].ToString();
                            var d02 = lite["DocRelti1"].ToString();
                            var d03 = lite["DocRelnr1"].ToString();
                            var d04 = lite["DocRelcs1"].ToString();
                            var d05 = lite["DocRelnm1"].ToString();
                            var d06 = (lite["DocRelnu2"].ToString() != "") ? lite["DocRelnu2"].ToString() : null;
                            var d07 = (lite["DocRelti2"].ToString() != "") ? lite["DocRelti2"].ToString() : null;
                            var d08 = (lite["DocRelnr2"].ToString() != "") ? lite["DocRelnr2"].ToString() : null;
                            var d09 = (lite["DocRelcs2"].ToString() != "") ? lite["DocRelcs2"].ToString() : null;
                            var d10 = (lite["DocRelnm2"].ToString() != "") ? lite["DocRelnm2"].ToString() : null;
                            // datos del emisor de la guía
                            var e01 = lite["EmisRuc"].ToString();
                            var e02 = lite["EmisNom"].ToString();
                            var e03 = lite["EmisUbi"].ToString();
                            var e04 = lite["EmisDir"].ToString();
                            var e05 = lite["EmisUrb"].ToString();
                            var e06 = lite["EmisDep"].ToString();
                            var e07 = lite["EmisPro"].ToString();
                            var e08 = lite["EmisDis"].ToString();
                            var e09 = lite["EmisPai"].ToString();
                            var e10 = lite["EmisCor"].ToString();
                            // datos del destinatario
                            var t01 = lite["DstTipdoc"].ToString();
                            var t02 = lite["DstNomTdo"].ToString();
                            var t03 = lite["DstNumdoc"].ToString();
                            var t04 = lite["DstNombre"].ToString();
                            var t05 = lite["DstDirecc"].ToString();
                            var t06 = lite["DstUbigeo"].ToString();
                            // datos del remitente
                            var r01 = lite["RemTipdoc"].ToString();
                            var r02 = lite["RemNomTdo"].ToString();
                            var r03 = lite["RemNumdoc"].ToString();
                            var r04 = lite["RemNombre"].ToString();
                            var r05 = lite["RemDirecc"].ToString();
                            var r06 = lite["RemUbigeo"].ToString();
                            // datos de quien pagará el servicio, en esta prueba asumimos que el detinatario pagará
                            var p01 = lite["PagTipdoc"].ToString();
                            var p02 = lite["PagNumdoc"].ToString();
                            var p03 = lite["PagNombre"].ToString();
                            // datos del camión subcontratado si fuera el caso ... en este caso estamos probando como camión propio
                            var s01 = (lite["SConTipdo"].ToString() != "") ? lite["SConTipdo"].ToString() : null;
                            var s02 = (lite["SConNomTi"].ToString() != "") ? lite["SConNomTi"].ToString() : null;
                            var s03 = (lite["SConNumdo"].ToString() != "") ? lite["SConNumdo"].ToString() : null;
                            var s04 = (lite["SconNombr"].ToString() != "") ? lite["SconNombr"].ToString() : null;
                            // datos del envío del (los) camiones, autorizaciones de trackto y carreta
                            var a01 = lite["EnvPlaca1"].ToString().Replace("-","");
                            var a02 = lite["EnvAutor1"].ToString();
                            var a03 = lite["EnvRegis1"].ToString();
                            var a04 = lite["EnvCodEn1"].ToString();
                            var a05 = lite["EnvNomEn1"].ToString();
                            var a06 = (lite["EnvPlaca2"].ToString().Trim() != "") ? lite["EnvPlaca2"].ToString().Replace("-", "") : "";
                            var a07 = lite["EnvAutor2"].ToString();
                            var a08 = lite["EnvRegis2"].ToString();
                            var a09 = lite["EnvCodEn2"].ToString();
                            var a10 = lite["EnvNomEn2"].ToString();
                            // datos de los choferes
                            var f01 = lite["ChoTipDi1"].ToString();
                            var f02 = lite["ChoNumDi1"].ToString();
                            var f03 = lite["ChoNomTi1"].ToString();
                            var f04 = lite["ChoNombr1"].ToString();
                            var f05 = lite["ChoApell1"].ToString();
                            var f06 = lite["ChoLicen1"].ToString().Replace("-", "");
                            var f07 = (lite["ChoTipDi2"].ToString() != "") ? lite["ChoTipDi2"].ToString() : null;
                            var f08 = (lite["ChoNumDi2"].ToString() != "") ? lite["ChoNumDi2"].ToString() : null;
                            var f09 = (lite["ChoNomTi2"].ToString() != "") ? lite["ChoNomTi2"].ToString() : null;
                            var f10 = (lite["ChoNombr2"].ToString() != "") ? lite["ChoNombr2"].ToString() : null;
                            var f11 = (lite["ChoApell2"].ToString() != "") ? lite["ChoApell2"].ToString() : null;
                            var f12 = (lite["ChoLicen2"].ToString() != "") ? lite["ChoLicen2"].ToString().Replace("-", "") : null;
                            // datos de direcciones partida y llegada
                            var i01 = lite["DirParUbi"].ToString();
                            var i02 = lite["DirParDir"].ToString();
                            var i03 = lite["DirLLeUbi"].ToString();
                            var i04 = lite["DirLLeDir"].ToString();
                            // motivo del traslado, solo para guias remitente
                            string m01="", m02="", m03="";
                            if (tipg == "09")
                            {
                                m01 = lite["MotTrasCo"].ToString();
                                m02 = lite["MotTrasDe"].ToString();
                                m03 = lite["CodModTra"].ToString();     // codigo sunat modalidad de transporte (propio ó público)
                            }
                            var o01 = lite["ObserGuia"].ToString();                // observaciones de la guía

                            GRE_T gRE = new GRE_T
                            {
                                // cabecera de la guia
                                CodGuia = c01,                  // "31",
                                NomGuia = c02,                  // "GUIA TRANSPORTISTA",
                                NumGuia = c03,                  // PdocuGR,
                                FecEmis = c04,                  // "2023-05-17",                 // fecha de emision de la guía
                                HorEmis = c05,                  // "10:31:13",
                                CantBul = c06,                  // 1,
                                PesoTot = c07,                  // 30,
                                CodUnid = c08,                  // "KGM",                        // código unidad de medida de sunat
                                FecIniT = c09,                  // "2023-05-17",                 // fecha de inicio del traslado, se debe cojer de la fecha del manifiesto
                                CargaUn = c10,                  // true,                        // marca de carga única, si=true, no=false
                                // documentos relacionados
                                DocRelnu1 = d01,              // "001-00054322", 
                                DocRelti1 = d02,              // "09",
                                DocRelnr1 = d03,              // "20430100344",
                                DocRelcs1 = d04,              // "6",
                                DocRelnm1 = d05,              // "GUIA DE REMISION REMITENTE",
                                DocRelnu2 = d06,
                                DocRelti2 = d07,
                                DocRelnr2 = d08,
                                DocRelcs2 = d09,
                                DocRelnm2 = d10,
                                // datos del emisor de la guía
                                EmisRuc = e01,                      // PrucEmi,
                                EmisNom = e02,                      // "J&L Technology SAC",
                                EmisUbi = e03,                      // "070101",
                                EmisDir = e04,                      // "Calle Sigma Mz.A19 Lt.16 Sector I",
                                EmisUrb = e05,                      // "Bocanegra",
                                EmisDep = e06,                      // "Callao",
                                EmisPro = e07,                      // "Callao",
                                EmisDis = e08,                      // "Callao",
                                EmisPai = e09,                      // "PE",                                 // código sunat de país
                                EmisCor = e10,                      // "neto.solorzano@solorsoft.com",
                                // datos del destinatario
                                DstTipdoc = t01,                        // "1",
                                DstNomTdo = t02,                        // "Documento Nacional de Identidad",
                                DstNumdoc = t03,                        // "09314486",
                                DstNombre = t04,                        // "Lucio Solórzano",
                                DstDirecc = t05,                        // "- -",
                                DstUbigeo = t06,                        // "070101",
                                // datos del remitente
                                RemTipdoc = r01,                        // "1",
                                RemNomTdo = r02,                        // "Documento Nacional de Identidad",
                                RemNumdoc = r03,                        // "10401018",
                                RemNombre = r04,                        // "Victoria Millan",
                                RemDirecc = r05,                        // "Bocanegra sector 1",
                                RemUbigeo = r06,                        // "070101",
                                // datos de quien pagará el servicio, en esta prueba asumimos que el detinatario pagará
                                PagTipdoc = p01,                        // "1",
                                PagNumdoc = p02,                        // "09314486",
                                PagNombre = p03,                        // "Lucio Solórzano",
                                // datos del camión subcontratado si fuera el caso ... en este caso estamos probando como camión propio
                                SConTipdo = s01,                        //"6",
                                SConNomTi = s02,                        //"Registro Unico de Contributentes",
                                SConNumdo = s03,                        //"20508074281",
                                SconNombr = s04,                        //"Transpostes Guapo Lindo",
                                // datos del envío del (los) camiones, autorizaciones de trackto y carreta
                                EnvPlaca1 = a01,                      // "F2N714",           // placa trackto
                                EnvAutor1 = a02,                      //"0151742078",       // autorización o certificado de habilitación del trackto
                                EnvRegis1 = a03,                      //"1550877CNG",       // número de registro MTC del trackto
                                EnvCodEn1 = a04,                      //"06",               // código sunat de la entidad que da el registro  ( MTC=06 )
                                EnvNomEn1 = a05,                      //"Ministerio de Transportes y Comunicaciones",     // nombre de la entidad
                                EnvPlaca2 = a06,                      //"AYS991",           // placa de la carreta ............ si es camión debe ir null 
                                EnvAutor2 = a07,                      //"15M21028161E",     // autorización  o certificado de habilitación de la carreta
                                EnvRegis2 = a08,                      //"1550877CNG",       // número de registro MTC de la carreta
                                EnvCodEn2 = a09,                      //"06",               // código sunat de la entidad que da la autorización ( MTC=06 )
                                EnvNomEn2 = a10,                      //"Ministerio de Transportes y Comunicaciones",     // nombre de la entidad
                                // datos de los choferes
                                ChoTipDi1 = f01,                         // "1",
                                ChoNumDi1 = f02,                         // "46785663",
                                ChoNomTi1 = f03,                         // "Documento Nacional de Identidad",
                                ChoNombr1 = f04,                         // "Williams Octavio",
                                ChoApell1 = f05,                         // "Yanqui Mamani",
                                ChoLicen1 = f06,                         // "U46785663",
                                ChoTipDi2 = f07,                        //"1",
                                ChoNumDi2 = f08,                        //"09314486",
                                ChoNomTi2 = f09,                        //"Documento Nacional de Identidad",
                                ChoNombr2 = f10,                        //"Neto",
                                ChoApell2 = f11,                        //"Solórzano Ramos",
                                ChoLicen2 = f12,                        //"Z09314486",
                                // datos de direcciones partida y llegada
                                DirParUbi = i01,                     // "150115",
                                DirParDir = i02,                     // "Jr. Hipolito Unanue 878 - La Victoria - Lima",
                                DirLLeUbi = i03,                     // "070101",
                                DirLLeDir = i04,                     // "Mz.A19 Lt.16 Sector I Bocanegra - Callao - Callao",
                                // motivo del traslado, solo para guias de remitente
                                MotTrasCo = m01,                     // codigo sunat, 
                                MotTrasDe = m02,                     // descripción motivo del traslado
                                CodModTra = m03,                     // código sunat modalidad de transporte
                                // observaciones
                                observ = o01,
                                // detalle de la guía
                                Detalle = detalle                   //new string[5] { "1", "ZZ", "30", "Servicio de Transporte de carga terrestre ", "Dice contener Enseres caseros" }     // Cant,Umed,Peso,Desc1,Desc2
                            };
                            if (tipg == "31")
                            {
                                retorna = UsoUBLDespachT(Pruta, IndFir, RAcert, Clacert,
                                    gRE.EmisRuc, gRE.EmisNom, gRE.EmisDir, gRE.EmisUbi, gRE.EmisDep, gRE.EmisPro, gRE.EmisDis, gRE.EmisUrb, gRE.EmisPai, gRE.EmisCor,
                                    gRE.CodGuia, gRE.NomGuia, gRE.NumGuia, gRE.FecEmis, gRE.HorEmis, gRE.CantBul, gRE.PesoTot, gRE.CodUnid, gRE.FecIniT, gRE.CargaUn,
                                    gRE.DocRelnu1, gRE.DocRelti1, gRE.DocRelnr1, gRE.DocRelcs1, gRE.DocRelnm1, gRE.DocRelnu2, gRE.DocRelti2, gRE.DocRelnr2, gRE.DocRelcs2, gRE.DocRelnm2,
                                    gRE.DstTipdoc, gRE.DstNomTdo, gRE.DstNumdoc, gRE.DstNombre, gRE.DstDirecc, gRE.DstUbigeo,
                                    gRE.RemTipdoc, gRE.RemNomTdo, gRE.RemNumdoc, gRE.RemNombre, gRE.RemDirecc, gRE.RemUbigeo,
                                    gRE.PagTipdoc, gRE.PagNomTip, gRE.PagNumdoc, gRE.PagNombre, gRE.SConTipdo, gRE.SConNomTi, gRE.SConNumdo, gRE.SconNombr,
                                    gRE.EnvPlaca1, gRE.EnvAutor1, gRE.EnvRegis1, gRE.EnvCodEn1, gRE.EnvNomEn1, gRE.EnvPlaca2, gRE.EnvAutor2, gRE.EnvRegis2, gRE.EnvCodEn2, gRE.EnvNomEn2,
                                    gRE.ChoTipDi1, gRE.ChoNumDi1, gRE.ChoNomTi1, gRE.ChoNombr1, gRE.ChoApell1, gRE.ChoLicen1,
                                    gRE.ChoTipDi2, gRE.ChoNumDi2, gRE.ChoNomTi2, gRE.ChoNombr2, gRE.ChoApell2, gRE.ChoLicen2,
                                    gRE.DirParUbi, gRE.DirParDir, gRE.DirLLeUbi, gRE.DirLLeDir, gRE.observ, gRE.Detalle);
                            }
                            if (tipg == "09")
                            {
                                retorna = UsoUBLDespachR(Pruta, IndFir, RAcert, Clacert,
                                    gRE.EmisRuc, gRE.EmisNom, gRE.EmisDir, gRE.EmisUbi, gRE.EmisDep, gRE.EmisPro, gRE.EmisDis, gRE.EmisUrb, gRE.EmisPai, gRE.EmisCor,
                                    gRE.CodGuia, gRE.NomGuia, gRE.NumGuia, gRE.FecEmis, gRE.HorEmis, gRE.CantBul, gRE.PesoTot, gRE.CodUnid, gRE.FecIniT, gRE.CargaUn,
                                    gRE.DocRelnu1, gRE.DocRelti1, gRE.DocRelnr1, gRE.DocRelcs1, gRE.DocRelnm1, gRE.DocRelnu2, gRE.DocRelti2, gRE.DocRelnr2, gRE.DocRelcs2, gRE.DocRelnm2,
                                    gRE.DstTipdoc, gRE.DstNomTdo, gRE.DstNumdoc, gRE.DstNombre, gRE.DstDirecc, gRE.DstUbigeo,
                                    gRE.RemTipdoc, gRE.RemNomTdo, gRE.RemNumdoc, gRE.RemNombre, gRE.RemDirecc, gRE.RemUbigeo,
                                    gRE.PagTipdoc, gRE.PagNomTip, gRE.PagNumdoc, gRE.PagNombre, gRE.SConTipdo, gRE.SConNomTi, gRE.SConNumdo, gRE.SconNombr,
                                    gRE.EnvPlaca1, gRE.EnvAutor1, gRE.EnvRegis1, gRE.EnvCodEn1, gRE.EnvNomEn1, gRE.EnvPlaca2, gRE.EnvAutor2, gRE.EnvRegis2, gRE.EnvCodEn2, gRE.EnvNomEn2,
                                    gRE.ChoTipDi1, gRE.ChoNumDi1, gRE.ChoNomTi1, gRE.ChoNombr1, gRE.ChoApell1, gRE.ChoLicen1,
                                    gRE.ChoTipDi2, gRE.ChoNumDi2, gRE.ChoNomTi2, gRE.ChoNombr2, gRE.ChoApell2, gRE.ChoLicen2,
                                    gRE.MotTrasCo, gRE.MotTrasDe, gRE.CodModTra,
                                    gRE.DirParUbi, gRE.DirParDir, gRE.DirLLeUbi, gRE.DirLLeDir, gRE.Detalle);
                            }
                        }
                    }
                    else
                    {
                        // no hay datos
                        //
                    }
                }
            };
            return retorna;
        }
        
        private static string UsoUBLDespachT(string Pruta, string IndFir, string RAcert, string Clacert, 
            string rucEmi, string nomEmi, string dirEmi, string ubiEmi, string depEmi, string proEmi, string disEmi, string urbEmi, string paiEmi, string corEmi,
            string codGuia, string nomGuia, string numGuia, string fecEmis, string horEmis, int cantBul, decimal pesoTot, string codunis, string feciniT, bool cargaun,
            string docRelnu1, string docRelti1, string docRelnr1, string docRelcs1, string docRelnm1, string docRelnu2, string docRelti2, string docRelnr2, string docRelcs2, string docRelnm2,
            string dstdocu, string dstnomt, string dstnumd, string dstnomb, string dstdire, string dstubig,
            string remdocu, string remnomt, string remnumd, string remnomb, string remdirec, string remubig,
            string pagdocu, string pagnomt, string pagnume, string pagnomb, string scontip, string sconnoT, string sconnum, string sconnom,
            string envPlaca1, string envAutor1, string envRegis1, string envCodEn1, string envNomEn1, string envPlaca2, string envAutor2, string envRegis2, string envCodEn2, string envNomEn2,
            string choTipDi1, string choNumDi1, string choNomTi1, string choNombr1, string choApell1, string choLicen1,
            string choTipDi2, string choNumDi2, string choNomTi2, string choNombr2, string choApell2, string choLicen2,
            string dirParubi, string dirPardir, string dirLLeubi, string dirLLedir, string observ, string[] deta)         // GUIA TRANSPORTISTA 
        {
            string retorna = "Fallo";
            // OBSERVACION (SI HAY)
            NoteType[] nota1 = null;
            if (string.IsNullOrEmpty(observ)) // observ != null && observ != ""
            {
                // TEXTO TITULO DEL DOCUMENTO
                nota1 = new NoteType[]
                {
                    new NoteType{ Value = nomGuia}
                };
            }
            else
            {
                // TEXTO TITULO DEL DOCUMENTO
                nota1 = new NoteType[]
                {
                    new NoteType{ Value = nomGuia},
                    new NoteType{ Value = observ}
                };
            }
            // CODIGO TIPO DE DOCUMENTO
            DespatchAdviceTypeCodeType codtipo = new DespatchAdviceTypeCodeType
            {
                listAgencyName = "PE:SUNAT",
                listName = "Tipo de Documento",
                listURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01",
                Value = codGuia // "31"
            };

            #region // DATOS DE EXTENSION DEL DOCUMENTO, acá va principalmente la FIRMA en el caso que el metodo de envío a sunat NO sea SFS
            XmlDocument xmlDocument = new XmlDocument();
            XmlElement firma = xmlDocument.CreateElement("ext:firma", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");  // 31/05/2023
            UBLExtensionType[] uBLExtensionTypes = new UBLExtensionType[] { new UBLExtensionType { ExtensionContent = firma } };        //  ExtensionContent = firma <- 31/05/2023
            #endregion

            #region // DATOS DEL DOCUMENTO RELACIONADO
            PartyType party = new PartyType
            {
                PartyIdentification = new PartyIdentificationType[]
                {
                    new PartyIdentificationType
                    {
                        ID = new IDType { Value = docRelnr1, schemeID = docRelcs1, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" }
                    }
                }
            };
            PartyType party2 = new PartyType
            {
                PartyIdentification = new PartyIdentificationType[]
                {
                    new PartyIdentificationType
                    {
                        ID = new IDType { Value = docRelnr2, schemeID = docRelcs2, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" }
                    }
                }
            };
            DocumentReferenceType[] refer = new DocumentReferenceType[]
            {
                new DocumentReferenceType { ID = new IDType { Value = docRelnu1},    // Si es GR remitente es: SSS-NNNNNNNN = 12 caracteres
                    DocumentTypeCode = new DocumentTypeCodeType { listAgencyName = "PE:SUNAT", listName = "Documento relacionado al transporte", listURI= "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo61", Value = docRelti1 },  // "50"
                    DocumentType = new DocumentTypeType { Value = docRelnm1 },
                    IssuerParty = party
                },
            };
            DocumentReferenceType[] refer2 = new DocumentReferenceType[]
            {
                new DocumentReferenceType { ID = new IDType { Value = docRelnu2},    // Value = "118-2022-10-26"
                    DocumentTypeCode = new DocumentTypeCodeType { listAgencyName = "PE:SUNAT", listName = "Documento relacionado al transporte", listURI= "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo61", Value = docRelti2 },  // "50"
                    DocumentType = new DocumentTypeType { Value = docRelnm2 },
                    IssuerParty = party2
                },
            };
            #endregion

            #region // FIRMA ELECTRONICA DE LA GUIA ELECTRONICA 
            PartyType iden = new PartyType
            {
                PartyIdentification = new PartyIdentificationType[]
                {
                    new PartyIdentificationType { ID = new IDType{Value = rucEmi } }
                },
                PartyName = new PartyNameType[]
                {
                    new PartyNameType { Name = new NameType1 {Value = nomEmi } }   // Value = nomEmi
                }
            };
            AttachmentType atach = new AttachmentType
            {
                ExternalReference = new ExternalReferenceType
                {
                    URI = new URIType { Value = numGuia }        // Value = "#SFT001-00000001"
                }
            };
            PartyType partid = new PartyType
            {
                PartyIdentification = new PartyIdentificationType[]
                {
                    new PartyIdentificationType { ID = new IDType { Value = rucEmi } }
                },
                PartyName = new PartyNameType[] { new PartyNameType { Name = new NameType1 { Value = nomEmi } } } // Value = nomEmi
            };
            AttachmentType attach = new AttachmentType
            {
                ExternalReference = new ExternalReferenceType { URI = new URIType { Value = "SigNode" } }
            };
            SignatureType tory = new SignatureType
            {
                ID = new IDType { Value = "SignSOLORSOFT" },
                SignatoryParty = partid,
                DigitalSignatureAttachment = attach
            };
            SignatureType[] signature = new SignatureType[]
            {
                tory
            };
            #endregion

            // DATOS DEL EMISOR DE LA GUIA ELECTRONICA
            SupplierPartyType prove = new SupplierPartyType
            {
                CustomerAssignedAccountID = new CustomerAssignedAccountIDType { Value = rucEmi, schemeID = "6" },     // prueba para el 18/05/2023
                Party = new PartyType 
                { 
                    PartyIdentification = new PartyIdentificationType[]
                    {
                        new PartyIdentificationType { ID = new IDType { Value = rucEmi, schemeID = "6", schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"} }
                    },
                    PostalAddress = new AddressType 
                    { 
                        ID = new IDType { Value = ubiEmi },
                        StreetName = new StreetNameType { Value = dirEmi },
                        CitySubdivisionName = new CitySubdivisionNameType { Value = urbEmi },
                        CityName = new CityNameType { Value = depEmi },
                        CountrySubentity = new CountrySubentityType { Value = proEmi },
                        District = new DistrictType { Value = disEmi },
                        Country = new CountryType { IdentificationCode = new IdentificationCodeType { Value = paiEmi } }
                    },
                    PartyLegalEntity = new PartyLegalEntityType[]
                    {
                        new PartyLegalEntityType { RegistrationName = new RegistrationNameType { Value = nomEmi } }
                    },
                    Contact = new ContactType { ElectronicMail = new ElectronicMailType { Value = corEmi } }
                }
            };

            // DATOS DEL DESTINATARIO DE LA GUIA ELECTRONICA     
            CustomerPartyType cliente = new CustomerPartyType
            {
                Party = new PartyType
                {
                    PartyIdentification = new PartyIdentificationType[]
                    {
                        new PartyIdentificationType { ID = new IDType{ Value = dstnumd, schemeID = dstdocu, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"} }
                    },
                    PartyLegalEntity = new PartyLegalEntityType[]
                    {
                        new PartyLegalEntityType { RegistrationName = new RegistrationNameType { Value = dstnomb } }
                    }
                }
            };

            // DATOS DE QUIEN PAGA EL SERVICIO
            CustomerPartyType pagador = new CustomerPartyType
            {
                Party = new PartyType
                {
                    PartyIdentification = new PartyIdentificationType[]
                    {
                    new PartyIdentificationType { ID = new IDType { Value = pagnume, schemeID = pagdocu, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"} }
                    },
                    PartyLegalEntity = new PartyLegalEntityType[]
                    {
                    new PartyLegalEntityType { RegistrationName = new RegistrationNameType { Value = pagnomb } }
                    }
                }
            };

            // Camión contratado ..............
            ConsignmentType[] contratado;
            if (scontip != null)
            {
                contratado = new ConsignmentType[]
                {
                new ConsignmentType { ID = new IDType { Value = "SUNAT_Envio" },        // Value = "SUNAT_Envio"
                    LogisticsOperatorParty = new PartyType
                    {
                        PartyIdentification = new PartyIdentificationType[] { new PartyIdentificationType {
                            ID = new IDType { Value = sconnum, schemeID = scontip, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" } } },
                        PartyLegalEntity = new PartyLegalEntityType[] { new PartyLegalEntityType { RegistrationName = new RegistrationNameType { Value = sconnom } } }
                    }
                }
                };
            }
            else contratado = new ConsignmentType[] { };
            // Choferes .......................
            PersonType[] choferes;
            if (choNumDi2 != null && choNumDi2 != "")
            {
                choferes = new PersonType[]
                {
                    new PersonType
                    {
                        ID = new IDType { Value = choNumDi1, schemeID = choTipDi1, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" },
                        FirstName = new FirstNameType { Value = choNombr1 },
                        FamilyName = new FamilyNameType { Value = choApell1 },
                        JobTitle = new JobTitleType { Value = "Principal" },
                        IdentityDocumentReference = new DocumentReferenceType[] { new DocumentReferenceType { ID = new IDType { Value = choLicen1 } } }
                    },
                    new PersonType
                    {
                        ID = new IDType { Value = choNumDi2, schemeID = choTipDi2, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" },
                        FirstName = new FirstNameType { Value = choNombr2 },
                        FamilyName = new FamilyNameType { Value = choApell2 },
                        JobTitle = new JobTitleType { Value = "Secundario" },
                        IdentityDocumentReference = new DocumentReferenceType[] { new DocumentReferenceType { ID = new IDType { Value = choLicen2 } } }
                    }
                };
            }
            else
            {
                choferes = new PersonType[] {
                    new PersonType
                    {
                        ID = new IDType { Value = choNumDi1, schemeID = choTipDi1, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" },
                        FirstName = new FirstNameType { Value = choNombr1 },
                        FamilyName = new FamilyNameType { Value = choApell1 },
                        JobTitle = new JobTitleType { Value = "Principal" },
                        IdentityDocumentReference = new DocumentReferenceType[] { new DocumentReferenceType { ID = new IDType { Value = choLicen1 } } }
                    }
                };
            }
            // DATOS DEL TRASLADO, VEHICULOS Y CHOFERES
            SpecialInstructionsType indicadorSubCont = null;
            SpecialInstructionsType indicadorCargaUnica = null;
            SpecialInstructionsType indicadorQuienpaga = null;
            if (scontip != null) indicadorSubCont = new SpecialInstructionsType { Value = "SUNAT_Envio_IndicadorTrasporteSubcontratado" };
            if (cargaun == true) indicadorCargaUnica = new SpecialInstructionsType { Value = "SUNAT_Envio_IndicadorTrasladoTotal" };
            if (pagnume == remnumd) indicadorQuienpaga = new SpecialInstructionsType { Value = "SUNAT_Envio_IndicadorPagadorFlete_Remitente" };
            else indicadorQuienpaga = new SpecialInstructionsType { Value = "SUNAT_Envio_IndicadorPagadorFlete_Tercero" };
            ShipmentType traslado = new ShipmentType
            {
                ID = new IDType { Value = "1" },
                GrossWeightMeasure = new GrossWeightMeasureType { Value = pesoTot, unitCode = codunis },                    // Unidad de medida y peso TOTAL de la carga
                SpecialInstructions = new SpecialInstructionsType[]
                {
                    /*
                    SUNAT_Envio_IndicadorTransbordoProgramado
                    SUNAT_Envio_IndicadorRetornoVehiculoEnvaseVacio
                    SUNAT_Envio_IndicadorRetornoVehiculoVacio
                    SUNAT_Envio_IndicadorTrasporteSubcontratado
                    SUNAT_Envio_IndicadorPagadorFlete_Remitente
                    SUNAT_Envio_IndicadorPagadorFlete_Subcontratador
                    SUNAT_Envio_IndicadorPagadorFlete_Tercero
                    SUNAT_Envio_IndicadorTrasladoTotal
                    */
                    indicadorSubCont,
                    //indicadorCargaUnica,
                    indicadorQuienpaga
                },
                Consignment = contratado,
                ShipmentStage = new ShipmentStageType[]                                                     // Datos de la carga, fecha de inicio y choferes
                {
                    new ShipmentStageType
                    {
                        TransitPeriod = new PeriodType { StartDate = new StartDateType { Value = DateTime.Parse(feciniT) } },

                        CarrierParty = new PartyType[]
                        {
                            new PartyType {
                                PartyLegalEntity = new PartyLegalEntityType[] { new PartyLegalEntityType { CompanyID = new CompanyIDType { Value = envRegis1 } } }
                                //AgentParty = new PartyType { PartyLegalEntity = new PartyLegalEntityType[] { new PartyLegalEntityType { CompanyID = new CompanyIDType { Value = envAutor1, schemeID = envCodEn1, schemeName = "Entidad Autorizadora", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoD37" } } } } 
                            }
                        },
                        /*
                        // adecuacion del 01/02/2024 10:10 pm.
                        CarrierParty = new PartyType[]
                        {
                            new PartyType
                            { 
                                PartyIdentification = new PartyIdentificationType[]
                                { 
                                    new PartyIdentificationType 
                                    { 
                                        ID = new IDType { Value = rucEmi, schemeID = "6", schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"} 
                                    } 
                                },
                                PartyLegalEntity = new PartyLegalEntityType[]
                                {
                                    new PartyLegalEntityType { RegistrationName = new RegistrationNameType { Value = nomEmi } }
                                }
                            }
                        },
                        */
                        DriverPerson = choferes,
                    }
                },
                Delivery = new DeliveryType                                                             // Direcciones punto de llegada y partida, datos del remitente
                {
                    DeliveryAddress = new AddressType
                    {
                        ID = new IDType { Value = dirLLeubi, schemeName = "Ubigeos", schemeAgencyName = "PE:INEI" },
                        StreetName = new StreetNameType { Value = dirLLedir },
                        AddressLine = new AddressLineType[] { new AddressLineType { Line = new LineType { Value = dirLLedir } } }
                        //LocationCoordinate = esto no es obligatorio, recontra opcional
                    },
                    Despatch = new DespatchType
                    {
                        DespatchAddress = new AddressType
                        {
                            ID = new IDType { Value = dirParubi, schemeName = "Ubigeos", schemeAgencyName = "PE:INEI" },
                            AddressLine = new AddressLineType[] { new AddressLineType { Line = new LineType { Value = dirPardir } } }
                            //LocationCoordinate = esto no es obligatorio, recontra opcional
                        },
                        DespatchParty = new PartyType
                        {
                            PartyIdentification = new PartyIdentificationType[] { new PartyIdentificationType { ID = new IDType { Value = remnumd, schemeID = remdocu, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" } } },
                            PartyLegalEntity = new PartyLegalEntityType[] { new PartyLegalEntityType { RegistrationName = new RegistrationNameType { Value = remnomb } } }
                        }
                    }
                },
                TransportHandlingUnit = new TransportHandlingUnitType[]
                {
                    new TransportHandlingUnitType
                    {
                        ID = new IDType { Value = "" },  // Value = "Numero Contenedor"
                        TransportEquipment = new TransportEquipmentType[]
                        {
                            new TransportEquipmentType { ID = new IDType { Value = envPlaca1 },                  //-- PLACA - VEHICULO PRINCIPAL
                                ApplicableTransportMeans = new TransportMeansType { RegistrationNationalityID = new RegistrationNationalityIDType { Value = envAutor1 } },     // envRegis1
                                AttachedTransportEquipment = new TransportEquipmentType[] { new TransportEquipmentType {
                                    ID = new IDType { Value = envPlaca2 },                                      //-- PLACA - VEHICULO SECUNDARIO O CARRETA 
                                    ApplicableTransportMeans = new TransportMeansType { RegistrationNationalityID = new RegistrationNationalityIDType { Value = envAutor2 } }   // envRegis2
                                    //ShipmentDocumentReference = new DocumentReferenceType[]         // Tarjeta Unica Circulacion / Cerificado Habilitacion Vehicular - Principal
                                    //    { new DocumentReferenceType {ID = new IDType { Value = envRegis2, schemeID = envCodEn2, schemeName = "Entidad Autorizadora", schemeAgencyName="PE:SUNAT", schemeURI="urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoD37" } } } }    // envAutor2
                                }
                                //ShipmentDocumentReference = new DocumentReferenceType[]       // comentamos porque es dato opcional y se repite con la aut. de circulación 26/07/2023
                                //{
                                //    new DocumentReferenceType { ID = new IDType { Value = envRegis1, schemeID = envCodEn1, schemeName = "Entidad Autorizadora", schemeAgencyName="PE:SUNAT", schemeURI="urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoD37" } }     // envAutor1
                                //}
                                }
                            }
                        }
                    }
                    /*,
                    FirstArrivalPortLocation = new LocationType1
                    {
                        ID = new IDType { Value = "Codigo", schemeName = "Puertos", schemeAgencyName = "PE:SUNAT" },
                        LocationTypeCode = new LocationTypeCodeType { Value = "1" }
                    } 
                    */
                },
            };
            #region // DETALLE DE LA GUIA DE REMISION ELECTRONICA
            DespatchLineType[] detalle = null;
            if (cargaun == true)
            {
                detalle = new DespatchLineType[]
                {
                    new DespatchLineType {
                        ID = new IDType { Value = "1"}, 
                        //DeliveredQuantity = new DeliveredQuantityType { Value = decimal.Parse(deta[0]), unitCode = deta[2], unitCodeListID = "UN/ECE rec 20", unitCodeListAgencyName = "United Nations Economic Commission for Europe"},
                        DeliveredQuantity = new DeliveredQuantityType { Value = decimal.Parse(deta[3]), unitCode = deta[4], unitCodeListID = "UN/ECE rec 20", unitCodeListAgencyName = "United Nations Economic Commission for Europe"},
                        OrderLineReference = new OrderLineReferenceType[] { new OrderLineReferenceType { LineID = new LineIDType {Value = "1" } } },
                        Item = new ItemType {
                            Description = new DescriptionType[] { new DescriptionType { Value = deta[5] + " " + deta[6] } }
                            //SellersItemIdentification = new ItemIdentificationType { ID = new IDType { Value = ""} },     comentado 02/02/2024 16:15 
                            //StandardItemIdentification = new ItemIdentificationType { ID = new IDType { Value = "", schemeID = ""} },     comentado 02/02/2024 16:15 
                        }
                    },
                };
            }
            else
            {
                detalle = new DespatchLineType[]
                {
                    new DespatchLineType {
                        ID = new IDType { Value = "1"}, 
                        DeliveredQuantity = new DeliveredQuantityType { Value = decimal.Parse(deta[0]), unitCode = deta[2], unitCodeListID = "UN/ECE rec 20", unitCodeListAgencyName = "United Nations Economic Commission for Europe"},
                        OrderLineReference = new OrderLineReferenceType[] { new OrderLineReferenceType { LineID = new LineIDType {Value = "1" } } },
                        Item = new ItemType {
                            Description = new DescriptionType[] { new DescriptionType { Value = deta[5] + " " + deta[6] } }
                            // Description = new DescriptionType[] { new DescriptionType { Value = deta[5] + " " + deta[6] + " - " + deta[1] + " BULTOS" } }    comantado 16/08/2024
                            // Description = new DescriptionType[] { new DescriptionType { Value = deta[5] + " " + deta[6] } }   comentado 14/08/2024
                            //SellersItemIdentification = new ItemIdentificationType { ID = new IDType { Value = ""} },     comentado 02/02/2024 16:15 
                            //StandardItemIdentification = new ItemIdentificationType { ID = new IDType { Value = "", schemeID = ""} },     comentado 02/02/2024 16:15 
                        }
                    },
                };
            }
            #endregion
            /* 15/12/2023 -> TODO ESTO SE COMENTO PORQUE ASI SEA CARGA UNICA SI DEBE LLEVAR DATOS DEL CONTENIDO O DETALLE DE LA CARGA
            if (indicadorCargaUnica != null && (
                "'09','01','04'".Contains(docRelti1) && !"'0','1','2','3','4','5','6','7','8','9'".Contains(docRelnu1.Substring(0, 1)) ||
                "'50','52'".Contains(docRelti1)))
            {
                // no lleva detalle de los bienes porque sunat dice que es carga total del documento origen .... FALSO 15/12/2023 Si debe llevar datos de la carga
            }
            else
            {
                detalle = new DespatchLineType[]
                {
                    new DespatchLineType {
                        ID = new IDType { Value = "1"},
                        DeliveredQuantity = new DeliveredQuantityType { Value = decimal.Parse(deta[0]), unitCode = deta[2], unitCodeListID = "UN/ECE rec 20", unitCodeListAgencyName = "United Nations Economic Commission for Europe"},
                        OrderLineReference = new OrderLineReferenceType[] { new OrderLineReferenceType { LineID = new LineIDType {Value = "1" } } },
                        Item = new ItemType {
                            Description = new DescriptionType[] { new DescriptionType { Value = deta[5] + " " + deta[6] } },
                            SellersItemIdentification = new ItemIdentificationType { ID = new IDType { Value = ""} },
                            StandardItemIdentification = new ItemIdentificationType { ID = new IDType { Value = "", schemeID = ""} },
                            //CommodityClassification = new CommodityClassificationType[] {new CommodityClassificationType { ItemClassificationCode = new ItemClassificationCodeType { Value = "50161509", listID = "UNSPSC", listAgencyName = "GS1 US", listName = "Item Classification" } } },
                            //AdditionalItemProperty = new ItemPropertyType[] {new ItemPropertyType {Value = new ValueType { Value = "3002159000" }, Name = new NameType1 { Value = "SubpartidaNacional"}, NameCode = new NameCodeType { Value = "7020", listAgencyName = "PE:SUNAT", listName = "Propiedad del item", listURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo55" } } }
                        }
                    },
                };
            }
            */
            // ARMAMOS EL XML
            XmlSerializer serial = new XmlSerializer(typeof(DespatchAdviceType));
            Stream fs = new FileStream(Pruta + rucEmi + "-" + codGuia + "-" + numGuia + ".xml", FileMode.Create, FileAccess.Write);
            var _comprobante = new DespatchAdviceType();
            _comprobante.UBLExtensions = uBLExtensionTypes;     //-- DATOS DE EXTENSION DEL DOCUMENTO
            _comprobante.UBLVersionID = new UBLVersionIDType { Value = "2.1" };     //     .
            _comprobante.CustomizationID = new CustomizationIDType { Value = "2.0" };   // .
            _comprobante.ID = new IDType { Value = numGuia };                   // "VG01-1000002"  .
            _comprobante.IssueDate = new IssueDateType { Value = DateTime.Parse(fecEmis)};  // FECHA DE EMISION  .
            _comprobante.IssueTime = new IssueTimeType { Value = horEmis };     // output        .
            _comprobante.DespatchAdviceTypeCode = codtipo;      //-- CODIGO TIPO DE DOCUMENTO    .
            _comprobante.Note = nota1;                          //-- TEXTO DEL TIPO DE DOCUMENTO .
            if (docRelnu1 != "") _comprobante.AdditionalDocumentReference = refer;   //-- DOCUMENTO RELACIONADO       .
            if (docRelnu2 != null) _comprobante.AdditionalDocumentReference = refer2;   //       .
            _comprobante.Signature = signature;                 //-- FIRMA DEL DOCUMENTO         .
            _comprobante.DespatchSupplierParty = prove;         //-- DATOS DEL EMISOR (TRANSPORTISTA) --//  .
            _comprobante.DeliveryCustomerParty = cliente;       //-- DATOS DEL RECEPTOR (DESTINATARIO) --// .
            _comprobante.OriginatorCustomerParty = pagador;     //-- DATOS DE QUIEN PAGA EL SERVICIO --//   .
            _comprobante.Shipment = traslado;                   //-- DATOS DEL TRASLADO --//                .
            _comprobante.DespatchLine = detalle;                //-- DETALLE DE LA GUIA --//                .

            var xns = new XmlSerializerNamespaces();
            xns.Add("", "urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2");
            xns.Add("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
            xns.Add("ds", "http://www.w3.org/2000/09/xmldsig#");
            xns.Add("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
            xns.Add("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
            xns.Add("xsd", "http://www.w3.org/2001/XMLSchema");

            if (IndFir == "true")           // llama al metodo que firma y graba el xml firmado
            {
                var oStringWriter = new StringWriter();
                serial.Serialize(XmlWriter.Create(oStringWriter), _comprobante, xns);
                string stringXml = oStringWriter.ToString();
                XmlDocument XmlpaFirmar = new XmlDocument();
                XmlpaFirmar.LoadXml(stringXml);
                //FirmarDocumentoXml(XmlpaFirmar, "certificado.pfx", "190969Sorol").Save("XmlFirmado.xml");
                using (Stream stream = fs)
                {
                    using (XmlWriter xmlWriter = new XmlTextWriter(stream, Encoding.GetEncoding("ISO-8859-1")))
                    {
                        FirmarDocumentoXml(XmlpaFirmar, RAcert, Clacert).Save(xmlWriter);  //  "certificado.pfx", "190969Sorol"
                    }
                }
            }
            else
            {
                using (Stream stream = fs)  // graba el xml sin firmar
                {
                    using (XmlWriter xmlWriter = new XmlTextWriter(stream, Encoding.GetEncoding("ISO-8859-1")))
                    {
                        serial.Serialize(xmlWriter, _comprobante, xns);
                    }
                    Console.WriteLine("Exito -> " + fs.ToString());
                    retorna = "Exito";
                }
            }
            return retorna;
        }

        private static string UsoUBLDespachR(string Pruta, string IndFir, string RAcert, string Clacert,
            string rucEmi, string nomEmi, string dirEmi, string ubiEmi, string depEmi, string proEmi, string disEmi, string urbEmi, string paiEmi, string corEmi,
            string codGuia, string nomGuia, string numGuia, string fecEmis, string horEmis, int cantBul, decimal pesoTot, string codunis, string feciniT, bool cargaun,
            string docRelnu1, string docRelti1, string docRelnr1, string docRelcs1, string docRelnm1, string docRelnu2, string docRelti2, string docRelnr2, string docRelcs2, string docRelnm2,
            string dstdocu, string dstnomt, string dstnumd, string dstnomb, string dstdire, string dstubig,
            string remdocu, string remnomt, string remnumd, string remnomb, string remdirec, string remubig,
            string pagdocu, string pagnomt, string pagnume, string pagnomb, string scontip, string sconnoT, string sconnum, string sconnom,
            string envPlaca1, string envAutor1, string envRegis1, string envCodEn1, string envNomEn1, string envPlaca2, string envAutor2, string envRegis2, string envCodEn2, string envNomEn2,
            string choTipDi1, string choNumDi1, string choNomTi1, string choNombr1, string choApell1, string choLicen1,
            string choTipDi2, string choNumDi2, string choNomTi2, string choNombr2, string choApell2, string choLicen2,
            string motTrasCo, string motTrasDe, string codModTra,
            string dirParubi, string dirPardir, string dirLLeubi, string dirLLedir, string[] deta)         // GUIA REMITENTE
        {
            string retorna = "Fallo";
            if (feciniT == "") feciniT = fecEmis;

            // TEXTO TITULO DEL DOCUMENTO
            NoteType[] nota1 = new NoteType[]
            {
                new NoteType{ Value = nomGuia}
            };
            // CODIGO TIPO DE DOCUMENTO
            DespatchAdviceTypeCodeType codtipo = new DespatchAdviceTypeCodeType
            {
                listAgencyName = "PE:SUNAT",
                listName = "Tipo de Documento",
                listURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01",
                Value = codGuia
            };

            #region // DATOS DE EXTENSION DEL DOCUMENTO, acá va principalmente la FIRMA en el caso que el metodo de envío a sunat NO sea SFS
            XmlDocument xmlDocument = new XmlDocument();
            XmlElement firma = xmlDocument.CreateElement("ext:firma", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");  // 31/05/2023
            UBLExtensionType[] uBLExtensionTypes = new UBLExtensionType[] { new UBLExtensionType { ExtensionContent = firma } };        //  ExtensionContent = firma <- 31/05/2023
            #endregion

            #region // DATOS DEL DOCUMENTO RELACIONADO
            PartyType party = new PartyType
            {
                PartyIdentification = new PartyIdentificationType[]
                {
                    new PartyIdentificationType
                    {
                        ID = new IDType { Value = docRelnr1, schemeID = docRelcs1, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" }
                    }
                }
            };
            DocumentReferenceType[] refer = new DocumentReferenceType[]
            {
                new DocumentReferenceType { ID = new IDType { Value = docRelnu1},    // Si es GR remitente es: SSS-NNNNNNNN = 12 caracteres
                    //DocumentTypeCode = new DocumentTypeCodeType { listAgencyName = "PE:SUNAT", listName = "Documento relacionado al transporte", listURI= "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo61", Value = docRelti1 },  // "50"
                    DocumentTypeCode = new DocumentTypeCodeType{ Value = docRelti1 },
                    DocumentType = new DocumentTypeType { Value = docRelnm1 },
                    IssuerParty = party
                },
            };
            #endregion

            #region // FIRMA ELECTRONICA DE LA GUIA ELECTRONICA 
            PartyType iden = new PartyType
            {
                PartyIdentification = new PartyIdentificationType[]
                {
                    new PartyIdentificationType { ID = new IDType{Value = rucEmi } }
                },
                PartyName = new PartyNameType[]
                {
                    new PartyNameType { Name = new NameType1 {Value = nomEmi } }   // Value = nomEmi
                }
            };
            AttachmentType atach = new AttachmentType
            {
                ExternalReference = new ExternalReferenceType
                {
                    URI = new URIType { Value = numGuia }        // Value = "#SFT001-00000001"
                }
            };
            PartyType partid = new PartyType
            {
                PartyIdentification = new PartyIdentificationType[]
                {
                    new PartyIdentificationType { ID = new IDType { Value = rucEmi } }
                },
                PartyName = new PartyNameType[] { new PartyNameType { Name = new NameType1 { Value = nomEmi } } } // Value = nomEmi
            };
            AttachmentType attach = new AttachmentType
            {
                ExternalReference = new ExternalReferenceType { URI = new URIType { Value = "SigNode" } }
            };
            SignatureType tory = new SignatureType
            {
                ID = new IDType { Value = "SignSOLORSOFT" },
                SignatoryParty = partid,
                DigitalSignatureAttachment = attach
            };
            SignatureType[] signature = new SignatureType[]
            {
                tory
            };
            #endregion

            // DATOS DEL EMISOR DE LA GUIA ELECTRONICA (REMITENTE)
            SupplierPartyType prove = new SupplierPartyType
            {
                CustomerAssignedAccountID = new CustomerAssignedAccountIDType { Value = rucEmi, schemeID = "6" },     // prueba para el 18/05/2023
                Party = new PartyType
                {
                    PartyIdentification = new PartyIdentificationType[]
                    {
                        new PartyIdentificationType { ID = new IDType { Value = rucEmi, schemeID = "6", schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"} }
                    },
                    PostalAddress = new AddressType
                    {
                        ID = new IDType { Value = ubiEmi },
                        StreetName = new StreetNameType { Value = dirEmi },
                        CitySubdivisionName = new CitySubdivisionNameType { Value = urbEmi },
                        CityName = new CityNameType { Value = depEmi },
                        CountrySubentity = new CountrySubentityType { Value = proEmi },
                        District = new DistrictType { Value = disEmi },
                        Country = new CountryType { IdentificationCode = new IdentificationCodeType { Value = paiEmi } }
                    },
                    PartyLegalEntity = new PartyLegalEntityType[]
                    {
                        new PartyLegalEntityType { RegistrationName = new RegistrationNameType { Value = nomEmi } }
                    },
                    Contact = new ContactType { ElectronicMail = new ElectronicMailType { Value = corEmi } }
                }
            };

            // DATOS DEL DESTINATARIO DE LA GUIA ELECTRONICA     
            CustomerPartyType cliente = new CustomerPartyType
            {
                Party = new PartyType
                {
                    PartyIdentification = new PartyIdentificationType[]
                    {
                        new PartyIdentificationType { ID = new IDType{ Value = dstnumd, schemeID = dstdocu, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"} }
                    },
                    PartyLegalEntity = new PartyLegalEntityType[]
                    {
                        new PartyLegalEntityType { RegistrationName = new RegistrationNameType { Value = dstnomb } }
                    }
                }
            };
            // DATOS DEL TRASLADO, VEHICULOS Y CHOFERES
            // Camión contratado ..............
            ConsignmentType[] contratado;
            if (scontip != null)
            {
                contratado = new ConsignmentType[]
                {
                new ConsignmentType { ID = new IDType { Value = "SUNAT_Envio" },        // Value = "SUNAT_Envio"
                    LogisticsOperatorParty = new PartyType
                    {
                        PartyIdentification = new PartyIdentificationType[] { new PartyIdentificationType {
                            ID = new IDType { Value = sconnum, schemeID = scontip, schemeName = sconnoT, schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" } } },
                        PartyLegalEntity = new PartyLegalEntityType[] { new PartyLegalEntityType { RegistrationName = new RegistrationNameType { Value = sconnom } } }
                    }
                }
                };
            }
            else contratado = new ConsignmentType[] { };
            // Choferes .......................
            PersonType[] choferes = null;
            if (codModTra == "02")  // datos de choferes va en transporte privado solamente
            {
                if (choTipDi2 != null && choTipDi2 != "")
                {
                    if (choTipDi1 != null && choTipDi1 != "")
                    {
                        choferes = new PersonType[]
                        {
                        new PersonType
                        {
                            ID = new IDType { Value = choNumDi1, schemeID = choTipDi1, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" },
                            FirstName = new FirstNameType { Value = choNombr1 },
                            FamilyName = new FamilyNameType { Value = choApell1 },
                            JobTitle = new JobTitleType { Value = "Principal" },
                            IdentityDocumentReference = new DocumentReferenceType[] { new DocumentReferenceType { ID = new IDType { Value = choLicen1 } } }
                        },
                        new PersonType
                        {
                            ID = new IDType { Value = choNumDi2, schemeID = choTipDi2, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" },
                            FirstName = new FirstNameType { Value = choNombr2 },
                            FamilyName = new FamilyNameType { Value = choApell2 },
                            JobTitle = new JobTitleType { Value = "Secundario" },
                            IdentityDocumentReference = new DocumentReferenceType[] { new DocumentReferenceType { ID = new IDType { Value = choLicen2 } } }
                        }
                        };
                    }
                    else
                    {
                        // caso imposible, no se permite chofer 2 y no existir chofer 1, esto se valida en el form
                    }
                }
                else
                {
                    if (choNumDi1 != null && choNumDi1 != "")
                    {
                        choferes = new PersonType[] {
                    new PersonType
                        {
                            ID = new IDType { Value = choNumDi1, schemeID = choTipDi1, schemeName = "Documento de Identidad", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" },
                            FirstName = new FirstNameType { Value = choNombr1 },
                            FamilyName = new FamilyNameType { Value = choApell1 },
                            JobTitle = new JobTitleType { Value = "Principal" },
                            IdentityDocumentReference = new DocumentReferenceType[] { new DocumentReferenceType { ID = new IDType { Value = choLicen1 } } }
                        }
                    };
                    }
                }
            }
            SpecialInstructionsType indicadorVehiculoMenor = null;
            PartyType[] vehiculos = null;
            if (envPlaca1 == "") //  && envPlaca2 == ""
            {
                indicadorVehiculoMenor = new SpecialInstructionsType { Value = "SUNAT_Envio_IndicadorTrasladoVehiculoM1L" };
            }
            else
            {
                if (codModTra == "01")  // Solo modalidad de traslado PUBLICO=01 lleva datos del transportista, el 02=privado no lleva estos datos 19/07/2023
                {
                    vehiculos = new PartyType[]
                    {
                        new PartyType {
                            PartyIdentification = new PartyIdentificationType[] { new PartyIdentificationType { ID = new IDType { schemeID = scontip, schemeName = sconnoT, schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06", Value = sconnum } } },
                            PartyLegalEntity = new PartyLegalEntityType[] { new PartyLegalEntityType { RegistrationName = new RegistrationNameType { Value = sconnom }, CompanyID = new CompanyIDType { Value = envRegis1 } } },
                            AgentParty = new PartyType { PartyLegalEntity = new PartyLegalEntityType[] { new PartyLegalEntityType { CompanyID = new CompanyIDType { Value = envAutor1, schemeID = envCodEn1, schemeName = "Entidad Autorizadora", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoD37" } } } }
                        }
                    };
                }
            }

            TransportMeansType placa = null;    // en el caso de TRANSPORTE PRIVADO 02, va la placa del vehículo
            if (codModTra == "02") placa = new TransportMeansType { RoadTransport = new RoadTransportType { LicensePlateID = new LicensePlateIDType { Value = envPlaca1 } } };

            TransportEquipmentType transports = new TransportEquipmentType{ };
            if (codModTra == "01")  // "01" = transporte público
            {
                transports = new TransportEquipmentType
                {
                    ID = new IDType { Value = envPlaca1 },                  //-- PLACA - VEHICULO PRINCIPAL
                                                                            //ApplicableTransportMeans = new TransportMeansType { RegistrationNationalityID = new RegistrationNationalityIDType{ Value = envRegis1 } },
                    AttachedTransportEquipment = new TransportEquipmentType[] { new TransportEquipmentType {
                                ID = new IDType { Value = envPlaca2 },                                      //-- PLACA - VEHICULO SECUNDARIO O CARRETA 
                                //ApplicableTransportMeans = new TransportMeansType { RegistrationNationalityID = new RegistrationNationalityIDType { Value = envRegis2 } },
                                ShipmentDocumentReference = new DocumentReferenceType[]         // Tarjeta Unica Circulacion / Cerificado Habilitacion Vehicular - Principal
                                    { new DocumentReferenceType {ID = new IDType { Value = envAutor2, schemeID = envCodEn2, schemeName = envNomEn2, schemeAgencyName="PE:SUNAT", schemeURI="urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoD37" } } } }
                            },
                    ShipmentDocumentReference = new DocumentReferenceType[]
                            {
                                new DocumentReferenceType { ID = new IDType { Value = envAutor1, schemeID = envCodEn1, schemeName = envNomEn1, schemeAgencyName="PE:SUNAT", schemeURI="urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoD37" } }
                            }
                            /*
                    //TransportEquipment = new TransportEquipmentType[]
                    {
                        new TransportEquipmentType { ID = new IDType { Value = envPlaca1},                  //-- PLACA - VEHICULO PRINCIPAL
                            //ApplicableTransportMeans = new TransportMeansType { RegistrationNationalityID = new RegistrationNationalityIDType{ Value = envRegis1 } },
                            AttachedTransportEquipment = new TransportEquipmentType[] { new TransportEquipmentType {
                                ID = new IDType { Value = envPlaca2 },                                      //-- PLACA - VEHICULO SECUNDARIO O CARRETA 
                                //ApplicableTransportMeans = new TransportMeansType { RegistrationNationalityID = new RegistrationNationalityIDType { Value = envRegis2 } },
                                ShipmentDocumentReference = new DocumentReferenceType[]         // Tarjeta Unica Circulacion / Cerificado Habilitacion Vehicular - Principal
                                    { new DocumentReferenceType {ID = new IDType { Value = envAutor2, schemeID = envCodEn2, schemeName = envNomEn2, schemeAgencyName="PE:SUNAT", schemeURI="urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoD37" } } } }
                            },
                            ShipmentDocumentReference = new DocumentReferenceType[]
                            {
                                new DocumentReferenceType { ID = new IDType { Value = envAutor1, schemeID = envCodEn1, schemeName = envNomEn1, schemeAgencyName="PE:SUNAT", schemeURI="urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoD37" } }
                            }
                        }
                    }
                    */
                };
            }
            AddressType ptopartida = null;
            if (motTrasCo != "18")
            {
                ptopartida = new AddressType
                {
                    ID = new IDType { Value = dirLLeubi, schemeName = "Ubigeos", schemeAgencyName = "PE:INEI" },    // listID pide que sea ruc -> dstnumd
                    AddressTypeCode = new AddressTypeCodeType { listAgencyName = "PE:SUNAT", listName = "Establecimientos anexos", listID = dstnumd, Value = "0000" },   // id=ruc destinat, Valor = cod.local anexo
                    StreetName = new StreetNameType { Value = dirLLedir },
                    AddressLine = new AddressLineType[] { new AddressLineType { Line = new LineType { Value = dirLLedir } } }
                    //LocationCoordinate = esto no es obligatorio, recontra opcional
                };
            }
            ShipmentType traslado = new ShipmentType
            {
                ID = new IDType { Value = "SUNAT_Envio" },
                // MOTIVO DEL TRASLADO
                HandlingCode = new HandlingCodeType { Value = motTrasCo },
                HandlingInstructions = new HandlingInstructionsType[] { new HandlingInstructionsType { Value = motTrasDe } },
                GrossWeightMeasure = new GrossWeightMeasureType { Value = pesoTot, unitCode = codunis },                    // Unidad de medida y peso TOTAL de la carga
                TotalTransportHandlingUnitQuantity = new TotalTransportHandlingUnitQuantityType { Value = cantBul },
                SpecialInstructions = new SpecialInstructionsType[]
                {
                    indicadorVehiculoMenor
                },
                ShipmentStage = new ShipmentStageType[]                                                     // Datos de la carga, fecha de inicio y choferes
                {
                    new ShipmentStageType
                    {
                        TransportModeCode = new TransportModeCodeType{ listName = "Modalidad de traslado", listAgencyName = "PE:SUNAT", listURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo18", Value = codModTra },
                        TransitPeriod = new PeriodType {StartDate = new StartDateType { Value = DateTime.Parse(feciniT) } },
                        CarrierParty = vehiculos,
                        TransportMeans = placa,
                        DriverPerson = choferes,
                    }
                },
                //Consignment = contratado,
                Delivery = new DeliveryType                                                             // Direcciones punto de llegada y partida, datos del remitente
                {
                    DeliveryAddress = ptopartida,
                    /*
                    DeliveryAddress = new AddressType
                    {
                        ID = new IDType { Value = dirLLeubi, schemeName = "Ubigeos", schemeAgencyName = "PE:INEI" },    // listID pide que sea ruc -> dstnumd
                        AddressTypeCode = new AddressTypeCodeType { listAgencyName = "PE:SUNAT", listName = "Establecimientos anexos", listID = dstnumd, Value = "0000" },   // id=ruc destinat, Valor = cod.local anexo
                        StreetName = new StreetNameType { Value = dirLLedir },
                        AddressLine = new AddressLineType[] { new AddressLineType { Line = new LineType { Value = dirLLedir } } }
                        //LocationCoordinate = esto no es obligatorio, recontra opcional
                    }, */
                    Despatch = new DespatchType
                    {
                        DespatchAddress = new AddressType
                        {
                            ID = new IDType { Value = dirParubi, schemeName = "Ubigeos", schemeAgencyName = "PE:INEI" },
                            AddressTypeCode = new AddressTypeCodeType { listAgencyName = "PE:SUNAT", listName = "Establecimientos anexos", listID = remnumd, Value = "0000" },   // id=ruc destinat, Valor = cod.local anexo
                            AddressLine = new AddressLineType[] { new AddressLineType { Line = new LineType { Value = dirPardir } } }
                        },
                        DespatchParty = new PartyType
                        {
                            /* ESTA PARTE ES PARA PERMISOS ESPECIALES DEL REMITENTE .... explosivos, insumos drogas, materiales peligrosos, etc.
                            AgentParty = new PartyType { 
                                PartyLegalEntity = new PartyLegalEntityType[] { new PartyLegalEntityType { CompanyID = new CompanyIDType { schemeID = "03", schemeName = "Entidad Autorizadora", schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogoD37", Value =  } } }
                            },
                            PartyIdentification = new PartyIdentificationType[] { new PartyIdentificationType { ID = new IDType { Value = remnumd, schemeID = remdocu, schemeName = remnomt, schemeAgencyName = "PE:SUNAT", schemeURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06" } } },
                            PartyLegalEntity = new PartyLegalEntityType[] { new PartyLegalEntityType { RegistrationName = new RegistrationNameType { Value = remnomb } } }
                            */
                        }
                    }
                },
                /*TransportHandlingUnit = new TransportHandlingUnitType[]
                {
                    new TransportHandlingUnitType { 
                        TransportEquipment = new TransportEquipmentType[]
                        {
                            //new TransportEquipmentType {  ID = new IDType { Value = envPlaca1} }
                            transports
                        } 
                    },
                }, */
            };

            // DETALLE DE LA GUIA DE REMISION ELECTRONICA
            DespatchLineType[] detalle = new DespatchLineType[]
            {
                new DespatchLineType {
                    ID = new IDType { Value = "1"},
                    DeliveredQuantity = new DeliveredQuantityType { Value = decimal.Parse(deta[0]), unitCode = deta[2]},
                    OrderLineReference = new OrderLineReferenceType[] { new OrderLineReferenceType { LineID = new LineIDType {Value = "1" } } },
                    Item = new ItemType {
                        Description = new DescriptionType[] { new DescriptionType { Value = deta[5] + " " + deta[6] } },
                        SellersItemIdentification = new ItemIdentificationType { ID = new IDType { Value = ""} },
                        //StandardItemIdentification = new ItemIdentificationType { ID = new IDType { Value = "", schemeID = ""} },
                        //CommodityClassification = new CommodityClassificationType[] {new CommodityClassificationType { ItemClassificationCode = new ItemClassificationCodeType { Value = "50161509", listID = "UNSPSC", listAgencyName = "GS1 US", listName = "Item Classification" } } },
                        //AdditionalItemProperty = new ItemPropertyType[] {new ItemPropertyType {Value = new ValueType { Value = "3002159000" }, Name = new NameType1 { Value = "SubpartidaNacional"}, NameCode = new NameCodeType { Value = "7020", listAgencyName = "PE:SUNAT", listName = "Propiedad del item", listURI = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo55" } } }
                    }
                },
            };

            // ARMAMOS EL XML
            XmlSerializer serial = new XmlSerializer(typeof(DespatchAdviceType));
            Stream fs = new FileStream(Pruta + rucEmi + "-" + codGuia + "-" + numGuia + ".xml", FileMode.Create, FileAccess.Write);
            var _comprobante = new DespatchAdviceType();
            _comprobante.UBLExtensions = uBLExtensionTypes;     //-- DATOS DE EXTENSION DEL DOCUMENTO
            _comprobante.UBLVersionID = new UBLVersionIDType { Value = "2.1" };
            _comprobante.CustomizationID = new CustomizationIDType { Value = "2.0" };
            _comprobante.ID = new IDType { Value = numGuia };                   // "VG01-1000002"
            _comprobante.IssueDate = new IssueDateType { Value = DateTime.Parse(fecEmis) };  // DateTime.Now.Date         // FECHA DE EMISION
            _comprobante.IssueTime = new IssueTimeType { Value = horEmis };     // output 
            _comprobante.DespatchAdviceTypeCode = codtipo;      //-- CODIGO TIPO DE DOCUMENTO
            _comprobante.Note = nota1;                          //-- TEXTO DEL TIPO DE DOCUMENTO
            _comprobante.AdditionalDocumentReference = refer;   //-- DOCUMENTO RELACIONADO
            _comprobante.Signature = signature;                 //-- FIRMA DEL DOCUMENTO
            _comprobante.DespatchSupplierParty = prove;         //-- DATOS DEL EMISOR (TRANSPORTISTA) --//
            _comprobante.DeliveryCustomerParty = cliente;       //-- DATOS DEL RECEPTOR (DESTINATARIO) --//
            //_comprobante.OriginatorCustomerParty = pagador;     //-- DATOS DE QUIEN PAGA EL SERVICIO --//
            _comprobante.Shipment = traslado;                   //-- DATOS DEL TRASLADO --// 
            _comprobante.DespatchLine = detalle;                //-- DETALLE DE LA GUIA --//

            var xns = new XmlSerializerNamespaces();
            xns.Add("", "urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2");
            xns.Add("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
            xns.Add("ds", "http://www.w3.org/2000/09/xmldsig#");
            xns.Add("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
            xns.Add("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
            xns.Add("xsd", "http://www.w3.org/2001/XMLSchema");

            if (IndFir == "true")           // llama al metodo que firma y graba el xml firmado
            {
                var oStringWriter = new StringWriter();
                serial.Serialize(XmlWriter.Create(oStringWriter), _comprobante, xns);
                string stringXml = oStringWriter.ToString();
                XmlDocument XmlpaFirmar = new XmlDocument();
                XmlpaFirmar.LoadXml(stringXml);
                //FirmarDocumentoXml(XmlpaFirmar, "certificado.pfx", "190969Sorol").Save("XmlFirmado.xml");
                using (Stream stream = fs)
                {
                    using (XmlWriter xmlWriter = new XmlTextWriter(stream, Encoding.GetEncoding("ISO-8859-1")))
                    {
                        FirmarDocumentoXml(XmlpaFirmar, RAcert, Clacert).Save(xmlWriter);  //  "certificado.pfx", "190969Sorol"
                    }
                }
            }
            else
            {
                using (Stream stream = fs)  // graba el xml sin firmar
                {
                    using (XmlWriter xmlWriter = new XmlTextWriter(stream, Encoding.GetEncoding("ISO-8859-1")))
                    {
                        serial.Serialize(xmlWriter, _comprobante, xns);
                    }
                    Console.WriteLine("Exito -> " + fs.ToString());
                    retorna = "Exito";
                }
            }
            return retorna;
        }
        public static XmlDocument FirmarDocumentoXml(XmlDocument XmlparaFirmar, string RutaCertificado, string ClaveCertificado)       // 31/05/2023
        {
            XmlparaFirmar.PreserveWhitespace = true;
            XmlNode ExtensionContent = XmlparaFirmar.GetElementsByTagName("ExtensionContent", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2").Item(0);
            ExtensionContent.RemoveAll();   // quitamos todos los montones de elementos porque solo vamos a firmar
            
            X509Certificate2 x509Certificate2 = new X509Certificate2(File.ReadAllBytes(RutaCertificado), ClaveCertificado, X509KeyStorageFlags.Exportable);
            RSACryptoServiceProvider key = new RSACryptoServiceProvider(new CspParameters(24));
            SignedXml xml = new SignedXml(XmlparaFirmar);
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            KeyInfo keyInfo = new KeyInfo();
            KeyInfoX509Data keyInfoX509Data = new KeyInfoX509Data(x509Certificate2);
            Reference reference = new Reference();

            string exportaLlave = x509Certificate2.PrivateKey.ToXmlString(true);
            key.PersistKeyInCsp = false;
            key.FromXmlString(exportaLlave);
            reference.AddTransform(env);
            reference.Uri = "";                                         // 31/05/2023 despues de error 16:45

            xml.SigningKey = key;

            Signature XMLSignature = xml.Signature;
            XMLSignature.SignedInfo.AddReference(reference);
            keyInfoX509Data.AddSubjectName(x509Certificate2.Subject);

            keyInfo.AddClause(keyInfoX509Data);
            XMLSignature.KeyInfo = keyInfo;
            XMLSignature.Id = "SignatureKG";
            xml.ComputeSignature();

            ExtensionContent.AppendChild(xml.GetXml());

            return XmlparaFirmar;
        }
    }
    public class GRE_T
    {
        public string EmisRuc { get; set; }         // Cabecera - Emisor de la guía - ruc
        public string EmisNom { get; set; }         // Cabecera - Emisor de la guía - nombre
        public string EmisUbi { get; set; }         // Cabecera - Emisor de la guía - ubigeo
        public string EmisDir { get; set; }         // Cabecera - Emisor de la guía - dirección completa
        public string EmisUrb { get; set; }         // Cabecera - Emisor de la guía - ciudad, urbanización, zona, barrio, etc
        public string EmisDep { get; set; }         // Cabecera - Emisor de la guía - departamento
        public string EmisPro { get; set; }         // Cabecera - Emisor de la guía - provincia
        public string EmisDis { get; set; }         // Cabecera - Emisor de la guía - distrito
        public string EmisPai { get; set; }         // Cabecera - Emisor de la guía - país
        public string EmisCor { get; set; }         // Cabecera - Emisor de la guía - correo de contacto
        public string NumGuia { get; set; }         // Cabecera - número de la guía de remisión electrónica completa con un guíon de separación
        public string FecEmis { get; set; }         // Cabecera - fecha de emisión de la guía, formato yyyy-mm-dd
        public string HorEmis { get; set; }         // Cabecera - hora de emisión de la guía, formato hh:mm:ss
        public string CodGuia { get; set; }         // Cabecera - código sunat del tipo de guía electrónica
        public string NomGuia { get; set; }         // Cabecera - texto nombre del tipo de guía electrónica
        public int CantBul { get; set; }            // Cabecera - cantidad total
        public decimal PesoTot { get; set; }        // Cabecera - peso total
        public string CodUnid { get; set; }         // Cabecera - código unidad de medida sunat
        public string FecIniT { get; set; }         // Cabecera - fecha de inicio del traslado
        public bool CargaUn { get; set; }           // Cabecera - carga única si=true, no=false
        public string DocRelnu1 { get; set; }        // Documento relacionado 1 - numero
        public string DocRelti1 { get; set; }        // Documento relacionado 1 - tipo del documento numero remitente
        public string DocRelnr1 { get; set; }        // Documento relacionado 1 - numero de identificación del emisor
        public string DocRelcs1 { get; set; }        // Documento relacionado 1 - codigo sunat del numero de identificación del emisor
        public string DocRelnm1 { get; set; }        // Documento relacionado 1 - nombre del documento
        public string DocRelnu2 { get; set; }        // Documento relacionado 2 - numero
        public string DocRelti2 { get; set; }        // Documento relacionado 2 - tipo del documento numero remitente
        public string DocRelnr2 { get; set; }        // Documento relacionado 2 - numero de identificación del emisor
        public string DocRelcs2 { get; set; }        // Documento relacionado 2 - codigo sunat del numero de identificación del emisor
        public string DocRelnm2 { get; set; }        // Documento relacionado 2 - nombre del documento
        public string DstTipdoc { get; set; }        // Destinatario de la GR - tipo de documento
        public string DstNomTdo { get; set; }        // Destinatario de la GR - nombre del tipo de documento
        public string DstNumdoc { get; set; }        // Destinatario de la GR - número de documento
        public string DstNombre { get; set; }        // Destinatario de la GR - nombre o razón social
        public string DstDirecc { get; set; }        // Destinatario de la GR - dirección completa
        public string DstUbigeo { get; set; }        // Destinatario de la GR - UBIGEO
        public string RemTipdoc { get; set; }        // Remitente de la GR - tipo de documento
        public string RemNomTdo { get; set; }        // Remitente de la GR - nombre del tipo de documento
        public string RemNumdoc { get; set; }        // Remitente de la GR - número de documento
        public string RemNombre { get; set; }        // Remitente de la GR - nombre o razón social
        public string RemDirecc { get; set; }        // Remitente de la GR - dirección completa
        public string RemUbigeo { get; set; }        // Remitente de la GR - UBIGEO
        public string PagTipdoc { get; set; }        // Pagador del servicio - tipo de documento 
        public string PagNomTip { get; set; }        // Pagador del servicio - nombre del tipo de documento
        public string PagNumdoc { get; set; }        // Pagador del servicio - número del documento
        public string PagNombre { get; set; }        // Pagador del servicio - nombre del pagador
        public string SConTipdo { get; set; }        // Subcontratado - tipo de documento
        public string SConNomTi { get; set; }        // Subcontratado - nombre del tipo de documento
        public string SConNumdo { get; set; }        // Subcontratado - número de documento
        public string SconNombr { get; set; }        // Subcontratado - nombre o razón social
        public string EnvPlaca1 { get; set; }        // Envíos - Placa del vehículo 1   ................. En el caso de guías transportista 1 = Trackto o Camion
        public string EnvAutor1 { get; set; }        // Envíos - Autorización de circulación
        public string EnvRegis1 { get; set; }        // Envíos - Número de registro
        public string EnvCodEn1 { get; set; }        // Envíos - Código de entidad que registra
        public string EnvNomEn1 { get; set; }        // Envíos - Nombre de la entidad que registra
        public string EnvPlaca2 { get; set; }        // Envíos - Placa del vehículo 2   ................. En el caso de guías transportista 2 = Carreta o Ranfla
        public string EnvAutor2 { get; set; }        // Envíos - Autorización de circulación
        public string EnvRegis2 { get; set; }        // Envíos - Número de registro
        public string EnvCodEn2 { get; set; }        // Envíos - Código de entidad que registra
        public string EnvNomEn2 { get; set; }        // Envíos - Nombre de la entidad que registra
        public string ChoTipDi1 { get; set; }        // Choferes - tipo de documento de identidad chofer 1
        public string ChoNumDi1 { get; set; }        // Choferes - número documento identidad chofer 1
        public string ChoNomTi1 { get; set; }        // Choferes - nombre del documento de identidad
        public string ChoNombr1 { get; set; }        // Choferes - nombres del chofer 1
        public string ChoApell1 { get; set; }        // Choferes - apeliidos del chofer 1
        public string ChoLicen1 { get; set; }        // Choferes - número de licencia 1
        public string ChoTipDi2 { get; set; }        // Choferes - tipo de documento de identidad chofer 2
        public string ChoNumDi2 { get; set; }        // Choferes - número documento identidad chofer 2
        public string ChoNomTi2 { get; set; }        // Choferes - nombre del documento de identidad
        public string ChoNombr2 { get; set; }        // Choferes - nombres del chofer 2
        public string ChoApell2 { get; set; }        // Choferes - apeliidos del chofer 2
        public string ChoLicen2 { get; set; }        // Choferes - número de licencia 2
        public string DirParUbi { get; set; }        // Direcciones Reparto/Entrega - Ubigeo punto de partida 
        public string DirParDir { get; set; }        // Direcciones Reparto/Entrega - Dirección completa
        public string DirLLeUbi { get; set; }        // Direcciones Reparto/Entrega - Ubigeo punto de llegada
        public string DirLLeDir { get; set; }        // Direcciones Reparto/Entrega - Dirección completa del punto de llegada
        public string MotTrasCo { get; set; }        // Motivo de traslado - codigo sunat, 
        public string MotTrasDe { get; set; }        // Motivo de traslado - descripción motivo del traslado
        public string CodModTra { get; set; }        // Código sunat modalidad de transporte
        public string observ { get; set; }           // observaciones de la guía
        // 
        public string[] Detalle { get; set; }        // Detalle de la guía

        public void asigna_datos(string rucEmi, string nomEmi, string ubiEmi, string dirEmi, string depEmi, string proEmi, string disEmi, string urbEmi, string paiEmi, string corEmi,
            string guia, string fecha, string hora, string codigo, string nombre, int cantBul, decimal pesoTot, string codunis, string feciniT, bool cargaun,
            string gremit1, string tipogrem1, string emigrem1, string codgrem1, string nomgrem1 , string gremit2, string tipogrem2, string emigrem2, string codgrem2, string nomgrem2,
            string dstdocu, string dstnomt, string dstnumd, string dstnomb, string dstdirec, string dstubig,
            string remdocu, string remnomt, string remnumd, string remnomb, string remdirec, string remubig,
            string pagdocu, string pagnomt, string pagnume, string pagnomb, string scontip, string sconnoT, string sconnum, string sconnom,
            string envPlaca1, string envAutor1, string envRegis1, string envCodEn1, string envNomEn1, string envPlaca2, string envAutor2, string envRegis2, string envCodEn2, string envNomEn2,
            string choTipDi1, string choNumDi1, string choNomTi1, string choNombr1, string choApell1, string choLicen1,
            string choTipDi2, string choNumDi2, string choNomTi2, string choNombr2, string choApell2, string choLicen2,
            string dirParubi, string dirPardir, string dirLLeubi, string dirLLedir,
            string detalle)      // CONSTRUCTOR
        {
            // cabecera
            EmisRuc = rucEmi;
            EmisNom = nomEmi;
            EmisUbi = ubiEmi;
            EmisDir = dirEmi;
            EmisDep = depEmi;
            EmisPro = proEmi;
            EmisDis = disEmi;
            EmisUrb = urbEmi;
            EmisPai = paiEmi;
            EmisCor = corEmi;
            NumGuia = guia;
            FecEmis = fecha;
            HorEmis = hora;
            CodGuia = codigo;
            NomGuia = nombre;
            CantBul = cantBul;
            PesoTot = pesoTot;
            CodUnid = codunis;
            FecIniT = feciniT;
            CargaUn = cargaun;
            // documentos relacionados
            DocRelnu1 = gremit1;
            DocRelti1 = tipogrem1;
            DocRelnr1 = emigrem1;
            DocRelcs1 = codgrem1;
            DocRelnm1 = nomgrem1;
            DocRelnu2 = gremit2;
            DocRelti2 = tipogrem2;
            DocRelnr2 = emigrem2;
            DocRelcs2 = codgrem2;
            DocRelnm2 = nomgrem2;
            // datos del destinatario
            DstTipdoc = dstdocu;
            DstNomTdo = dstnomt;
            DstNumdoc = dstnumd;
            DstNombre = dstnomb;
            DstDirecc = dstdirec;
            DstUbigeo = dstubig;
            // datos del remitente
            RemTipdoc = remdocu;
            RemNomTdo = remnomt;
            RemNumdoc = remnumd;
            RemNombre = remnomb;
            RemDirecc = remdirec;
            RemUbigeo = remubig;
            // datos de quien paga el servicio
            PagTipdoc = pagdocu;
            PagNomTip = pagnomt;
            PagNumdoc = pagnume;
            PagNombre = pagnomb;
            // datos de transportista subcontratado (si lo hubiera) 
            SConTipdo = scontip;
            SConNomTi = sconnoT;
            SConNumdo = sconnum;
            SconNombr = sconnom;
            // datos del envío del (los) camiones, autorizaciones de trackto y carreta
            EnvPlaca1 = envPlaca1;
            EnvAutor1 = envAutor1;
            EnvRegis1 = envRegis1;
            EnvCodEn1 = envCodEn1;
            EnvNomEn1 = envNomEn1;
            EnvPlaca2 = envPlaca2;
            EnvAutor2 = envAutor2;
            EnvRegis2 = envRegis2;
            EnvCodEn2 = envCodEn2;
            EnvNomEn2 = envNomEn2;
            // datos de los choferes
            ChoTipDi1 = "";
            ChoNumDi1 = "";
            ChoNomTi1 = "";
            ChoNombr1 = "";
            ChoApell1 = "";
            ChoLicen1 = "";
            ChoTipDi2 = "";
            ChoNumDi2 = "";
            ChoNomTi2 = "";
            ChoNombr2 = "";
            ChoApell2 = "";
            ChoLicen2 = "";
            // datos de direcciones partida y llegada
            DirParUbi = "";
            DirParDir = "";
            DirLLeUbi = "";
            DirLLeDir = "";
            // Detalle de la guía
            Detalle[0] = "";
            Detalle[1] = "";
            Detalle[2] = "";
            Detalle[3] = "";
            Detalle[4] = "";
        }
    }
    
}