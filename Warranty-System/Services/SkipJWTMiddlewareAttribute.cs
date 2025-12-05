namespace Warranty_System.Services
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SkipJWTMiddlewareAttribute : Attribute
    {
    }
}
