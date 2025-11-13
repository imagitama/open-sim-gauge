namespace OpenGaugeClient
{
    public static class PathHelper
    {
        public static string GetFilePath(string relativePath, bool forceToGitRoot = true)
        {
            if (Path.IsPathRooted(relativePath))
                return relativePath;

            string baseDir;

#if DEBUG
            if (forceToGitRoot)
                baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../../"));
            else
                baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
#else
            baseDir = AppContext.BaseDirectory;
#endif

            var absolutePath = Path.GetFullPath(Path.Combine(baseDir, relativePath));

            return absolutePath;
        }

        public static string GetShortFileName(string path)
        {
            var fileName = Path.GetFileName(path?.ToString() ?? string.Empty);
            const int maxLength = 15;

            if (fileName.Length > maxLength)
                fileName = "..." + fileName[^maxLength..];

            return fileName;
        }

        public static string GetFileName(string path)
        {
            var fileName = Path.GetFileName(path?.ToString() ?? string.Empty);
            return fileName;
        }
    }
}