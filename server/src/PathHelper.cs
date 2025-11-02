namespace OpenGaugeServer
{
    public static class PathHelper
    {
        public static string GetProjectRootPath()
        {
#if DEBUG
            var dir = AppContext.BaseDirectory;
            var projectDir = Path.GetFullPath(Path.Combine(dir, @"../../../"));
            return projectDir;
#else
        return AppContext.BaseDirectory;
#endif
        }

        public static string GetFilePath(string relativePath)
        {
#if DEBUG
            if (!relativePath.Contains("config.json"))
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