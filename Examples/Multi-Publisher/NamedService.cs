using Microsoft.Extensions.Logging;

namespace Publisher
{
    
    public class NamedService<T>
    {
        public string Name { get; set;}
        public T Service { get;set; }
    }
}
