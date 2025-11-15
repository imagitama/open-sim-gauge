using System.Reflection;

namespace OpenGaugeServer
{
    public static class Cli
    {
        public static Dictionary<string, string?> ParseArgs(string[] args)
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (!arg.StartsWith("--"))
                    continue;

                var key = arg.Substring(2);

                string? value = null;

                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    value = args[++i];
                }

                dict[key] = value ?? "true";
            }

            return dict;
        }

        public static void ApplyArgsToConfig(object config, Dictionary<string, string?> args)
        {
            foreach (var (key, val) in args)
            {
                ApplyNestedProperty(config, key.Split('.'), val);
            }
        }

        private static void ApplyNestedProperty(object obj, string[] parts, string? stringValue)
        {
            var type = obj.GetType();
            var propName = parts[0];

            var prop = type.GetProperty(
                propName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
            );

            if (prop == null || !prop.CanWrite)
                return;

            if (parts.Length == 1)
            {
                SetConvertedValue(obj, prop, stringValue);
                return;
            }

            var nested = prop.GetValue(obj);
            if (nested == null)
            {
                nested = Activator.CreateInstance(prop.PropertyType);
                prop.SetValue(obj, nested);
            }

            if (nested == null)
                throw new Exception("Is null");

            ApplyNestedProperty(nested, parts.Skip(1).ToArray(), stringValue);
        }

        private static void SetConvertedValue(object target, PropertyInfo prop, string? raw)
        {
            var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            object? converted;

            if (t == typeof(bool))
            {
                converted = raw == null ? true : bool.Parse(raw);
            }
            else if (t.IsEnum)
            {
                converted = Enum.Parse(t, raw!, true);
            }
            else
            {
                converted = Convert.ChangeType(raw, t);
            }

            prop.SetValue(target, converted);
        }

    }
}