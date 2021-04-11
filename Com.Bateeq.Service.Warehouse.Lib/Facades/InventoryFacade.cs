using Com.Bateeq.Service.Warehouse.Lib.Helpers;
using Com.Bateeq.Service.Warehouse.Lib.Models.InventoryModel;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.InventoryViewModel;
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

namespace Com.Bateeq.Service.Warehouse.Lib.Facades
{
    public class InventoryFacade
    {
        private string USER_AGENT = "Facade";

        private readonly WarehouseDbContext dbContext;
        private readonly DbSet<Inventory> dbSet;
        private readonly IServiceProvider serviceProvider;

       // private readonly string GarmentPreSalesContractUri = "merchandiser/garment-pre-sales-contracts/";

        public InventoryFacade(IServiceProvider serviceProvider, WarehouseDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<Inventory>();
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
    }
}
