using Com.DanLiris.Service.Warehouse.Lib.ViewModels.SpkDocsViewModel;
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
using Com.Bateeq.Service.Warehouse.Lib;
using Com.Bateeq.Service.Warehouse.Lib.Helpers;
using System.Threading.Tasks;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.SpkDocsViewModel;
using Com.Bateeq.Service.Warehouse.Lib.Models.SPKDocsModel;
using MongoDB.Bson;
using HashidsNet;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.NewIntegrationViewModel;
using Com.Bateeq.Service.Warehouse.Lib.Interfaces;
using System.Net.Http;
using Com.Moonlay.Models;

namespace Com.Bateeq.Service.Warehouse.Lib.Facades
{
    public class SPKDocsFacade
    {
        private string USER_AGENT = "Facade";

        private readonly WarehouseDbContext dbContext;
        private readonly DbSet<Inventory> dbSet;
        private readonly IServiceProvider serviceProvider;

        public SPKDocsFacade(IServiceProvider serviceProvider, WarehouseDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<Inventory>();
        }

        #region Monitoring By User
        public IQueryable<SPKDocsReportViewModel> GetReportQuery(DateTime? dateFrom, DateTime? dateTo, string destinationCode, bool status, int transaction, string packingList, int offset, string username)
        {
            DateTime DateFrom = dateFrom == null ? new DateTime(1970, 1, 1) : (DateTime)dateFrom;
            DateTime DateTo = dateTo == null ? DateTime.Now : (DateTime)dateTo;

            var Query = (from a in dbContext.SPKDocs
                         join b in dbContext.SPKDocsItems on a.Id equals b.SPKDocsId
                         where a.IsDeleted == false
                             && b.IsDeleted == false
                             && a.DestinationCode == (string.IsNullOrWhiteSpace(destinationCode) ? a.DestinationCode : destinationCode)
                             && a.CreatedBy == (string.IsNullOrWhiteSpace(username) ? a.CreatedBy : username)
                             && !a.Reference.Contains("RTT")
                             // && a.Code == (string.IsNullOrWhiteSpace(code) ? a.Code : code)
                             && a.Date.AddHours(offset).Date >= DateFrom.Date
                             && a.Date.AddHours(offset).Date <= DateTo.Date
                             && a.IsDistributed == status
                             && (transaction == 0 ? a.SourceCode == "GDG.01" : a.SourceCode != "GDG.01" && !a.Reference.Contains("RTP"))
                             && a.PackingList.Contains(string.IsNullOrWhiteSpace(packingList) ? a.PackingList : packingList)

                         select new SPKDocsReportViewModel
                         {
                             no = a.Code,
                             date = a.Date,
                             sourceCode = a.SourceCode,
                             sourceName = a.SourceName,
                             destinationCode = a.DestinationCode,
                             destinationName = a.DestinationName,
                             isReceived = a.IsReceived,
                             isDistributed = a.IsDistributed,
                             packingList = a.PackingList,
                             password = a.Password,
                             itemCode = b.ItemCode,
                             itemName = b.ItemName,
                             itemSize = b.ItemSize,
                             itemUom = b.ItemUom,
                             itemArticleRealizationOrder = b.ItemArticleRealizationOrder,
                             Quantity = b.Quantity,
                             itemDomesticSale = b.ItemDomesticSale,
                             LastModifiedUtc = b.LastModifiedUtc
                         });
            //return Query;
            return Query.AsQueryable();
        }

        public Tuple<List<SPKDocsReportViewModel>, int> GetReport(DateTime? dateFrom, DateTime? dateTo, string destinationCode, bool status, int transaction, string packingList, int page, int size, string Order, int offset, string username)
        {
            var Query = GetReportQuery(dateFrom, dateTo, destinationCode, status, transaction, packingList, offset, username);

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

            Pageable<SPKDocsReportViewModel> pageable = new Pageable<SPKDocsReportViewModel>(Query, page - 1, size);
            List<SPKDocsReportViewModel> Data = pageable.Data.ToList<SPKDocsReportViewModel>();
            int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, TotalData);
        }

