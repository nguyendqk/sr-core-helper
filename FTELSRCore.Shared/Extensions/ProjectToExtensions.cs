using FTELSRCore.Abstractions.Entities;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Reflection;

namespace FTELSRCore.Extensions
{
    public static class ProjectToExtensions
    {
        public record QueryContext<TTable, TDto>(
            FilterDefinition<TTable> Predicate,
            SortDefinition<TTable> Sorting = null,
            Expression<Func<TTable, TDto>> Selector = null) where TDto : class
                                                            where TTable : class;

        /// <summary>
        /// Chuyển đổi một đối tượng từ kiểu <typeparamref name="TEntity"/> sang kiểu <typeparamref name="TDto"/>.
        /// </summary>
        /// <typeparam name="TEntity">Kiểu dữ liệu của đối tượng nguồn cần chuyển đổi.</typeparam>
        /// <typeparam name="TDto">Kiểu dữ liệu của đối tượng đích sau khi chuyển đổi.</typeparam>
        /// <param name="entity">Đối tượng nguồn cần chuyển đổi.</param>
        /// <returns>
        /// Trả về một đối tượng kiểu <typeparamref name="TDto"/> sau khi ánh xạ các thuộc tính từ đối tượng nguồn.
        /// Nếu đối tượng nguồn là null, trả về một đối tượng mới của kiểu <typeparamref name="TDto"/>.
        /// </returns>
        ///
        public static TDto ProjectTo<TEntity, TDto>(this TEntity entity)
        {
            object dto = Activator.CreateInstance(typeof(TDto));

            if (entity is null) return (TDto)dto;

            Type dtoType = typeof(TDto);
            Type entityType = typeof(TEntity);
            PropertyInfo[] entityProps = entityType.GetProperties();

            string guidId = Guid.NewGuid().ToString();

            foreach (PropertyInfo entityProp in entityProps)
            {
                try
                {
                    PropertyInfo dtoProp = dtoType.GetProperty(
                        entityProp.Name,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                    if (dtoProp is null || dtoProp.CanWrite is false
                       || dtoProp.GetCustomAttribute<NoMapAttribute>() is not null
                       || entityProp.GetCustomAttribute<NoMapAttribute>() is not null)
                    {
                        continue;
                    }

                    dtoProp.SetValue(dto, entityProp.GetValue(entity));
                }
                catch (Exception exception)
                {
                    CommonBaseConstant.ConfigLoggerExceptionByConsole(nameof(ProjectToExtensions), nameof(ProjectTo), exception, guidId);
                }
            }

            return (TDto)dto;
        }

        /// <summary>
        /// Chuyển đổi một danh sách các đối tượng từ kiểu <typeparamref name="TEntity"/> sang danh sách các đối tượng kiểu <typeparamref name="TDto"/>.
        /// </summary>
        /// <typeparam name="TEntity">Kiểu dữ liệu của các đối tượng nguồn cần chuyển đổi.</typeparam>
        /// <typeparam name="TDto">Kiểu dữ liệu của các đối tượng đích sau khi chuyển đổi.</typeparam>
        /// <param name="entities">Danh sách các đối tượng nguồn cần chuyển đổi.</param>
        /// <returns>
        /// Trả về một danh sách các đối tượng kiểu <typeparamref name="TDto"/> sau khi ánh xạ các thuộc tính từ các đối tượng nguồn.
        /// Nếu danh sách nguồn là null hoặc rỗng, trả về một danh sách trống.
        /// </returns>
        ///
        public static List<TDto> ProjectTo<TEntity, TDto>(this List<TEntity> entities)
        {
            List<TDto> dtos = [];

            Type dtoType = typeof(TDto);

            Type entityType = typeof(TEntity);

            string guidId = Guid.NewGuid().ToString();

            PropertyInfo[] entityProps = entityType.GetProperties();

            foreach (TEntity entity in entities)
            {
                try
                {
                    object dto = Activator.CreateInstance<TDto>();

                    foreach (PropertyInfo entityProp in entityProps)
                    {
                        try
                        {
                            PropertyInfo dtoProp = dtoType.GetProperty(
                                entityProp.Name,
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy
                            );

                            if (dtoProp is null || dtoProp.CanWrite is false
                               || dtoProp.GetCustomAttribute<NoMapAttribute>() is not null
                               || entityProp.GetCustomAttribute<NoMapAttribute>() is not null)
                            {
                                continue;
                            }

                            dtoProp.SetValue(dto, entityProp.GetValue(entity));
                        }
                        catch (Exception exception)
                        {
                            CommonBaseConstant.ConfigLoggerExceptionByConsole(nameof(ProjectToExtensions), $"{nameof(ProjectTo)}", exception, guidId);
                        }
                    }

                    dtos.Add((TDto)dto);
                }
                catch (Exception exception)
                {
                    CommonBaseConstant.ConfigLoggerExceptionByConsole(nameof(ProjectToExtensions), $"{nameof(ProjectTo)}", exception, guidId);
                }
            }

            return dtos;
        }

        /// <summary>
        /// Ánh xạ dữ liệu từ đối tượng kiểu <typeparamref name="TFrom"/> sang đối tượng kiểu <typeparamref name="TTo"/>
        /// bằng cách sử dụng biểu thức (expression tree).
        /// </summary>
        /// <typeparam name="TFrom">Kiểu dữ liệu của đối tượng nguồn cần ánh xạ.</typeparam>
        /// <typeparam name="TTo">Kiểu dữ liệu của đối tượng đích sau khi ánh xạ.</typeparam>
        /// <param name="source">Đối tượng nguồn cần ánh xạ.</param>
        /// <returns>Trả về đối tượng kiểu <typeparamref name="TTo"/> với các thuộc tính đã được ánh xạ từ đối tượng nguồn.</returns>
        ///
        public static TTo MapUsingExpression<TFrom, TTo>(TFrom source)
            where TTo : class
            where TFrom : class
        {
            string guidId = Guid.NewGuid().ToString();

            ParameterExpression parameter = Expression.Parameter(typeof(TFrom), "sr");

            List<MemberAssignment> bindings =
                typeof(TTo).GetProperties().Select(
                    targetProp =>
                    {
                        try
                        {
                            PropertyInfo sourceProp = typeof(TFrom).GetProperty(
                                targetProp.Name,
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy
                            );

                            if (sourceProp is null
                                || sourceProp.CanWrite is false
                                || sourceProp.PropertyType != targetProp.PropertyType
                                )
                            {
                                return null;
                            }

                            MemberExpression sourcePropAccess =
                                Expression.Property(
                                    // Ép kiểu về lớp chứa thuộc tính
                                    Expression.Convert(parameter, sourceProp.DeclaringType),
                                    sourceProp
                                );

                            return Expression.Bind(targetProp, sourcePropAccess);
                        }
                        catch (Exception exception)
                        {
                            CommonBaseConstant.ConfigLoggerExceptionByConsole(nameof(ProjectToExtensions), nameof(MapUsingExpression), exception, guidId);
                        }

                        return null;
                    })
                .Where(binding => binding is not null).ToList();

            MemberInitExpression body = Expression.MemberInit(Expression.New(typeof(TTo)), bindings);

            Func<TFrom, TTo> lambda = Expression.Lambda<Func<TFrom, TTo>>(body, parameter).Compile();

            return lambda(source);
        }

        /// <summary>
        /// Thay thế tham số của biểu thức điều kiện từ kiểu <typeparamref name="TFrom"/> sang kiểu <typeparamref name="TTo"/>.
        /// </summary>
        /// <typeparam name="TFrom">Kiểu dữ liệu ban đầu của biểu thức điều kiện.</typeparam>
        /// <typeparam name="TTo">Kiểu dữ liệu mới mà tham số biểu thức cần được thay thế.</typeparam>
        /// <param name="target">Biểu thức điều kiện ban đầu với tham số kiểu <typeparamref name="TFrom"/>.</param>
        /// <returns>Trả về biểu thức điều kiện với tham số kiểu <typeparamref name="TTo"/>. Nếu biểu thức đầu vào là null, trả về một biểu thức điều kiện luôn đúng.</returns>
        ///
        public static Expression<Func<TTo, bool>> ReplaceParameter<TFrom, TTo>(this Expression<Func<TFrom, bool>> target)
        {
            if (target is null) return x => true;

            Expression<Func<TTo, bool>> result =
                (Expression<Func<TTo, bool>>)new WhereReplacerVisitor<TFrom, TTo>().Visit(target);

            if (result is null) return x => true;

            return result;
        }

        /// <summary>
        /// Chuyển đổi một đối tượng từ kiểu TTableFrom sang kiểu TTableTo bằng cách sao chép các thuộc tính tương ứng.
        /// </summary>
        /// <typeparam name="TTableFrom">Kiểu nguồn (đối tượng cần chuyển đổi).</typeparam>
        /// <typeparam name="TTableTo">Kiểu đích (kiểu đối tượng cần chuyển đổi sang).</typeparam>
        /// <param name="source">Đối tượng nguồn cần chuyển đổi.</param>
        /// <returns>Đối tượng mới của kiểu TTableTo với các thuộc tính sao chép từ đối tượng nguồn.</returns>
        ///
        public static TTableTo ConvertTo<TTableFrom, TTableTo>(TTableFrom source)
        {
            // Initialize the target object
            TTableTo target = Activator.CreateInstance<TTableTo>();

            // Use reflection to copy properties from source to target
            foreach (var sourceProperty in typeof(TTableFrom).GetProperties())
            {
                var targetProperty = typeof(TTableTo).GetProperty(sourceProperty.Name);

                if (targetProperty != null && targetProperty.CanWrite)
                {
                    // Check if types are compatible and assign the value
                    if (targetProperty.PropertyType == sourceProperty.PropertyType)
                    {
                        targetProperty.SetValue(target, sourceProperty.GetValue(source));
                    }
                }
            }

            return target;
        }

        /// <summary>
        /// Chuyển đổi một tập hợp các đối tượng từ kiểu TTableFrom sang kiểu TTableTo.
        /// </summary>
        /// <typeparam name="TTableFrom">Kiểu nguồn (danh sách các đối tượng cần chuyển đổi).</typeparam>
        /// <typeparam name="TTableTo">Kiểu đích (kiểu đối tượng cần chuyển đổi sang).</typeparam>
        /// <param name="sources">Danh sách các đối tượng nguồn cần chuyển đổi.</param>
        /// <returns>Danh sách các đối tượng đích với các thuộc tính sao chép từ các đối tượng nguồn.</returns>
        ///
        public static IEnumerable<TTableTo> ConvertTo<TTableFrom, TTableTo>(IEnumerable<TTableFrom> sources)
        {
            if (sources is null || !sources.Any()) return [];

            List<TTableTo> result = [];

            foreach (TTableFrom source in sources)
            {
                TTableTo data = ConvertTo<TTableFrom, TTableTo>(source);

                if (data is not null)
                {
                    result.Add(data);
                }
            }

            return result;
        }

        /// <summary>
        /// Tạo một <see cref="UpdateDefinition{TTableTo}"/> cho đối tượng <paramref name="request"/> bằng cách sao chép các giá trị của các thuộc tính trong đối tượng này.
        /// </summary>
        /// <param name="audit"></param>
        /// <typeparam name="TTableTo">Kiểu đối tượng mà cần tạo UpdateDefinition.</typeparam>
        /// <param name="request">Đối tượng chứa các giá trị cần cập nhật.</param>
        /// <returns>UpdateDefinition mô tả các thao tác cập nhật cho từng thuộc tính trong đối tượng <paramref name="request"/>.</returns>
        ///
        public static UpdateDefinition<TTableTo> MapUpdateDefinition<TTableTo>(TTableTo request)
        {
            List<UpdateDefinition<TTableTo>> entitys = [];

            // Tạo một builder cho UpdateDefinition
            UpdateDefinitionBuilder<TTableTo> updateBuilder = Builders<TTableTo>.Update;

            // Lấy các thuộc tính của đối tượng request
            PropertyInfo[] properties = typeof(TTableTo).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (!property.CanRead) continue;

                // Tìm thuộc tính tương ứng trong request (giả sử chúng có tên giống nhau)
                object value = typeof(TTableTo).GetProperty(property.Name)?.GetValue(request);

                if (value is null)
                {
                    continue;
                }

                if (property.GetCustomAttribute<NoMapUpdateDefinitionAttribute>() is not null)
                {
                    continue;
                }

                // Tạo thao tác Set cho mỗi trường
                entitys.Add(updateBuilder.Set(property.Name, value));
            }

            if (entitys is null || entitys.Count <= 0) return null;

            // Kết hợp tất cả các thao tác thành một UpdateDefinition duy nhất
            return updateBuilder.Combine(entitys);
        }

