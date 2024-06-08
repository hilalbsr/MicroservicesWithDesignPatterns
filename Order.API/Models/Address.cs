using Microsoft.EntityFrameworkCore;

namespace Order.API.Models
{
    /// <summary>
    /// Ayrı bir tablo olmasın. Order tablosu içerisinde kolonları olsun.
    /// </summary>
    [Owned]
    public class Address
    {
        public string Line { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
    }
}