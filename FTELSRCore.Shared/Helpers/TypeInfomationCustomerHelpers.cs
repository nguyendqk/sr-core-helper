using System.Text.RegularExpressions;

namespace FTELSRCore.Helpers
{
    public static class TypeInfomationCustomerHelpers
    {
        /// <summary>
        /// Ràng chuỗi các input, Các input cách nhau bởi dấu ;
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        private static void IsSearch(this string input, ref List<TypeInfomationCustomerEnum> type)
        {
            const string pattern = @"([\s*a-zA-Z0-9'-Š\s*]+?[,]{1}[\s*a-zA-Z0-9'-Š\s*]+)+";

            if (Regex.IsMatch(input, pattern))
            {
                type.Add(TypeInfomationCustomerEnum.FullName);
            }
        }

        /// <summary>
        /// Chuỗi bắt đầu bằng số 0 và tiếp theo là từ 9 đến 10 chữ số hay không
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static void IsPhone(this string input, ref List<TypeInfomationCustomerEnum> type)
        {
            const string pattern = @"^0\d{9,10}$";
            if (Regex.IsMatch(input, pattern))
            {
                type.Add(TypeInfomationCustomerEnum.PhoneNumber);
            }
        }

        /// <summary>
        /// Chuỗi bắt đầu từ 3 đến 6 chữ cái (cả viết thường và viết hoa), tiếp theo là ít nhất 3 chữ số, và không chứa bất kỳ ký tự nào khác
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        private static void IsContract(this string input, ref List<TypeInfomationCustomerEnum> type)
        {
            const string pattern = @"^[a-zA-Z]{3,6}\d{3,}$";
            if (Regex.IsMatch(input, pattern))
            {
                type.Add(TypeInfomationCustomerEnum.ContractNo);
            }
        }

        /// <summary>
        /// Tên KH hoặc tên Cty: Ký tự đầu khác số từ 0 tới 9 hoặc dấu +
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static void IsName(this string input, ref List<TypeInfomationCustomerEnum> type)
        {
            const string pattern = @"^[^0-9+\s]";
            if (Regex.IsMatch(input, pattern))
            {
                type.Add(TypeInfomationCustomerEnum.FullName);
            }
        }

        /// <summary>
        /// CMND/CCCD hoặc Passport: Chuỗi có đúng 9 chữ số hoặc 12 chữ số
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type">validate CMND 9/12 số</param>
        /// <returns></returns>
        private static void IsPassport(this string input, ref List<TypeInfomationCustomerEnum> type)
        {
            const string pattern = @"^\d{9}$|^\d{12}$";
            if (Regex.IsMatch(input, pattern))
            {
                type.Add(TypeInfomationCustomerEnum.IdentityNo);
            }
        }

        /// <summary>
        /// Có ít nhất 5 chử số
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        private static void IsIdentityNo(this string input, ref List<TypeInfomationCustomerEnum> type)
        {
            const string pattern = @"\d{5,}";
            if (Regex.IsMatch(input, pattern))
            {
                type.Add(TypeInfomationCustomerEnum.IdentityNo);
                type.Add(TypeInfomationCustomerEnum.BusinessTaxCode);
            }
        }

        /// <summary>
        /// Chuỗi có thể là ký tự chữ cái hoặc số hoặc dấu gạch dưới hoặc dấu gạch ngang hoặc dấu chấm + Ký tự @ + phần tên miền có thể là các ký tự chữ cái hoặc số hoặc dấu gạch ngang
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static void IsEmail(this string input, ref List<TypeInfomationCustomerEnum> type)
        {
            //string pattern = @"^[a-zA-Z0-9]+([-._][a-zA-Z0-9]+)*@[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*\.[a-zA-Z]{1,5}$";
            const string pattern = @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$";
            if (Regex.IsMatch(input, pattern))
            {
                type.Add(TypeInfomationCustomerEnum.Email);
            }
        }

        /// <summary>
        /// Chuỗi hiển thị dạng mst + 10 số hoặc có thể có thêm dấu - và 3 số
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type">validate TaxCode: mst0123456789 || mst0123456789-123</param>
        /// <returns></returns>
        private static void IsTaxCode(this string input, ref List<TypeInfomationCustomerEnum> type)
        {
            input = input.ToLower();
            const string pattern = @"^mst\d{10}(\-\d{3})?$";
            if (Regex.IsMatch(input, pattern))
            {
                type.Add(TypeInfomationCustomerEnum.BusinessTaxCode);
            }
        }

        /// <summary>
        /// Toàn là số không chứa các kí tự
        /// </summary>
        /// <param name="input"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static void IsObjId(this string input, ref List<TypeInfomationCustomerEnum> type)
        {
            const string pattern = @"^\d+$";
            if (Regex.IsMatch(input, pattern))
            {
                type.Add(TypeInfomationCustomerEnum.ObjId);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        ///
        public static List<TypeInfomationCustomerEnum> TypeInfomationCustomerEnums(string input)
        {
            List<TypeInfomationCustomerEnum> typeSearches = new();

            if (string.IsNullOrWhiteSpace(input))
            {
                typeSearches.Add(TypeInfomationCustomerEnum.FullName);

                return typeSearches;
            }

            input = input.Trim();

            IsObjId(input, ref typeSearches);
            IsContract(input, ref typeSearches);
            IsSearch(input, ref typeSearches);
            IsPhone(input, ref typeSearches);
            IsName(input, ref typeSearches);
            IsPassport(input, ref typeSearches);
            IsEmail(input, ref typeSearches);
            IsTaxCode(input, ref typeSearches);
            IsIdentityNo(input, ref typeSearches);

            if (typeSearches?.Any() is false)
            {
                typeSearches.Add(TypeInfomationCustomerEnum.FullName);

                return typeSearches;
            }

            return typeSearches.Distinct().OrderBy(x => (int)x).ToList();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        ///
        public static (TypeInfomationCustomerEnum typeSearch, bool exact) GetExactForTypeInfomationCustomerEnum(TypeInfomationCustomerEnum type)
        {
            switch (type)
            {
                case TypeInfomationCustomerEnum.ObjId
                    or TypeInfomationCustomerEnum.ContractNo
                    or TypeInfomationCustomerEnum.IdentityNo
                    or TypeInfomationCustomerEnum.PhoneNumber
                    or TypeInfomationCustomerEnum.FullName
                    or TypeInfomationCustomerEnum.Email:
                    {
                        return (typeSearch: type, exact: true);
                    }
                default:
                    {
                        return (typeSearch: type, exact: false);
                    }
            }
        }
    }
}