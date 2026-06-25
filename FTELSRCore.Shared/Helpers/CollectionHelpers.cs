namespace FTELSRCore.Helpers
{
    public static class CollectionHelpers
    {
        /// <summary>
        /// Kiểm tra xem một đối tượng <see cref="IEnumerable{T}"/> có bị null hoặc không chứa phần tử nào hay không.
        /// </summary>
        /// <typeparam name="T">Kiểu phần tử trong danh sách.</typeparam>
        /// <param name="enumerable">Đối tượng <see cref="IEnumerable{T}"/> cần kiểm tra.</param>
        /// <returns>
        /// Trả về <c>true</c> nếu đối tượng là <c>null</c> hoặc không có phần tử nào; ngược lại trả về <c>false</c>.
        /// </returns>
        ///
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable is null)
            {
                return true;
            }

            if (enumerable is ICollection<T> collection)
            {
                return collection.Count is 0;
            }

            if (enumerable is IReadOnlyCollection<T> readOnlyCollection)
            {
                return readOnlyCollection.Count is 0;
            }

            if (enumerable is List<T> list)
            {
                return list.Count is 0;
            }

            return !enumerable.Any();
        }
    }
}