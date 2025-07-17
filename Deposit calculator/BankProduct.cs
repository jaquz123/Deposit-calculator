using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptalaevCalculatorNew2
{
    public class BankProduct
    {
        public string BankName { get; set; }
        public double Rate { get; set; } //Ставка в процентах
        public int Days { get; set; } //Срок в днях
        public string AmountRange { get; set; }
        public decimal MinAmount { get; set; }
        public string Description { get; set; }
        public Image BankImage { get; set; } //Картинка банка
        public decimal InitialAmount { get; set; } //Стартовая сумма для расчета
        public  string WebsiteUrl { get; set; }
    }
}
