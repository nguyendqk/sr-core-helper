using System.ComponentModel;
using System.Reflection;

namespace FTELSRCore.Constants.Permissions
{
    public static class SRTypePermissions
    {
        [DisplayName("Request")]
        [Description("Yêu cầu")]
        public static class Requests
        {
            /*----------------------------------------------------------------*/

            #region ::::::::::::::::::::::::: LEVEL 1 :::::::::::::::::::::::::

            public const string REQUEST_VIEW = $"{CommonBaseConstant.APPCode}.REQUEST.{SRTypeActions.VIEW}";

            #endregion ::::::::::::::::::::::::: LEVEL 1 :::::::::::::::::::::::::

            /*----------------------------------------------------------------*/

            #region ::::::::::::::::::::::::: LEVEL 2 :::::::::::::::::::::::::

            #region -------------------------- CREATE --------------------------

            public const string CREATE_VIEW = $"{CommonBaseConstant.APPCode}.REQUEST.CREATE.{SRTypeActions.VIEW}";

            public const string CREATE_UPLOAD = $"{CommonBaseConstant.APPCode}.REQUEST.CREATE.{SRTypeActions.UPLOAD}";

            public const string CREATE_CREATE = $"{CommonBaseConstant.APPCode}.REQUEST.CREATE.{SRTypeActions.CREATE}";

            public const string CREATE_SENDMAIL = $"{CommonBaseConstant.APPCode}.REQUEST.CREATE.{SRTypeActions.SENDMAIL}";

            #endregion -------------------------- CREATE --------------------------

            #region -------------------------- DETAIL --------------------------

            public const string DETAIL_MOVE = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.{SRTypeActions.MOVE}";

            public const string DETAIL_VIEW = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.{SRTypeActions.VIEW}";

            public const string DETAIL_ASSIGN = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.{SRTypeActions.ASSIGN}";

            public const string DETAIL_CANCEL = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.{SRTypeActions.CANCEL}";

            public const string DETAIL_UPDATE = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.{SRTypeActions.UPDATE}";

            public const string DETAIL_RECEIVE = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.{SRTypeActions.RECEIVE}";

            #endregion -------------------------- DETAIL --------------------------

            #endregion ::::::::::::::::::::::::: LEVEL 2 :::::::::::::::::::::::::

            /*----------------------------------------------------------------*/

            #region ::::::::::::::::::::::::: LEVEL 3 :::::::::::::::::::::::::

            #region -------------------------- TICKET --------------------------

            public const string TICKET_VIEW = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.TICKET.{SRTypeActions.VIEW}";

            public const string TICKET_CREATE = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.TICKET.{SRTypeActions.CREATE}";

            #endregion -------------------------- TICKET --------------------------

            #region -------------------------- HISTORY --------------------------

            public const string HISTORY_VIEW = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.HISTORY.{SRTypeActions.VIEW}";

            #endregion -------------------------- HISTORY --------------------------

            #region -------------------------- PROCESS --------------------------

            public const string PROCESS_VIEW = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.PROCESS.{SRTypeActions.VIEW}";

            #endregion -------------------------- PROCESS --------------------------

            #region -------------------------- DOCUMENT --------------------------

            public const string DOCUMENT_VIEW = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.DOCUMENT.{SRTypeActions.VIEW}";

            public const string DOCUMENT_UPLOAD = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.DOCUMENT.{SRTypeActions.UPLOAD}";

            public const string DOCUMENT_DELETE = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.DOCUMENT.{SRTypeActions.DELETE}";

            public const string DOCUMENT_DOWNLOAD = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.DOCUMENT.{SRTypeActions.DOWNLOAD}";

            #endregion -------------------------- DOCUMENT --------------------------

            #region -------------------------- WORKFLOW --------------------------

            public const string WORKFLOW_VIEW = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.WORKFLOW.{SRTypeActions.VIEW}";

            #endregion -------------------------- WORKFLOW --------------------------

            #region -------------------------- DISCUSSION --------------------------

            public const string DISCUSSION_VIEW = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.DISCUSSION.{SRTypeActions.VIEW}";

            public const string DISCUSSION_UPLOAD = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.DISCUSSION.{SRTypeActions.UPLOAD}";

            public const string DISCUSSION_DOWNLOAD = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.DISCUSSION.{SRTypeActions.DOWNLOAD}";// TODO

