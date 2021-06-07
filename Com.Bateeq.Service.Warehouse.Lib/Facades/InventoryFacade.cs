using Com.Bateeq.Service.Warehouse.Lib.Helpers;
using Com.Bateeq.Service.Warehouse.Lib.Interfaces;
using Com.Bateeq.Service.Warehouse.Lib.Models.InventoryModel;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.InventoryViewModel;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.NewIntegrationViewModel;
using Com.Moonlay.NetCore.Lib;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;

namespace Com.Bateeq.Service.Warehouse.Lib.Facades
{
    public class InventoryFacade
    {
        private string USER_AGENT = "Facade";

        private readonly WarehouseDbContext dbContext;
        private readonly DbSet<Inventory> dbSet;
        private readonly DbSet<InventoryMovement> dbSetMovement;
        private readonly IServiceProvider serviceProvider;

       // private readonly string GarmentPreSalesContractUri = "merchandiser/garment-pre-sales-contracts/";

        public InventoryFacade(IServiceProvider serviceProvider, WarehouseDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<Inventory>();
            this.dbSetMovement = dbContext.Set<InventoryMovement>();
        }

        public IQueryable<InventoryViewModel> GetQuery(string itemCode, string storageCode)
        {
            //GarmentCorrectionNote garmentCorrectionNote = new GarmentCorrectionNote();
            //var garmentCorrectionNotes = dbContext.Set<GarmentCorrectionNote>().AsQueryable();



            var Query = (from a in dbContext.Inventories


                         where
                         a.ItemCode == itemCode
                         && a.StorageCode == storageCode
                         //&& z.CodeRequirment == (string.IsNullOrWhiteSpace(category) ? z.CodeRequirment : category)


                         select new InventoryViewModel
                         {
                             item = new ViewModels.NewIntegrationViewModel.ItemViewModel {
                                 code = a.ItemCode,
                                 articleRealizationOrder = a.ItemArticleRealizationOrder

                             }, //a.ItemCode,
                             //ItemArticleRealization = a.ItemArticleRealizationOrder,
                             //ItemDomesticCOGS = a.ItemDomesticCOGS,
                             quantity = a.Quantity
                                
                             //Price = a.Price

                         });

            return Query;
        }

        public Tuple<List<InventoryViewModel>, int> GetItemPack(string itemCode, string storageCode, string order, int page, int size)
        {
            var Query = GetQuery(itemCode, storageCode);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(order);
            //if (OrderDictionary.Count.Equals(0))
            //{
            //	Query = Query.OrderByDescending(b => b.poExtDate);
            //}

            Pageable<InventoryViewModel> pageable = new Pageable<InventoryViewModel>(Query, page - 1, size);
            List<InventoryViewModel> Data = pageable.Data.ToList<InventoryViewModel>();
            int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, TotalData);
        }

        public List<Inventory> getDatabyCode(string itemCode, int StorageId)
        {
            var inventory = dbSet.Where(x => x.ItemCode == itemCode && x.StorageId == StorageId).ToList();
            return inventory;

        }

        public List<Inventory> getDatabyName(string itemName, int StorageId)
        {
            var inventory = dbSet.Where(x => x.ItemName ==itemName && x.StorageId == StorageId).ToList();
            return inventory;

        }

        public Inventory getStock(int source, int item)
        {
            var inventory = dbSet.Where(x => x.StorageId == source && x.ItemId == item).FirstOrDefault();
            return inventory;
        }

