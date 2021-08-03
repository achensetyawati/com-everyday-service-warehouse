using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Com.Bateeq.Service.Warehouse.Lib.Utilities;
using Com.Bateeq.Service.Warehouse.Lib.ViewModels.NewIntegrationViewModel;

namespace Com.Bateeq.Service.Warehouse.Lib.ViewModels.SpkDocsViewModel
{
    public class SPKDocsFromFinihsingOutsViewModel : BaseViewModel, IValidatableObject
    {   
        public DateTimeOffset FinishingOutDate { get; set; }
        public DestinationViewModel UnitTo { get; set; }
        public SourceViewModel Unit { get; set; }
        public string PackingList { get; set; }
        public string Password { get; set; }
        public bool IsDifferentSize { get; set; }
        public int Weight { get; set; }
        public Comodity Comodity { get; set; }
        public ItemArticleProcesViewModel process { get; set; }
        public ItemArticleMaterialViewModel materials { get; set; }
        public ItemArticleMaterialCompositionViewModel materialCompositions { get; set; }
        public ItemArticleCollectionViewModel collections { get; set; }
        public ItemArticleSeasonViewModel seasons { get; set; }
        public ItemArticleCounterViewModel counters { get; set; }
        public ItemArticleSubCounterViewModel subCounters { get; set; }
        public ItemArticleCategoryViewModel categories { get; set; }
        public ItemArticleColorViewModel color { get; set; }
        public string RONo { get; set; }
        public List<SPKDocItemsFromFinihsingOutsViewModel> Items { get; set; }
        public string ImagePath { get; set; }
        public string ImgFile { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (this.FinishingOutDate.Equals(DateTimeOffset.MinValue) || this.FinishingOutDate == null)
            {
                yield return new ValidationResult("Date is required", new List<string> { "date" });
            }
            if (this.UnitTo == null)
            {
                yield return new ValidationResult("Destination is required", new List<string> { "destination" });
            }
            if (this.Unit == null)
            {
                yield return new ValidationResult("Source is required", new List<string> { "source" });
            }
        }
    }

    public class Comodity
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
    }
}
