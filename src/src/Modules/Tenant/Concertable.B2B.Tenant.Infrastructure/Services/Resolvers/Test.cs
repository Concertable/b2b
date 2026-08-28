using System;
using System.Linq;

namespace Probe;

public readonly record struct TenantContact(string Name, string Email);

public static class Program
{
    public static void Main()
    {
        var empty = Array.Empty<TenantContact>();

        TenantContact? result = empty.Select(v => new TenantContact(v.Name, v.Email)).FirstOrDefault();

        Console.WriteLine($"HasValue={result.HasValue}");
        Console.WriteLine($"Value={result.Value}");
        Console.WriteLine($"Name is null: {result.Value.Name is null}");
    }
}
