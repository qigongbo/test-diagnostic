using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myHook
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System.Diagnostics;
    using System.Text.Json;

    public class CustomMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomMiddleware> _logger;

        public CustomMiddleware(RequestDelegate next, ILogger<CustomMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // 记录请求信息
            _logger.LogInformation("请求开始: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            // 添加自定义响应头
            context.Response.Headers.Add("X-Custom-Middleware", "Injected by StartupHook");

            // 保存原始响应流
            var originalBodyStream = context.Response.Body;

            try
            {
                // 使用内存流捕获响应内容
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                // 继续处理请求
                await _next(context);

                // 重置流位置并读取响应内容
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();

                // 记录响应信息
                _logger.LogInformation("请求完成: {Method} {Path} => {StatusCode} ({Elapsed}ms)",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                // 输出响应内容（仅用于调试，生产环境可能需要限制）
                if (context.Response.StatusCode >= 400)
                {
                    _logger.LogDebug("响应内容: {Response}", TruncateString(responseText, 500));
                }

                // 将响应内容写回原始流
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "请求处理过程中发生异常: {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                context.Response.StatusCode = 500;
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
                stopwatch.Stop();
            }
        }

        private string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }
    }
}
