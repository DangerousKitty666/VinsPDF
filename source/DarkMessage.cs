using System;
using System.Drawing;
using System.Windows.Forms;
namespace VinsPDF {
internal static class DarkMessage {
    sealed class Dialog : DarkWindow {
        public Dialog() { Resizable=false; }
        public void AddTitle() { InstallTitle(); }
    }
    public static DialogResult Show(IWin32Window owner,string text,string title="VinsPDF",MessageBoxButtons buttons=MessageBoxButtons.OK,MessageBoxIcon icon=MessageBoxIcon.None,MessageBoxDefaultButton defaultButton=MessageBoxDefaultButton.Button1) {
        using(var form=new Dialog()) {
            form.Text=title; form.Width=550; form.Font=new Font("Segoe UI",9); form.StartPosition=FormStartPosition.CenterParent;
            var parent=owner as Form; if(parent!=null)form.Icon=parent.Icon;
            int height=TextRenderer.MeasureText(text,form.Font,new Size(510,1000),TextFormatFlags.WordBreak).Height;
            form.Height=Math.Min(680,Math.Max(160,height+110));
            var body=new TextBox { Text=text,ReadOnly=true,Multiline=true,BorderStyle=BorderStyle.None,BackColor=Color.Black,ForeColor=Color.Gainsboro,Dock=DockStyle.Fill,TabStop=false,ScrollBars=ScrollBars.Vertical };
            var content=new Panel { Dock=DockStyle.Fill,Padding=new Padding(18) }; content.Controls.Add(body);
            var footer=new FlowLayoutPanel { Dock=DockStyle.Bottom,Height=46,Padding=new Padding(8,4,8,4),FlowDirection=FlowDirection.RightToLeft,BackColor=Color.Black };
            Button first;
            if(buttons==MessageBoxButtons.YesNo) {
                var no=Button("No",DialogResult.No); var yes=Button("Yes",DialogResult.Yes); footer.Controls.Add(no); footer.Controls.Add(yes); first=defaultButton==MessageBoxDefaultButton.Button2?no:yes; form.CancelButton=no;
            } else { first=Button("OK",DialogResult.OK); footer.Controls.Add(first); form.CancelButton=first; }
            form.AcceptButton=first; form.Controls.Add(content); form.Controls.Add(footer); form.AddTitle(); form.Shown+=delegate { first.Focus(); }; return form.ShowDialog(owner);
        }
    }
    static Button Button(string text,DialogResult result) { return new Button { Text=text,DialogResult=result,Width=80,Height=28,BackColor=Color.FromArgb(18,18,18),ForeColor=Color.Gainsboro,FlatStyle=FlatStyle.Flat }; }
}
}
