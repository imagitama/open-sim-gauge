namespace OpenGaugeServer
{
    public static class PathHelper
    {
        public static string GetFilePath(string relativePath, bool forceToGitRoot = true)
        {
            if (Path.IsPathRooted(relativePath))
                return relativePath;

            string baseDir = AppContext.BaseDirectory;

            var absolutePath = Path.GetFullPath(Path.Combine(baseDir, relativePath));

            return absolutePath;
        }
    }
}