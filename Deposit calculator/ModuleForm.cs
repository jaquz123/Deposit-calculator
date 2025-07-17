using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AptalaevCalculatorNew2
{
    public partial class ModuleForm : Form
    {
        private BankProduct _product;
        public ModuleForm(BankProduct product)
        {
            InitializeComponent();
            _product = product;

            button1.Click += button1_Click; // Переход на сайт

            //Заполняем данные на форме
            lblBankName.Text = product.BankName;
            lblRate.Text = $"{product.Rate}%";
            lblDays.Text = $"{product.Days} дней";
           // lblAmount.Text=product.AmountRange; //Диапазон показываем
            pictureBox1.Image=product.BankImage;
            label2.Text = product.Description;
            
            textBox1.Text=_product.MinAmount.ToString("N0");

            textBox1.TextChanged += (s, e) => CalculateResults();
            CalculateResults();

            ////Расчитываем и показываем доход 
            //decimal finalAmount=CalculateFinalAmount(product.InitialAmount,product.Rate,product.Days);
            //lblFinalAmount.Text=finalAmount.ToString("N2");
            //lblProfit.Text = (finalAmount - product.InitialAmount).ToString("N2");
            void CalculateResults()
            {
                if(decimal.TryParse(textBox1.Text,out decimal amount))
                {
                    decimal finalAmount = amount * (1 + (decimal)(_product.Rate / 100) * _product.Days / 365);
                    lblFinalAmount.Text=finalAmount.ToString("N2");
                    lblProfit.Text = (finalAmount - amount).ToString("N2");
                }
                else
                {
                    lblFinalAmount.Text = "-";
                    lblProfit.Text = "-";
                }
            }

        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public ModuleForm(List<BankProduct> products)
        {
        }

        private decimal CalculateFinalAmount(decimal amount,double rate,int days)
        {
            //Формула простых процентов
            return amount + amount * (decimal)(rate / 100) * days / 365;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(_product.WebsiteUrl);
        }
    }
}
