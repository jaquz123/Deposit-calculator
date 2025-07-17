using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AptalaevCalculatorNew2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            AddComboBox();
            pnlTiers.Visible = false;
            rdoFixed.CheckedChanged += RateTypeChanged;
            rdoByAmount.CheckedChanged += RateTypeChanged;
            rdoByTerm.CheckedChanged += RateTypeChanged;
        }
        private void RateTypeChanged(object sender, EventArgs e)
        {
            pnlTiers.Visible = rdoByAmount.Checked || rdoByTerm.Checked; //rdo-radioButton
            txtRate.Enabled = rdoFixed.Checked;
        }

        private void txtAmount_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;
            if (!Char.IsDigit(number) && number != 8) //Цифры и клавиша BackSpace
            {
                e.Handled = true;
            }
        }

        private void txtRate_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;
            if (!Char.IsDigit(number) && number != 8) //Цифры и клавиша BackSpace
            {
                e.Handled = true;
            }
        }
        private void AddComboBox() //Добавление в comboBox 
        {
            cmbCurrency.Items.AddRange(new[] { "RUB", "KZT", "BYN", "EUR", "USD" });
        }


        private decimal GetRate(decimal principal, int termValue)
        {
            string rate = txtRate.Text;
            if(rate=="")
            {
                MessageBox.Show("Укажите процентную ставку", "Окно ошибки", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                return 0;
            }
            if (rdoFixed.Checked)
                return decimal.Parse(rate) / 100m;
                

            var tiers = new List<(decimal Threshold, decimal Rate)>();
            dgvTiers.EndEdit();
            foreach (DataGridViewRow row in dgvTiers.Rows)
            {
                if (row.IsNewRow) continue;

                decimal th = Convert.ToDecimal(row.Cells["AmountFrom"].Value); //Сумма от
                decimal rt = Convert.ToDecimal(row.Cells["Rate"].Value) / 100m; //Ставка

                if (th == null || rt == null) continue;
                if (string.IsNullOrWhiteSpace(th.ToString()) || string.IsNullOrWhiteSpace(rt.ToString())) continue;

                tiers.Add((th, rt));
            }

            decimal key = rdoByAmount.Checked ? principal : termValue;
            decimal selectedRate = 0;
            decimal maxThreshold = 0;
            foreach (var t in tiers)
            {
                if (key >= t.Threshold && t.Threshold >= maxThreshold)
                {
                    maxThreshold = t.Threshold;
                    selectedRate = t.Rate;
                }
            }
            return selectedRate;
        }

        private List<DateTime> BuildDates(DateTime start, DateTime end, string freq)
        {
            var dates = new List<DateTime> { start };
            DateTime cursor = start;
            while (cursor < end)
            {
                DateTime next;
                switch (freq)
                {
                    case "Каждый день": next = cursor.AddDays(1); break;
                    case "Каждую неделю": next = cursor.AddDays(7); break;
                    case "Раз в месяц": next = cursor.AddMonths(1); break;
                    case "Раз в квартал": next = cursor.AddMonths(3); break;
                    case "Раз в полгода": next = cursor.AddMonths(6); break;
                    case "Раз в год": next = cursor.AddYears(1); break;
                    default: next = end; break;
                }
                cursor = next > end ? end : next;
                dates.Add(cursor);
            }
            if (dates.Last() != end) dates.Add(end);
            return dates;
        }

        private class ScheduleRecord
        {
            //Это колонки из DataGridView
            public DateTime Date { get; set; }
            public string DateText { get; set; }
            public decimal Accrued { get; set; } //Начисленно процентов
            public decimal Change { get; set; } // Изменение баланса
            public decimal Payout { get; set; } //Выплаты
            public decimal Balance { get; set; }
        }

        private void btnCalculate_Click_1(object sender, EventArgs e)
        {
            decimal principal=0;
            if (txtAmount.Text!="")
            {
               principal = decimal.Parse(txtAmount.Text); //Получаем сумму вклада 50 000руб
            }
            else
            {
                MessageBox.Show("Введите сумму вклада","Окно ошибки",MessageBoxButtons.OKCancel,MessageBoxIcon.Error);
            }
            
            int termValue = (int)numTermValue.Value; //Получаем срок 12
            string termUnit="";
            if(cmbTermUnit.SelectedItem!=null)
            {
                termUnit = cmbTermUnit.SelectedItem.ToString(); // Получаем месяцев
            }
            else
            {
                MessageBox.Show("Выберите мясяцы,недели,года", "Окно ошибки", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                return;
            }

            bool capitalization = checkBox1.Checked;
            DateTime startDate = dtpStartDate.Value.Date; //Получаем дату

            //Ставка процента
            decimal ratePercent = GetRate(principal, termValue);

            //Дата окончания
            DateTime endDate;
            switch (termUnit)
            {
                case "Дней": endDate = startDate.AddDays(termValue); break;
                case "Месяцев": endDate = startDate.AddMonths(termValue); break;
                case "Лет": endDate = startDate.AddYears(termValue); break;
                default: endDate = startDate; break;
            }
            var records = new List<ScheduleRecord>();

            //Начальная запись с депозитом
            records.Add(new ScheduleRecord
            {
                //Это все из класса
                Date = startDate,
                Accrued = 0, //Начисленно процентов
                Change = principal, //Изменение баланса
                Payout = 0, //Выплаты
                Balance = principal //Баланс
            });

            //Периодические даты
            try
            {
                string frequency = "";
                if(cmbFrequency.SelectedItem!=null)
                {
                    frequency = cmbFrequency.SelectedItem.ToString();
                    
                }
                else
                {
                    MessageBox.Show("Выберите частоту выплат(дни,недели,месяцы)", "Окно ошибки", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                    return;
                }

                var dates = BuildDates(startDate, endDate,frequency);
                decimal balance = principal;
                decimal totalAccrued = 0;
                DateTime prev = startDate;

                foreach (var date in dates.Skip(1))
                {
                    //Начисленные проценты за период
                    double days = (date - prev).TotalDays;
                    decimal accrued = balance * ratePercent * (decimal)(days / 365.0);
                    totalAccrued += accrued;

                    //Выплата процентов (без капитализации)
                    //Капитализация
                    if (capitalization)
                    {
                        balance += accrued;
                    }
                    decimal payout = accrued;


                    //Баланс остается неизменных(депозит не капитализируется)

                    records.Add(new ScheduleRecord
                    {
                        Date = date,
                        Accrued = accrued,
                        Change = 0,
                        Payout = payout,
                        Balance = balance
                    });
                    prev = date;
                }
                //Итоговая строка
                records.Add(new ScheduleRecord
                {
                    DateText = "Итого:",
                    Accrued = totalAccrued,
                    Change = 0,
                    Payout = 0,
                    Balance = balance
                });

                //Вывод
                dgvSchedule.Rows.Clear();
                foreach (var r in records)
                {
                    dgvSchedule.Rows.Add(
                        r.DateText ?? r.Date.ToShortDateString(),
                        r.Accrued == 0 ? string.Empty : r.Accrued.ToString("N2") + " P",
                        r.Change == 0 ? string.Empty : "+ " + r.Change.ToString("N2") + " P",
                        r.Payout == 0 ? string.Empty : r.Payout.ToString("N2") + " P",
                        r.Balance.ToString("N2") + " P"
                    );
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Установите частоту выплат(месяцы,дни,недели и тд)", "Окно ошибки", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                return;
                throw;
            }
            
           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var products = new BankProduct
            {
                BankName = "ДОМ.РФ",
                Rate = 20.0,
                Days = 181,
                AmountRange = "10 000 - 15млн.Р",
                Description = "ДОМа надежно",
                BankImage = Properties.Resources.wgrR2hufPeg,
                InitialAmount = 1000000,
                //MinAmount = ParseMinAmount("10 000 - 15млн.Р")
                WebsiteUrl = "https://www.banki.ru/products/deposits/marketplace/?id=24376&amount=1000000&period=181&rate=20&rateId=2889028"
            };
            ModuleForm moduleForm = new ModuleForm(products);
            moduleForm.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var products = new BankProduct
            {
                BankName = "Локо-Банк",
                Rate = 20.5,
                Days = 182,
                AmountRange = "300 000 - 50млн.Р",
                Description = "Локо-Вклад Банки.ру",
                BankImage = Properties.Resources.gtn5b6NW474,
                InitialAmount = 3000000,
                WebsiteUrl = "https://www.banki.ru/products/deposits/marketplace/?id=24585&amount=300000&period=182&rate=20.5&rateId=2890057"
            };
            ModuleForm moduleForm = new ModuleForm(products);
            moduleForm.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var products = new BankProduct
            {
                BankName = "КАМКОМБАНК",
                Rate = 20.25,
                Days = 186,
                AmountRange = "10 000 - 10млн.Р",
                Description = "Онлайн",
                BankImage = Properties.Resources.HLMWgG1Rquw,
                InitialAmount = 10000,
                WebsiteUrl= "https://www.banki.ru/products/deposits/marketplace/?id=24460&amount=10000&period=186&rate=20.25&rateId=2889304"
            };
            ModuleForm moduleForm = new ModuleForm(products);
            moduleForm.ShowDialog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var products = new BankProduct
            {
                BankName = "МТС Банк",
                Rate = 20.51,
                Days = 91,
                AmountRange = "10 000",
                Description = "МТС Вклад плюс",
                BankImage = Properties.Resources._7e8G01uWaa0,
                InitialAmount = 10000,
                WebsiteUrl= "https://www.mtsbank.ru/"
            };
            ModuleForm moduleForm = new ModuleForm(products);
            moduleForm.ShowDialog();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var products = new BankProduct
            {
                BankName = "ВТБ",
                Rate = 19.00,
                Days = 91,
                AmountRange = "1 000 - 1млн.Р",
                Description = "Накопительный ВТБ-Счет",
                BankImage = Properties.Resources.OOIqirmRDBg,
                InitialAmount = 10000,
                WebsiteUrl= "https://www.vtb.ru/personal/vklady-i-scheta/"
            };
            ModuleForm moduleForm = new ModuleForm(products);
            moduleForm.ShowDialog();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            var products = new BankProduct
            {
                BankName = "Банк ТКБ",
                Rate = 18.75,
                Days = 367,
                AmountRange = "100 000 - 3млн.Р",
                Description = "ТКБ.Потенциальный доход",
                BankImage = Properties.Resources.AAnOc93WXwc,
                InitialAmount = 100000,
                WebsiteUrl = "https://www.banki.ru/products/deposits/marketplace/?id=24810&amount=100000&period=367&rate=18.75&rateId=2891227"
            };
            ModuleForm moduleForm = new ModuleForm(products);
            moduleForm.ShowDialog();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if(Program.UserRole == "user")
            {
                tabControl1.TabPages.Remove(tabPage3);
            }
            else if(Program.UserRole == "admin")
            {

            }
        }
    }
}
