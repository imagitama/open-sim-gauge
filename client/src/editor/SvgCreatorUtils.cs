namespace OpenGaugeClient.Editor
{
    public static class SvgCreatorUtils
    {
        public static async Task<SvgCreator> LoadSvgCreator(string path)
        {
            Console.WriteLine($"[SvgCreatorUtils] Load path={path}");
            var svgCreator = await JsonHelper.LoadTypedJson<SvgCreator>(path);
            svgCreator.Source = path;
            return svgCreator;
        }

        public static async Task SaveSvgCreator(SvgCreator svgCreator, string path)
        {
            Console.WriteLine($"[SvgCreatorUtils] Save creator={svgCreator} path={path}");
            await JsonHelper.SaveJson(svgCreator, path);
        }
    }
}