namespace EasyScale.Core.Interop;

/// <summary>
/// 由原生调用返回的非零码构造的异常。
/// Core 层不含本地化文本，界面层据此错误码呈现提示，避免语义漂移。
/// </summary>
public sealed class NativeCallException : Exception
{
    public string Operation { get; }
    public int ErrorCode { get; }

    public NativeCallException(string operation, int errorCode)
        : base($"{operation} failed with Win32 error {errorCode}.")
    {
        Operation = operation;
        ErrorCode = errorCode;
    }
}

/// <summary>集中处理原生返回码，避免在调用处散落魔法数字。</summary>
internal static class Win32Error
{
    internal const int Success = 0;

    internal static void ThrowIfFailed(int code, string operation)
    {
        if (code != Success)
        {
            throw new NativeCallException(operation, code);
        }
    }
}
