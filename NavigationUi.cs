using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class HomeButton : Button {
 public HomeButton(){Text="";AccessibleName="Voltar ao início";Size=new Size(64,60);FlatStyle=FlatStyle.Flat;BackColor=Color.FromArgb(4,35,68);ForeColor=Color.White;Cursor=Cursors.Hand;FlatAppearance.BorderColor=Color.FromArgb(100,177,224);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var p=new Pen(ForeColor,2)){g.DrawLines(p,new[]{new Point(16,27),new Point(32,12),new Point(48,27)});g.DrawLines(p,new[]{new Point(20,24),new Point(20,41),new Point(44,41),new Point(44,24)});g.DrawRectangle(p,29,30,7,11);}TextRenderer.DrawText(g,"Início",new Font("Segoe UI",8),new Rectangle(0,44,Width,14),ForeColor,TextFormatFlags.HorizontalCenter);}
}
public class HomeTile : Button {
 public int Symbol;public string Caption;
 public HomeTile(string caption,int symbol){Caption=caption;Symbol=symbol;Text="";AccessibleName=caption;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.FromArgb(4,29,62);ForeColor=Color.White;Cursor=Cursors.Hand;}
 protected override void OnResize(EventArgs e){base.OnResize(e);using(var path=new GraphicsPath()){path.AddEllipse(1,1,Math.Max(1,Width-2),Math.Max(1,Height-2));var old=Region;Region=new Region(path);if(old!=null)old.Dispose();}}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var fill=new SolidBrush(BackColor))g.FillEllipse(fill,1,1,Width-3,Height-3);using(var edge=new Pen(Color.FromArgb(135,186,220),1.5f))g.DrawEllipse(edge,2,2,Width-5,Height-5);
  var saved=g.Save();float scale=Width/190f;g.ScaleTransform(scale,scale);int x=59,y=51;using(var pen=new Pen(Color.FromArgb(209,232,246),3)){g.DrawRectangle(pen,x,y,72,49);if(Symbol==2)for(int i=0;i<3;i++){g.DrawRectangle(pen,x+3,y+4+i*15,9,10);g.DrawRectangle(pen,x+60,y+4+i*15,9,10);}if(Symbol==1)for(int i=0;i<3;i++){g.DrawLine(pen,x+12,y+12+i*11,x+19,y+12+i*11);g.DrawLine(pen,x+25,y+12+i*11,x+60,y+12+i*11);}if(Symbol>=2)g.DrawPolygon(pen,new[]{new Point(x+29,y+13),new Point(x+29,y+37),new Point(x+48,y+25)});if(Symbol!=2){g.DrawLine(pen,x+36,y+50,x+36,y+60);g.DrawLine(pen,x+18,y+61,x+54,y+61);}}using(var font=new Font("Segoe UI Light",16))using(var brush=new SolidBrush(ForeColor))using(var format=new StringFormat(){Alignment=StringAlignment.Center})g.DrawString(Caption,font,brush,new RectangleF(0,136,190,40),format);g.Restore(saved);
 }
}
public class QuietListBox : ListBox {
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool ShowScrollBar(IntPtr h,int bar,bool show);
 bool hiding; protected override void WndProc(ref Message m){base.WndProc(ref m);if(!hiding&&IsHandleCreated&&(m.Msg==15||m.Msg==133||m.Msg==5)){hiding=true;try{ShowScrollBar(Handle,3,false);}finally{hiding=false;}}}
}
public class MediaGrid : Panel {
 public sealed class Collection : List<object>{readonly MediaGrid owner;public Collection(MediaGrid owner){this.owner=owner;}public new void Add(object value){base.Add(value);owner.Changed();}public new void Clear(){base.Clear();owner.selection=-1;owner.Changed();}}
 public Collection Items;public Image DefaultArtwork;int selection=-1,suspended,scrollY;public bool HorizontalScrollbar{get;set;}
 public object SelectedItem{get{return selection>=0&&selection<Items.Count?Items[selection]:null;}}
 readonly Dictionary<string,Image> artwork=new Dictionary<string,Image>();readonly HashSet<string> pending=new HashSet<string>(),failed=new HashSet<string>();readonly SemaphoreSlim downloads=new SemaphoreSlim(4);bool closing;
 public MediaGrid(){Items=new Collection(this);AutoScroll=false;DoubleBuffered=true;BackColor=Color.FromArgb(9,45,84);TabStop=true;Resize+=(s,e)=>Changed();MouseDown+=(s,e)=>{Focus();int col=e.X/CellWidth,row=(e.Y+scrollY)/CellHeight;int index=row*Columns+col;if(col<Columns&&index<Items.Count){selection=index;Invalidate();}};KeyDown+=(s,e)=>{int next=selection;if(e.KeyCode==Keys.Right)next++;else if(e.KeyCode==Keys.Left)next--;else if(e.KeyCode==Keys.Down)next+=Columns;else if(e.KeyCode==Keys.Up)next-=Columns;else if(e.KeyCode==Keys.Enter){OnDoubleClick(EventArgs.Empty);return;}else return;selection=Math.Max(0,Math.Min(Items.Count-1,next));scrollY=Math.Max(0,Math.Min(MaxScroll,(selection/Columns)*CellHeight));Invalidate();e.Handled=true;};}
 int Columns{get{return Math.Max(1,ClientSize.Width/170);}}int CellWidth{get{return Math.Max(150,ClientSize.Width/Columns);}}const int CellHeight=214;
 public void BeginUpdate(){suspended++;}public void EndUpdate(){suspended=Math.Max(0,suspended-1);Changed();}void Changed(){if(suspended>0)return;scrollY=Math.Min(scrollY,MaxScroll);Invalidate();}
 int MaxScroll{get{return Math.Max(0,((Items.Count+Columns-1)/Columns)*CellHeight-ClientSize.Height);}}
 protected override void OnMouseWheel(MouseEventArgs e){scrollY=Math.Max(0,Math.Min(MaxScroll,scrollY-e.Delta/120*CellHeight/2));Invalidate();base.OnMouseWheel(e);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);int first=Math.Max(0,scrollY/CellHeight);int last=Math.Min((Items.Count+Columns-1)/Columns,first+ClientSize.Height/CellHeight+2);
  for(int row=first;row<last;row++)for(int col=0;col<Columns;col++){int index=row*Columns+col;if(index>=Items.Count)break;var entry=Items[index] as Entry;if(entry==null)continue;var rect=new Rectangle(col*CellWidth+6,row*CellHeight-scrollY+6,CellWidth-12,CellHeight-12);using(var fill=new SolidBrush(index==selection?Color.FromArgb(20,95,147):Color.FromArgb(17,26,35)))e.Graphics.FillRectangle(fill,rect);using(var border=new Pen(index==selection?Color.Gold:Color.FromArgb(50,88,118),index==selection?2:1))e.Graphics.DrawRectangle(border,rect);
   var picture=new Rectangle(rect.X+3,rect.Y+3,rect.Width-6,143);Image art=null;if(!string.IsNullOrEmpty(entry.Icon)){if(!artwork.TryGetValue(entry.Icon,out art))RequestArtwork(entry.Icon);}if(art==null)art=DefaultArtwork;
   if(art!=null){float scale=Math.Min((float)picture.Width/art.Width,(float)picture.Height/art.Height);int w=(int)(art.Width*scale),h=(int)(art.Height*scale);e.Graphics.DrawImage(art,picture.X+(picture.Width-w)/2,picture.Y+(picture.Height-h)/2,w,h);}else TextRenderer.DrawText(e.Graphics,"▶",Font,picture,Color.LightBlue,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
   TextRenderer.DrawText(e.Graphics,entry.Name,Font,new Rectangle(rect.X+5,rect.Y+151,rect.Width-10,44),Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);
  }
  if(Items.Count==0)TextRenderer.DrawText(e.Graphics,"Nenhum item nesta seleção",Font,ClientRectangle,Color.LightBlue,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
 }
 async void RequestArtwork(string url){Uri uri;if(closing||pending.Count>=24||pending.Contains(url)||failed.Contains(url)||!Uri.TryCreate(url,UriKind.Absolute,out uri)||(uri.Scheme!="http"&&uri.Scheme!="https"))return;pending.Add(url);Image result=null;
  try{await downloads.WaitAsync();try{result=await Task.Run(()=>{try{var request=(HttpWebRequest)WebRequest.Create(uri);request.Timeout=8000;request.ReadWriteTimeout=8000;request.UserAgent="XCIPTV-PC/0.1";using(var response=request.GetResponse())using(var stream=response.GetResponseStream())using(var data=new MemoryStream()){byte[] buffer=new byte[8192];int count;while((count=stream.Read(buffer,0,buffer.Length))>0){if(data.Length+count>2*1024*1024)return null;data.Write(buffer,0,count);}data.Position=0;using(var image=Image.FromStream(data)){if(image.Width>8192||image.Height>8192)return null;var bitmap=new Bitmap(240,180);using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.FromArgb(17,26,35));float scale=Math.Min(240f/image.Width,180f/image.Height);int w=(int)(image.Width*scale),h=(int)(image.Height*scale);g.DrawImage(image,(240-w)/2,(180-h)/2,w,h);}return (Image)bitmap;}}}catch{return null;}});}finally{downloads.Release();}
   if(closing){if(result!=null)result.Dispose();return;}if(result==null)failed.Add(url);else{if(artwork.Count>=256){var key=artwork.Keys.First();artwork[key].Dispose();artwork.Remove(key);}artwork[url]=result;}pending.Remove(url);Invalidate();
  }catch{if(result!=null)result.Dispose();if(!closing){pending.Remove(url);failed.Add(url);}}
 }
 protected override void Dispose(bool disposing){closing=true;if(disposing){foreach(var image in artwork.Values)image.Dispose();artwork.Clear();}base.Dispose(disposing);}
}

