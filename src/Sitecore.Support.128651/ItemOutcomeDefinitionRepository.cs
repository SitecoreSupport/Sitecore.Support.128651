using Sitecore.Common;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Marketing.Core;
using Sitecore.Marketing.Definitions.Outcomes;
using Sitecore.Marketing.Definitions.Outcomes.Data;
using Sitecore.Marketing.Definitions.Outcomes.Model;
using Sitecore.Marketing.Definitions.Repository;
using Sitecore.Marketing.Taxonomy.Extensions;
using Sitecore.Marketing.Taxonomy.Model.OutcomeGroup;
using Sitecore.Resources.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Sitecore.Support.Marketing.Definitions.Outcomes.Data.ItemDb
{
  public class ItemOutcomeDefinitionRepository : ItemDefinitionRepositoryBase<OutcomeDefinitionRecord>, IOutcomeDefinitionRepository, IDefinitionRepository<OutcomeDefinitionRecord>
  {
    public class OutcomeMapper : GeneralFieldsMapper<OutcomeDefinitionRecord>
    {
      public OutcomeMapper() : this(WellKnownIdentifiers.Fields.Classification.AllFieldIds.ToList<ID>().AsReadOnly(), WellKnownIdentifiers.Fields.CustomValues.AllFieldIds.ToList<ID>().AsReadOnly())
      {
      }

      public OutcomeMapper(IReadOnlyCollection<ID> classificationFields, IReadOnlyCollection<ID> customValuesFields) : base(classificationFields, customValuesFields)
      {
        Assert.ArgumentNotNull(classificationFields, "classificationFields");
        Assert.ArgumentNotNull(customValuesFields, "customValuesFields");
      }

      public override OutcomeDefinitionRecord Map(Item item, bool includeCultureSpecificFields)
      {
        Assert.ArgumentNotNull(item, "item");
        OutcomeDefinitionRecord record = base.Map(item, includeCultureSpecificFields);
        string str = item[WellKnownIdentifiers.Fields.Classification.OutcomeGroupId];
        record.TypeId = string.IsNullOrEmpty(str) ? ID.Null : new ID(item[WellKnownIdentifiers.Fields.Classification.OutcomeGroupId]);
        record.Name = includeCultureSpecificFields ? item[WellKnownIdentifiers.Fields.Name] : string.Empty;
        record.IsMonetaryValueApplicable = MainUtil.GetBool(item[WellKnownIdentifiers.Fields.MonetaryValueApplicable], false);
        record.AdditionalRegistrationsAreIgnored = MainUtil.GetBool(item[WellKnownIdentifiers.Fields.AdditionalRegistrationsAreIgnored], false);
        record.Description = includeCultureSpecificFields ? item[WellKnownIdentifiers.Fields.Description] : string.Empty;
        return record;
      }

      public override void SetCultureInvariantFields(OutcomeDefinitionRecord source, Item target)
      {
        Assert.ArgumentNotNull(source, "source");
        Assert.ArgumentNotNull(target, "target");
        base.SetCultureInvariantFields(source, target);
        target[WellKnownIdentifiers.Fields.MonetaryValueApplicable] = System.Convert.ToByte(source.IsMonetaryValueApplicable).ToString();
        target[WellKnownIdentifiers.Fields.AdditionalRegistrationsAreIgnored] = System.Convert.ToByte(source.AdditionalRegistrationsAreIgnored).ToString();
        target[WellKnownIdentifiers.Fields.AdditionalRegistrationsAreIgnored] = System.Convert.ToByte(source.AdditionalRegistrationsAreIgnored).ToString();
      }

      public override void SetCultureSpecificFields(OutcomeDefinitionRecord source, Item target)
      {
        Assert.ArgumentNotNull(source, "source");
        Assert.ArgumentNotNull(target, "target");
        base.SetCultureSpecificFields(source, target);
        target[WellKnownIdentifiers.Fields.Description] = source.Description;
        target[WellKnownIdentifiers.Fields.Name] = source.Name;
      }

    }

    private static readonly ID OutcomeContainerId = WellKnownIdentifiers.MarketingCenterOutcomeContainerId;

    private static readonly ID OutcomeTemplateId = WellKnownIdentifiers.OutcomeDefinitionTemplateId;

    private readonly Database itemDb;

    public ItemOutcomeDefinitionRepository(string databaseName) : this(databaseName, false)
    {
      Assert.ArgumentNotNull(databaseName, "databaseName");
    }

    public ItemOutcomeDefinitionRepository(string databaseName, bool assumeActive) : this(databaseName, assumeActive, new ItemOutcomeDefinitionRepository.OutcomeMapper())
    {
      Assert.ArgumentNotNull(databaseName, "databaseName");
    }

    public ItemOutcomeDefinitionRepository(string databaseName, IDefinitionRecordMapper<OutcomeDefinitionRecord> mapper) : this(databaseName, false, mapper)
    {
      Assert.ArgumentNotNull(databaseName, "databaseName");
      Assert.ArgumentNotNull(mapper, "mapper");
    }

    public ItemOutcomeDefinitionRepository(string databaseName, bool assumeActive, IDefinitionRecordMapper<OutcomeDefinitionRecord> mapper) : base(Assert.ResultNotNull<Database>(Database.GetDatabase(databaseName), "database not found " + databaseName), ItemOutcomeDefinitionRepository.OutcomeTemplateId, ItemOutcomeDefinitionRepository.OutcomeContainerId, assumeActive, mapper)
    {
      Assert.ArgumentNotNull(databaseName, "databaseName");
      Assert.ArgumentNotNull(mapper, "mapper");
      this.itemDb = Assert.ResultNotNull<Database>(Database.GetDatabase(databaseName), "database not found " + databaseName);
      Assert.IsNotNull(this.itemDb, "item database '{0}' should be available", new object[]
      {
        databaseName
      });
      Item item = this.itemDb.GetItem(ItemOutcomeDefinitionRepository.OutcomeContainerId);
      Assert.IsNotNull(item, "analytics root item should exist in the database");
    }

    public override IMarketingImage GetImage(ID definitionId, CultureInfo cultureInfo)
    {
      Assert.ArgumentNotNull(definitionId, "definitionId");
      Assert.ArgumentNotNull(cultureInfo, "cultureInfo");
      Item latestApprovedVersionForItemId = base.GetLatestApprovedVersionForItemId(this.itemDb, ItemOutcomeDefinitionRepository.OutcomeTemplateId, definitionId, cultureInfo);
      if(latestApprovedVersionForItemId == null)
      {
        throw new ArgumentException(string.Format("Outcome definition with ID {0} cannot be found.", definitionId));
      }
      ImageField imageField = latestApprovedVersionForItemId.Fields[WellKnownIdentifiers.Fields.Image];
      Guid imageId = imageField.MediaID.ToGuid();
      return this.GetImageByItemId(imageId, cultureInfo);
    }

    public override void SaveImage(ID definitionId, IMarketingImage image)
    {
      Assert.ArgumentNotNull(definitionId, "definitionId");
      Assert.ArgumentNotNull(image, "image");
      throw new NotImplementedException("This implementation of the repository does not support saving images");
    }

    public override void DeleteImage(ID definitionId)
    {
      Assert.ArgumentNotNull(definitionId, "definitionId");
      throw new NotImplementedException("This implementation of the repository does not support deleting images");
    }

    [Obsolete("Use OutcomeGroupTaxonomyManager to save Outcome group (formerly known as type).")]
    public virtual void SaveType(IOutcomeDefinitionType type)
    {
      Assert.ArgumentNotNull(type, "type");
      throw new NotImplementedException("This implementation of the repository does not support saving types");
    }

    [Obsolete("Use OutcomeGroupTaxonomyManager to delete Outcome group (formerly known as type).")]
    public virtual void DeleteType(ID typeId)
    {
      Assert.ArgumentNotNull(typeId, "typeId");
      throw new NotImplementedException("This implementation of the repository does not support deleting types");
    }

    [Obsolete("Outcome group (formerly known as type) is a classification that can be retrieved using OutcomeGroupTaxonomyManager and IOutcomeDefinition.OutcomeGroupUri.")]
    public virtual IOutcomeDefinitionType GetType(ID typeId)
    {
      Assert.ArgumentNotNull(typeId, "typeId");
      Log.Warn("IOutcomeDefinitionType is retrieved from OutcomeGroup item taxonomy instead of OutcomeType tree.", this);
      Sitecore.Marketing.Taxonomy.ITaxonomyManagerProvider provider = Sitecore.Marketing.Taxonomy.TaxonomyManager.Provider;
      Assert.Required(provider, "TaxonomyManager.Provider has not been initialized.");
      Sitecore.Marketing.Taxonomy.OutcomeGroupTaxonomyManager outcomeGroupManager = TaxonomyManagerProviderExtensions.GetOutcomeGroupManager(provider);
      Assert.Required(outcomeGroupManager, "TaxonomyManager.Provider.GetOutcomeGroupManager() is required (returned null).");
      OutcomeGroup outcomeGroup = outcomeGroupManager.GetOutcomeGroup(typeId, CultureInfo.InvariantCulture);
      if(outcomeGroup == null)
      {
        return null;
      }
      return this.ConvertFromOutcomeGroupTaxon(outcomeGroup);
    }

    [Obsolete("Use OutcomeGroupTaxonomyManager to retrieve Outcome groups (formerly known as types).")]
    public virtual IReadOnlyCollection<IOutcomeDefinitionType> GetAllTypes()
    {
      Log.Warn("IOutcomeDefinitionType collection is retrieved from OutcomeGroup item taxonomy instead of OutcomeType tree.", this);
      Sitecore.Marketing.Taxonomy.ITaxonomyManagerProvider provider = Sitecore.Marketing.Taxonomy.TaxonomyManager.Provider;
      Assert.Required(provider, "TaxonomyManager.Provider has not been initialized.");
      Sitecore.Marketing.Taxonomy.OutcomeGroupTaxonomyManager outcomeGroupManager = TaxonomyManagerProviderExtensions.GetOutcomeGroupManager(provider);
      Assert.Required(outcomeGroupManager, "TaxonomyManager.Provider.GetOutcomeGroupManager() is required (returned null).");
      OutcomeGroupTaxonomy taxonomy = outcomeGroupManager.GetTaxonomy(CultureInfo.InvariantCulture);
      Assert.Required(taxonomy, "TaxonomyManager.Provider.GetOutcomeGroupManager().GetTaxonomy(CultureInfo.InvariantCulture) is required (returned null)");
      IEnumerable<OutcomeGroup> outcomeGroups = taxonomy.OutcomeGroups;
      return outcomeGroups.Select(new Func<OutcomeGroup, IOutcomeDefinitionType>(this.ConvertFromOutcomeGroupTaxon)).ToList<IOutcomeDefinitionType>().AsReadOnly();
    }

    [Obsolete("Use CreateRecordFromItem(Item) instead.")]
    protected virtual OutcomeDefinitionRecord CreateDefinitionFromItem(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      return this.CreateRecordFromItem(item, true);
    }

    [Obsolete("Use OutcomeTypeTaxonomyManager to retrieve Outcome groups (formerly known as Outcome types).")]
    protected virtual IOutcomeDefinitionType CreateTypeFromItem(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      OutcomeDefinitionType outcomeDefinitionType = new OutcomeDefinitionType();
      outcomeDefinitionType.Id = item.ID;
      outcomeDefinitionType.Name = item["name"];
      return outcomeDefinitionType;
    }

    protected virtual IMarketingImage GetImageByItemId(Guid imageId, CultureInfo cultureInfo)
    {
      Assert.ArgumentNotNull(cultureInfo, "cultureInfo");
      if(imageId == Guid.Empty)
      {
        return null;
      }
      MediaItem mediaItem = base.GetLatestApprovedVersionForImageItemId(this.itemDb, TypeExtensions.ToID(imageId), cultureInfo);
      Assert.IsNotNull(mediaItem, "Image item with ID {0} cannot be found.", new object[]
      {
        imageId
      });
      string mimeType = MediaManager.MimeResolver.GetMimeType(mediaItem);
      MediaStream stream = MediaManager.GetMedia(mediaItem).GetStream();
      Assert.IsNotNull(stream, string.Format("Could not get media stream for image with id: [{0}].", imageId));
      Stream stream2 = stream.Stream;
      byte[] array = new byte[stream2.Length];
      stream2.Read(array, 0, (int)stream2.Length);
      return new MarketingImage(array, mimeType);
    }

    [Obsolete("Only used by other obsoleted methods within this class.")]
    private IOutcomeDefinitionType ConvertFromOutcomeGroupTaxon(OutcomeGroup entity)
    {
      Assert.ArgumentNotNull(entity, "entity");
      OutcomeDefinitionType outcomeDefinitionType = new OutcomeDefinitionType();
      outcomeDefinitionType.Id = entity.Id;
      outcomeDefinitionType.Name = entity.Name;
      return outcomeDefinitionType;
    }
  }
}
