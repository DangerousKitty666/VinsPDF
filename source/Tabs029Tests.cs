using System;using System.Drawing;using System.IO;using System.Windows.Forms;
namespace VinsPDF {
internal sealed partial class TabWindow {
 internal void TestTabs029(string directory){var original=active;var button=buttons[original];Version029Tests.Assert(tabTips.GetToolTip(button)==Path.GetFileName(original.DocumentPath),"Full filename tooltip missing");int count=DocumentCount;Open(null);Version029Tests.Assert(DocumentCount==count+1,"Home tab failed");Version028Tests.Capture(this,Path.Combine(directory,"home.png"));using(var test=new PageCanvas{Width=800,Height=600}){int opens=0;test.OpenRequested+=delegate{opens++;};Rectangle r=test.HomeLogoRectangle;Version029Tests.Assert(r.Left+r.Width/2==400&&r.Top+r.Height/2==300,"Home logo not centred");Version028Tests.Mouse(test,"OnMouseDown",new Point(400,300));Version028Tests.Mouse(test,"OnMouseDown",new Point(2,2));Version029Tests.Assert(opens==1,"Home logo click failed");}Version028Tests.Mouse(button,"OnMouseDown",new Point(button.Width-12,button.Height/2));Version028Tests.Mouse(button,"OnMouseUp",new Point(button.Width-12,button.Height/2));Version029Tests.Assert(DocumentCount==count,"Tab close button failed");}
}
}
