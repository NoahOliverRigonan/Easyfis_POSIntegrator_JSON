﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApiContrib.Formatting;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using System.Diagnostics;
using System.Globalization;

namespace POSIntegrator
{
    // ==========
    // Collection 
    // ==========
    public class Collection
    {
        public string SIDate { get; set; }
        public string BranchCode { get; set; }
        public string CustomerManualArticleCode { get; set; }
        public string CreatedBy { get; set; }
        public string Term { get; set; }
        public string DocumentReference { get; set; }
        public string ManualSINumber { get; set; }
        public string Remarks { get; set; }
        public List<CollectionLines> listPOSIntegrationTrnSalesInvoiceItem { get; set; }
    }

    // ================
    // Collection Lines
    // ================
    public class CollectionLines
    {
        public string ItemManualArticleCode { get; set; }
        public string Particulars { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public string Discount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetPrice { get; set; }
        public decimal Amount { get; set; }
        public string VAT { get; set; }
    }

    // ==============
    // Stock Transfer
    // ==============
    public class TrnStockTransfer
    {
        public String BranchCode { get; set; }
        public String Branch { get; set; }
        public String STNumber { get; set; }
        public String STDate { get; set; }
        public String ToBranch { get; set; }
        public String ToBranchCode { get; set; }
        public String Article { get; set; }
        public String Particulars { get; set; }
        public String ManualSTNumber { get; set; }
        public String PreparedBy { get; set; }
        public String CheckedBy { get; set; }
        public String ApprovedBy { get; set; }
        public Boolean IsLocked { get; set; }
        public String CreatedBy { get; set; }
        public String CreatedDateTime { get; set; }
        public String UpdatedBy { get; set; }
        public String UpdatedDateTime { get; set; }
        public List<TrnStockTransferItems> listPOSIntegrationTrnStockTransferItem { get; set; }
    }

    // ====================
    // Stock Transfer Items
    // ====================
    public class TrnStockTransferItems
    {
        public Int32 STId { get; set; }
        public String ItemCode { get; set; }
        public String Item { get; set; }
        public String InventoryCode { get; set; }
        public String Particulars { get; set; }
        public String Unit { get; set; }
        public Decimal Quantity { get; set; }
        public Decimal Cost { get; set; }
        public Decimal Amount { get; set; }
        public String BaseUnit { get; set; }
        public Decimal BaseQuantity { get; set; }
        public Decimal BaseCost { get; set; }
    }

    // =======
    // Program
    // =======
    class Program
    {
        // =============
        // Data Contexts
        // =============
        private static POSdb1.POSdb1DataContext posData1 = new POSdb1.POSdb1DataContext();
        private static POSdb2.POSdb2DataContext posData2 = new POSdb2.POSdb2DataContext();
        private static POSdb3.POSdb3DataContext posData3 = new POSdb3.POSdb3DataContext();

