[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DataSourceNameAttribute : Attribute
{
    public string Name { get; }
    public DataSourceNameAttribute(string name) => Name = name;
}
