namespace Polydigm.Errors
{
    public abstract class NamedExceptionBase(string name, string type, string? message = null, Exception? innerException = null) 
        : TypedExceptionBase(type, message, innerException)
    {
        public virtual string Name => name;
    }
}
