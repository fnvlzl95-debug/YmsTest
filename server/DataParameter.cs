namespace YMS.Server;

public class DataParameter
{
    public string Name { get; }
    public object? Value { get; }

    public DataParameter(string name, object? value)
    {
        Name = name;
        Value = value;
    }
}