        /// <summary>
        /// Chuyển đổi một tập hợp các đối tượng từ kiểu TTableTo thành một danh sách các UpdateDefinition.
        /// </summary>
        /// <typeparam name="TTableTo">Kiểu đối tượng cần chuyển đổi sang UpdateDefinition.</typeparam>
        /// <param name="request">Danh sách các đối tượng cần chuyển đổi thành UpdateDefinition.</param>
        /// <returns>Danh sách các UpdateDefinition tương ứng với các đối tượng trong danh sách.</returns>
        ///
        public static IEnumerable<UpdateDefinition<TTableTo>> MapUpdateDefinition<TTableTo>(IEnumerable<TTableTo> request)
        {
            if (request is null || !request.Any()) return [];

            List<UpdateDefinition<TTableTo>> result = [];

            foreach (TTableTo item in request)
            {
                UpdateDefinition<TTableTo> data = MapUpdateDefinition(item);

                if (data is not null)
                {
                    result.Add(data);
                }
            }

            return result;
        }

        /// <summary>
        /// Thay thế tham số của các biểu thức điều kiện từ kiểu <typeparamref name="TFrom"/> sang kiểu <typeparamref name="TTo"/> trong một mảng các biểu thức.
        /// </summary>
        /// <typeparam name="TFrom">Kiểu dữ liệu ban đầu của các biểu thức điều kiện.</typeparam>
        /// <typeparam name="TTo">Kiểu dữ liệu mới mà các tham số biểu thức cần được thay thế.</typeparam>
        /// <param name="targets">Mảng các biểu thức điều kiện ban đầu với tham số kiểu <typeparamref name="TFrom"/>.</param>
        /// <returns>Mảng các biểu thức điều kiện với tham số kiểu <typeparamref name="TTo"/>. Nếu mảng đầu vào là null hoặc rỗng, trả về mảng có một biểu thức luôn đúng.</returns>
        ///
        public static Expression<Func<TTo, bool>>[] ReplaceParameters<TFrom, TTo>(this Expression<Func<TFrom, bool>>[] targets)
        {
            if (targets is null || targets.Length <= 0) return [x => true];

            List<Expression<Func<TTo, bool>>> result = [];

            foreach (Expression<Func<TFrom, bool>> target in targets)
            {
                Expression<Func<TTo, bool>> item =
                    (Expression<Func<TTo, bool>>)new WhereReplacerVisitor<TFrom, TTo>().Visit(target);

                if (item is null) continue;

                result.Add(item: item);
            }

            return [.. result];
        }

