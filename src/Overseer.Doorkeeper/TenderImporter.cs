using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using log4net;
using Overseer.Common;

namespace Overseer.Doorkeeper
{
    public class TenderImporter
    {
        private readonly ILog log = LogManager.GetLogger(typeof (TenderImporter));
        private readonly IFileReader reader;
        private readonly ITenderRepository repo;
        private int countImported;
        private readonly Dictionary<string, string> folderNameToRegionId = new Dictionary<string, string>
                                                                           {
                                                                               {"Adygeja_Resp", "01"},
                                                                               {"Altajskij_kraj", "22"},
                                                                               {"Altaj_Resp", "04"},
                                                                               {"Amurskaja_obl", "28"},
                                                                               {"Arkhangelskaja_obl", "29"},
                                                                               {"Astrakhanskaja_obl", "30"},
                                                                               {"Bajkonur_g", "99"},
                                                                               {"Bashkortostan_Resp", "02"},
                                                                               {"Belgorodskaja_obl", "31"},
                                                                               {"Brjanskaja_obl", "32"},
                                                                               {"Burjatija_Resp", "03"},
                                                                               {"Chechenskaja_Resp", "20"},
                                                                               {"Cheljabinskaja_obl", "74"},
                                                                               {"Chukotskij_AO", "87"},
                                                                               {"Chuvashskaja_Respublika_-_Chuvashija", "21"},
                                                                               {"Dagestan_Resp", "05"},
                                                                               {"Evrejskaja_Aobl", "79"},
                                                                               {"Ingushetija_Resp", "06"},
                                                                               {"Irkutskaja_obl", "38"},
                                                                               {
                                                                                   "Irkutskaja_obl_Ust-Ordynskij_Burjatskij_okrug",
                                                                                   "38"
                                                                               },
                                                                               {"Ivanovskaja_obl", "37"},
                                                                               {"Jamalo-Neneckij_AO", "89"},
                                                                               {"Jaroslavskaja_obl", "76"},
                                                                               {"Kabardino-Balkarskaja_Resp", "07"},
                                                                               {"Kaliningradskaja_obl", "39"},
                                                                               {"Kalmykija_Resp", "08"},
                                                                               {"Kaluzhskaja_obl", "40"},
                                                                               {"Kamchatskij_kraj", "41"},
                                                                               {"Karachaevo-Cherkesskaja_Resp", "09"},
                                                                               {"Karelija_Resp", "10"},
                                                                               {"Kemerovskaja_obl", "42"},
                                                                               {"Khabarovskij_kraj", "27"},
                                                                               {"Khakasija_Resp", "19"},
                                                                               {
                                                                                   "Khanty-Mansijskij_Avtonomnyj_okrug_-_Jugra_AO",
                                                                                   "86"
                                                                               },
                                                                               {"Kirovskaja_obl", "43"},
                                                                               {"Komi_Resp", "11"},
                                                                               {"Kostromskaja_obl", "44"},
                                                                               {"Krasnodarskij_kraj", "23"},
                                                                               {"Krasnojarskij_kraj", "24"},
                                                                               {"Kurganskaja_obl", "45"},
                                                                               {"Kurskaja_obl", "46"},
                                                                               {"Leningradskaja_obl", "47"},
                                                                               {"Lipeckaja_obl", "48"},
                                                                               {"Magadanskaja_obl", "49"},
                                                                               {"Marij_El_Resp", "12"},
                                                                               {"Mordovija_Resp", "13"},
                                                                               {"Moskovskaja_obl", "50"},
                                                                               {"Moskva", "77"},
                                                                               {"Murmanskaja_obl", "51"},
                                                                               {"Neneckij_AO", "83"},
                                                                               {"Nizhegorodskaja_obl", "52"},
                                                                               {"Novgorodskaja_obl", "53"},
                                                                               {"Novosibirskaja_obl", "54"},
                                                                               {"Omskaja_obl", "55"},
                                                                               {"Orenburgskaja_obl", "56"},
                                                                               {"Orlovskaja_obl", "57"},
                                                                               {"Penzenskaja_obl", "58"},
                                                                               {"Permskij_kraj", "59"},
                                                                               {"Primorskij_kraj", "25"},
                                                                               {"Pskovskaja_obl", "60"},
                                                                               {"Rjazanskaja_obl", "62"},
                                                                               {"Rostovskaja_obl", "61"},
                                                                               {"Sakhalinskaja_obl", "65"},
                                                                               {"Sakha_Jakutija_Resp", "14"},
                                                                               {"Samarskaja_obl", "63"},
                                                                               {"Sankt-Peterburg", "78"},
                                                                               {"Saratovskaja_obl", "64"},
                                                                               {"Severnaja_Osetija_-_Alanija_Resp", "15"},
                                                                               {"Smolenskaja_obl", "67"},
                                                                               {"Stavropolskij_kraj", "26"},
                                                                               {"Sverdlovskaja_obl", "66"},
                                                                               {"Tambovskaja_obl", "68"},
                                                                               {"Tatarstan_Resp", "16"},
                                                                               {"Tjumenskaja_obl", "72"},
                                                                               {"Tomskaja_obl", "70"},
                                                                               {"Tulskaja_obl", "71"},
                                                                               {"Tverskaja_obl", "69"},
                                                                               {"Tyva_Resp", "17"},
                                                                               {"Udmurtskaja_Resp", "18"},
                                                                               {"Uljanovskaja_obl", "73"},
                                                                               {"Vladimirskaja_obl", "33"},
                                                                               {"Volgogradskaja_obl", "34"},
                                                                               {"Vologodskaja_obl", "35"},
                                                                               {"Voronezhskaja_obl", "36"},
                                                                               {"Zabajkalskij_kraj", "75"},
                                                                               {"Zabajkalskij_kraj_Aginskij_Burjatskij_okrug", "75"}
                                                                           };

