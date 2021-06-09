using Com.Bateeq.Service.Warehouse.Lib.Helpers;
using Com.Bateeq.Service.Warehouse.Lib.Models.InventoryModel;
using Com.Bateeq.Service.Warehouse.Lib.Models.SPKDocsModel;
using Com.Bateeq.Service.Warehouse.Lib.Models.TransferModel;
using Com.Moonlay.Models;
using Com.Moonlay.NetCore.Lib;
using HashidsNet;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.Bateeq.Service.Warehouse.Lib.Facades.Stores
{
    public class TransferInStoreFacade
    {
        private string USER_AGENT = "Facade";

        private readonly WarehouseDbContext dbContext;
        private readonly DbSet<TransferInDoc> dbSet;
        private readonly DbSet<SPKDocs> dbSetSpk;
        private readonly IServiceProvider serviceProvider;
        private readonly DbSet<Inventory> dbSetInventory;
        private readonly DbSet<InventoryMovement> dbSetInventoryMovement;

        public TransferInStoreFacade(IServiceProvider serviceProvider, WarehouseDbContext dbContext)
        {
            this.serviceProvider = serviceProvider;
            this.dbContext = dbContext;
            this.dbSet = dbContext.Set<TransferInDoc>();
            this.dbSetInventory = dbContext.Set<Inventory>();
            this.dbSetSpk = dbContext.Set<SPKDocs>();
            this.dbSetInventoryMovement = dbContext.Set<InventoryMovement>();
        }

        public Tuple<List<TransferInDoc>, int, Dictionary<string, string>> Read(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}")
        {
            IQueryable<TransferInDoc> Query = this.dbSet.Include(m => m.Items).Where(x=>x.DestinationCode != "GDG.05").OrderByDescending(x => x.CreatedUtc);

            List<string> searchAttributes = new List<string>()
            {
                "Code"
            };

            Query = QueryHelper<TransferInDoc>.ConfigureSearch(Query, searchAttributes, Keyword);

            Dictionary<string, string> FilterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Filter);
            Query = QueryHelper<TransferInDoc>.ConfigureFilter(Query, FilterDictionary);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            Query = QueryHelper<TransferInDoc>.ConfigureOrder(Query, OrderDictionary);

            Pageable<TransferInDoc> pageable = new Pageable<TransferInDoc>(Query, Page - 1, Size);
            List<TransferInDoc> Data = pageable.Data.ToList();
            int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, TotalData, OrderDictionary);
        }

        public Tuple<List<SPKDocs>, int, Dictionary<string, string>> ReadPending(int Page = 1, int Size = 25, string Order = "{}", string Keyword = null, string Filter = "{}")
        {
            //IQueryable<SPKDocs> Query = this.dbSetSpk.Include(m => m.Items).Where(i=>i.IsDistributed == true && i.IsReceived == false);
            IQueryable<SPKDocs> Query = this.dbSetSpk.Include(m => m.Items).Where(i => i.IsDistributed == true && i.IsReceived == false && i.DestinationName != "FINISHING" && i.SourceName != "FINISHING");

            List<string> searchAttributes = new List<string>()
            {
                "Code",
                "Reference",
                "PackingList"
            };

            foreach(var i in Query)
            {
                if (/*i.Reference != null || i.Reference != ""*/ !String.IsNullOrWhiteSpace(i.Reference) && i.Reference.Contains("RTT"))
                {
                    var transferout = dbContext.TransferOutDocs.Where(x => x.Code == i.Reference).FirstOrDefault();
                    if (transferout != null)
                    {
                        i.SourceId = transferout.SourceId;
                        i.SourceCode = transferout.SourceCode;
                        i.SourceName = transferout.SourceName;
                        i.DestinationId = transferout.DestinationId;
                        i.DestinationName = transferout.DestinationName;
                        i.DestinationCode = transferout.DestinationCode;
                    }
                    else
                    {
                        i.SourceId = 0;
                        i.SourceCode = "-";
                        i.SourceName = "-";
                        i.DestinationId = 0;
                        i.DestinationName = "-";
                        i.DestinationCode = "-";
                    }
                }
            }

            Query = QueryHelper<SPKDocs>.ConfigureSearch(Query, searchAttributes, Keyword);

            Dictionary<string, string> FilterDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Filter);
            Query = QueryHelper<SPKDocs>.ConfigureFilter(Query, FilterDictionary);

            Dictionary<string, string> OrderDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Order);
            Query = QueryHelper<SPKDocs>.ConfigureOrder(Query, OrderDictionary);

            Pageable<SPKDocs> pageable = new Pageable<SPKDocs>(Query, Page - 1, Size);
            List<SPKDocs> Data = pageable.Data.ToList();
            int TotalData = pageable.TotalCount;

            return Tuple.Create(Data, TotalData, OrderDictionary);
        }

        public TransferInDoc ReadById(int id)
        {
            var model = dbSet.Where(m => m.Id == id)
                 .Include(m => m.Items)
                 .FirstOrDefault();
            return model;
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

        public async Task<int> Create(TransferInDoc model, string username, int clientTimeZoneOffset = 7)
        {
            int Created = 0;

            using (var transaction = this.dbContext.Database.BeginTransaction())
            {
                try
                {
                    string code = GenerateCode("EFR-TB/BBT");
                    model.Code = code;
                    var SPK = dbContext.SPKDocs.Where(x => x.PackingList == model.Reference).Single();
                    SPK.IsReceived = true;
                    var Id = SPK.Id;
                    EntityExtension.FlagForCreate(model, username, USER_AGENT);
                    foreach (var i in model.Items)
                    {
                        i.Id = 0;
                        EntityExtension.FlagForCreate(i, username, USER_AGENT);
                        var SPKItems = dbContext.SPKDocsItems.Where(x => x.ItemArticleRealizationOrder == i.ArticleRealizationOrder && x.ItemCode == i.ItemCode && i.ItemName == i.ItemName && x.SPKDocsId == Id).Single();
                        SPKItems.SendQuantity = i.Quantity;
                        var inventorymovement = new InventoryMovement();
                        var inven = dbContext.Inventories.Where(x => x.ItemId == i.ItemId && x.StorageId == model.DestinationId).FirstOrDefault();
                        if (inven != null)
                        {
                            inventorymovement.Before = inven.Quantity;
                            inven.Quantity = inven.Quantity + i.Quantity;//inven.Quantity + i.quantity;
                                                                         //dbSetInventory.Update(inven);
                        }
                        else
                        {
                            Inventory inventory = new Inventory
                            {
                                ItemArticleRealizationOrder = i.ArticleRealizationOrder,
                                ItemCode = i.ItemCode,
                                ItemDomesticCOGS = i.DomesticCOGS,
                                ItemDomesticRetail = i.DomesticRetail,
                                ItemDomesticSale = i.DomesticSale,
                                ItemDomesticWholeSale = i.DomesticWholeSale,
                                ItemId = i.ItemId,
                                ItemInternationalCOGS = 0,
                                ItemInternationalRetail = 0,
                                ItemInternationalSale = 0,
                                ItemInternationalWholeSale = 0,
                                ItemName = i.ItemName,
                                ItemSize = i.Size,
                                ItemUom = i.Uom,
                                Quantity = i.Quantity,
                                StorageCode = model.DestinationCode,
                                StorageId = model.DestinationId,
                                StorageName = model.DestinationName,
                                StorageIsCentral = model.DestinationName.Contains("GUDANG") ? true : false,
                            };
                            EntityExtension.FlagForCreate(inventory, username, USER_AGENT);
                            dbSetInventory.Add(inventory);
                        }

                        inventorymovement.After = inventorymovement.Before + i.Quantity;
                        inventorymovement.Date = DateTimeOffset.UtcNow;
                        inventorymovement.ItemCode = i.ItemCode;
                        inventorymovement.ItemDomesticCOGS = i.DomesticCOGS;
                        inventorymovement.ItemDomesticRetail = i.DomesticRetail;
                        inventorymovement.ItemDomesticWholeSale = i.DomesticRetail;
                        inventorymovement.ItemDomesticSale = i.DomesticSale;
                        inventorymovement.ItemId = i.ItemId;
                        inventorymovement.ItemInternationalCOGS = 0;
                        inventorymovement.ItemInternationalRetail = 0;
                        inventorymovement.ItemInternationalSale = 0;
                        inventorymovement.ItemInternationalWholeSale = 0;
                        inventorymovement.ItemName = i.ItemName;
                        inventorymovement.ItemSize = i.Size;
                        inventorymovement.ItemUom = i.Uom;
                        inventorymovement.Quantity = i.Quantity;
                        inventorymovement.StorageCode = model.DestinationCode;
                        inventorymovement.StorageId = model.DestinationId;
                        inventorymovement.StorageName = model.DestinationName;
                        inventorymovement.Type = "IN";
                        inventorymovement.Reference = code;
                        inventorymovement.Remark = model.Remark;
                        inventorymovement.StorageIsCentral = model.DestinationName.Contains("GUDANG") ? true : false;
                        EntityExtension.FlagForCreate(inventorymovement, username, USER_AGENT);
                        dbSetInventoryMovement.Add(inventorymovement);

                    }

                    dbSet.Add(model);
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




    }
}
