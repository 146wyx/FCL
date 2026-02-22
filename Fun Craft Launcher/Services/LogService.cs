using System;
using System.IO;
using System.Text;

namespace FunCraftLauncher.Services
{
    public class LogService
    {
        private static LogService? _instance;
        private static readonly object _lock = new object();
        private string? _logFilePath;
        private StreamWriter? _logWriter;

        public static LogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LogService();
                        }
                    }
                }
                return _instance;
            }
        }

        private LogService()
        {
            InitializeLogFile();
        }

        private void InitializeLogFile()
        {
            try
            {
                // 创建日志目录
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // 创建日志文件路径（使用当前日期）
                string fileName = $"log_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.log";
                _logFilePath = Path.Combine(logDirectory, fileName);

                // 初始化StreamWriter，设置为自动刷新
                _logWriter = new StreamWriter(_logFilePath, true, Encoding.UTF8) {
                    AutoFlush = true
                };

                // 写入启动日志
                WriteLog("INFO", "LogService", "日志服务已初始化");
                WriteLog("INFO", "LogService", $"日志文件路径: {_logFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化日志服务失败: {ex.Message}");
            }
        }

        public void WriteLog(string level, string category, string message)
        {
            try
            {
                if (_logWriter == null)
                {
                    InitializeLogFile();
                }

                if (_logWriter != null)
                {
                    string logEntry = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] [{level}] [{category}] {message}";
                    _logWriter.WriteLine(logEntry);
                    Console.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入日志失败: {ex.Message}");
            }
        }

        public void WriteInfo(string category, string message)
        {
            WriteLog("INFO", category, message);
        }

        public void WriteError(string category, string message, Exception? ex = null)
        {
            if (ex != null)
            {
                message += $"\n异常信息: {ex.Message}\n堆栈跟踪: {ex.StackTrace}";
            }
            WriteLog("ERROR", category, message);
        }

        public void WriteWarning(string category, string message)
        {
            WriteLog("WARN", category, message);
        }

        public void WriteDebug(string category, string message)
        {
            WriteLog("DEBUG", category, message);
        }

        public void Dispose()
        {
            try
            {
                if (_logWriter != null)
                {
                    WriteLog("INFO", "LogService", "日志服务已关闭");
                    _logWriter.Close();
                    _logWriter.Dispose();
                    _logWriter = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"关闭日志服务失败: {ex.Message}");
            }
        }
    }
}
