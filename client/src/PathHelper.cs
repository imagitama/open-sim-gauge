namespace OpenGaugeClient
{
    public static class PathHelper
    {
        public static string GetProjectRootPath()
        {
#if DEBUG
            var dir = AppContext.BaseDirectory;
            var projectDir = Path.GetFullPath(Path.Combine(dir, @"../../../../"));
            return projectDir;
#else
        return AppContext.BaseDirectory;
#endif
        }

        public static string GetFilePath(string relativePath, bool useDevRoot = false)
        {
#if DEBUG
            if (!useDevRoot)
            {
                var dir = AppContext.BaseDirectory;
                var gitRepoRoot = Path.GetFullPath(Path.Combine(dir, @"../../../../../../"));
                return Path.Combine(gitRepoRoot, relativePath);
            }
#endif
            return Path.Combine(GetProjectRootPath(), relativePath);
        }
    }
}