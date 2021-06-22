using Com.Bateeq.Service.Warehouse.Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Com.Bateeq.Service.Warehouse.Lib.ViewModels.InventoryViewModel
{
    public class InventoryByRoReportViewModel : BaseViewModel
    {
        public string StorageCode { get; set; }
        public string StorageName { get; set; }
        public string ItemArticleRealizationOrder { get; set; }
        public string ItemSize { get; set; }
        public decimal DateDiff { get; set; }
        public double StockQuantity { get; set; }
        public double SaleQuantity { get; set; }
    }
}