        #region Monitoring By User
        public IQueryable<InventoriesReportViewModel> GetReportQuery(string storageId, string filter)
        {
            //DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : (DateTime)dateFrom;
            //DateTime DateTo = dateTo == null ? DateTime.Now : (DateTime)dateTo;

            var Query = (from a in dbContext.Inventories
                         where a.IsDeleted == false
                         && a.StorageId == Convert.ToInt64((string.IsNullOrWhiteSpace(storageId) ? a.StorageId.ToString() :  storageId))
                         //&& a.StorageCode == (string.IsNullOrWhiteSpace(storageId) ? a.StorageCode : storageId)
                         //&& a.ItemName.Contains((string.IsNullOrWhiteSpace(filter) ? a.ItemName : filter))
                         //|| a.ItemArticleRealizationOrder.Contains((string.IsNullOrWhiteSpace(filter) ? a.ItemArticleRealizationOrder : filter))

                         select new InventoriesReportViewModel
                         {
                             ItemCode = a.ItemCode,
                             ItemName = a.ItemName,
                             ItemArticleRealizationOrder = a.ItemArticleRealizationOrder,
                             ItemSize = a.ItemSize,
                             ItemUom = a.ItemUom,
                             ItemDomesticSale = a.ItemDomesticSale,
                             Quantity = a.Quantity,
                             StorageId = a.StorageId,
                             StorageCode = a.StorageCode,
                             StorageName = a.StorageName
                         });

            var Query2 = (from a in Query
                          where a.ItemName.Contains((string.IsNullOrWhiteSpace(filter) ? a.ItemName : filter))
                          || a.ItemArticleRealizationOrder.Contains((string.IsNullOrWhiteSpace(filter) ? a.ItemArticleRealizationOrder : filter))

                          select new InventoriesReportViewModel
                          {
                              ItemCode = a.ItemCode,
                              ItemName = a.ItemName,
                              ItemArticleRealizationOrder = a.ItemArticleRealizationOrder,
                              ItemSize = a.ItemSize,
                              ItemUom = a.ItemUom,
                              ItemDomesticSale = a.ItemDomesticSale,
                              Quantity = a.Quantity,
                              StorageId = a.StorageId,
                              StorageCode = a.StorageCode,
                              StorageName = a.StorageName
                          });

            return Query2;

        }

        //public Tuple<List<InventoryReportViewModel>, int> GetReport(string no, string unitId, string categoryId, string budgetId, string prStatus, string poStatus, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order, int offset, string username)
        public Tuple<List<InventoriesReportViewModel>, int> GetReport(string storageId, string filter, int page, int size, string Order, int offset, string username)
        {
            var Query = GetReportQuery(storageId, filter);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            if (OrderDictionary.Count.Equals(0))
            {
                Query = Query.OrderByDescending(b => b.LastModifiedUtc);
            }
            else
            {
                string Key = OrderDictionary.Keys.First();
                string OrderType = OrderDictionary[Key];

                Query = Query.OrderBy(string.Concat(Key, " ", OrderType));
            }

            // Pageable<InventoriesReportViewModel> pageable = new Pageable<InventoriesReportViewModel>(Query, page - 1, size);
            List<InventoriesReportViewModel> Data = Query.ToList<InventoriesReportViewModel>();
            return Tuple.Create(Data, Data.Count());
        }