        public TenderImporter(IFileReader reader, ITenderRepository repo)
        {
            this.reader = reader;
            this.repo = repo;
        }

        public void Import()
        {
            var stopwatch = Stopwatch.StartNew();
            foreach (var file in reader.ReadNewFiles())
            {
                log.InfoFormat("importing file {0}", file.Uri);
                var result = new Tender();
                XDocument xDoc = null;
                try
                {
                    xDoc = XDocument.Parse(file.Content);
                }
                catch (XmlException)
                {
                }
                if (xDoc == null)
                    continue;
                var tenderIdElement = xDoc.Descendants().FirstOrDefault(el => el.Name.LocalName == "purchaseNumber");
                if (tenderIdElement == null)
                    continue;

                var regionName = new Uri(file.Uri).Segments[2].TrimEnd('/');
                if (!folderNameToRegionId.ContainsKey(regionName))
                {
                    log.WarnFormat("unkown region {0}", regionName);
                    continue;
                }
                result.Region = folderNameToRegionId[regionName];

                var name = xDoc.Descendants().FirstOrDefault(el => el.Name.LocalName == "purchaseObjectInfo");
                if (name != null)
                    result.Name = name.Value;

                var differentPrices = xDoc.Descendants().Where(el => el.Name.LocalName == "maxPrice").GroupBy(el => el.Value).ToList();
                if (differentPrices.Count() > 1)
                {
                    log.WarnFormat("multiple price elements with different price found in {0}", file.Uri);
                    continue;
                }
                if (differentPrices.Count() == 1)
                    result.TotalPrice = decimal.Parse(differentPrices.First().Key);

                var firstOrDefault = xDoc.Descendants().FirstOrDefault(el => el.Name.LocalName == "docPublishDate");
                if (firstOrDefault != null)
                {
                    DateTimeOffset publishDate;
                    if (DateTimeOffset.TryParse(firstOrDefault.Value, out publishDate))
                        result.PublishDate = publishDate.UtcDateTime;
                }

                result.Id = tenderIdElement.Value;
                result.Type = xDoc.Root.Name.LocalName;
                result.Source = file.Uri;
                repo.Save(result);
                reader.MarkImported(file.Uri);
                countImported++;

                log.InfoFormat("imported {0}", file.Uri);
            }
            log.InfoFormat("{0} tenders imported", countImported);
            stopwatch.Stop();
            log.InfoFormat("{0:hh\\:mm\\:ss} elapsed", stopwatch.Elapsed);
        }
    }
}