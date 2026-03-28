namespace PdfMasterIndex.Service.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class LifetimeAttribute(ServiceLifetime lifetime) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;
}