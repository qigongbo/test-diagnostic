using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


// 订阅所有 DiagnosticListener
//var observer = new DiagnosticSourceObserver();
//using var subscription = DiagnosticListener.AllListeners.Subscribe(observer);

app.MapGet("/", () => "Hello World!");
app.MapGet("/users", () => "User List");
app.MapGet("/error", () => { throw new Exception("Test error"); });

app.Run();

public class DiagnosticSourceObserver : IObserver<DiagnosticListener>
{
    private IDisposable _aspNetCoreSubscription;

    public void OnCompleted() => Console.WriteLine("[DiagnosticSourceObserver] 事件流结束");
    public void OnError(Exception error) => Console.WriteLine($"[DiagnosticSourceObserver] 错误: {error.Message}");

    public void OnNext(DiagnosticListener listener)
    {
        Console.WriteLine("listener.Name: " + listener.Name);
        // 只处理 ASP.NET Core 的监听器
        if (listener.Name == "Microsoft.AspNetCore")
        {
            Console.WriteLine($"订阅 ASP.NET Core 事件: {listener.Name}");
            _aspNetCoreSubscription?.Dispose();
            _aspNetCoreSubscription = listener.Subscribe(new AspNetCoreObserver());
        }
    }
}

public class AspNetCoreObserver : IObserver<KeyValuePair<string, object>>
{
    private readonly ConcurrentDictionary<HttpContext, long> _requestStartTimes = new();

    public void OnCompleted() => Console.WriteLine("[AspNetCoreObserver] 事件流结束");
    public void OnError(Exception error) => Console.WriteLine($"[AspNetCoreObserver] 错误: {error.Message}");

    public void OnNext(KeyValuePair<string, object> evt)
    {
        // 取消注释以查看所有事件名称
        // Console.WriteLine($"收到事件: {evt.Key}");

        switch (evt.Key)
        {
            case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                ProcessRequestStart(evt.Value);
                break;

            case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                ProcessRequestStop(evt.Value);
                break;
        }
    }

    private void ProcessRequestStart(object eventData)
    {
        try
        {
            var httpContext = GetPropertyValue<HttpContext>(eventData, "HttpContext");
            if (httpContext == null) return;

            var path = httpContext.Request.Path;
            var method = httpContext.Request.Method;
            var traceId = httpContext.TraceIdentifier;

            var timestamp = Stopwatch.GetTimestamp();
            _requestStartTimes[httpContext] = timestamp;

            Console.WriteLine($"\n[请求开始] {DateTime.Now:HH:mm:ss.fff}");
            Console.WriteLine($"  路径: {path}");
            Console.WriteLine($"  方法: {method}");
            Console.WriteLine($"  TraceId: {traceId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理请求开始事件出错: {ex.Message}");
        }
    }

    private void ProcessRequestStop(object eventData)
    {
        try
        {
            var httpContext = GetPropertyValue<HttpContext>(eventData, "HttpContext");
            if (httpContext == null) return;

            var statusCode = httpContext.Response.StatusCode;
            var path = httpContext.Request.Path;
            var traceId = httpContext.TraceIdentifier;

            if (_requestStartTimes.TryRemove(httpContext, out var startTimestamp))
            {
                var duration = Stopwatch.GetElapsedTime(startTimestamp);
                Console.WriteLine($"\n[请求结束] {DateTime.Now:HH:mm:ss.fff}");
                Console.WriteLine($"  路径: {path}");
                Console.WriteLine($"  状态码: {statusCode}");
                Console.WriteLine($"  耗时: {duration.TotalMilliseconds:F2}ms");
                Console.WriteLine($"  TraceId: {traceId}");
            }
            else
            {
                Console.WriteLine($"\n[警告] 未找到开始记录: {traceId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理请求结束事件出错: {ex.Message}");
        }
    }

    private static T GetPropertyValue<T>(object obj, string propertyName)
    {
        try
        {
            var prop = obj.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

            if (prop != null && prop.GetValue(obj) is T value)
            {
                return value;
            }

            Console.WriteLine($"无法获取属性: {propertyName} from {obj.GetType().Name}");
            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取属性 {propertyName} 时出错: {ex.Message}");
            return default;
        }
    }
}