        /// <summary>
        /// Kết hợp một mảng các biểu thức điều kiện kiểu <typeparamref name="TTo"/> thành một biểu thức logic duy nhất.
        /// </summary>
        /// <typeparam name="TTo">Kiểu dữ liệu của biểu thức điều kiện.</typeparam>
        /// <param name="expressions">Mảng các biểu thức điều kiện kiểu <typeparamref name="TTo"/> cần được kết hợp.</param>
        /// <returns>Biểu thức logic kết hợp các biểu thức điều kiện trong mảng. Nếu mảng đầu vào null hoặc rỗng, trả về một biểu thức luôn đúng.</returns>
        ///
        public static Expression<Func<TTo, bool>> CombineExpressions<TTo>(Expression<Func<TTo, bool>>[] expressions)
        {
            if (expressions == null || expressions.Length <= 0)
                return x => true; // hoặc x => false, tùy vào yêu cầu của bạn

            var parameter = Expression.Parameter(typeof(TTo), "sr");

            // Chuyển đổi từng Expression<Func<TTo, bool>> thành Expression với cùng parameter
            Expression combined = expressions[0].Body;

            foreach (var expression in expressions.Skip(1))
            {
                var currentExpression = Expression.Invoke(expression, parameter);
                combined = Expression.AndAlso(combined, currentExpression); // Hoặc Expression.OrElse nếu bạn cần kết hợp bằng "hoặc"
            }

            return Expression.Lambda<Func<TTo, bool>>(combined, parameter);
        }

