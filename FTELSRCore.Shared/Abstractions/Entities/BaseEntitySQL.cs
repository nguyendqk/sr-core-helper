namespace FTELSRCore.Abstractions.Entities
{
    public interface IEntityCreatedAndModifiedNotHaveAreaBase<T> : IBaseEntitySQL
    {
        public T Id { get; set; }
    }

    public interface IEntityFullCreatedAndModifiedBase<T> : IBaseEntitySQL
    {
        public T Id { get; set; }

        public int? CreatedUserRegionId { get; set; }
        public int? CreatedUserLocationId { get; set; }
        public int? CreatedUserBranchId { get; set; }

        public int? ModifiedUserRegionId { get; set; }
        public int? ModifiedUserLocationId { get; set; }
        public int? ModifiedUserBranchId { get; set; }
    }

    public interface IBaseEntitySQL
    {
        public bool IsDeleted { get; set; }

        public string CreatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedUserCode { get; set; }
        public string CreatedUserOrganization { get; set; }

        public string ModifiedUser { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedUserCode { get; set; }
        public string ModifiedUserOrganization { get; set; }
    }

    public interface IEntityCreatedAndModifiedShortBase<T>
    {
        public T Id { get; set; }

        public bool IsDeleted { get; set; }

        public string CreatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }

        public string ModifiedUser { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}