using System.ComponentModel;

namespace FTELSRCore.Enum
{
    public enum MemberAccessControl
    {
        /// <summary>
        /// MASTER SR
        /// </summary>
        ///
        [Description("MASTER SR")]
        Admin = 0,

        /// <summary>
        /// HỆ THỐNG TRANSACTION
        /// </summary>
        ///
        [Description("HỆ THỐNG TRANSACTION")]
        OWNER_TRANSACTION = 1,

        /// <summary>
        /// HỆ THỐNG SALE PLATFORM
        /// </summary>
        ///
        [Description("HỆ THỐNG SALE PLATFORM")]
        OWNER_SPF = 2,

        /// <summary>
        /// HỆ THỐNG OMNIAGENT
        /// </summary>
        ///
        [Description("HỆ THỐNG OMNIAGENT")]
        OWNER_MO = 3,

        /// <summary>
        /// HỆ THỐNG HIFPT
        /// </summary>
        ///
        [Description("HỆ THỐNG HIFPT")]
        OWNER_HIFPT = 4,

        /// <summary>
        /// HỆ THỐNG SR PROACTIVE
        /// </summary>
        ///
        [Description("HỆ THỐNG SR PROACTIVE")]
        OWNER_PROACTIVE = 5,

        /// <summary>
        /// HỆ THỐNG POLICYMANAGER
        /// </summary>
        ///
        [Description("HỆ THỐNG POLICYMANAGER")]
        OWNER_POLICYMANAGER = 6,

        /// <summary>
        /// HỆ THỐNG SOP
        /// </summary>
        ///
        [Description("HỆ THỐNG SOP")]
        OWNER_SOP = 7,
    }
}