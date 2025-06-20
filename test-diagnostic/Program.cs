

namespace test_diagnostic;
// 自定义事件生产者
using System;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        // 1. 创建诊断监听器
        var listener = new DiagnosticListener("SimpleApp");

        // 2. 创建观察者并提前订阅
        var observer = new SimpleEventObserver();
        using var subscription = listener.Subscribe(observer);




        // 3. 创建事件源
        var source = new SimpleEventSource(listener);

        // 4. 执行工作
        Console.WriteLine("=== 工作执行 ===");
        source.DoWork();
    }
}

public class SimpleEventSource
{
    public readonly DiagnosticListener? _listener;

    // 通过构造函数传入监听器
    // https://github.com/dotnet/aspnetcore/blob/main/src/Hosting/Hosting/src/Internal/HostingApplication.cs#L24
    public SimpleEventSource(DiagnosticListener? listener)
    {
        _listener = listener;
    }

    public void DoWork()
    {
        // 现在IsEnabled()应该返回true
        if (_listener?.IsEnabled("Work.Started")==true)
        {
            Console.WriteLine("检测到有订阅者，发布开始事件");
            _listener.Write("Work.Started", "工作开始");
        }
        else
        {
            Console.WriteLine("警告：没有订阅者");
        }

        Console.WriteLine("框架执行，工作中...");
        System.Threading.Thread.Sleep(1000);

        _listener?.Write("Work.Completed", "工作完成");
    }
}

public class SimpleEventObserver : IObserver<KeyValuePair<string, object>>
{
    public void OnCompleted() => Console.WriteLine("事件流结束");
    public void OnError(Exception error) => Console.WriteLine($"错误: {error.Message}");

    public void OnNext(KeyValuePair<string, object> evt)
    {
        Console.WriteLine($"\n[收到事件] {evt.Key}");
        Console.WriteLine($"  内容: {evt.Value}");
        Console.WriteLine($"  时间: {DateTime.Now:HH:mm:ss.fff}");
    }
}