        public class WhereReplacerVisitor<TFrom, TTo> : ExpressionVisitor
        {
            // Tham số mới cho kiểu dữ liệu TTo
            private readonly ParameterExpression _parameter = Expression.Parameter(typeof(TTo), "sr");

            /// <summary>
            /// Thay thế tham số trong biểu thức lambda với tham số mới (_parameter).
            /// </summary>
            /// <typeparam name="T">Kiểu của biểu thức lambda.</typeparam>
            /// <param name="node">Biểu thức lambda cần thay thế tham số.</param>
            /// <returns>Biểu thức lambda với tham số mới (_parameter).</returns>
            ///
            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                // replace parameter here
                return Expression.Lambda(Visit(node.Body), [_parameter]);
            }

            /// <summary>
            /// Thay thế việc truy cập thành viên trong biểu thức với kiểu dữ liệu mới (TTo).
            /// </summary>
            /// <param name="node">Biểu thức truy cập thành viên (Property/Field).</param>
            /// <returns>Biểu thức truy cập thành viên với kiểu dữ liệu mới (_parameter).</returns>
            ///
            protected override Expression VisitMember(MemberExpression node)
            {
                // replace parameter member access with new type
                if (node.Expression is ParameterExpression)
                {
                    return Expression.PropertyOrField(_parameter, node.Member.Name);
                }

                return base.VisitMember(node);
            }
        }

