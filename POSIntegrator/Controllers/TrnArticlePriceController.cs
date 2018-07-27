﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class TrnArticlePriceController
    {
        // ============
        // Data Context
        // ============
        private static Data.POSDatabaseDataContext posData;

        // ==============
        // GET Item Price
        // ==============
        public void GetItemPrice(String database, String apiUrlHost, String branchCode)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/itemPrice/" + branchCode + "/" + currentDate);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<POSIntegrator.TrnArticlePrice> itemPriceLists = (List<POSIntegrator.TrnArticlePrice>)js.Deserialize(result, typeof(List<POSIntegrator.TrnArticlePrice>));

                    foreach (var itemPriceList in itemPriceLists)
                    {
                        var itemPriceData = new POSIntegrator.TrnArticlePrice()
                        {
                            BranchCode = itemPriceList.BranchCode,
                            IPNumber = itemPriceList.IPNumber,
                            IPDate = itemPriceList.IPDate,
                            ItemCode = itemPriceList.ItemCode,
                            ItemDescription = itemPriceList.ItemDescription,
                            Price = itemPriceList.Price,
                            TriggerQuantity = itemPriceList.TriggerQuantity
                        };

                        String jsonPath = "d:/innosoft/json/IP";
                        String fileName = "IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.ItemCode + ")";

                        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                        {
                            fileName = fileName.Replace(c, '_');
                        }

                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(itemPriceData);
                        String jsonFileName = jsonPath + "\\" + fileName + ".json";

                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        posData = new Data.POSDatabaseDataContext(newConnectionString);

                        var item = from d in posData.MstItems
                                   where d.BarCode.Equals(itemPriceList.ItemCode)
                                   select d;

                        if (item.Any())
                        {
                            var itemPrices = from d in posData.MstItemPrices
                                             where d.ItemId == item.FirstOrDefault().Id
                                             && d.PriceDescription.Equals("IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.IPDate + ")")
                                             && d.Price != itemPriceList.Price
                                             && d.TriggerQuantity != itemPriceList.TriggerQuantity
                                             select d;

                            if (itemPrices.Any())
                            {
                                File.WriteAllText(jsonFileName, json);
                                Console.WriteLine("Updating Item Price...");

                                UpdateItemPrice(database);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // =================
        // UPDATE Item Price
        // =================
        public void UpdateItemPrice(String database)
        {
            try
            {
                String jsonPath = "d:/innosoft/json/IP";
                List<String> files = new List<String>(Directory.EnumerateFiles(jsonPath));

                foreach (var file in files)
                {
                    // ==============
                    // Read json file
                    // ==============
                    String json;
                    using (StreamReader r = new StreamReader(file))
                    {
                        json = r.ReadToEnd();
                    }

                    var json_serializer = new JavaScriptSerializer();
                    POSIntegrator.TrnArticlePrice itemPriceList = json_serializer.Deserialize<POSIntegrator.TrnArticlePrice>(json);

                    var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                    posData = new Data.POSDatabaseDataContext(newConnectionString);

                    var item = from d in posData.MstItems
                               where d.BarCode.Equals(itemPriceList.ItemCode)
                               select d;

                    if (item.Any())
                    {
                        var updateItem = item.FirstOrDefault();
                        updateItem.Price = itemPriceList.Price;
                        posData.SubmitChanges();

                        var itemPrices = from d in posData.MstItemPrices
                                         where d.ItemId == item.FirstOrDefault().Id
                                         && d.PriceDescription.Equals("IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.IPDate + ")")
                                         && d.Price != itemPriceList.Price
                                         && d.TriggerQuantity != itemPriceList.TriggerQuantity
                                         select d;

                        if (itemPrices.Any())
                        {
                            var updateItemPrice = itemPrices.FirstOrDefault();
                            updateItemPrice.Price = itemPriceList.Price;
                            updateItemPrice.TriggerQuantity = itemPriceList.TriggerQuantity;
                            posData.SubmitChanges();

                            item.FirstOrDefault().Price = itemPriceList.Price;
                            posData.SubmitChanges();

                            Console.WriteLine("Barcode: " + itemPriceList.ItemCode);
                            Console.WriteLine("Item: " + itemPriceList.ItemDescription);
                            Console.WriteLine("Price Description: IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.IPDate + ")");
                            Console.WriteLine("Update Successful!");
                            Console.WriteLine();

                            File.Delete(file);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
