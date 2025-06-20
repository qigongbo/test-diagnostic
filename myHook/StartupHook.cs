using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

// 必须使用无命名空间的StartupHook类和Initialize方法
internal class StartupHook
{
    public static void Initialize()
    {
        try
        {
            // 设置控制台编码为UTF-8，确保中文正常显示
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;

            Console.WriteLine($"env:{Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS")}");

            Console.WriteLine($"启动钩子已初始化，等待ASP.NET Core加载...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动钩子初始化失败: {ex.Message}");
            throw;
        }
    }

    private static void OnAssemblyLoaded(object sender, AssemblyLoadEventArgs args)
    {
        if (args.LoadedAssembly.FullName.StartsWith("Microsoft.AspNetCore.Hosting,"))
        {
            try
            {
                var serviceProvider = GetServiceProvider();
                if (serviceProvider != null)
                {
                    var appLifetime = serviceProvider.GetService(
                        typeof(Microsoft.Extensions.Hosting.IHostApplicationLifetime))
                        as Microsoft.Extensions.Hosting.IHostApplicationLifetime;

                    if (appLifetime != null)
                    {
                        appLifetime.ApplicationStarted.Register(() =>
                        {
                            InjectMiddleware(serviceProvider);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注入中间件失败: {ex.Message}");
            }

            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoaded;
        }
    }

    private static void InjectMiddleware(IServiceProvider serviceProvider)
    {
        try
        {
            var appBuilderType = Type.GetType("Microsoft.AspNetCore.Builder.IApplicationBuilder, Microsoft.AspNetCore.Http.Abstractions");
            if (appBuilderType == null)
            {
                Console.WriteLine("无法获取IApplicationBuilder类型");
                return;
            }

            var appBuilderFactory = serviceProvider.GetService(
                typeof(Func<Microsoft.AspNetCore.Builder.IApplicationBuilder>))
                as Func<Microsoft.AspNetCore.Builder.IApplicationBuilder>;

            if (appBuilderFactory == null)
            {
                Console.WriteLine("无法获取IApplicationBuilder工厂");
                return;
            }

            var appBuilder = appBuilderFactory();
            if (appBuilder == null)
            {
                Console.WriteLine("无法创建IApplicationBuilder实例");
                return;
            }

            // 获取UseMiddleware扩展方法
            var middlewareExtensionsType = typeof(Microsoft.AspNetCore.Builder.UseMiddlewareExtensions);
            var useMiddlewareMethod = middlewareExtensionsType.GetMethod(
                "UseMiddleware",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { appBuilderType, typeof(Type) },
                null);

            if (useMiddlewareMethod == null)
            {
                Console.WriteLine("无法找到UseMiddleware扩展方法");
                return;
            }

            // 调用UseMiddleware方法，传递中间件类型
            useMiddlewareMethod.Invoke(null, new object[] { appBuilder, typeof(CustomMiddleware) });
            Console.WriteLine("自定义中间件已成功注入!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"注入中间件时发生异常: {ex.Message}");
        }
    }

    private static IServiceProvider GetServiceProvider()
    {
        try
        {
            Microsoft.AspNetCore.Hosting.WebHostBuilder webHost;
           

            // 获取HostBuilder类型
            Type hostBuilderType = Type.GetType("Microsoft.AspNetCore.Hosting.WebHostBuilder, Microsoft.AspNetCore.Hosting");
            if (hostBuilderType == null)
                hostBuilderType = Type.GetType("Microsoft.Extensions.Hosting.HostBuilder, Microsoft.Extensions.Hosting");

            if (hostBuilderType == null)
            {
                Console.WriteLine("无法获取HostBuilder类型");
                return null;
            }

            // 获取静态字段Current的值（HostBuilder实例）
            FieldInfo currentHostBuilderField = hostBuilderType.GetField("Current", BindingFlags.Static | BindingFlags.NonPublic);
            if (currentHostBuilderField == null)
            {
                Console.WriteLine("无法获取HostBuilder的Current字段");
                return null;
            }

            object hostBuilder = currentHostBuilderField.GetValue(null);
            if (hostBuilder == null)
            {
                Console.WriteLine("HostBuilder实例为null");
                return null;
            }

            // 调用Build()方法获取IHost实例
            MethodInfo buildMethod = hostBuilderType.GetMethod("Build");
            if (buildMethod == null)
            {
                Console.WriteLine("无法获取Build方法");
                return null;
            }

            object host = buildMethod.Invoke(hostBuilder, null);
            if (host == null)
            {
                Console.WriteLine("Build方法返回null");
                return null;
            }

            // 获取Services属性
            PropertyInfo servicesProperty = host.GetType().GetProperty("Services");
            if (servicesProperty == null)
            {
                Console.WriteLine("无法获取Services属性");
                return null;
            }

            return servicesProperty.GetValue(host) as IServiceProvider;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取服务提供程序失败: {ex.Message}");
            return null;
        }
    }
}

// 自定义中间件类
public class CustomMiddleware
{
    private readonly RequestDelegate _next;

    public CustomMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Console.WriteLine($"[中间件日志] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - 接收到请求: {context.Request.Method} {context.Request.Path}");

        // 继续处理请求
        await _next(context);

        Console.WriteLine($"[中间件日志] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - 请求处理完成: 状态码 {context.Response.StatusCode}");
    }
}