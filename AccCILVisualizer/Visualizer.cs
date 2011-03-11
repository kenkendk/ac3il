using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AccCILVisualizer
{
    public partial class Visualizer : Form
    {
        public Visualizer(IEnumerable<AccCIL.IR.MethodEntry> methods)
        {
            InitializeComponent();

            tv.BeginUpdate();

            tv.Nodes.Clear();

            foreach (var m in methods)
            {
                TreeNode rn = new TreeNode(m.Method.ToString());
                rn.Tag = m;
                
                foreach(var el in m.Childnodes)
                    AddRecursive(el, rn);

                tv.Nodes.Add(rn);
                rn.ExpandAll();
            }

            tv.EndUpdate();
        }

        private void AddRecursive(AccCIL.IR.InstructionElement el, TreeNode parentNode)
        {
            TreeNode n = new TreeNode(el.Instruction.OpCode.ToString());
            n.Tag = el;
            parentNode.Nodes.Add(n);

            foreach (var elc in el.Childnodes)
                AddRecursive(elc, n);
        }

        private void tv_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                data.BeginUpdate();
                data.Nodes.Clear();

                if (e != null && e.Node != null & e.Node.Tag != null)
                {
                    foreach (System.Reflection.PropertyInfo pi in e.Node.Tag.GetType().GetProperties(System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    {
                        object v = pi.GetValue(e.Node.Tag, null);
                        if (v == null)
                            v = "<null>";

                        TreeNode n = data.Nodes.Add(pi.Name + ": " + v.ToString());
                        n.ExpandAll();
                    }

                    foreach (System.Reflection.FieldInfo fi in e.Node.Tag.GetType().GetFields(System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    {
                        object v = fi.GetValue(e.Node.Tag);
                        if (v == null)
                            v = "<null>";

                        TreeNode n = data.Nodes.Add(fi.Name + ": " + v.ToString());
                        n.ExpandAll();
                    }
                }
            }
            finally
            {
                data.EndUpdate();
            }
        }
    }
}
