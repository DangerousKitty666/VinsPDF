using System;
using System.Drawing;
using System.Windows.Forms;
namespace VinsPDF {
internal class SettingsWindow : DarkWindow {
 protected Panel Body=new Panel();protected Button Apply;
 public SettingsWindow(string title,int height){Text=title;Width=480;Height=height;Resizable=false;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",9);Body.Dock=DockStyle.Fill;Body.Padding=new Padding(14);Controls.Add(Body);var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=45,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(5)};Apply=MakeButton("Apply");Apply.DialogResult=DialogResult.OK;var cancel=MakeButton("Cancel");cancel.DialogResult=DialogResult.Cancel;footer.Controls.Add(cancel);footer.Controls.Add(Apply);Controls.Add(footer);InstallTitle();AcceptButton=Apply;CancelButton=cancel;}
 protected Button MakeButton(string text){return new Button{Text=text,Width=85,Height=28,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(20,20,20),ForeColor=Color.White};}
 protected Label LabelAt(string text,int x,int y,int width=180){var l=new Label{Text=text,Left=x,Top=y,Width=width,Height=24,ForeColor=Color.Silver};Body.Controls.Add(l);return l;}
 protected TextBox TextAt(string text,int x,int y,int width,int height=24,bool multiline=false){var t=new TextBox{Text=text,Left=x,Top=y,Width=width,Height=height,Multiline=multiline,AcceptsReturn=multiline,ScrollBars=multiline?ScrollBars.Vertical:ScrollBars.None,BackColor=Color.FromArgb(20,20,20),ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle};Body.Controls.Add(t);return t;}
 protected NumericUpDown Number(decimal value,decimal min,decimal max,int x,int y,int decimals=1){var n=new NumericUpDown{Left=x,Top=y,Width=88,Minimum=min,Maximum=max,DecimalPlaces=decimals,Value=Math.Max(min,Math.Min(max,value)),BackColor=Color.FromArgb(20,20,20),ForeColor=Color.White};Body.Controls.Add(n);return n;}
 protected ComboBox Choices(string[] values,int selected,int x,int y,int width=170){var c=new ComboBox{Left=x,Top=y,Width=width,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Color.FromArgb(20,20,20),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};c.Items.AddRange(values);c.SelectedIndex=Math.Max(0,selected);Body.Controls.Add(c);return c;}
 protected CheckBox Check(string text,bool value,int x,int y){var c=new CheckBox{Text=text,Checked=value,Left=x,Top=y,Width=205,ForeColor=Color.Silver};Body.Controls.Add(c);return c;}
}
internal sealed class RichTextDialog : SettingsWindow {
 readonly TextBox input;readonly ComboBox family;readonly NumericUpDown size;readonly CheckBox bold,italic;readonly TextStyle original;
 public RichTextDialog(string text,TextStyle style,bool fontOnly=false):base(fontOnly?"Font settings":"Edit text",fontOnly?240:400){original=style.Copy();LabelAt("Font",14,16);var families=PdfEngine.AvailableFamilies;family=Choices(families,Array.IndexOf(families,style.Family),85,12,210);LabelAt("Size",310,16,45);size=Number((decimal)style.Size,1,500,355,12);bold=Check("Bold",style.Bold,85,48);bold.Width=100;italic=Check("Italic",style.Italic,195,48);italic.Width=100;if(!fontOnly){LabelAt("Text · Enter = new line; blank line = paragraph",14,86,430);input=TextAt(text,14,112,430,164,true);LabelAt("Uses the selected embedded font. Spacing is set separately.",14,286,435);}else LabelAt("Applies to selected text, or sets the font for new text.",14,100,430);}
 public string Value {get{return input==null?null:input.Text;}}
 public TextStyle Style {get{var s=original.Copy();s.Family=family.Text;s.Size=(float)size.Value;s.Bold=bold.Checked;s.Italic=italic.Checked;return s;}}
}
internal sealed class SpacingDialog : SettingsWindow {
 readonly NumericUpDown line,paragraph,character;readonly TextStyle original;
 public SpacingDialog(TextStyle style):base("Text spacing",255){original=style.Copy();LabelAt("Line height (multiple)",14,20);line=Number((decimal)style.LineHeight,.5m,5,270,16,2);LabelAt("After blank line / paragraph (pt)",14,65,250);paragraph=Number((decimal)style.ParagraphGap,0,100,270,61);LabelAt("Between characters (pt)",14,110,240);character=Number((decimal)style.CharacterSpacing,-2,30,270,106);LabelAt("Applies to selected text or the next text you add.",14,152,430);}
 public TextStyle Style{get{var s=original.Copy();s.LineHeight=(float)line.Value;s.ParagraphGap=(float)paragraph.Value;s.CharacterSpacing=(float)character.Value;return s;}}
}
internal sealed class GridDialog : SettingsWindow {
 readonly NumericUpDown spacing,alpha;readonly CheckBox show,snap;
 public GridDialog(PageCanvas canvas):base("Grid",255){show=Check("Show grid",canvas.GridVisible,14,16);snap=Check("Snap selection to grid",canvas.SnapGrid,230,16);LabelAt("Spacing (mm)",14,65);spacing=Number((decimal)canvas.GridMm,1,100,260,61);LabelAt("Visibility (%)",14,110);alpha=Number((decimal)(canvas.GridAlpha/255f*100),5,80,260,106,0);LabelAt("Grid is a screen guide; it is never added to the PDF.",14,152,430);}
 public void ApplyTo(PageCanvas canvas){canvas.GridVisible=show.Checked;canvas.SnapGrid=snap.Checked;canvas.GridMm=(float)spacing.Value;canvas.GridAlpha=(int)(alpha.Value*255/100);canvas.Invalidate();}
}
internal sealed class HeaderFooterDialog : SettingsWindow {
 readonly TextBox header,footer;readonly ComboBox scope,align;readonly NumericUpDown margin;
 public HeaderFooterDialog():base("Add header / footer",350){LabelAt("Header",14,16);header=TextAt("",95,12,345);LabelAt("Footer",14,55);footer=TextAt("Page {page} of {pages}",95,51,345);LabelAt("Pages",14,100);scope=Choices(new[]{"Current page","All pages"},0,95,96);LabelAt("Alignment",14,140);align=Choices(new[]{"Left","Centre","Right"},1,95,136);LabelAt("Margin (mm)",14,180);margin=Number(12,3,50,150,176);LabelAt("{page} and {pages} insert page numbers. Uses current font.\nAdds new objects; Undo removes this operation.",14,220,430).Height=40;}
 public string Header{get{return header.Text;}}public string Footer{get{return footer.Text;}}public bool All{get{return scope.SelectedIndex==1;}}public int Alignment{get{return align.SelectedIndex;}}public float MarginMm{get{return(float)margin.Value;}}
}
}