        public MemoryStream GenerateExcelReportByUser(string storecode, string filter)
        {
            var Query = GetReportQuery(storecode, filter);
            // Query = Query.OrderByDescending(a => a.ReceiptDate);
            DataTable result = new DataTable();
            //No	Unit	Budget	Kategori	Tanggal PR	Nomor PR	Kode Barang	Nama Barang	Jumlah	Satuan	Tanggal Diminta Datang	Status	Tanggal Diminta Datang Eksternal

            result.Columns.Add(new DataColumn() { ColumnName = "No", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Kode Toko", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Nama", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Barcode", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Nama Barang", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "RO", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Kuantitas", DataType = typeof(double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Harga", DataType = typeof(double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Subtotal", DataType = typeof(double) });
           



            if (Query.ToArray().Count() == 0)
                result.Rows.Add("", "", "", "", "", "", "", "", "", 0, 0, 0, 0, 0, 0);
            // to allow column name to be generated properly for empty data as template
            else
            {
                int index = 0;

                foreach (var item in Query)
                {
                    index++;
                    // string date = item.Date == null ? "-" : item.Date.ToOffset(new TimeSpan(7, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));
                    //string pr_date = item.PRDate == null ? "-" : item.PRDate.ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));
                    //string do_date = item.DODate == null ? "-" : item.ReceiptDate.ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));

                    result.Rows.Add(index, item.StorageCode, item.StorageName, item.ItemCode, item.ItemName, item.ItemArticleRealizationOrder, item.Quantity, item.ItemDomesticSale, item.Quantity * item.ItemDomesticSale);
                }
            }

            return Excel.CreateExcel(new List<KeyValuePair<DataTable, string>>() { new KeyValuePair<DataTable, string>(result, "Territory") }, true);
        }
        #endregion

        #region Monitoring By Search
        public IQueryable<InventoriesReportViewModel> GetSearchQuery(string itemCode, int offset, string username)
        {
            //DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : (DateTime)dateFrom;
            //DateTime DateTo = dateTo == null ? DateTime.Now : (DateTime)dateTo;

            var Query = (from a in dbContext.Inventories
                         where a.IsDeleted == false
                         && a.ItemCode == (string.IsNullOrWhiteSpace(itemCode) ? a.ItemCode : itemCode)

                         select new InventoriesReportViewModel
                         {
                             ItemCode = a.ItemCode,
                             ItemName = a.ItemName,
                             ItemArticleRealizationOrder = a.ItemArticleRealizationOrder,
                             ItemSize = a.ItemSize,
                             ItemUom = a.ItemUom,
                             ItemDomesticSale = a.ItemDomesticSale,
                             Quantity = a.Quantity,
                             StorageId = a.StorageId,
                             StorageCode = a.StorageCode,
                             StorageName = a.StorageName
                         });
            return Query;
        }

        //public Tuple<List<InventoryReportViewModel>, int> GetReport(string no, string unitId, string categoryId, string budgetId, string prStatus, string poStatus, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order, int offset, string username)
        public Tuple<List<InventoriesReportViewModel>, int> GetSearch(string itemCode, int page, int size, string Order, int offset, string username)
        {
            var Query = GetSearchQuery(itemCode, offset, username);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            if (OrderDictionary.Count.Equals(0))
            {
                Query = Query.OrderByDescending(b => b.LastModifiedUtc);
            }
            else
            {
                string Key = OrderDictionary.Keys.First();
                string OrderType = OrderDictionary[Key];

                Query = Query.OrderBy(string.Concat(Key, " ", OrderType));
            }

            // Pageable<InventoriesReportViewModel> pageable = new Pageable<InventoriesReportViewModel>(Query, page - 1, size);
            List<InventoriesReportViewModel> Data = Query.ToList<InventoriesReportViewModel>();
            // int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, Data.Count());
        }
        #endregion

        public Inventory getStockPOS(string sourcecode, string itemCode)
        {
            var inventory = dbSet.Where(x => x.StorageCode == sourcecode && x.ItemCode == itemCode).FirstOrDefault();
            return inventory;

        }

        #region Monitoring Inventory Movements
        public IQueryable<InventoryMovementsReportViewModel> GetMovementQuery(string storageId, string itemCode)
        {
            //DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : (DateTime)dateFrom;
            //DateTime DateTo = dateTo == null ? DateTime.Now : (DateTime)dateTo;

            var Query = (from c in dbContext.InventoryMovements
                         where c.IsDeleted == false
                         //&& c.StorageId == (string.IsNullOrWhiteSpace(storageId) ? c.StorageId : storageId)
                         && c.StorageId == Convert.ToInt64((string.IsNullOrWhiteSpace(storageId) ? c.StorageId.ToString() : storageId))
                         && c.ItemCode == (string.IsNullOrWhiteSpace(itemCode) ? c.ItemCode : itemCode)
                         //&& a.ItemName == (string.IsNullOrWhiteSpace(info) ? a.ItemName : info)

                         select new InventoryMovementsReportViewModel
                         {
                             Date = c.Date,
                             ItemCode = c.ItemCode,
                             ItemName = c.ItemName,
                             ItemArticleRealizationOrder = c.ItemArticleRealizationOrder,
                             ItemSize = c.ItemSize,
                             ItemUom = c.ItemUom,
                             ItemDomesticSale = c.ItemDomesticSale,
                             Quantity = c.Quantity,
                             Before = c.Before,
                             After = c.After,
                             Type = c.Type,
                             Reference = c.Reference,
                             Remark = c.Remark,
                             StorageId = c.StorageId,
                             StorageCode = c.StorageCode,
                             StorageName = c.StorageName,
                             CreatedUtc = c.CreatedUtc,
                         });
            return Query;
        }

        //public Tuple<List<InventoryReportViewModel>, int> GetReport(string no, string unitId, string categoryId, string budgetId, string prStatus, string poStatus, DateTime? dateFrom, DateTime? dateTo, int page, int size, string Order, int offset, string username)
        public Tuple<List<InventoryMovementsReportViewModel>, int> GetMovements(string storageId, string itemCode, string info, string Order, int offset, string username, int page = 1, int size = 25)
        {
            var Query = GetMovementQuery(storageId, itemCode);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            if (OrderDictionary.Count.Equals(0))
            {
                Query = Query.OrderByDescending(b => b.LastModifiedUtc);
            }
            else
            {
                string Key = OrderDictionary.Keys.First();
                string OrderType = OrderDictionary[Key];

                Query = Query.OrderBy(string.Concat(Key, " ", OrderType));
            }

            Pageable<InventoryMovementsReportViewModel> pageable = new Pageable<InventoryMovementsReportViewModel>(Query, page - 1, size);
            //List<InventoriesReportViewModel> Data = Query.ToList<InventoriesReportViewModel>();
            List<InventoryMovementsReportViewModel> Data = pageable.Data.ToList<InventoryMovementsReportViewModel>();
            int TotalData = pageable.TotalCount;

            //return Tuple.Create(Data, Data.Count());
            return Tuple.Create(Data, TotalData);

        }


        public MemoryStream GenerateExcelReportByMovement(string storecode, string itemCode)
        {
            var Query = GetMovementQuery(storecode, itemCode);
            // Query = Query.OrderByDescending(a => a.ReceiptDate);
            DataTable result = new DataTable();
            //No	Unit	Budget	Kategori	Tanggal PR	Nomor PR	Kode Barang	Nama Barang	Jumlah	Satuan	Tanggal Diminta Datang	Status	Tanggal Diminta Datang Eksternal

            result.Columns.Add(new DataColumn() { ColumnName = "No", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Kode Toko", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Nama", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Barcode", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Nama Barang", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Tanggal", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Referensi", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Tipe", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Sebelum", DataType = typeof(double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Kuantitas", DataType = typeof(double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Setelah", DataType = typeof(double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Keterangan", DataType = typeof(String) });




            if (Query.ToArray().Count() == 0)
                result.Rows.Add("", "", "", "", "", "", "", "",0, 0, 0,"");
            // to allow column name to be generated properly for empty data as template
            else
            {

                int index = 0;
                foreach (var item in Query)
                {
                    index++;
                    string date = item.Date == null ? "-" : item.Date.ToOffset(new TimeSpan(7, 0, 0)).ToString("dd MMM yyyy - HH:mm:ss", new CultureInfo("id-ID"));
                    //string pr_date = item.PRDate == null ? "-" : item.PRDate.ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));
                    //string do_date = item.DODate == null ? "-" : item.ReceiptDate.ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));

                    result.Rows.Add(index, item.StorageCode, item.StorageName, item.ItemCode, item.ItemName, date, 
                        item.Reference, item.Type, item.Before, item.Quantity, item.After, item.Remark);



                }

            }

            return Excel.CreateExcel(new List<KeyValuePair<DataTable, string>>() { new KeyValuePair<DataTable, string>(result, "Territory") }, true);
        }
        #endregion

        #region Stock Availability

        private List<StoreViewModel> getNearestStore(string code)
        {
            string itemUri = "master/stores/nearest-store";
            string queryUri = "?code=" + code;
            string uri = itemUri + queryUri;

            IHttpClientService httpClient = (IHttpClientService)serviceProvider.GetService(typeof(IHttpClientService));
            var response = httpClient.GetAsync($"{APIEndpoint.Core}{uri}").Result;
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                List<StoreViewModel> viewModel = JsonConvert.DeserializeObject<List<StoreViewModel>>(result.GetValueOrDefault("data").ToString());
                return viewModel;
            }
            else
            {
                return null;
            }
        }

        public async Task<List<InventoryViewModel2>> GetNearestStorageStock(string storageCode, string itemCode)
        {
            var itemList = new List<InventoryViewModel2>();
            var stores = getNearestStore(storageCode);

            if(stores != null)
            {
                foreach (var store in stores)
                {
                    var item = (from a in dbContext.Inventories
                                where a.StorageCode == store.Code
                                && a.ItemCode == itemCode
                                select new InventoryViewModel2
                                {
                                    ItemCode = a.ItemCode,
                                    ItemName = a.ItemName,
                                    StorageCode = a.StorageCode,
                                    StorageName = a.StorageName,
                                    City = store.City,
                                    Quantity = a.Quantity,
                                    LatestDate = new DateTimeOffset()
                                }).FirstOrDefault();
                    if (item != null)
                    {
                        if (item.StorageCode != storageCode && !item.StorageCode.Contains("GDG"))
                        {
                            itemList.Add(item);
                        }
                    }
                }

            }

            return itemList;
        }

        public IQueryable<InventoryViewModel> GetAllStockByStorageQuery(string storageId)
        {
            var Query = (from a in dbContext.Inventories
                         where a.StorageId == Convert.ToInt64(storageId)
                         select new InventoryViewModel
                         {
                             storage = new ViewModels.NewIntegrationViewModel.StorageViewModel
                             {
                                 code = a.StorageCode,
                                 name = a.StorageName
                             },
                             item = new ViewModels.NewIntegrationViewModel.ItemViewModel
                             {
                                 code = a.ItemCode,
                                 name = a.ItemName,
                                 articleRealizationOrder = a.ItemArticleRealizationOrder
                             },
                             quantity = a.Quantity
                         });
            return Query;
        }

        public List<InventoryViewModel> GetAllStockByStorageId (string storageId)
        {
            var Query = GetAllStockByStorageQuery(storageId);

            return Query.ToList();
        }

        #endregion

        #region Monthly Stock
        public List<MonthlyStockViewModel> GetOverallMonthlyStock (string _year, string _month)
        {
            var month = Convert.ToInt32(_month);
            var year = Convert.ToInt32(_year);

            var firstDay = new DateTime(year, month, 1);
            var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            List<MonthlyStockViewModel> monthlyStock = GetMonthlyStockQuery(firstDay, lastDay).ToList();

            return monthlyStock;
        }
        
        public IEnumerable<MonthlyStockViewModel> GetMonthlyStockQuery(DateTime firstDay, DateTime lastDay)
        {
            var movementStock = (from a in dbContext.InventoryMovements
                                 where a.CreatedUtc <= lastDay
                                 && a.IsDeleted == false
                                 select new
                                 {
                                     ItemCode = a.ItemCode,
                                     ItemName = a.ItemName,
                                     StorageCode = a.StorageCode,
                                     StorageName = a.StorageName,
                                     CreatedUtc = a.CreatedUtc,
                                     After = a.After,
                                     ItemDomesticCOGS = a.ItemDomesticCOGS,
                                     ItemInternationalCOGS = a.ItemInternationalCOGS,
                                     ItemDomesticSale = a.ItemDomesticSale,
                                     ItemInternationalSale = a.ItemInternationalSale

                                 }).ToList();

            var earlyStock = (from a in movementStock
                              orderby a.CreatedUtc descending
                              where a.CreatedUtc < firstDay
                              group a by new { a.ItemCode, a.ItemName, a.StorageCode, a.StorageName } into aa
                               
                              select new StockPerItemViewModel
                              {
                                  ItemCode = aa.Key.ItemCode,
                                  ItemName = aa.Key.ItemName,
                                  StorageCode = aa.Key.StorageCode,
                                  StorageName = aa.Key.StorageName,
                                  Quantity = aa.FirstOrDefault().After,
                                  HPP = (aa.FirstOrDefault().ItemDomesticCOGS > 0 ? aa.FirstOrDefault().ItemDomesticCOGS : aa.FirstOrDefault().ItemInternationalCOGS) * aa.FirstOrDefault().After,
                                  Sale = (aa.FirstOrDefault().ItemDomesticSale > 0 ? aa.FirstOrDefault().ItemDomesticSale : aa.FirstOrDefault().ItemInternationalSale) * aa.FirstOrDefault().After

                              });

            var overallEarlyStock = (from b in earlyStock
                                     group b by new {b.StorageCode, b.StorageName} into bb

                                     select new MonthlyStockViewModel
                                     {
                                         StorageCode = bb.Key.StorageCode,
                                         StorageName = bb.Key.StorageName,
                                         EarlyQuantity = bb.Sum(x=>x.Quantity),
                                         EarlyHPP = bb.Sum(x=>x.HPP),
                                         EarlySale = bb.Sum(x=>x.Sale),
                                         LateQuantity = 0,
                                         LateHPP = 0,
                                         LateSale = 0
                                     });

            var lateStock = (from a in movementStock
                             orderby a.CreatedUtc descending
                             where a.CreatedUtc <= lastDay
                             group a by new { a.ItemCode, a.ItemName, a.StorageCode, a.StorageName } into aa

                             select new StockPerItemViewModel
                             {
                                 ItemCode = aa.Key.ItemCode,
                                 ItemName = aa.Key.ItemName,
                                 StorageCode = aa.Key.StorageCode,
                                 StorageName = aa.Key.StorageName,
                                 Quantity = aa.FirstOrDefault().After,
                                 HPP = (aa.FirstOrDefault().ItemDomesticCOGS > 0 ? aa.FirstOrDefault().ItemDomesticCOGS : aa.FirstOrDefault().ItemInternationalCOGS) * aa.FirstOrDefault().After,
                                 Sale = (aa.FirstOrDefault().ItemDomesticSale > 0 ? aa.FirstOrDefault().ItemDomesticSale : aa.FirstOrDefault().ItemInternationalSale) * aa.FirstOrDefault().After
                             });

            var overallLateStock = (from b in lateStock
                                    group b by new { b.StorageCode, b.StorageName } into bb

                                    select new MonthlyStockViewModel
                                    {
                                        StorageCode = bb.Key.StorageCode,
                                        StorageName = bb.Key.StorageName,
                                        EarlyQuantity = 0,
                                        EarlyHPP = 0,
                                        EarlySale = 0,
                                        LateQuantity = bb.Sum(x=>x.Quantity),
                                        LateHPP = bb.Sum(x=>x.HPP),
                                        LateSale = bb.Sum(x=>x.Sale)
                                    });

            var overallMonthlyStock = overallEarlyStock.Union(overallLateStock).ToList();

            var data = (from query in overallMonthlyStock
                        group query by new { query.StorageCode, query.StorageName} into groupdata

                        select new MonthlyStockViewModel
                        {
                            StorageCode = groupdata.Key.StorageCode,
                            StorageName = groupdata.Key.StorageName,
                            EarlyQuantity = groupdata.Sum(x=>x.EarlyQuantity),
                            EarlyHPP = groupdata.Sum(x=>x.EarlyHPP),
                            EarlySale = groupdata.Sum(x=>x.EarlySale),
                            LateQuantity = groupdata.Sum(x=>x.LateQuantity),
                            LateHPP = groupdata.Sum(x=>x.LateHPP),
                            LateSale = groupdata.Sum(x=>x.LateSale)
                        });

            return data.AsQueryable();
        }

        public List<StockPerItemViewModel> GetOverallStorageStock(string code, string _year, string _month)
        {
            var month = Convert.ToInt32(_month);
            var year = Convert.ToInt32(_year);

            var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            List<StockPerItemViewModel> latestStockByStorage = GetLatestStockByStorageQuery(code, lastDay).ToList();

            return latestStockByStorage;
        }

        public IEnumerable<StockPerItemViewModel> GetLatestStockByStorageQuery(string code, DateTime date)
        {
            var LatestStock = (from a in dbContext.InventoryMovements
                               orderby a.CreatedUtc descending
                               where a.CreatedUtc <= date
                               && a.StorageCode == code
                               group a by new { a.ItemCode } into aa

                               select new StockPerItemViewModel
                               {
                                   ItemCode = aa.FirstOrDefault().ItemCode,
                                   ItemName = aa.FirstOrDefault().ItemName,
                                   StorageCode = aa.FirstOrDefault().StorageCode,
                                   StorageName = aa.FirstOrDefault().StorageName,
                                   Quantity = aa.FirstOrDefault().After,
                                   HPP = (aa.FirstOrDefault().ItemDomesticCOGS > 0 ? aa.FirstOrDefault().ItemDomesticCOGS : aa.FirstOrDefault().ItemInternationalCOGS) * aa.FirstOrDefault().After,
                                   Sale = (aa.FirstOrDefault().ItemDomesticSale > 0 ? aa.FirstOrDefault().ItemDomesticSale : aa.FirstOrDefault().ItemInternationalSale) * aa.FirstOrDefault().After
                               });
            var _LatestStock = (from b in LatestStock
                                where b.Quantity > 0
                                select b);

            return _LatestStock.AsQueryable();
        }

        public MemoryStream GenerateExcelForLatestStockByStorage (string code, string _month, string _year)
        {
            var month = Convert.ToInt32(_month);
            var year = Convert.ToInt32(_year);

            var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            var Query = GetLatestStockByStorageQuery(code, lastDay);

            DataTable result = new DataTable();

            result.Columns.Add(new DataColumn() { ColumnName = "No", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Kode Toko", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Bulan", DataType = typeof(Int32) });
            result.Columns.Add(new DataColumn() { ColumnName = "Tahun", DataType = typeof(Int32) });
            result.Columns.Add(new DataColumn() { ColumnName = "Barcode", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Nama Barang", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Kuantitas", DataType = typeof(double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Total HPP", DataType = typeof(double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Total Sale", DataType = typeof(double) });

            if (Query.ToArray().Count() == 0)
                result.Rows.Add("", "", "", "", "", "", "", "", "");

            else
            {
                int index = 0;

                foreach (var item in Query)
                {
                    index++;

                    result.Rows.Add(index, item.StorageCode, month, year, item.ItemCode, item.ItemName, item.Quantity, item.HPP, item.Sale);

                }
            }

            return Excel.CreateExcel(new List<KeyValuePair<DataTable, string>>() { new KeyValuePair<DataTable, string>(result, "Territory") }, true);
        }
        #endregion

        #region ByRO
        public List<InventoryByRoReportViewModel> GetInventoryReportByRo(string articleRealizationOrder)
        {
            var Query = GetInventoryByRoQuery(articleRealizationOrder);
            return Query.ToList();
        }

        public IEnumerable<InventoryByRoReportViewModel> GetInventoryByRoQuery(string articleRealizationOrder)
        {
            var Query = (from a in dbContext.Inventories
                         join b in dbContext.ExpeditionDetails on a.ItemArticleRealizationOrder equals b.ArticleRealizationOrder
                         join bb in dbContext.ExpeditionItems on b.ExpeditionItemId equals bb.Id
                         join bbb in dbContext.Expeditions on bb.ExpeditionId equals bbb.Id

                         where a.ItemArticleRealizationOrder == articleRealizationOrder
                         && a.StorageCode == bb.DestinationCode
                         && a.IsDeleted == false
                         && b.IsDeleted == false
                         && bb.IsDeleted == false
                         && bbb.IsDeleted == false

                         orderby bbb.CreatedUtc descending

                         group new { a, b, bb, bbb } by new { a.StorageCode, a.ItemArticleRealizationOrder, a.ItemSize, bb.DestinationCode} into data

                         select new InventoryByRoReportViewModel
                         {
                             StorageCode = data.FirstOrDefault().a.StorageCode,
                             StorageName = data.FirstOrDefault().a.StorageName,

                             DateDiff = Math.Truncate(Convert.ToDecimal(DateTime.Now.Subtract(data.FirstOrDefault().bbb.CreatedUtc).TotalDays)),

                             ItemArticleRealizationOrder = data.FirstOrDefault().a.ItemArticleRealizationOrder,
                             ItemSize = data.FirstOrDefault().a.ItemSize,
                             StockQuantity = data.FirstOrDefault().a.Quantity,
                             SaleQuantity = 0

                         });

            return Query;
        }

        private List<SalesDocByRoViewModel> getSalesPerRo(string ro)
        {
            string itemUri = "sales-docs/readbyro";
            string queryUri = "/" + ro;
            string uri = itemUri + queryUri;

            IHttpClientService httpClient = (IHttpClientService)serviceProvider.GetService(typeof(IHttpClientService));
            var response = httpClient.GetAsync($"{APIEndpoint.POS}{uri}").Result;

            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                List<SalesDocByRoViewModel> viewModel = JsonConvert.DeserializeObject<List<SalesDocByRoViewModel>>(result.GetValueOrDefault("data").ToString());
                return viewModel;
            }
            else
            {
                return null;
            }

        }
        #endregion
    }
}