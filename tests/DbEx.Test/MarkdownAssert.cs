using NUnit.Framework;

namespace DbEx.Test
{
    /// <summary>
    /// Compares markdown text independent of the platform line endings (CRLF versus LF) and leading/trailing whitespace.
    /// </summary>
    internal static class MarkdownAssert
    {
        public static void AreEqual(string expected, string actual) => Assert.That(Normalize(actual), Is.EqualTo(Normalize(expected)));

        private static string Normalize(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    }
}
