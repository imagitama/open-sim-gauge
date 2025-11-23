using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using SkiaSharp;
using OpenGaugeClient;

// avalonia has no public API to load fonts at runtime
internal static class RuntimeGlyphFactory
{
    private static readonly ConstructorInfo GlyphCtor;

    static RuntimeGlyphFactory()
    {
        var skiaAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Avalonia.Skia")
            ?? throw new Exception("Avalonia.Skia assembly not loaded");

        var glyphType = skiaAssembly.GetType("Avalonia.Skia.GlyphTypefaceImpl", throwOnError: true) ?? throw new Exception("Type not found");

        GlyphCtor = glyphType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(SKTypeface), typeof(FontSimulations) },
            modifiers: null
        ) ?? throw new Exception("Could not find GlyphTypefaceImpl(SKTypeface, FontSimulations) constructor.");
    }

    public static IGlyphTypeface Create(SKTypeface skTypeface)
    {
        return (IGlyphTypeface)GlyphCtor.Invoke([skTypeface, FontSimulations.None]);
    }
}

public class CustomFontCollection : FontCollectionBase
{
    public override Uri Key => new Uri("fonts:Custom");
    public override int Count => _families.Count;
    public override FontFamily this[int index] => _families[index];
    public override IEnumerator<FontFamily> GetEnumerator() => _families.GetEnumerator();

    private List<FontFamily> _families = []; // leave empty as we override anyway
    private Dictionary<string, IGlyphTypeface> _glyphTypefacesByPath = [];
    private Dictionary<string, IGlyphTypeface> _glyphTypefacesByFamilyName = [];
    private Dictionary<string, string> _absolutePathToFamilyNameMap = [];

    public override void Initialize(IFontManagerImpl fontManager)
    {
        if (ConfigManager.Config.Debug)
            Console.WriteLine("[FontProvider] Initialize");
    }

    public override bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
    {
        glyphTypeface = null;

        if (_glyphTypefacesByFamilyName.TryGetValue(familyName, out IGlyphTypeface? value))
        {
            glyphTypeface = value;
            return true;
        }

        return false;
    }

    public Typeface GetTypefaceFromPath(string absolutePath, string? familyName = null)
    {
        if (!_absolutePathToFamilyNameMap.ContainsKey(absolutePath) || !_glyphTypefacesByPath.ContainsKey(absolutePath))
        {
            familyName = RegisterFontFile(absolutePath);
        }

        if (familyName == null)
            familyName = _absolutePathToFamilyNameMap[absolutePath];

        var uri = $"{Key}#{familyName}";

        var fontFamily = new FontFamily(uri);

        var typeface = new Typeface(fontFamily);

        return typeface;
    }

    public string RegisterFontFile(string absolutePath, string? familyName = null)
    {
        familyName ??= Path.GetFileNameWithoutExtension(absolutePath);

        var uri = $"{Key}#{familyName}";

        var family = new FontFamily(uri);
        _families.Add(family);

        using var stream = File.OpenRead(absolutePath);
        var skTypeface = SKTypeface.FromStream(stream);

        var actualFamilyName = skTypeface.FamilyName;

        var glyphTypeface = RuntimeGlyphFactory.Create(skTypeface);

        _glyphTypefacesByPath[absolutePath] = glyphTypeface;
        _glyphTypefacesByFamilyName[familyName] = glyphTypeface;
        _absolutePathToFamilyNameMap[absolutePath] = familyName;

        if (ConfigManager.Config.Debug)
            Console.WriteLine($"[CustomFontCollection] Registered font file '{absolutePath}' as '{uri}' (actual '{actualFamilyName}')");

        FontManager.Current.AddFontCollection(this);

        return uri;
    }
}

namespace OpenGaugeClient
{
    public class FontProvider
    {
        public CustomFontCollection myCollection = new();

        public FontProvider()
        {
            // TODO: iterate over each font file
            var gordonFontPath = PathHelper.GetFilePath("fonts/Gordon.ttf", forceToGitRoot: false);
            myCollection.RegisterFontFile(gordonFontPath);

            // var myFont = PathHelper.GetFilePath("/Users/jared/Downloads/baby-plums-font/BabyPlums-rv2gL.ttf", forceToGitRoot: false);
            // myCollection.RegisterFontFile(myFont, "Baby Plums");

            FontManager.Current.AddFontCollection(myCollection);
        }

        public Typeface GetTypefaceFromPath(string absolutePath, string? familyName = null)
        {
            return myCollection.GetTypefaceFromPath(absolutePath, familyName);
        }

        public Typeface GetTypefaceFromFamilyName(string familyName)
        {
            var uri = $"{myCollection.Key}#{familyName}";

            var fontFamily = new FontFamily(uri);

            var typeface = new Typeface(fontFamily);

            return typeface;
        }
    }
}