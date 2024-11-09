using WMS_UI_API.Exceptions;

namespace WMS_UI_API.Middlewares
{
    public class DomainNotFoundException : DomainException
    {
        public DomainNotFoundException(string message) : base(message)
        {

        }
    }
}
