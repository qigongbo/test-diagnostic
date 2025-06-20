using System.Reflection;
public static class Program
{
    public static WebApplication app { get; private set; }

    public static void Main(string[] args)
    {
        Console.WriteLine("主程序已经启动");
        var builder = WebApplication.CreateBuilder(args);



        // ????????
        //var environmentVariables = Environment.GetEnvironmentVariables();

        //// ????????
        //Console.WriteLine("???????");
        //foreach (var key in environmentVariables.Keys)
        //{
        //    Console.WriteLine($"{key}: {environmentVariables[key]}");
        //}

        // Add services to the container.

        app = builder.Build();

        // Configure the HTTP request pipeline.



        app.MapGet("/weatherforecast", () =>
        {

            //var s = typeof(Microsoft.AspNetCore.Builder.WebApplicationBuilder).GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            //var mfield = s.Where(t => t.Name == "_builtApplication").FirstOrDefault();
            //Console.WriteLine( mfield.FieldType.FullName  ); 

            //var assembly = AppDomain.CurrentDomain.GetAssemblies();
            //var fie = FindRunningWebApplication();
            //Type appDomainType = typeof(AppDomain);


            //// 获取所有字段，包括私有和实例字段
            //FieldInfo[] fields = appDomainType.GetFields(
            //    BindingFlags.NonPublic |
            //    BindingFlags.Public |
            //    BindingFlags.Instance |
            //    BindingFlags.Static);

            //// 打印所有字段信息
            //Console.WriteLine($"AppDomain类共有 {fields.Length} 个字段：");
            //foreach (FieldInfo field in fields)
            //{
            //    string accessModifier = field.IsPublic ? "public" :
            //                            field.IsPrivate ? "private" :
            //                            field.IsFamily ? "protected" : "internal";

            //    string fieldType = field.FieldType.FullName;
            //    string fieldName = field.Name;

            //    Console.WriteLine($"[{accessModifier}] {fieldType} {fieldName}");
            //}

            //var serviceProviderField = AppDomain.CurrentDomain.GetType()
            //       .GetField("_serviceProvider", BindingFlags.NonPublic | BindingFlags.Instance);


            return "forecast";
        });

        app.Run();

    }
    private static object FindRunningWebApplication()
    {
        // 获取当前 AppDomain 中加载的所有程序集
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        // 查找包含 WebApplication 实例的类型
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // 查找包含 WebApplication 实例的静态字段
                    var fields = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (var field in fields)
                    {
                        if (field.FieldType.FullName == "Microsoft.AspNetCore.Builder.WebApplication")
                        {
                            return field.GetValue(null);
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 忽略无法加载的程序集
            }
        }

        return null;
    }

    static WebApplication FindWebApplicationInstance()
    {
        // 通过 AppDomain 查找所有加载的程序集
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                // 查找包含 WebApplication 的类型
                foreach (var type in assembly.GetTypes())
                {
                    if (type.Name == "Program" || type.Name == "Startup")
                    {
                        // 查找静态字段或属性
                        foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            if (field.FieldType == typeof(WebApplication))
                            {
                                return (WebApplication)field.GetValue(null);
                            }
                        }

                        foreach (var prop in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            if (prop.PropertyType == typeof(WebApplication))
                            {
                                return (WebApplication)prop.GetValue(null);
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException) { }
            catch (Exception) { }
        }
        return null;
    }

}
