using System.Reflection;

namespace WikitextParser.Tests;

public static class SampleTextLoader
{
    public static string Load(string name)
    {
        Assembly asm = typeof(SampleTextLoader).Assembly;
        string[] names = asm.GetManifestResourceNames();
        string path = names.SingleOrDefault(x => x.EndsWith(name))
            ?? throw new InvalidOperationException($"Stream `{name}` not found");

        using Stream s = asm.GetManifestResourceStream(path)!;
        using TextReader r = new StreamReader(s);

        return r.ReadToEnd();
    }

    public static string LoadSampleText1() => Load("WikiText1.txt");

    public static string LoadFictionalPage() => Load("FictionalPage.txt");
}
