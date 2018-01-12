﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class TrnCollectionController
    {
        // ============
        // Data Context
        // ============
        private static Data.POSDatabaseDataContext posData;

        // ==============
        // Get Collection
        // ==============
        public void GetCollection(String database, String apiUrlHost, String branchCode, String userCode)
        {
            try
            {
                String jsonPath = "d:/innosoft/json/SI";

                var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                posData = new Data.POSDatabaseDataContext(newConnectionString);

                var collections = from d in posData.TrnCollections
                                  where d.PostCode == null
                                  && d.CollectionNumber != "NA"
                                  select d;

                if (collections.Any())
                {
                    foreach (var collection in collections)
                    {
                        List<TrnCollectionLines> listCollectionLines = new List<TrnCollectionLines>();

                        if (collection.TrnSale != null)
                        {
                            foreach (var salesLine in collection.TrnSale.TrnSalesLines)
                            {
                                listCollectionLines.Add(new TrnCollectionLines()
                                {
                                    ItemManualArticleCode = salesLine.MstItem.BarCode,
                                    Particulars = salesLine.MstItem.ItemDescription,
                                    Unit = salesLine.MstUnit.Unit,
                                    Quantity = salesLine.Quantity,
                                    Price = salesLine.Price,
                                    Discount = salesLine.MstDiscount.Discount,
                                    DiscountAmount = salesLine.DiscountAmount,
                                    NetPrice = salesLine.NetPrice,
                                    Amount = salesLine.Amount,
                                    VAT = salesLine.MstTax.Tax,
                                    SalesItemTimeStamp = salesLine.SalesLineTimeStamp.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)
                                });
                            }

                            var collectionData = new POSIntegrator.TrnCollection()
                            {
                                SIDate = collection.CollectionDate.ToShortDateString(),
                                BranchCode = branchCode,
                                CustomerManualArticleCode = collection.TrnSale.MstCustomer.CustomerCode,
                                CreatedBy = userCode,
                                Term = collection.TrnSale.MstTerm.Term,
                                DocumentReference = collection.CollectionNumber,
                                ManualSINumber = collection.TrnSale.SalesNumber,
                                Remarks = collection.MstUser4.UserName,
                                ListPOSIntegrationTrnSalesInvoiceItem = listCollectionLines.ToList()
                            };

                            String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(collectionData);
                            String jsonFileName = jsonPath + "\\" + collection.CollectionNumber + ".json";

                            if (!File.Exists(jsonFileName))
                            {
                                File.WriteAllText(jsonFileName, json);
                            }
                        }
                    }
                }

                SendSIJsonFiles(database, apiUrlHost);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // ==================
        // Send SI Json Files
        // ==================
        public void SendSIJsonFiles(String database, String apiUrlHost)
        {
            try
            {
                String jsonPath = "d:/innosoft/json/SI";
                List<String> files = new List<String>(Directory.EnumerateFiles(jsonPath));

                if (files.Any())
                {
                    var file = files.FirstOrDefault();

                    // ==============
                    // Read json file
                    // ==============
                    String json;
                    using (StreamReader r = new StreamReader(file))
                    {
                        json = r.ReadToEnd();
                    }

                    // ===================
                    // Send json to server
                    // ===================
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/add/POSIntegration/salesInvoice");
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";

                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        var json_serializer = new JavaScriptSerializer();
                        POSIntegrator.TrnCollection c = json_serializer.Deserialize<POSIntegrator.TrnCollection>(json);

                        Console.WriteLine("Sending Collection Number " + c.DocumentReference + "...");
                        streamWriter.Write(new JavaScriptSerializer().Serialize(c));
                    }

                    // ================
                    // Process response
                    // ================
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        if (result != null)
                        {
                            var json_serializer = new JavaScriptSerializer();
                            POSIntegrator.TrnCollection c = json_serializer.Deserialize<POSIntegrator.TrnCollection>(json);

                            Console.WriteLine("Collection Number " + c.DocumentReference + " was successfully sent!");
                            Console.WriteLine("Post Code: " + result.Replace("\"", ""));
                            Console.WriteLine();

                            var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                            posData = new Data.POSDatabaseDataContext(newConnectionString);

                            var collections = from d in posData.TrnCollections
                                              where d.CollectionNumber.Equals(c.DocumentReference)
                                              select d;

                            if (collections.Any())
                            {
                                var collection = collections.FirstOrDefault();
                                collection.PostCode = result.Replace("\"", "");
                                posData.SubmitChanges();

                                File.Delete(file);
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
    }
}