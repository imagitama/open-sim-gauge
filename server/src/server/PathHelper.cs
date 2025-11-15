namespace OpenGaugeServer
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
                baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../"));
            else
                baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
#else
            baseDir = AppContext.BaseDirectory;
#endif

            var absolutePath = Path.GetFullPath(Path.Combine(baseDir, relativePath));

            return absolutePath;
        }
    }
}