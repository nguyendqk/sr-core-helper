using Microsoft.Extensions.DependencyInjection;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.RegisterImplementationExtensions
{
    public static class RegisterImplementationExtensions
    {
        #region :::::::::::::::::::::::::::::: REGISTER IMPLEMENTATION ONLY EXTENSIONS ::::::::::::::::::::::::::::::

        /// <summary>
        /// Đăng ký các lớp có tên kết thúc với hậu tố chỉ định dưới dạng Transient.
        /// Chỉ áp dụng cho các lớp kế thừa từ kiểu cơ sở (kiểu quản lý).
        /// </summary>
        /// <param name="services">Dịch vụ IServiceCollection để đăng ký các dịch vụ.</param>
        /// <param name="suffixFilter">Hậu tố để lọc tên các lớp.</param>
        /// <param name="typeofImplementation">Kiểu cơ sở để đảm bảo lớp kế thừa từ kiểu này.</param>
        /// <returns>Dịch vụ IServiceCollection đã được cập nhật.</returns>
        ///

        public static IServiceCollection AddTransientImplementationsOnly(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
        {
            Type managers = typeofImplementation;

            var types = managers
                .Assembly
                .GetExportedTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith(suffixFilter))
                .Select(t => new
                {
                    Implementation = t
                });

            foreach (var type in from type in types
                                 where managers.IsAssignableFrom(type.Implementation)
                                 select type)
            {
                _ = services.AddTransient(type.Implementation);
            }

            return services;
        }

        /// <summary>
        /// Đăng ký các lớp có tên kết thúc với hậu tố chỉ định dưới dạng Scoped.
        /// Chỉ áp dụng cho các lớp kế thừa từ kiểu cơ sở (kiểu quản lý).
        /// </summary>
        /// <param name="services">Dịch vụ IServiceCollection để đăng ký các dịch vụ.</param>
        /// <param name="suffixFilter">Hậu tố để lọc tên các lớp.</param>
        /// <param name="typeofImplementation">Kiểu cơ sở để đảm bảo lớp kế thừa từ kiểu này.</param>
        /// <returns>Dịch vụ IServiceCollection đã được cập nhật.</returns>
        ///
        public static IServiceCollection AddScopedImplementationsOnly(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
        {
            Type managers = typeofImplementation;

            var types = managers
                .Assembly
                .GetExportedTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith(suffixFilter))
                .Select(t => new
                {
                    Implementation = t
                });

            foreach (var type in from type in types
                                 where managers.IsAssignableFrom(type.Implementation)
                                 select type)
            {
                _ = services.AddScoped(type.Implementation);
            }

            return services;
        }

        /// <summary>
        /// Đăng ký các lớp có tên kết thúc với hậu tố chỉ định dưới dạng Singleton.
        /// Chỉ áp dụng cho các lớp kế thừa từ kiểu cơ sở (kiểu quản lý).
        /// </summary>
        /// <param name="services">Dịch vụ IServiceCollection để đăng ký các dịch vụ.</param>
        /// <param name="suffixFilter">Hậu tố để lọc tên các lớp.</param>
        /// <param name="typeofImplementation">Kiểu cơ sở để đảm bảo lớp kế thừa từ kiểu này.</param>
        /// <returns>Dịch vụ IServiceCollection đã được cập nhật.</returns>
        ///
        public static IServiceCollection AddSingletonImplementationsOnly(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
        {
            Type managers = typeofImplementation;

            var types = managers
                .Assembly
                .GetExportedTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith(suffixFilter))
                .Select(t => new
                {
                    Implementation = t
                });

            foreach (var type in from type in types
                                 where managers.IsAssignableFrom(type.Implementation)
                                 select type)
            {
                _ = services.AddSingleton(type.Implementation);
            }

            return services;
        }

        #endregion :::::::::::::::::::::::::::::: REGISTER IMPLEMENTATION ONLY EXTENSIONS ::::::::::::::::::::::::::::::

        #region :::::::::::::::::::::::::::::: REGISTER IMPLEMENTATION EXTENSIONS ::::::::::::::::::::::::::::::

        /// <summary>
        /// Đăng ký các lớp có tên kết thúc với hậu tố chỉ định dưới dạng Singleton,
        /// đồng thời đăng ký interface tương ứng với lớp nếu có.
        /// </summary>
        /// <param name="services">Dịch vụ IServiceCollection để đăng ký các dịch vụ.</param>
        /// <param name="suffixFilter">Hậu tố để lọc tên các lớp.</param>
        /// <param name="typeofImplementation">Kiểu cơ sở để đảm bảo lớp kế thừa từ kiểu này.</param>
        /// <returns>Dịch vụ IServiceCollection đã được cập nhật.</returns>
        ///
        public static IServiceCollection AddSingletonImplementationsWithInterface(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
        {
            Type managers = typeofImplementation;

            var types = managers
                  .Assembly
                  .GetExportedTypes()
                  .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith(suffixFilter))
                  .Select(t => new
                  {
                      Service = t.GetInterface($"I{t.Name}"),
                      Implementation = t
                  }).Where(t => t.Service != null);

            foreach (var type in from type in types
                                 where managers.IsAssignableFrom(type.Service)
                                 select type)
            {
                _ = services.AddSingleton(type.Service, type.Implementation);
            }

            return services;
        }

        /// <summary>
        /// Đăng ký các lớp có tên kết thúc với hậu tố chỉ định dưới dạng Scoped,
        /// đồng thời đăng ký interface tương ứng với lớp nếu có.
        /// </summary>
        /// <param name="services">Dịch vụ IServiceCollection để đăng ký các dịch vụ.</param>
        /// <param name="suffixFilter">Hậu tố để lọc tên các lớp.</param>
        /// <param name="typeofImplementation">Kiểu cơ sở để đảm bảo lớp kế thừa từ kiểu này.</param>
        /// <returns>Dịch vụ IServiceCollection đã được cập nhật.</returns>
        ///
        public static IServiceCollection AddScopedImplementationsWithInterface(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
        {
            Type managers = typeofImplementation;

            var types = managers
                  .Assembly
                  .GetExportedTypes()
                  .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith(suffixFilter))
                  .Select(t => new
                  {
                      Service = t.GetInterface($"I{t.Name}"),
                      Implementation = t
                  }).Where(t => t.Service != null);

            foreach (var type in from type in types
                                 where managers.IsAssignableFrom(type.Service)
                                 select type)
            {
                _ = services.AddScoped(type.Service, type.Implementation);
            }

            return services;
        }

        /// <summary>
        /// Đăng ký các lớp có tên kết thúc với hậu tố chỉ định dưới dạng Transient,
        /// đồng thời đăng ký interface tương ứng với lớp nếu có.
        /// </summary>
        /// <param name="services">Dịch vụ IServiceCollection để đăng ký các dịch vụ.</param>
        /// <param name="suffixFilter">Hậu tố để lọc tên các lớp.</param>
        /// <param name="typeofImplementation">Kiểu cơ sở để đảm bảo lớp kế thừa từ kiểu này.</param>
        /// <returns>Dịch vụ IServiceCollection đã được cập nhật.</returns>
        ///
        public static IServiceCollection AddTransientImplementationsWithInterface(this IServiceCollection services, string suffixFilter, Type typeofImplementation)
        {
            Type managers = typeofImplementation;

            var types = managers
                  .Assembly
                  .GetExportedTypes()
                  .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith(suffixFilter))
                  .Select(t => new
                  {
                      Service = t.GetInterface($"I{t.Name}"),
                      Implementation = t
                  }).Where(t => t.Service != null);

            foreach (var type in from type in types
                                 where managers.IsAssignableFrom(type.Service)
                                 select type)
            {
                _ = services.AddTransient(type.Service, type.Implementation);
            }

            return services;
        }

        #endregion :::::::::::::::::::::::::::::: REGISTER IMPLEMENTATION EXTENSIONS ::::::::::::::::::::::::::::::
    }
}