            public const string DISCUSSION_DELETE = $"{CommonBaseConstant.APPCode}.REQUEST.DETAIL.DISCUSSION.{SRTypeActions.DELETE}";// TODO

            #endregion -------------------------- DISCUSSION --------------------------

            #endregion ::::::::::::::::::::::::: LEVEL 3 :::::::::::::::::::::::::

            /*----------------------------------------------------------------*/
        }

        [DisplayName("Ticket")]
        [Description("Ticket")]
        public static class Tickets
        {
            /*----------------------------------------------------------------*/

            #region ::::::::::::::::::::::::: LEVEL 1 :::::::::::::::::::::::::

            public const string TICKET_VIEW = $"{CommonBaseConstant.APPCode}.TICKET.{SRTypeActions.VIEW}";

            #endregion ::::::::::::::::::::::::: LEVEL 1 :::::::::::::::::::::::::

            /*----------------------------------------------------------------*/

            #region ::::::::::::::::::::::::: LEVEL 2 :::::::::::::::::::::::::

            public const string DETAIL_VIEW = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.{SRTypeActions.VIEW}";

            public const string DETAIL_UPDATE = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.{SRTypeActions.UPDATE}";

            public const string DETAIL_ASSIGN = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.{SRTypeActions.ASSIGN}";

            public const string DETAIL_RECEIVE = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.{SRTypeActions.RECEIVE}";

            public const string DETAIL_APPROVE = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.{SRTypeActions.APPROVE}";

            public const string DETAIL_CANCEL = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.{SRTypeActions.CANCEL}";

            #endregion ::::::::::::::::::::::::: LEVEL 2 :::::::::::::::::::::::::

            /*----------------------------------------------------------------*/

            #region ::::::::::::::::::::::::: LEVEL 3 :::::::::::::::::::::::::

            #region -------------------------- SUBTICKET --------------------------

            public const string SUBTICKET_VIEW = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.SUBTICKET.{SRTypeActions.VIEW}";

            public const string SUBTICKET_CREATE = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.SUBTICKET.{SRTypeActions.CREATE}";

            #endregion -------------------------- SUBTICKET --------------------------

            #region -------------------------- DISCUSSION --------------------------

            public const string DISCUSSION_VIEW = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.DISCUSSION.{SRTypeActions.VIEW}";

            public const string DISCUSSION_UPLOAD = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.DISCUSSION.{SRTypeActions.UPLOAD}";

            public const string DISCUSSION_DELETE = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.DISCUSSION.{SRTypeActions.DELETE}"; // TODO

            public const string DISCUSSION_DOWNLOAD = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.DISCUSSION.{SRTypeActions.DOWNLOAD}"; // TODO

            #endregion -------------------------- DISCUSSION --------------------------

            #region -------------------------- DOCUMENT --------------------------

            public const string DOCUMENT_VIEW = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.DOCUMENT.{SRTypeActions.VIEW}";

            public const string DOCUMENT_UPLOAD = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.DOCUMENT.{SRTypeActions.UPLOAD}";

            public const string DOCUMENT_DELETE = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.DOCUMENT.{SRTypeActions.DELETE}";

            public const string DOCUMENT_DOWNLOAD = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.DOCUMENT.{SRTypeActions.DOWNLOAD}";

            #endregion -------------------------- DOCUMENT --------------------------

            #region -------------------------- WORKFLOW --------------------------

            public const string WORKFLOW_VIEW = $"{CommonBaseConstant.APPCode}.TICKET.DETAIL.WORKFLOW.{SRTypeActions.VIEW}";

            #endregion -------------------------- WORKFLOW --------------------------

            #endregion ::::::::::::::::::::::::: LEVEL 3 :::::::::::::::::::::::::

            /*----------------------------------------------------------------*/
        }

        [DisplayName("Config")]
        [Description("Cấu hình")]
        public static class Configs
        {
            /*----------------------------------------------------------------*/

            #region ::::::::::::::::::::::::: LEVEL 1 :::::::::::::::::::::::::

            public const string CONFIG_VIEW = $"{CommonBaseConstant.APPCode}.CONFIG.{SRTypeActions.VIEW}";

            #endregion ::::::::::::::::::::::::: LEVEL 1 :::::::::::::::::::::::::

            /*----------------------------------------------------------------*/

            #region ::::::::::::::::::::::::: LEVEL 2 :::::::::::::::::::::::::

            #region -------------------------- PROCESS --------------------------