        public static TTable SetDataUpdatedDefault<TTable>(TTable entity, AuditModel audit = default)
        {
            if (entity is null || audit is null) return entity;

            string userName = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Name) ? audit?.CreatorInfo?.Name : CommonBaseConstant.Anonymous);

            string userCode = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Code) ? audit?.CreatorInfo?.Code : CommonBaseConstant.AnonymousCode);

            string organization = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Organization) ? audit?.CreatorInfo?.Organization : CommonBaseConstant.OrganizationForISC);

            // Lấy danh sách property của entity
            PropertyInfo[] properties = typeof(TTable).GetProperties();

            // Hàm hỗ trợ gán giá trị nếu property có tồn tại và đang là null
            void SetPropertyIfNull(string propertyName, object value)
            {
                PropertyInfo prop = properties.FirstOrDefault(p => p.Name == propertyName);
                if (prop != null && prop.GetValue(entity) is null)
                {
                    prop.SetValue(entity, value);
                }
            }

            // Gán giá trị mặc định nếu null
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.ModifiedUser), userName);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.ModifiedUserCode), userCode);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.ModifiedUserOrganization), organization);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.ModifiedDate), CommonBaseConstant.DateTimeUtc());

            return entity;
        }

        public static UpdateDefinition<TTable> SetDataUpdatedDefault<TTable>(UpdateDefinition<TTable> updateDefinition, AuditModel audit = default)
        {
            if (updateDefinition is null || audit is null) return updateDefinition;

            string userName = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Name) ? audit?.CreatorInfo?.Name : CommonBaseConstant.Anonymous);

            string userCode = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Code) ? audit?.CreatorInfo?.Code : CommonBaseConstant.AnonymousCode);

            string organization = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Organization) ? audit?.CreatorInfo?.Organization : CommonBaseConstant.OrganizationForISC);

            // Lấy danh sách property của entity
            PropertyInfo[] properties = typeof(TTable).GetProperties();

            // Hàm hỗ trợ gán giá trị nếu property có tồn tại và đang là null
            void SetPropertyIfNull(string propertyName, object value)
            {
                PropertyInfo prop = properties.FirstOrDefault(p => p.Name == propertyName);

                if (prop != null && prop.GetValue(updateDefinition) is null)
                {
                    updateDefinition.Set(prop.Name, value);
                }
            }

            // Gán giá trị mặc định nếu null
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.ModifiedUser), userName);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.ModifiedUserCode), userCode);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.ModifiedUserOrganization), organization);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.ModifiedDate), CommonBaseConstant.DateTimeUtc());

            return updateDefinition;
        }

        public static TTable SetDataCreatedDefault<TTable>(TTable entity, AuditModel audit = default)
        {
            if (entity is null) return entity;

            string userName = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Name) ? audit?.CreatorInfo?.Name : CommonBaseConstant.Anonymous);

            string userCode = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Code) ? audit?.CreatorInfo?.Code : CommonBaseConstant.AnonymousCode);

            string organization = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Organization) ? audit?.CreatorInfo?.Organization : CommonBaseConstant.OrganizationForISC);

            // Lấy danh sách property của entity
            PropertyInfo[] properties = typeof(TTable).GetProperties();

            // Hàm hỗ trợ gán giá trị nếu property có tồn tại và đang là null
            void SetPropertyIfNull(string propertyName, object value)
            {
                PropertyInfo prop = properties.FirstOrDefault(p => p.Name == propertyName);

                if (prop != null && prop.GetValue(entity) is null)
                {
                    prop.SetValue(entity, value);
                }
            }

            SetPropertyIfNull(nameof(BaseEntityMongoDB.IsDeleted), false);
            // Gán giá trị mặc định nếu null
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.CreatedUser), userName);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.CreatedUserCode), userCode);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.CreatedUserOrganization), organization);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.CreatedDate), CommonBaseConstant.DateTimeUtc());

            return entity;
        }

        public static UpdateDefinition<TTable> SetDataCreatedDefault<TTable>(UpdateDefinition<TTable> updateDefinition, AuditModel audit = default)
        {
            if (updateDefinition is null) return updateDefinition;

            string userName = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Name) ? audit?.CreatorInfo?.Name : CommonBaseConstant.Anonymous);

            string userCode = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Code) ? audit?.CreatorInfo?.Code : CommonBaseConstant.AnonymousCode);

            string organization = (!string.IsNullOrWhiteSpace(audit?.CreatorInfo?.Organization) ? audit?.CreatorInfo?.Organization : CommonBaseConstant.OrganizationForISC);

            // Lấy danh sách property của entity
            PropertyInfo[] properties = typeof(TTable).GetProperties();

            // Hàm hỗ trợ gán giá trị nếu property có tồn tại và đang là null
            void SetPropertyIfNull(string propertyName, object value)
            {
                PropertyInfo prop = properties.FirstOrDefault(p => p.Name == propertyName);

                if (prop != null && prop.GetValue(updateDefinition) is null)
                {
                    updateDefinition.Set(prop.Name, value);
                }
            }

            SetPropertyIfNull(nameof(BaseEntityMongoDB.IsDeleted), false);
            // Gán giá trị mặc định nếu null
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.CreatedUser), userName);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.CreatedUserCode), userCode);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.CreatedUserOrganization), organization);
            SetPropertyIfNull(nameof(EntityFullCreatedAndModifiedBase.CreatedDate), CommonBaseConstant.DateTimeUtc());

            return updateDefinition;
        }

        [AttributeUsage(AttributeTargets.All)]
        public class NoMapAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.All)]
        public class NoMapUpdateDefinitionAttribute : Attribute
        {
        }
    }
}