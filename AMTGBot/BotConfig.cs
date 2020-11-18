using System.ComponentModel;
namespace System.Runtime.CompilerServices
{
    // C# 9 requires this class to be defined to act as modreq in records and init-only members.
    // This is defined in .NET 5 but not lower-level targets like .NET Standard 2.0.
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IsExternalInit { }
}

namespace AMTGBot
{
    public sealed record BotConfig
    {
        public string Token { get; init; }
        public int ComputationTimeLimit { get; init; } = 5000;
        public long PhotoStorageChatId { get; init; }
        public int SimplifyComplexityThreshold { get; init; } = 30;
    }
}