        // ===============
        // Send Json Files
        // ===============
        public static void sendJsonFiles(string jsonPath, string apiUrl, string database)
        {
            try
            {
                List<string> files = new List<string>(Directory.EnumerateFiles(jsonPath));
                foreach (var file in files)
                {
                    // ==============
                    // Read json file
                    // ==============
                    string json;
                    using (StreamReader r = new StreamReader(file))
                    {
                        json = r.ReadToEnd();
                    }

                    // ===================
                    // Send json to server
                    // ===================
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiUrl);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        var json_serializer = new JavaScriptSerializer();
                        Collection c = json_serializer.Deserialize<Collection>(json);
                        streamWriter.Write(new JavaScriptSerializer().Serialize(c));
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    // ================
                    // Process response
                    // ================
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        Console.WriteLine(result);

                        var json_serializer = new JavaScriptSerializer();
                        Collection c = json_serializer.Deserialize<Collection>(json);

                        if (database.Equals("1"))
                        {
                            var collections = from d in posData1.TrnCollections
                                              where d.CollectionNumber == c.DocumentReference
                                              select d;

                            if (collections.Any())
                            {
                                var collection = collections.FirstOrDefault();
                                collection.PostCode = result.Replace("\"", "");
                                posData1.SubmitChanges();
                                File.Delete(file);
                            }
                        }
                        else
                        {
                            if (database.Equals("2"))
                            {
                                var collections = from d in posData2.TrnCollections
                                                  where d.CollectionNumber == c.DocumentReference
                                                  select d;

                                if (collections.Any())
                                {
                                    var collection = collections.FirstOrDefault();
                                    collection.PostCode = result.Replace("\"", "");
                                    posData2.SubmitChanges();
                                    File.Delete(file);
                                }
                            }
                            else
                            {
                                if (database.Equals("3"))
                                {
                                    var collections = from d in posData3.TrnCollections
                                                      where d.CollectionNumber == c.DocumentReference
                                                      select d;

                                    if (collections.Any())
                                    {
                                        var collection = collections.FirstOrDefault();
                                        collection.PostCode = result.Replace("\"", "");
                                        posData3.SubmitChanges();
                                        File.Delete(file);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Database not found!");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // ==============
        // Fetch Stock In
        // ==============
        public static void fetchStockInItems(string stockTransferDate, string toBranchCode)
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.easyfis.com/api/get/POSIntegration/stockTransferItems/IN/" + stockTransferDate + "/" + toBranchCode);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<TrnStockTransfer> stockTransferLists = (List<TrnStockTransfer>)js.Deserialize(result, typeof(List<TrnStockTransfer>));

                    foreach (var stockTransferList in stockTransferLists)
                    {
                        List<TrnStockTransferItems> listStockTransferItems = new List<TrnStockTransferItems>();
                        foreach (var stockTransferListItem in stockTransferList.listPOSIntegrationTrnStockTransferItem)
                        {
                            listStockTransferItems.Add(new TrnStockTransferItems()
                            {
                                STId = stockTransferListItem.STId,
                                ItemCode = stockTransferListItem.ItemCode,
                                Item = stockTransferListItem.Item,
                                InventoryCode = stockTransferListItem.InventoryCode,
                                Particulars = stockTransferListItem.Particulars,
                                Unit = stockTransferListItem.Unit,
                                Quantity = stockTransferListItem.Quantity,
                                Cost = stockTransferListItem.Cost,
                                Amount = stockTransferListItem.Amount,
                                BaseUnit = stockTransferListItem.BaseUnit,
                                BaseQuantity = stockTransferListItem.BaseQuantity,
                                BaseCost = stockTransferListItem.BaseCost
                            });
                        }

                        var stockTransferData = new TrnStockTransfer()
                        {
                            BranchCode = stockTransferList.BranchCode,
                            Branch = stockTransferList.Branch,
                            STNumber = stockTransferList.STNumber,
                            STDate = stockTransferList.STDate,
                            ToBranch = stockTransferList.ToBranch,
                            ToBranchCode = stockTransferList.ToBranchCode,
                            Article = stockTransferList.Article,
                            Particulars = stockTransferList.Particulars,
                            ManualSTNumber = stockTransferList.ManualSTNumber,
                            PreparedBy = stockTransferList.PreparedBy,
                            CheckedBy = stockTransferList.CheckedBy,
                            ApprovedBy = stockTransferList.ApprovedBy,
                            IsLocked = stockTransferList.IsLocked,
                            CreatedBy = stockTransferList.CreatedBy,
                            CreatedDateTime = stockTransferList.CreatedDateTime,
                            UpdatedBy = stockTransferList.UpdatedBy,
                            UpdatedDateTime = stockTransferList.UpdatedDateTime,
                            listPOSIntegrationTrnStockTransferItem = stockTransferList.listPOSIntegrationTrnStockTransferItem.ToList()
                        };

                        string jsonPath = "d:/innosoft/json/IN";
                        string fileName = stockTransferList.BranchCode + "-" + stockTransferList.STNumber;

                        string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(stockTransferData);
                        string jsonFileName = jsonPath + "\\" + fileName + ".json";
                        File.WriteAllText(jsonFileName, json);

                        Console.WriteLine("Saving " + stockTransferList.STNumber + "...");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // ===========
        // Main Method
        // ===========
        static void Main(string[] args)
        {
            int i = 0;
            string jsonPath = "", apiUrl = "", database = "";
            foreach (var arg in args)
            {
                if (i == 0) { jsonPath = arg; }
                else if (i == 1) { apiUrl = arg; }
                else if (i == 2) { database = arg; }
                i++;
            }

            Console.WriteLine("Innosoft POS Uploader");
            Console.WriteLine("Version: 1.20170905");
            Console.WriteLine("=====================");

            while (true)
            {
                try
                {
                    if (database.Equals("1"))
                    {
                        // =====================
                        // Settings and Defaults
                        // =====================
                        var sysSettings = from d in posData1.SysSettings select d;
                        if (sysSettings.Any())
                        {
                            // =============
                            // Sales Invoice
                            // =============
                            var collections = from d in posData1.TrnCollections where d.PostCode == null && d.CollectionNumber != "NA" select d;
                            if (collections.Any())
                            {
                                foreach (var collection in collections)
                                {
                                    List<CollectionLines> listCollectionLines = new List<CollectionLines>();
                                    foreach (var salesLine in collection.TrnSale.TrnSalesLines)
                                    {
                                        listCollectionLines.Add(new CollectionLines()
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
                                            VAT = salesLine.MstTax.Tax
                                        });
                                    }

                                    var collectionData = new Collection()
                                    {
                                        SIDate = collection.CollectionDate.ToShortDateString(),
                                        BranchCode = sysSettings.FirstOrDefault().BranchCode,
                                        CustomerManualArticleCode = collection.TrnSale.MstCustomer.CustomerCode,
                                        CreatedBy = sysSettings.FirstOrDefault().UserCode,
                                        Term = collection.TrnSale.MstTerm.Term,
                                        DocumentReference = collection.CollectionNumber,
                                        ManualSINumber = collection.TrnSale.SalesNumber,
                                        Remarks = collection.MstUser4.UserName,
                                        listPOSIntegrationTrnSalesInvoiceItem = listCollectionLines.ToList()
                                    };

                                    string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(collectionData);
                                    string jsonFileName = jsonPath + "\\" + collection.CollectionNumber + ".json";
                                    File.WriteAllText(jsonFileName, json);

                                    Console.WriteLine("Saving " + collection.CollectionNumber + "...");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error... Retrying...");
                            }

                            // ========
                            // Stock-In
                            // ========
                            DateTime dateTimeToday = DateTime.Today;
                            var currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
                            var branchCode = sysSettings.FirstOrDefault().BranchCode;
                            fetchStockInItems(currentDate, branchCode);
                        }
                    }
                    else if (database.Equals("2"))
                    {
                        // =====================
                        // Settings and Defaults
                        // =====================
                        var sysSettings = from d in posData2.SysSettings select d;
                        if (sysSettings.Any())
                        {
                            // =============
                            // Sales Invoice
                            // =============
                            var collections = from d in posData2.TrnCollections where d.PostCode == null && d.CollectionNumber != "NA" select d;
                            if (collections.Any())
                            {
                                foreach (var collection in collections)
                                {
                                    List<CollectionLines> listCollectionLines = new List<CollectionLines>();
                                    foreach (var salesLine in collection.TrnSale.TrnSalesLines)
                                    {
                                        listCollectionLines.Add(new CollectionLines()
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
                                            VAT = salesLine.MstTax.Tax
                                        });
                                    }

                                    var collectionData = new Collection()
                                    {
                                        SIDate = collection.CollectionDate.ToShortDateString(),
                                        BranchCode = sysSettings.FirstOrDefault().BranchCode,
                                        CustomerManualArticleCode = collection.TrnSale.MstCustomer.CustomerCode,
                                        CreatedBy = sysSettings.FirstOrDefault().UserCode,
                                        Term = collection.TrnSale.MstTerm.Term,
                                        DocumentReference = collection.CollectionNumber,
                                        ManualSINumber = collection.TrnSale.SalesNumber,
                                        Remarks = collection.MstUser4.UserName,
                                        listPOSIntegrationTrnSalesInvoiceItem = listCollectionLines.ToList()
                                    };

                                    string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(collectionData);
                                    string jsonFileName = jsonPath + "\\" + collection.CollectionNumber + ".json";
                                    File.WriteAllText(jsonFileName, json);

                                    Console.WriteLine("Saving " + collection.CollectionNumber + "...");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error... Retrying...");
                            }

                            // ========
                            // Stock-In
                            // ========
                            DateTime dateTimeToday = DateTime.Today;
                            var currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
                            var branchCode = sysSettings.FirstOrDefault().BranchCode;
                            fetchStockInItems(currentDate, branchCode);
                        }
                    }
                    else if (database.Equals("3"))
                    {
                        // =====================
                        // Settings and Defaults
                        // =====================
                        var sysSettings = from d in posData3.SysSettings select d;
                        if (sysSettings.Any())
                        {
                            // =============
                            // Sales Invoice
                            // =============
                            var collections = from d in posData3.TrnCollections where d.PostCode == null && d.CollectionNumber != "NA" select d;
                            if (collections.Any())
                            {
                                foreach (var collection in collections)
                                {
                                    List<CollectionLines> listCollectionLines = new List<CollectionLines>();
                                    foreach (var salesLine in collection.TrnSale.TrnSalesLines)
                                    {
                                        listCollectionLines.Add(new CollectionLines()
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
                                            VAT = salesLine.MstTax.Tax
                                        });
                                    }

                                    var collectionData = new Collection()
                                    {
                                        SIDate = collection.CollectionDate.ToShortDateString(),
                                        BranchCode = sysSettings.FirstOrDefault().BranchCode,
                                        CustomerManualArticleCode = collection.TrnSale.MstCustomer.CustomerCode,
                                        CreatedBy = sysSettings.FirstOrDefault().UserCode,
                                        Term = collection.TrnSale.MstTerm.Term,
                                        DocumentReference = collection.CollectionNumber,
                                        ManualSINumber = collection.TrnSale.SalesNumber,
                                        Remarks = collection.MstUser4.UserName,
                                        listPOSIntegrationTrnSalesInvoiceItem = listCollectionLines.ToList()
                                    };

                                    string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(collectionData);
                                    string jsonFileName = jsonPath + "\\" + collection.CollectionNumber + ".json";
                                    File.WriteAllText(jsonFileName, json);

                                    Console.WriteLine("Saving " + collection.CollectionNumber + "...");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error... Retrying...");
                            }

                            // ========
                            // Stock-In
                            // ========
                            DateTime dateTimeToday = DateTime.Today;
                            var currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
                            var branchCode = sysSettings.FirstOrDefault().BranchCode;
                            fetchStockInItems(currentDate, branchCode);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Database not found... Retrying...");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Thread.Sleep(5000);

                // ===============
                // Send Json Files
                // ===============
                sendJsonFiles(jsonPath, apiUrl, database);
            }
        }
    }
}