using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;
using static FTELSRCore.Helpers.JSonParseHelpers;

namespace FTELSRCore.Abstractions.Entities
{
    [BsonIgnoreExtraElements]
    public class BaseEntityMongoDB
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        private string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonRepresentation(BsonType.Boolean)]
        [BsonElement(nameof(IsDeleted))]
        public bool IsDeleted { get; set; } = false;
    }

    /// <summary>
    /// Base dành cho đẩy đủ thông tin tạo và cập nhật.
    /// </summary>
    ///
    [BsonIgnoreExtraElements]
    public abstract class EntityFullCreatedAndModifiedBase : BaseEntityMongoDB
    {
        [BsonElement(nameof(CreatedUser))]
        public string CreatedUser { get; set; }

        [BsonElement(nameof(CreatedDate))]
        //[BsonRepresentation(BsonType.DateTime)]
        //[BsonSerializer(typeof(VietnamDateTimeSerializer))]
        [JsonConverter(typeof(DateTimeNullAbleConverter))]
        public DateTime? CreatedDate { get; set; }

        [BsonElement(nameof(CreatedUserCode))]
        public string CreatedUserCode { get; set; }

        [BsonElement(nameof(CreatedUserOrganization))]
        public string CreatedUserOrganization { get; set; }

        [BsonElement(nameof(CreatedUserRegionId))]
        public int? CreatedUserRegionId { get; set; }

        [BsonElement(nameof(CreatedUserLocationId))]
        public int? CreatedUserLocationId { get; set; }

        [BsonElement(nameof(CreatedUserBranchId))]
        public int? CreatedUserBranchId { get; set; }

        [BsonElement(nameof(ModifiedUser))]
        public string ModifiedUser { get; set; }

        [BsonElement(nameof(ModifiedDate))]
        //[BsonRepresentation(BsonType.DateTime)]
        //[BsonSerializer(typeof(VietnamDateTimeSerializer))]
        [JsonConverter(typeof(DateTimeNullAbleConverter))]
        public DateTime? ModifiedDate { get; set; }

        [BsonElement(nameof(ModifiedUserCode))]
        public string ModifiedUserCode { get; set; }

        [BsonElement(nameof(ModifiedUserOrganization))]
        public string ModifiedUserOrganization { get; set; }

        [BsonElement(nameof(ModifiedUserRegionId))]
        public int? ModifiedUserRegionId { get; set; }

        [BsonElement(nameof(ModifiedUserLocationId))]
        public int? ModifiedUserLocationId { get; set; }

        [BsonElement(nameof(ModifiedUserBranchId))]
        public int? ModifiedUserBranchId { get; set; }
    }

    /// <summary>
    /// Base dành cho đẩy đủ thông tin tạo.
    /// </summary>
    ///
    [BsonIgnoreExtraElements]
    public abstract class EntityFullCreatedBase : BaseEntityMongoDB
    {
        [BsonElement(nameof(CreatedUser))]
        public string CreatedUser { get; set; }

        [BsonElement(nameof(CreatedDate))]
        //[BsonRepresentation(BsonType.DateTime)]
        //[BsonSerializer(typeof(VietnamDateTimeSerializer))]
        [JsonConverter(typeof(DateTimeNullAbleConverter))]
        public DateTime? CreatedDate { get; set; }

        [BsonElement(nameof(CreatedUserCode))]
        public string CreatedUserCode { get; set; }

        [BsonElement(nameof(CreatedUserOrganization))]
        public string CreatedUserOrganization { get; set; }

        [BsonElement(nameof(CreatedUserRegionId))]
        public int? CreatedUserRegionId { get; set; }

        [BsonElement(nameof(CreatedUserLocationId))]
        public int? CreatedUserLocationId { get; set; }

        [BsonElement(nameof(CreatedUserBranchId))]
        public int? CreatedUserBranchId { get; set; }
    }

    /// <summary>
    /// Base dành cho thông tin tạo không bao gồm địa bàn.
    /// </summary>
    ///
    [BsonIgnoreExtraElements]
    public abstract class EntityCreatedNotHaveAreaBase : BaseEntityMongoDB
    {
        [BsonElement(nameof(CreatedUser))]
        public string CreatedUser { get; set; }

        [BsonElement(nameof(CreatedDate))]
        //[BsonRepresentation(BsonType.DateTime)]
        //[BsonSerializer(typeof(VietnamDateTimeSerializer))]
        [JsonConverter(typeof(DateTimeNullAbleConverter))]
        public DateTime? CreatedDate { get; set; }

        [BsonElement(nameof(CreatedUserCode))]
        public string CreatedUserCode { get; set; }

        [BsonElement(nameof(CreatedUserOrganization))]
        public string CreatedUserOrganization { get; set; }
    }

    /// <summary>
    /// Base dành cho thông tin tạo và cập nhật không bao gồm địa bàn.
    /// </summary>
    ///
    [BsonIgnoreExtraElements]
    public abstract class EntityCreatedAndModifiedNotHaveAreaBase : BaseEntityMongoDB
    {
        [BsonElement(nameof(CreatedUser))]
        public string CreatedUser { get; set; }

        [BsonElement(nameof(CreatedDate))]
        //[BsonRepresentation(BsonType.DateTime)]
        //[BsonSerializer(typeof(VietnamDateTimeSerializer))]
        [JsonConverter(typeof(DateTimeNullAbleConverter))]
        public DateTime? CreatedDate { get; set; }

        [BsonElement(nameof(CreatedUserCode))]
        public string CreatedUserCode { get; set; }

        [BsonElement(nameof(CreatedUserOrganization))]
        public string CreatedUserOrganization { get; set; }

        [BsonElement(nameof(ModifiedUser))]
        public string ModifiedUser { get; set; }

        [BsonElement(nameof(ModifiedDate))]
        //[BsonRepresentation(BsonType.DateTime)]
        //[BsonSerializer(typeof(VietnamDateTimeSerializer))]
        [JsonConverter(typeof(DateTimeNullAbleConverter))]
        public DateTime? ModifiedDate { get; set; }

        [BsonElement(nameof(ModifiedUserCode))]
        public string ModifiedUserCode { get; set; }

        [BsonElement(nameof(ModifiedUserOrganization))]
        public string ModifiedUserOrganization { get; set; }
    }
}