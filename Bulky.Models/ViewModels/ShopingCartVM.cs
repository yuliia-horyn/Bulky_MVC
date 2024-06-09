using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models.ViewModels
{
    public class ShopingCartVM
    {
        public IEnumerable <ShopingCart> ShopingCartList{ get; set; }
        public OrderHeader OrderHeader { get; set; }

    }
}
