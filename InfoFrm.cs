using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Test0311
{
    public partial class InfoFrm : Form
    {
        public InfoFrm(string strInfo)
        {
            InitializeComponent();
            rtxtInfo.Text = strInfo;
        }

        private void InfoFrm_Load(object sender, EventArgs e)
        {

        }

        
    }
}
