using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AptalaevCalculatorNew2
{
    public partial class AutorizationForm : Form
    {
        public AutorizationForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string userName=txtUserName.Text;
            string passworld=txtPassworld.Text;

            if(userName == "admin" && passworld == "admin")
            {
                Program.UserRole = "admin";
                Program.IsAutorization = true;
                this.Close();
            }
            else if (userName == "user" && passworld == "user")
            {
                Program.UserRole = "user";
                Program.IsAutorization = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль");
            }
        }
    }
}
