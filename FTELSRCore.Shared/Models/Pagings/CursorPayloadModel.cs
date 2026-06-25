using System.Text;

namespace FTELSRCore.Models.Pagings
{
    public class CursorPayloadModel
    {
        public enum TypeCursorPayload
        {
            NextPage = 1,

            PreviousPage = 2
        }

        public TypeCursorPayload TypeCursor { get; set; }

        public int StatusId { get; set; }

        public DateTime CreatedDate { get; set; }

        public int PageSize { get; set; }

        public int PageNumber { get; set; }

        public string Encode()
        {
            string json = this.ToJSon();

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public static CursorPayloadModel Decode(string cursor)
        {
            if (string.IsNullOrEmpty(cursor)) return null;

            try
            {
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));

                _ = json.JSonTryParse(out CursorPayloadModel result);

                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}