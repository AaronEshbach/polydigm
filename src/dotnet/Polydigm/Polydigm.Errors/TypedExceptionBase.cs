namespace Polydigm.Errors
{
    public abstract class TypedExceptionBase(string type, string? message = null, Exception? innerException = null)
        : Exception(message, innerException)
    {
        public virtual string Type => type;
    }
}