        public MemoryStream GenerateExcel(DateTime? dateFrom, DateTime? dateTo, string destinationCode, bool status, int transaction, string packingList, int offset, string username)
        {
            var Query = GetReportQuery(dateFrom, dateTo, destinationCode, status, transaction, packingList, offset, username);
            Query = Query.OrderByDescending(b => b.LastModifiedUtc);
            DataTable result = new DataTable();
            //No	Unit	Budget	Kategori	Tanggal PR	Nomor PR	Kode Barang	Nama Barang	Jumlah	Satuan	Tanggal Diminta Datang	Status	Tanggal Diminta Datang Eksternal


            result.Columns.Add(new DataColumn() { ColumnName = "Tanggal", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Sumber Penyimpanan", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Tujuan Penyimpanan", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Packing List", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Barcode", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Deskripsi", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Size", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "RO", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Satuan", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Quantity", DataType = typeof(double) });
            result.Columns.Add(new DataColumn() { ColumnName = "Diterima", DataType = typeof(String) });
            result.Columns.Add(new DataColumn() { ColumnName = "Ekspedisi", DataType = typeof(String) });
            //result.Columns.Add(new DataColumn() { ColumnName = "Jumlah Diminta", DataType = typeof(double) });
            //result.Columns.Add(new DataColumn() { ColumnName = "Satuan Diminta", DataType = typeof(String) });
            //result.Columns.Add(new DataColumn() { ColumnName = "Tanggal diminta datang", DataType = typeof(String) });
            //result.Columns.Add(new DataColumn() { ColumnName = "Tanggal diminta datang PO Eksternal", DataType = typeof(String) });
            //result.Columns.Add(new DataColumn() { ColumnName = "Jumlah Deal PO Eksternal", DataType = typeof(double) });
            //result.Columns.Add(new DataColumn() { ColumnName = "Satuan Deal PO Eksternal", DataType = typeof(String) });
            //result.Columns.Add(new DataColumn() { ColumnName = "Status PR", DataType = typeof(String) });
            //result.Columns.Add(new DataColumn() { ColumnName = "Status Barang", DataType = typeof(String) });
            if (Query.ToArray().Count() == 0)
                result.Rows.Add("", "", "", "", "", "", "", "", "", 0, "", ""); // to allow column name to be generated properly for empty data as template
            else
            {
                int index = 0;
                foreach (var item in Query)
                {
                    index++;
                    string date = item.date == null ? "-" : item.date.ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));
                    //string prDate = item.expectedDeliveryDatePR == new DateTime(1970, 1, 1) ? "-" : item.expectedDeliveryDatePR.ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));
                    //string epoDate = item.expectedDeliveryDatePO == new DateTime(1970, 1, 1) ? "-" : item.expectedDeliveryDatePO.ToOffset(new TimeSpan(offset, 0, 0)).ToString("dd MMM yyyy", new CultureInfo("id-ID"));
                    result.Rows.Add(item.date, item.sourceName, item.destinationName, item.packingList, item.itemCode, item.itemName, item.itemSize, item.itemArticleRealizationOrder, item.itemUom, item.Quantity, item.isReceived, item.isDistributed);
                }
            }

            return Excel.CreateExcel(new List<KeyValuePair<DataTable, string>>() { new KeyValuePair<DataTable, string>(result, "Territory") }, true);
        }
        #endregion

        public async Task<int> Create(SPKDocsFromFinihsingOutsViewModel viewModel, string username, string token, int clientTimeZoneOffset = 7)
        {
            int Created = 0;

            using (var transaction = this.dbContext.Database.BeginTransaction())
            {
                try
                {
                    List<SPKDocsItem> sPKDocsItems = new List<SPKDocsItem>();
                    int itemIdx = 1;
                    foreach(var item in viewModel.Items) {

                        // ambil product code, ambil first or default dari garment invoice detail yang product code dan no ro nya sama
                        // lalu ambil invoiceItemid,
                        // lalu ambil invoiceId, lalu ambil suplier

                        //string itemsUri = "items/finished-goods";
                        //var httpClient = (IHttpClientService)serviceProvider.GetService(typeof(IHttpClientService));
                        //var response = await httpClient.PostAsync($"{APIEndpoint.Core}{itemsUri}", new StringContent(JsonConvert.SerializeObject(item).ToString(), Encoding.UTF8, General.JsonMediaType));

                        //response.EnsureSuccessStatusCode();

                        var isDifferentSize = item.IsDifferentSize;
                        if (isDifferentSize == true)
                        {
                            int idx = 1;
                            foreach(var detail in item.Details)
                            {
                                var sizeId = detail.Size.Id;
                                var barcode = GenerateBarcode(idx,sizeId);
                                var itemx = GetItem(barcode);

                                if (itemx.Count() == 0 || itemx == null) //barcode belum terdaftar, insert ke tabel items (BMS) terlebih dahulu
                                {
                                    ItemCoreViewModelUsername itemCore = new ItemCoreViewModelUsername
                                    {
                                        dataDestination = new List<ItemViewModelRead>
                                        {
                                           new ItemViewModelRead
                                           {
                                               ArticleRealizationOrder = viewModel.RONo,
                                               code = barcode,
                                               name = viewModel.Comodity.name,
                                               Size = item.Size.Size,
                                               Uom = item.Uom.Unit,
                                               ImagePath = viewModel.ImagePath,
                                               ImgFile = "",
                                               Tags = "",
                                               Remark = "",
                                               Description = "",
                                               _id = 0
                                           }
                                        },
                                        color = viewModel.color,
                                        process = viewModel.process,
                                        materials = viewModel.materials,
                                        materialCompositions = viewModel.materialCompositions,
                                        collections = viewModel.collections,
                                        seasons = viewModel.seasons,
                                        counters = viewModel.counters,
                                        subCounters = viewModel.subCounters,
                                        categories = viewModel.categories,
                                        DomesticCOGS = item.BasicPrice,
                                        DomesticRetail = 0,
                                        DomesticSale = item.BasicPrice + item.ComodityPrice,
                                        DomesticWholesale = 0,
                                        InternationalCOGS = 0,
                                        InternationalWholesale = 0,
                                        InternationalRetail = 0,
                                        InternationalSale = 0,
                                        ImageFile = viewModel.ImgFile,
                                        _id = 0,
                                        Username = username,
                                        Token = token
                                    };

                                    string itemsUri = "items/finished-goods";
                                    var httpClient = (IHttpClientService)serviceProvider.GetService(typeof(IHttpClientService));
                                    var response = await httpClient.PostAsync($"{APIEndpoint.Core}{itemsUri}", new StringContent(JsonConvert.SerializeObject(itemCore).ToString(), Encoding.UTF8, General.JsonMediaType));

                                    response.EnsureSuccessStatusCode();

                                    var item2 = GetItem(barcode);

                                    sPKDocsItems.Add(new SPKDocsItem
                                    {
                                        ItemArticleRealizationOrder = viewModel.RONo,
                                        ItemCode = barcode,
                                        ItemDomesticCOGS = item.BasicPrice,
                                        ItemDomesticSale = item.BasicPrice + item.ComodityPrice,
                                        ItemId = item2.Single()._id,
                                        ItemName = viewModel.Comodity.name,
                                        ItemSize = item.Size.Size,
                                        ItemUom = item.Uom.Unit,
                                        Quantity = item.Quantity,
                                        Remark = "",
                                        SendQuantity = item.Quantity,
                                    });
                                }
                                else // barcode sudah terdaftar
                                {
                                    sPKDocsItems.Add(new SPKDocsItem
                                    {
                                        ItemArticleRealizationOrder = viewModel.RONo,
                                        ItemCode = barcode,
                                        ItemDomesticCOGS = item.BasicPrice,
                                        ItemDomesticSale = item.BasicPrice + item.ComodityPrice,
                                        ItemId = itemx.Single()._id,
                                        ItemName = viewModel.Comodity.name,
                                        ItemSize = item.Size.Size,
                                        ItemUom = item.Uom.Unit,
                                        Quantity = item.Quantity,
                                        Remark = "",
                                        SendQuantity = item.Quantity,
                                    });
                                }
                                idx++;
                            }
                        }
                        else
                        {
                            var sizeId = item.Size.Id;
                            var barcode = GenerateBarcode(itemIdx, sizeId);
                            var itemx = GetItem(barcode);

                            if (itemx.Count() == 0 || itemx == null) //barcode belum terdaftar, insert ke tabel items (BMS) terlebih dahulu
                            {
                                ItemCoreViewModelUsername itemCore = new ItemCoreViewModelUsername
                                {
                                    dataDestination = new List<ItemViewModelRead>
                                        {
                                           new ItemViewModelRead
                                           {
                                               ArticleRealizationOrder = viewModel.RONo,
                                               code = barcode,
                                               name = viewModel.Comodity.name,
                                               Size = item.Size.Size,
                                               Uom = item.Uom.Unit,
                                               ImagePath = viewModel.ImagePath,
                                               ImgFile = "",
                                               Tags = "",
                                               Remark = "",
                                               Description = "",
                                               _id = 0
                                           }
                                        },
                                    color = viewModel.color,
                                    process = viewModel.process,
                                    materials = viewModel.materials,
                                    materialCompositions = viewModel.materialCompositions,
                                    collections = viewModel.collections,
                                    seasons = viewModel.seasons,
                                    counters = viewModel.counters,
                                    subCounters = viewModel.subCounters,
                                    categories = viewModel.categories,
                                    DomesticCOGS = item.BasicPrice,
                                    DomesticRetail = 0,
                                    DomesticSale = item.BasicPrice + item.ComodityPrice,
                                    DomesticWholesale = 0,
                                    InternationalCOGS = 0,
                                    InternationalWholesale = 0,
                                    InternationalRetail = 0,
                                    InternationalSale = 0,
                                    ImageFile = "",
                                    _id = 0,
                                    Username = username,
                                    Token = token
                                };

                                string itemsUri = "items/finished-goods/item";
                                var httpClient = (IHttpClientService)serviceProvider.GetService(typeof(IHttpClientService));
                                var response = await httpClient.PostAsync($"{APIEndpoint.Core}{itemsUri}", new StringContent(JsonConvert.SerializeObject(itemCore).ToString(), Encoding.UTF8, General.JsonMediaType));

                                response.EnsureSuccessStatusCode();

                                var item2 = GetItem(barcode);

                                sPKDocsItems.Add(new SPKDocsItem
                                {
                                    ItemArticleRealizationOrder = viewModel.RONo,
                                    ItemCode = barcode,
                                    ItemDomesticCOGS = item.BasicPrice,
                                    ItemDomesticSale = item.BasicPrice + item.ComodityPrice,
                                    ItemId = item2.Single()._id,
                                    ItemName = viewModel.Comodity.name,
                                    ItemSize = item.Size.Size,
                                    ItemUom = item.Uom.Unit,
                                    Quantity = item.Quantity,
                                    Remark = "",
                                    SendQuantity = item.Quantity,
                                });
                            }
                            else // barcode sudah terdaftar
                            {
                                sPKDocsItems.Add(new SPKDocsItem
                                {
                                    ItemArticleRealizationOrder = viewModel.RONo,
                                    ItemCode = barcode,
                                    ItemDomesticCOGS = item.BasicPrice,
                                    ItemDomesticSale = item.BasicPrice + item.ComodityPrice,
                                    ItemId = itemx.Single()._id,
                                    ItemName = viewModel.Comodity.name,
                                    ItemSize = item.Size.Size,
                                    ItemUom = item.Uom.Unit,
                                    Quantity = item.Quantity,
                                    Remark = "",
                                    SendQuantity = item.Quantity,
                                });
                            }
                            itemIdx++;
                        }
                    }

                    var packingListCode = GeneratePackingList();

                    SPKDocs data = new SPKDocs()
                    {
                        Code = GenerateCode("EFR-PK/PBJ"),
                        Date = viewModel.FinishingOutDate,
                        DestinationId = (long)viewModel.UnitTo._id,
                        DestinationCode = viewModel.UnitTo.code,
                        DestinationName = viewModel.UnitTo.name,
                        IsDistributed = true,
                        IsReceived = false,
                        PackingList = packingListCode,
                        Password = "1",
                        Reference = packingListCode,
                        SourceId = (long)viewModel.Unit._id,
                        SourceCode = viewModel.Unit.code,
                        SourceName = viewModel.Unit.name,
                        Weight = 0,
                        Items = sPKDocsItems
                    };

                    foreach (var i in data.Items)
                    {
                        EntityExtension.FlagForCreate(i, username, USER_AGENT);
                    }
                    EntityExtension.FlagForCreate(data, username, USER_AGENT);
                    dbContext.Add(data);

                    Created = await dbContext.SaveChangesAsync();
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new Exception(e.Message);
                }
            }

            return Created;
        }

        public string GenerateCode(string ModuleId)
        {
            var uid = ObjectId.GenerateNewId().ToString();
            var hashids = new Hashids(uid, 8, "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890");
            var now = DateTime.Now;
            var begin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var diff = (now - begin).Milliseconds;
            string code = String.Format("{0}/{1}/{2}", hashids.Encode(diff), ModuleId, DateTime.Now.ToString("MM/yyyy"));
            return code;
        }

        public string GenerateBarcode(int idx, int sizeId)
        {
            string code = "barcode "+idx+sizeId;
            return code;
        }

        private List<ItemCoreViewModel> GetItem(string itemCode)
        {
            string itemUri = "items/finished-goods/Code";
            IHttpClientService httpClient = (IHttpClientService)serviceProvider.GetService(typeof(IHttpClientService));

            var response = httpClient.GetAsync($"{APIEndpoint.Core}{itemUri}/{itemCode}").Result;
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                List<ItemCoreViewModel> viewModel = JsonConvert.DeserializeObject<List<ItemCoreViewModel>>(result.GetValueOrDefault("data").ToString());
                //return viewModel.OrderByDescending(s => s.Date).FirstOrDefault(s => s.Date < doDate.AddDays(1)); ;
                return viewModel;
            }
            else
            {
                return null;
            }
        }

        public string GeneratePackingList() // nomor urut/EFR-FN/bulan/tahun
        {
            var generatedNo = "";
            var date = DateTime.Now;
            var lastSPKDoc = dbContext.SPKDocs.OrderByDescending(entity => entity.Id).FirstOrDefault(entity => entity.PackingList.Contains("EFR-FN"));
            string lastPackingListCode = "";

            if (lastSPKDoc != null)
            {
                lastPackingListCode = lastSPKDoc.PackingList;
                var code = lastPackingListCode.Split("/");
                int nomorUrut = int.Parse(code.ElementAt(0));
                nomorUrut++;

                generatedNo = $"{nomorUrut.ToString("0000")}/EFR-FN/{date.ToString("MM")}/{date.ToString("yy")}";
            }
            else
            {
                generatedNo = $"0001/EFR-FN/{date.ToString("MM")}/{date.ToString("yy")}";
            }

            return generatedNo;
        }
    }
}
