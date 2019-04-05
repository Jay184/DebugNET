using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DebugNET;

namespace DebugNETExample {
    public partial class MainForm : Form {
        public DebugNET.Debugger Debugger { get; set; }

        public MainForm() {
            InitializeComponent();
            RefreshProcesses();
        }

        private void btnRefresh_Click(object sender, EventArgs e) {
            RefreshProcesses();
        }
        private void btnAttach_Click(object sender, EventArgs e) {
            //if (Debugger == null || !Debugger.Attached) {

            //    if (listProcesses.SelectedIndex == -1) return;

            //    Debugger = new DebugNET.Debugger(listProcesses.SelectedItem.ToString());
            //    //if (Debugger.Attach()) {
            //    //    ( (Button)sender ).Text = "Detach";
            //    //}

            //} else if (Debugger.Detach()) {

            //    ( (Button)sender ).Text = "Attach";

            //}
        }

        private void RefreshProcesses() {
            listProcesses.Items.Clear();

            Process[] processes = Process.GetProcesses();
            string[] processNames = processes.Select(p => p.ProcessName).ToArray();
            listProcesses.Items.AddRange(processNames);
        }
    }
}
