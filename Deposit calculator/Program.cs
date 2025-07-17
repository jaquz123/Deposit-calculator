using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AptalaevCalculatorNew2
{
    internal static class Program
    {
        public static string UserRole { get; set; }
        public static bool IsAutorization { get; set; }

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AutorizationForm form = new AutorizationForm();
            Application.Run(form);

            if (IsAutorization)
            {
                Application.Run(new Form1());
            }
        }
    }
}
