using Com.Bateeq.Service.Warehouse.Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Com.Bateeq.Service.Warehouse.Lib.ViewModels.NewIntegrationViewModel
{
    class SalesDocByRoViewModel : BaseViewModel
    {
        public long StoreId { get; set; }
        public string StoreCode { get; set; }
        public string StoreName { get; set; }
        public long StoreStorageId { get; set; }
        public string StoreStorageCode { get; set; }
        public string StoreStorageName { get; set; }

        public string ItemCode { get; set; }
        public string ItemArticleRealizationOrder { get; set; }
        public string ItemSize { get; set; }
        public double Quantity { get; set; }
    }
}