            public const string PROCESS_VIEW = $"{CommonBaseConstant.APPCode}.CONFIG.PROCESS.{SRTypeActions.VIEW}";

            public const string PROCESS_CREATE = $"{CommonBaseConstant.APPCode}.CONFIG.PROCESS.{SRTypeActions.CREATE}";

            public const string PROCESS_UPDATE = $"{CommonBaseConstant.APPCode}.CONFIG.PROCESS.{SRTypeActions.UPDATE}";

            #endregion -------------------------- PROCESS --------------------------

            #region -------------------------- PRIORITY --------------------------

            public const string PRIORITY_VIEW = $"{CommonBaseConstant.APPCode}.CONFIG.PRIORITY.{SRTypeActions.VIEW}";

            public const string PRIORITY_CREATE = $"{CommonBaseConstant.APPCode}.CONFIG.PRIORITY.{SRTypeActions.CREATE}";

            public const string PRIORITY_UPDATE = $"{CommonBaseConstant.APPCode}.CONFIG.PRIORITY.{SRTypeActions.UPDATE}";

            public const string PRIORITY_DELETE = $"{CommonBaseConstant.APPCode}.CONFIG.PRIORITY.{SRTypeActions.DELETE}";

            #endregion -------------------------- PRIORITY --------------------------

            #endregion ::::::::::::::::::::::::: LEVEL 2 :::::::::::::::::::::::::

            /*----------------------------------------------------------------*/
        }

        [DisplayName("Employee")]
        [Description("Nhân sự")]
        public static class Employees
        {
            /*----------------------------------------------------------------*/

            #region ::::::::::::::::::::::::: LEVEL 1 :::::::::::::::::::::::::

            public const string EMPLOYEE_VIEW = $"{CommonBaseConstant.APPCode}.EMPLOYEE.{SRTypeActions.VIEW}";

            #endregion ::::::::::::::::::::::::: LEVEL 1 :::::::::::::::::::::::::

            /*----------------------------------------------------------------*/

            #region ::::::::::::::::::::::::: LEVEL 2 :::::::::::::::::::::::::

            #region -------------------------- CALENDAR --------------------------

            public const string CALENDAR_VIEW = $"{CommonBaseConstant.APPCode}.EMPLOYEE.CALENDAR.{SRTypeActions.VIEW}";

            public const string CALENDAR_CREATE = $"{CommonBaseConstant.APPCode}.EMPLOYEE.CALENDAR.{SRTypeActions.CREATE}";

            public const string CALENDAR_UPDATE = $"{CommonBaseConstant.APPCode}.EMPLOYEE.CALENDAR.{SRTypeActions.UPDATE}";

            public const string CALENDAR_DELETE = $"{CommonBaseConstant.APPCode}.EMPLOYEE.CALENDAR.{SRTypeActions.DELETE}";

            #endregion -------------------------- CALENDAR --------------------------

            #region -------------------------- EMPLOYEE --------------------------

            public const string EMPLOYEE_DETAIL_VIEW = $"{CommonBaseConstant.APPCode}.EMPLOYEE.EMPLOYEE.{SRTypeActions.VIEW}";

            #endregion -------------------------- EMPLOYEE --------------------------

            #endregion ::::::::::::::::::::::::: LEVEL 2 :::::::::::::::::::::::::

            /*----------------------------------------------------------------*/
        }

        [DisplayName("Dashboards")]
        [Description("Trang chủ thống kê")]
        public static class Dashboards
        {
            /*----------------------------------------------------------------*/

            #region :::::::::::::::::::::::  LEVEL 1 :::::::::::::::::::::::

            public const string DASHBOARD_VIEW = $"{CommonBaseConstant.APPCode}.DASHBOARD.{SRTypeActions.VIEW}";

            #endregion :::::::::::::::::::::::  LEVEL 1 :::::::::::::::::::::::

            /*----------------------------------------------------------------*/
        }

        /// <summary>
        /// Returns a list of Permissions.
        /// </summary>
        /// <returns>List Permissions</returns>
        public static List<string> GetRegisteredPermissions()
        {
            List<string> permissions = [];

            foreach (FieldInfo prop in typeof(SRTypePermissions).GetNestedTypes().SelectMany(c => c.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)))
            {
                object propertyValue = prop.GetValue(null);

                if (propertyValue is null)
                {
                    continue;
                }

                permissions.Add($"{propertyValue}");
            }

            return permissions;
        }
    }
}