using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

public class Entry {
 public string Name, Id, Extension, Url, Category, Icon; public MediaDetails Details;
 public override string ToString(){return Name;}
}
public partial class XCIPTVPC : Form {
 TextBox server=new TextBox(),user=new TextBox(),pass=new TextBox(),search=new TextBox();
 ComboBox mode=new ComboBox(),category=new ComboBox(); MediaGrid items=new MediaGrid();
 Button login=new Button(),play=new Button(),back=new Button(),star=new Button(),guide=new Button(),m3u=new Button();
 Label status=new Label(); List<Entry> entries=new List<Entry>();
 Panel loginScreen,loadingScreen;TableLayoutPanel catalog;Label loginStatus,loadingStatus;Label[] loadingStages;
 Image logoImage;
 Panel homeScreen,playbackScreen,playbackHost;HomeButton homeButton;ListBox categorySide;bool suppressMode,syncCategories,guideMode;
 sealed class CatalogData {public List<Entry> Entries;public List<Entry> Categories;}
 Dictionary<string,CatalogData> catalogs=new Dictionary<string,CatalogData>();

 CheckBox rememberLogin=new CheckBox();bool automaticLogin;
 EmbeddedVlc engine;Panel playerPane,videoSurface;SplitContainer videoSplit;Label mediaTitle,playbackTime;TimelineBar seek;TrackBar volume;Button pauseButton;System.Windows.Forms.Timer playbackTimer;bool seeking;Form fullscreen;

 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr hWnd,int msg,IntPtr wParam,string lParam);
 string host,username,password,kind="live",vlc; bool busy=false,episodes=false;
 JavaScriptSerializer json=new JavaScriptSerializer(){MaxJsonLength=int.MaxValue};
 HashSet<string> favorites=new HashSet<string>();
 string dataDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EtherPlayer");
 public XCIPTVPC(){TestTrace("constructor");
  Text="XCIPTV-PC — Windows • prévia 0.2.2"; Width=1100;Height=740;MinimumSize=new Size(850,600);
  DoubleBuffered=true;AutoScaleMode=AutoScaleMode.Dpi;StartPosition=FormStartPosition.CenterScreen;
  BackgroundImage=Asset("Background");BackgroundImageLayout=ImageLayout.Stretch;logoImage=Asset("Logo");
  using(var iconStream=Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon")){if(iconStream!=null)Icon=new Icon(iconStream);}
  BackColor=Color.FromArgb(12,65,140);ForeColor=Color.White;Font=new Font("Segoe UI",10);
  var root=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(16)};
  root.RowStyles.Add(new RowStyle(SizeType.Absolute,85));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Absolute,32));Controls.Add(root);
  catalog=root;root.BackColor=Color.Transparent;
  var header=new Panel(){Dock=DockStyle.Fill,BackColor=Color.FromArgb(8,38,79)};
  header.Controls.Add(new PictureBox(){Image=logoImage,SizeMode=PictureBoxSizeMode.Zoom,Location=new Point(90,8),Size=new Size(62,62)});
  header.Controls.Add(new Label(){Text="XCIPTV-PC",AutoSize=true,Font=new Font("Segoe UI",20,FontStyle.Regular),Location=new Point(165,15),ForeColor=Color.White});
  var leave=new Button(){Text="Sair da conta",Dock=DockStyle.Right,Width=145,FlatStyle=FlatStyle.Flat,ForeColor=Color.White};leave.Click+=(sender,eventArgs)=>{host=null;password=null;catalogs.Clear();ForgetSession();StopPlayback();pass.Clear();entries.Clear();items.Items.Clear();ShowLogin();};header.Controls.Add(leave);var configureVlc=new Button(){Text="Configurar VLC",Dock=DockStyle.Right,Width=145,FlatStyle=FlatStyle.Flat,ForeColor=Color.White,BackColor=Color.FromArgb(3,31,61)};configureVlc.Click+=(sender,eventArgs)=>SelectVlc();header.Controls.Add(configureVlc);root.Controls.Add(header);
  var filters=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=3};foreach(float width in new[]{22f,35f,43f})filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,width));
  mode.Items.AddRange(new object[]{"TV ao vivo","Filmes","Séries","Favoritos (lista atual)"});mode.SelectedIndex=0;mode.DropDownStyle=category.DropDownStyle=ComboBoxStyle.DropDownList;mode.Dock=category.Dock=search.Dock=DockStyle.Fill;
  filters.Controls.Add(mode);filters.Controls.Add(category);filters.Controls.Add(search);root.Controls.Add(filters);
  items.Dock=DockStyle.Fill;items.BackColor=Color.FromArgb(30,36,48);items.ForeColor=Color.White;items.BorderStyle=BorderStyle.None;items.HorizontalScrollbar=true;BuildPlayer(root);
  var actions=new FlowLayoutPanel(){Dock=DockStyle.Fill};play.Text="▶ Reproduzir";star.Text="☆ Favorito";guide.Text="Programação";back.Text="Voltar às séries";back.Width=155;play.Width=140;guide.Width=140;m3u.Text="Abrir M3U";m3u.Width=120;foreach(Button button in new[]{play,star,guide,back,m3u}){button.AutoSize=true;button.MinimumSize=new Size(110,32);button.BackColor=Color.FromArgb(3,31,61);button.ForeColor=Color.White;button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderColor=Color.FromArgb(75,157,224);}actions.Controls.AddRange(new Control[]{play,star,guide,back,m3u});root.Controls.Add(actions);
  status.Dock=DockStyle.Fill;root.Controls.Add(status);status.Text="Informe os dados do seu provedor. Login automático disponível após entrar.";
  login.Click+=async(s,e)=>await Connect();mode.SelectedIndexChanged+=async(s,e)=>{if(suppressMode)return;if(mode.SelectedIndex==3){Filter();return;}if(host!=null)await LoadList();};
  search.TextChanged+=(s,e)=>Filter();category.SelectedIndexChanged+=(s,e)=>Filter();play.Click+=(s,e)=>Play();items.DoubleClick+=(s,e)=>Play();
  back.Click+=async(s,e)=>await LoadList();star.Click+=(s,e)=>ToggleFavorite();guide.Click+=async(s,e)=>await Guide();
  m3u.Click+=async(s,e)=>await ImportM3u();
  TestTrace("login");BuildLogin();BuildLoading();TestTrace("navigation");BuildNavigation();TestTrace("cinema");BuildCinema();ShowLogin();TestTrace("ready");Resize+=(sender,eventArgs)=>CenterScreens();Shown+=(sender,eventArgs)=>CenterScreens();
  Directory.CreateDirectory(dataDir);var fav=Path.Combine(dataDir,"favorites.txt");if(File.Exists(fav))favorites=new HashSet<string>(File.ReadAllLines(fav));
  LoadPreferences();LoadSession();vlc=FindVlc(vlc);FormClosing+=(sender,eventArgs)=>{SavePreferences();if(fullscreen!=null)fullscreen.Close();CloseCinema();if(playbackTimer!=null){playbackTimer.Stop();playbackTimer.Dispose();}if(engine!=null){engine.Dispose();engine=null;}};
  Shown+=async(sender,eventArgs)=>{if(automaticLogin&&!Environment.GetCommandLineArgs().Any(x=>x.StartsWith("--")))await Connect();};
 }
 static Image Asset(string name){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)){if(stream==null)throw new InvalidOperationException("Recurso visual ausente: "+name);using(var image=Image.FromStream(stream))return new Bitmap(image);}}
 Panel Field(TextBox input,string placeholder,int symbol,bool reveal){
  var panel=new Panel(){Dock=DockStyle.Fill,BackColor=Color.FromArgb(207,234,243),Margin=new Padding(0,3,0,3),Padding=new Padding(58,15,reveal?84:16,10)};
  input.BorderStyle=BorderStyle.None;input.BackColor=panel.BackColor;input.ForeColor=Color.FromArgb(16,53,77);input.Font=new Font("Segoe UI",15);input.Dock=DockStyle.None;panel.Resize+=(sender,eventArgs)=>input.SetBounds(58,Math.Max(8,(panel.ClientSize.Height-input.PreferredHeight)/2),Math.Max(100,panel.ClientSize.Width-58-(reveal?88:16)),input.PreferredHeight);input.AccessibleName=placeholder;input.HandleCreated+=(sender,eventArgs)=>SendMessage(input.Handle,0x1501,new IntPtr(1),placeholder);panel.Controls.Add(input);
  panel.Paint+=(sender,eventArgs)=>{var g=eventArgs.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var pen=new Pen(Color.FromArgb(33,67,89),1.4f)){
   if(symbol==0){g.DrawRectangle(pen,16,15,25,10);g.DrawRectangle(pen,16,29,25,10);g.DrawLine(pen,20,20,29,20);g.DrawLine(pen,20,34,29,34);g.DrawEllipse(pen,35,19,2,2);g.DrawEllipse(pen,35,33,2,2);}
   else if(symbol==1){g.DrawEllipse(pen,23,10,17,17);g.DrawArc(pen,15,28,33,28,180,180);g.DrawLine(pen,15,42,48,42);}
   else {g.DrawArc(pen,23,10,17,24,180,180);g.DrawRectangle(pen,17,23,29,24);}
  }};
  if(reveal){var toggle=new Button(){Text="Mostrar",Width=77,Height=38,Anchor=AnchorStyles.Top|AnchorStyles.Right,FlatStyle=FlatStyle.Flat,TabStop=false,ForeColor=Color.FromArgb(16,53,77),Font=new Font("Segoe UI",9)};toggle.FlatAppearance.BorderSize=0;panel.Padding=new Padding(58,12,4,10);input.UseSystemPasswordChar=true;toggle.Click+=(sender,eventArgs)=>{input.UseSystemPasswordChar=!input.UseSystemPasswordChar;toggle.Text=input.UseSystemPasswordChar?"Mostrar":"Ocultar";};panel.Controls.Add(toggle);panel.Resize+=(sender,eventArgs)=>toggle.SetBounds(panel.ClientSize.Width-82,8,77,panel.ClientSize.Height-16);toggle.BringToFront();}
  return panel;
 }
 void BuildLogin(){
  loginScreen=new Panel(){Dock=DockStyle.Fill,BackColor=Color.Transparent};Controls.Add(loginScreen);
  var card=new TableLayoutPanel(){Size=new Size(470,530),ColumnCount=1,RowCount=10,BackColor=Color.Transparent};
  foreach(int height in new[]{130,38,62,62,62,62,28,32,32,22})card.RowStyles.Add(new RowStyle(SizeType.Absolute,height));card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  card.Controls.Add(new PictureBox(){Image=logoImage,Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,Margin=new Padding(0,0,0,12)},0,0);
  card.Controls.Add(new Label(){Text="XTREAM CODES COMPATIBLE API",Font=new Font("Segoe UI Light",16),ForeColor=Color.White,TextAlign=ContentAlignment.MiddleCenter,Dock=DockStyle.Fill},0,1);
  card.Controls.Add(Field(server,"URL do servidor Xtream",0,false),0,2);card.Controls.Add(Field(user,"Nome de usuário",1,false),0,3);card.Controls.Add(Field(pass,"Senha",2,true),0,4);
  login.Text="ENTRAR";login.Font=new Font("Segoe UI",19);login.Dock=DockStyle.Fill;login.Margin=new Padding(0,9,0,0);login.BackColor=Color.FromArgb(3,31,61);login.ForeColor=Color.White;login.FlatStyle=FlatStyle.Flat;login.FlatAppearance.BorderColor=Color.FromArgb(75,157,224);card.Controls.Add(login,0,5);
  rememberLogin.Text="Lembrar senha e entrar automaticamente";rememberLogin.Checked=true;rememberLogin.Dock=DockStyle.Fill;rememberLogin.ForeColor=Color.White;rememberLogin.Margin=new Padding(0,6,0,0);rememberLogin.CheckedChanged+=(sender,eventArgs)=>{if(!rememberLogin.Checked)ForgetSession();};card.Controls.Add(rememberLogin,0,6);
  loginStatus=new Label(){Text="Informe os dados do seu provedor",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.White,Font=new Font("Segoe UI",9)};card.Controls.Add(loginStatus,0,7);
  var local=new LinkLabel(){Text="Abrir lista M3U local",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,LinkColor=Color.White,ActiveLinkColor=Color.LightCyan};local.LinkClicked+=async(sender,eventArgs)=>{await ImportM3u();if(entries.Count>0){loginScreen.Visible=false;ShowBrowser();}};card.Controls.Add(local,0,8);
  card.Controls.Add(new Label(){Text="XCIPTV-PC • Cliente independente para Windows",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.FromArgb(197,224,246),Font=new Font("Segoe UI",8)},0,9);
  loginScreen.Controls.Add(card);loginScreen.Resize+=(sender,eventArgs)=>{card.Width=Math.Min(470,Math.Max(300,loginScreen.ClientSize.Width-48));card.Left=(loginScreen.ClientSize.Width-card.Width)/2;card.Top=Math.Max(8,(loginScreen.ClientSize.Height-card.Height)/2);};
 }
 void CenterScreens(){foreach(var panel in new[]{loginScreen,loadingScreen}){if(panel==null||panel.Controls.Count==0)continue;var card=panel.Controls.OfType<TableLayoutPanel>().FirstOrDefault();if(card==null)continue;card.Width=Math.Min(panel==loginScreen?470:820,Math.Max(300,panel.ClientSize.Width-48));card.Left=Math.Max(0,(panel.ClientSize.Width-card.Width)/2);card.Top=Math.Max(8,(panel.ClientSize.Height-card.Height)/2);}}
 void ShowLogin(){HideOverlays();if(detailsScreen!=null)detailsScreen.Visible=false;if(homeScreen!=null)homeScreen.Visible=false;if(playbackScreen!=null)playbackScreen.Visible=false;catalog.Visible=false;if(loadingScreen!=null)loadingScreen.Visible=false;loginScreen.Visible=true;loginScreen.BringToFront();CenterScreens();AcceptButton=login;if(homeButton!=null){AttachHome(loginScreen);}}
 void BuildLoading(){
  loadingScreen=new Panel(){Dock=DockStyle.Fill,BackColor=Color.Transparent,Visible=false};Controls.Add(loadingScreen);
  var card=new TableLayoutPanel(){Size=new Size(820,300),ColumnCount=1,RowCount=5,BackColor=Color.Transparent};foreach(int height in new[]{52,100,26,52,52})card.RowStyles.Add(new RowStyle(SizeType.Absolute,height));card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  card.Controls.Add(new Label(){Text="Atualizando conteúdo de mídia",BackColor=Color.FromArgb(237,243,249),ForeColor=Color.FromArgb(20,50,80),Font=new Font("Segoe UI Light",17),TextAlign=ContentAlignment.MiddleCenter,Dock=DockStyle.Fill});
  var stages=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,Margin=new Padding(0)};loadingStages=new Label[4];string[] names={"TV AO VIVO","FILMES","SÉRIES","GUIA DE TV"};for(int i=0;i<4;i++){stages.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));loadingStages[i]=new Label(){Text=names[i]+"\n"+(i==0?"Aguardando…":"Ao abrir a seção"),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.FromArgb(6,34,64),ForeColor=Color.White,Margin=new Padding(1,0,1,0),Font=new Font("Segoe UI",11)};stages.Controls.Add(loadingStages[i]);}card.Controls.Add(stages);
  card.Controls.Add(new ProgressBar(){Style=ProgressBarStyle.Marquee,MarqueeAnimationSpeed=24,Dock=DockStyle.Fill,Margin=new Padding(0,8,0,4)});
  loadingStatus=new Label(){Text="Carregando canais ao vivo…",TextAlign=ContentAlignment.MiddleCenter,Dock=DockStyle.Fill,ForeColor=Color.White};card.Controls.Add(loadingStatus);
  card.Controls.Add(new Label(){Text="O guia é consultado ao escolher um canal na seção Guia da TV.",TextAlign=ContentAlignment.MiddleCenter,Dock=DockStyle.Fill,ForeColor=Color.FromArgb(202,230,250),BackColor=Color.FromArgb(6,34,64),Font=new Font("Segoe UI",9)});
  loadingScreen.Controls.Add(card);loadingScreen.Resize+=(sender,eventArgs)=>{card.Width=Math.Min(820,loadingScreen.ClientSize.Width-48);card.Left=(loadingScreen.ClientSize.Width-card.Width)/2;card.Top=(loadingScreen.ClientSize.Height-card.Height)/2;};
 }
 void SetLoading(int index,string text){string[] names={"TV AO VIVO","FILMES","SÉRIES","GUIA DE TV"};loadingStages[index].Text=names[index]+"\n"+text;}
 void Snapshot(string name){TestTrace("snapshot "+name);PerformLayout();string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ui-previews");Directory.CreateDirectory(dir);using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(Path.Combine(dir,name+".png"));}}
 void CheckLayout(){CenterScreens();if(!loginScreen.Visible||catalog.Visible||!loginScreen.Bounds.Contains(loginScreen.Controls.OfType<TableLayoutPanel>().First().Bounds))throw new Exception("Login layout is outside the window");foreach(Control input in new Control[]{server,user,pass,login})if(!input.Visible||input.Width<100||input.Height<15)throw new Exception("Login control is not usable");var toggle=pass.Parent.Controls.OfType<Button>().Single();toggle.PerformClick();if(pass.UseSystemPasswordChar)throw new Exception("Password reveal failed");toggle.PerformClick();Snapshot("login");
  HidePages();loadingScreen.Visible=true;loadingScreen.BringToFront();AttachHome(loadingScreen);CenterScreens();SetLoading(0,"SUCESSO!");SetLoading(1,"SUCESSO!");SetLoading(2,"Carregando…");Snapshot("loading");homeButton.PerformClick();if(!homeScreen.Visible||homeButton.Visible)throw new Exception("Home route failed");Snapshot("home");
  entries=Enumerable.Range(1,30).Select(i=>new Entry(){Name="Canal de demonstração "+i,Id=i.ToString(),Category=i%2==0?"news":"sports"}).ToList();category.Items.Clear();category.Items.Add(new Entry(){Name="Todas as categorias",Id=""});category.Items.Add(new Entry(){Name="Esportes",Id="sports"});category.Items.Add(new Entry(){Name="Notícias",Id="news"});category.SelectedIndex=0;SyncSidebar();Filter();ShowBrowser();if(!homeButton.Visible||items.Items.Count!=30)throw new Exception("Browser grid failed");Snapshot("catalog");items.ScrollWheel(-1200);if(items.ScrollOffset==0)throw new Exception("Catalog did not scroll for genre test");category.SelectedIndex=2;if(items.ScrollOffset!=0||items.SelectedItem!=null||((Entry)items.Items[0]).Id!="2")throw new Exception("Genre did not reset to first media");items.ScrollWheel(-1200);category.SelectedIndex=1;if(items.ScrollOffset!=0||((Entry)items.Items[0]).Id!="1")throw new Exception("Reverse genre reset failed");if(items.Items.Count!=15)throw new Exception("Category filter failed");search.Text="demonstração 1";if(items.Items.Count!=6)throw new Exception("Search filter failed");search.Clear();homeButton.PerformClick();if(!homeScreen.Visible||homeButton.Visible)throw new Exception("Catalog home failed");ShowPlayback();if(playerBack==null||!playerBack.Visible)throw new Exception("Playback back missing");Snapshot("player");CinemaLayoutTest();ShowHome();if(!homeScreen.Visible)throw new Exception("Player home failed");entries.Clear();items.Items.Clear();
 }
 void HidePages(){if(detailsScreen!=null)detailsScreen.Visible=false;HideOverlays();catalog.Visible=false;loginScreen.Visible=false;loadingScreen.Visible=false;if(homeScreen!=null)homeScreen.Visible=false;if(playbackScreen!=null)playbackScreen.Visible=false;AcceptButton=null;}
 void AttachHome(Control target){homeButton.Parent=target;homeButton.Location=new Point(12,10);homeButton.Visible=true;homeButton.BringToFront();}
 void ShowHome(){if(fullscreen!=null)fullscreen.Close();StopPlayback();HidePages();homeScreen.Visible=true;homeScreen.BringToFront();LayoutHome();homeButton.Visible=false;}
 void ShowBrowser(){HidePages();catalog.Visible=true;catalog.BringToFront();AttachHome(catalog.GetControlFromPosition(0,0));}
 void ShowPlayback(){HidePages();playbackScreen.Visible=true;playbackScreen.BringToFront();homeButton.Visible=false;playbackScreen.Controls.Cast<Control>().First(x=>x.Dock==DockStyle.Top).Visible=false;playbackHost.Padding=Padding.Empty;lastActivity=DateTime.UtcNow;RefreshOverlays();}
 async Task OpenSection(int index,bool epg){if(busy)return;if(host==null&&entries.Count==0){ShowLogin();return;}guideMode=epg;suppressMode=true;mode.SelectedIndex=index;suppressMode=false;search.Clear();ShowBrowser();if(host!=null)await LoadList();else Filter();}
 void SyncSidebar(){if(categorySide==null||syncCategories)return;syncCategories=true;categorySide.BeginUpdate();categorySide.Items.Clear();categorySide.Items.Add(new Entry(){Name="FAVORITOS",Id="__favorites"});for(int i=0;i<category.Items.Count;i++){var cat=category.Items[i] as Entry;if(cat==null)continue;int count=cat.Id==""?entries.Count:entries.Count(x=>x.Category==cat.Id);categorySide.Items.Add(new Entry(){Name=cat.Name+" ("+count+")",Id=cat.Id});}categorySide.SelectedIndex=category.Items.Count==0?0:Math.Min(categorySide.Items.Count-1,Math.Max(1,category.SelectedIndex+1));categorySide.EndUpdate();syncCategories=false;}
 void LayoutHome(){var homeLogo=homeScreen.Controls.OfType<PictureBox>().FirstOrDefault();var tiles=homeScreen.Controls.OfType<FlowLayoutPanel>().FirstOrDefault(x=>x.Dock==DockStyle.None);if(homeLogo==null||tiles==null)return;homeLogo.Left=(homeScreen.ClientSize.Width-homeLogo.Width)/2;homeLogo.Top=40;int width=Math.Max(120,Math.Min(380,(homeScreen.ClientSize.Width-100)/4-20));foreach(Control tile in tiles.Controls)tile.Size=new Size(width,width);tiles.Size=new Size(4*(width+20),width+20);tiles.Left=(homeScreen.ClientSize.Width-tiles.Width)/2;tiles.Top=Math.Max(170,(homeScreen.ClientSize.Height-tiles.Height)/2);}
 void BuildNavigation(){
  items.DefaultArtwork=logoImage;videoSplit.Panel1.Controls.Remove(items);videoSplit.Panel2.Controls.Remove(playerPane);
  categorySide=new QuietListBox(){Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(7,41,79),ForeColor=Color.White,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=48,IntegralHeight=false};categorySide.DrawItem+=(sender,eventArgs)=>{if(eventArgs.Index<0)return;bool selected=(eventArgs.State&DrawItemState.Selected)!=0;using(var fill=new SolidBrush(selected?Color.FromArgb(22,85,137):categorySide.BackColor))eventArgs.Graphics.FillRectangle(fill,eventArgs.Bounds);TextRenderer.DrawText(eventArgs.Graphics,categorySide.Items[eventArgs.Index].ToString(),Font,new Rectangle(eventArgs.Bounds.X+12,eventArgs.Bounds.Y+5,eventArgs.Bounds.Width-18,eventArgs.Bounds.Height-7),selected?Color.Gold:Color.White,TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);};
  categorySide.SelectedIndexChanged+=(sender,eventArgs)=>{if(syncCategories)return;int index=categorySide.SelectedIndex;if(index<0)return;if(index==0){suppressMode=true;mode.SelectedIndex=3;suppressMode=false;Filter();}else{suppressMode=true;if(mode.SelectedIndex==3)mode.SelectedIndex=kind=="vod"?1:kind=="series"?2:0;suppressMode=false;category.SelectedIndex=index-1;Filter();}};
  videoSplit.Panel1.Controls.Add(categorySide);videoSplit.Panel2.Controls.Add(items);category.SelectedIndexChanged+=(sender,eventArgs)=>SyncSidebar();search.AccessibleName="Pesquisar canais, filmes ou séries";search.HandleCreated+=(sender,eventArgs)=>SendMessage(search.Handle,0x1501,new IntPtr(1),"Pesquisar canais, filmes ou séries…");
  playbackScreen=new Panel(){Dock=DockStyle.Fill,BackColor=Color.Transparent,Visible=false};Controls.Add(playbackScreen);var playbackHeader=new Panel(){Dock=DockStyle.Top,Height=80,BackColor=Color.FromArgb(8,38,79)};playbackHeader.Controls.Add(new Label(){Text="REPRODUÇÃO",Location=new Point(102,24),AutoSize=true,Font=new Font("Segoe UI",17),ForeColor=Color.White});var returnList=PlayerButton("Voltar à lista",145);returnList.Dock=DockStyle.Right;returnList.Click+=(sender,eventArgs)=>{StopPlayback();ShowBrowser();};playbackHeader.Controls.Add(returnList);playbackHost=new Panel(){Dock=DockStyle.Fill,Padding=new Padding(18),BackColor=Color.Transparent};playbackHost.Controls.Add(playerPane);playbackScreen.Controls.Add(playbackHost);playbackScreen.Controls.Add(playbackHeader);
  homeScreen=new Panel(){Dock=DockStyle.Fill,BackColor=Color.Transparent,Visible=false};Controls.Add(homeScreen);
  var homeLogo=new PictureBox(){Image=logoImage,SizeMode=PictureBoxSizeMode.Zoom,Size=new Size(116,116)};homeScreen.Controls.Add(homeLogo);
  var welcome=new Label(){Text="XCIPTV-PC",AutoSize=true,Font=new Font("Segoe UI Light",18),ForeColor=Color.White,Location=new Point(24,24)};homeScreen.Controls.Add(welcome);
  var tiles=new FlowLayoutPanel(){Size=new Size(840,210),BackColor=Color.Transparent,WrapContents=false};string[] captions={"TV AO VIVO","GUIA DA TV","FILMES","SÉRIES"};for(int i=0;i<4;i++){int chosen=i;var tile=new HomeTile(captions[i],i){Size=new Size(190,190),Margin=new Padding(10)};tile.Click+=async(sender,eventArgs)=>await OpenSection(chosen==2?1:chosen==3?2:0,chosen==1);tiles.Controls.Add(tile);}homeScreen.Controls.Add(tiles);
  var footer=new FlowLayoutPanel(){Dock=DockStyle.Bottom,Height=100,Padding=new Padding(20,16,20,8),WrapContents=false,AutoScroll=true};
  var account=PlayerButton("Conta / sair",130);account.Click+=(sender,eventArgs)=>{if(host==null){ShowLogin();return;}if(MessageBox.Show("Sair da conta e remover o login automático salvo?","XCIPTV-PC",MessageBoxButtons.YesNo)==DialogResult.Yes){host=null;password=null;catalogs.Clear();ForgetSession();pass.Clear();entries.Clear();items.Items.Clear();ShowLogin();}};footer.Controls.Add(account);
  var update=PlayerButton("Atualizar listas",135);update.Click+=async(sender,eventArgs)=>{catalogs.Clear();await RefreshCatalogs();};footer.Controls.Add(update);
  var config=PlayerButton("Configurar VLC",135);config.Click+=(sender,eventArgs)=>SelectVlc();footer.Controls.Add(config);
  foreach(string name in new[]{"Multitelas","Catch-up","Gravações"}){var future=PlayerButton(name+" • em breve",145);future.Enabled=false;footer.Controls.Add(future);}homeScreen.Controls.Add(footer);
  homeScreen.Resize+=(sender,eventArgs)=>LayoutHome();
  homeButton=new HomeButton(){Location=new Point(20,14)};homeButton.Click+=(sender,eventArgs)=>ShowHome();Controls.Add(homeButton);
 }
 static byte[] ProtectSecret(string text){return ProtectedData.Protect(Encoding.UTF8.GetBytes(text),Encoding.UTF8.GetBytes("XCIPTV-PC/login/v1"),DataProtectionScope.CurrentUser);}
 static string UnprotectSecret(byte[] bytes){return Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes,Encoding.UTF8.GetBytes("XCIPTV-PC/login/v1"),DataProtectionScope.CurrentUser));}
 void LoadSession(){try{string path=Path.Combine(dataDir,"credentials.bin");if(!File.Exists(path))return;var saved=Dict(new JavaScriptSerializer().DeserializeObject(UnprotectSecret(File.ReadAllBytes(path))));if(Get(saved,"server")!=server.Text.Trim()||Get(saved,"username")!=user.Text.Trim())return;string secret=Get(saved,"password");if(secret.Length==0)return;pass.Text=secret;automaticLogin=true;}catch{automaticLogin=false;loginStatus.Text="Digite a senha novamente para renovar o login salvo.";}}
 void SaveSession(){if(!rememberLogin.Checked){ForgetSession();return;}try{var saved=new Dictionary<string,string>{{"server",server.Text.Trim()},{"username",user.Text.Trim()},{"password",pass.Text}};byte[] bytes=ProtectSecret(new JavaScriptSerializer().Serialize(saved));string path=Path.Combine(dataDir,"credentials.bin");string tmp=path+".tmp";File.WriteAllBytes(tmp,bytes);if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path);automaticLogin=true;}catch{automaticLogin=false;MessageBox.Show("Você entrou, mas não foi possível guardar a senha protegida neste Windows.","XCIPTV-PC");}}
 void ForgetSession(){automaticLogin=false;try{string path=Path.Combine(dataDir,"credentials.bin");if(File.Exists(path))File.Delete(path);}catch{MessageBox.Show("Não foi possível remover o login salvo. Verifique as permissões da pasta de dados.","XCIPTV-PC");}}
 static Button PlayerButton(string text,int width){var button=new Button(){Text=text,Width=width,Height=31,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(3,31,61),ForeColor=Color.White};button.FlatAppearance.BorderColor=Color.FromArgb(75,157,224);return button;}
 static string Clock(long milliseconds){var time=TimeSpan.FromMilliseconds(Math.Max(0,milliseconds));return ((int)time.TotalHours).ToString("00")+":"+time.Minutes.ToString("00")+":"+time.Seconds.ToString("00");}
 static string PreferencesJson(string address,string name,string player){return new JavaScriptSerializer().Serialize(new Dictionary<string,string>{{"server",address??""},{"username",name??""},{"vlcPath",player??""}});}
 void LoadPreferences(){try{string path=Path.Combine(dataDir,"preferences.json");if(!File.Exists(path))return;var saved=Dict(new JavaScriptSerializer().DeserializeObject(File.ReadAllText(path)));server.Text=Get(saved,"server");user.Text=Get(saved,"username");vlc=Get(saved,"vlcPath");}catch{loginStatus.Text="Não foi possível carregar as preferências. Preencha os dados novamente.";}}
 void SavePreferences(){try{Directory.CreateDirectory(dataDir);File.WriteAllText(Path.Combine(dataDir,"preferences.json"),PreferencesJson(server.Text.Trim(),user.Text.Trim(),vlc));}catch{status.Text="Não foi possível salvar as preferências neste computador.";}}
 static string FindVlc(string previous){
  if(!string.IsNullOrWhiteSpace(previous)&&File.Exists(previous))return previous;
  foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})foreach(var hive in new[]{RegistryHive.LocalMachine,RegistryHive.CurrentUser}){
   try{using(var root=RegistryKey.OpenBaseKey(hive,view)){
    using(var key=root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vlc.exe")){if(key!=null){string candidate=Convert.ToString(key.GetValue("")).Trim('"');if(File.Exists(candidate))return candidate;}}
    using(var key=root.OpenSubKey(@"SOFTWARE\VideoLAN\VLC")){if(key!=null){string folder=Convert.ToString(key.GetValue("InstallDir"));if(!string.IsNullOrEmpty(folder)){string candidate=Path.Combine(folder,"vlc.exe");if(File.Exists(candidate))return candidate;}}}
   }}catch{}
  }
  return new[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"VideoLAN","VLC","vlc.exe"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"VideoLAN","VLC","vlc.exe")}.FirstOrDefault(File.Exists);
 }
 bool SelectVlc(){using(var dialog=new OpenFileDialog(){Title="Selecione o vlc.exe da instalação atual",Filter="VLC Media Player (vlc.exe)|vlc.exe",CheckFileExists=true}){
  if(dialog.ShowDialog()!=DialogResult.OK)return false;if(!string.Equals(Path.GetFileName(dialog.FileName),"vlc.exe",StringComparison.OrdinalIgnoreCase)){MessageBox.Show("Selecione o arquivo vlc.exe.");return false;}
  if(engine!=null){engine.Dispose();engine=null;}vlc=dialog.FileName;SavePreferences();status.Text="VLC configurado. Selecione um vídeo e clique em Reproduzir.";return true;
 }}
 string Esc(string value){return Uri.EscapeDataString(value??"");}
 string Api(string action){return host+"/player_api.php?username="+Esc(username)+"&password="+Esc(password)+(action==""?"":"&action="+action);}
 async Task<object> Fetch(string url){
  return await Task.Run(()=>{
   var req=(HttpWebRequest)WebRequest.Create(url);req.Timeout=45000;req.ReadWriteTimeout=45000;req.UserAgent="XCIPTV-PC/0.1.8";
   req.Accept="application/json, text/plain, */*";req.AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate;
   try{using(var response=(HttpWebResponse)req.GetResponse())using(var stream=response.GetResponseStream())using(var reader=new StreamReader(stream)){
    string body=reader.ReadToEnd();return ParseResponse(body);
   }}catch(WebException ex){string message=NetworkFailure(ex);if(ex.Response!=null)ex.Response.Close();throw new InvalidOperationException(message);}
  });
 }
 static object ParseResponse(string body){
  if(string.IsNullOrWhiteSpace(body))throw new InvalidOperationException("O servidor respondeu sem conteúdo. Confirme o endereço completo e a porta fornecidos pelo provedor.");
  string text=body.TrimStart('\uFEFF',' ','\r','\n','\t');
  if(text.StartsWith("<"))throw new InvalidOperationException("O endereço respondeu com uma página HTML, não com a API Xtream. Pode ser uma página de login, redirecionamento ou bloqueio. Confirme o servidor e a porta usados no XCIPTV.");
  if(text=="0"||text.Equals("false",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("O servidor retornou uma recusa de acesso. Confirme a conta e o tipo de login com o provedor.");
  try{object result=new JavaScriptSerializer(){MaxJsonLength=int.MaxValue}.DeserializeObject(text);if(!(result is Dictionary<string,object>)&&!(result is object[]))throw new ArgumentException();return result;}
  catch(ArgumentException){throw new InvalidOperationException("O servidor respondeu em um formato diferente do JSON esperado pela API Xtream. O login deste painel pode exigir outra integração.");}
 }
 static string NetworkFailure(WebException ex){
  var response=ex.Response as HttpWebResponse;
  if(response!=null){int code=(int)response.StatusCode;string hint;
   switch(code){
    case 401:hint="O servidor exigiu autenticação ou recusou o acesso. Isso, isoladamente, não confirma que a senha esteja incorreta.";break;
    case 403:hint="O servidor negou a requisição. Pode haver uma restrição do provedor, do endereço IP ou do tipo de cliente.";break;
    case 404:hint="A rota player_api.php não foi encontrada. Confirme endereço, porta e se o serviço oferece login Xtream.";break;
    case 429:hint="O servidor limitou as tentativas. Aguarde antes de tentar novamente.";break;
    case 500:case 502:case 503:case 504:hint="O servidor ou o gateway apresentou uma falha. Tente novamente mais tarde.";break;
    default:hint="O servidor retornou uma resposta HTTP de erro. Confirme o endereço completo e a porta com o provedor.";break;
   }
   return "HTTP "+code+" — "+hint;
  }
  switch(ex.Status){
   case WebExceptionStatus.NameResolutionFailure:return "DNS: não foi possível localizar o domínio do servidor. Confira a digitação do endereço e sua conexão.";
   case WebExceptionStatus.ProxyNameResolutionFailure:return "Proxy: não foi possível localizar o proxy configurado no Windows.";
   case WebExceptionStatus.ConnectFailure:return "Conexão recusada ou indisponível. Confira a porta fornecida pelo provedor e se o XCIPTV conecta na mesma rede.";
   case WebExceptionStatus.Timeout:return "Tempo de conexão esgotado. O servidor não respondeu dentro do prazo. Confira conexão, endereço e porta.";
   case WebExceptionStatus.TrustFailure:return "HTTPS: o certificado do servidor não foi aceito pelo Windows. Confirme o endereço com o provedor; a validação do certificado foi mantida.";
   case WebExceptionStatus.SecureChannelFailure:return "HTTPS/TLS: não foi possível estabelecer uma conexão segura. Verifique o relógio do Windows, as atualizações do sistema e o endereço fornecido pelo provedor.";
   case WebExceptionStatus.ConnectionClosed:case WebExceptionStatus.ReceiveFailure:case WebExceptionStatus.SendFailure:case WebExceptionStatus.KeepAliveFailure:return "A conexão foi encerrada durante a comunicação. Pode haver uma falha de rede, proxy ou restrição do servidor.";
   default:return "Falha de comunicação ("+ex.Status.ToString()+"). Confira conexão, endereço e porta. Não é possível concluir que os dados de acesso estejam incorretos.";
  }
 }
 static Dictionary<string,object> ValidateLogin(object response){
  var root=Dict(response);object raw;if(!root.TryGetValue("user_info",out raw)||!(raw is Dictionary<string,object>))throw new InvalidOperationException("O servidor respondeu, mas não retornou user_info da API Xtream. Confirme se o login usa servidor, usuário e senha ou outra modalidade.");
  var info=Dict(raw);string auth=Get(info,"auth");if(auth!="1"&&!auth.Equals("true",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("A API Xtream recusou a autenticação. Confira usuário, senha e estado da conta no provedor.");
  string state=Get(info,"status","Active");if(!state.Equals("Active",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("A autenticação respondeu, mas a conta não está ativa. Verifique a situação com o provedor.");return info;
 }
 static void SelfTest(){
  byte[] protectedBytes=ProtectSecret("synthetic-test-password");if(Encoding.UTF8.GetString(protectedBytes).Contains("synthetic-test-password")||UnprotectSecret(protectedBytes)!="synthetic-test-password")throw new Exception("Windows credential protection failed");

  string preferences=PreferencesJson("https://example.invalid","test-user",@"C:\VideoLAN\VLC\vlc.exe");var saved=Dict(new JavaScriptSerializer().DeserializeObject(preferences));if(saved.Count!=3||Get(saved,"username")!="test-user"||saved.ContainsKey("password"))throw new Exception("Preferences roundtrip failed");
  string temporary=Path.GetTempFileName();try{if(FindVlc(temporary)!=temporary)throw new Exception("Saved player path was not reused");}finally{File.Delete(temporary);}

  var ok=ParseResponse("{\"user_info\":{\"auth\":1,\"status\":\"Active\"}}");ValidateLogin(ok);
  ValidateLogin(ParseResponse("{\"user_info\":{\"auth\":true,\"status\":\"active\"}}"));
  if(Rows(ParseResponse("[]")).Count()!=0)throw new Exception("Array parse failed");
  foreach(string text in new[]{"","<html>private-server-token</html>","invalid","0","null"}){
   bool failed=false;try{ParseResponse(text);}catch(InvalidOperationException ex){failed=true;if(ex.Message.Contains("private-server-token"))throw new Exception("Response body leaked");}if(!failed)throw new Exception("Invalid response accepted");
  }
  foreach(string text in new[]{"{}","{\"user_info\":{\"auth\":0}}","{\"user_info\":{\"auth\":1,\"status\":\"Expired\"}}"}){bool failed=false;try{ValidateLogin(ParseResponse(text));}catch(InvalidOperationException){failed=true;}if(!failed)throw new Exception("Invalid login accepted");}
  foreach(var item in new[]{WebExceptionStatus.NameResolutionFailure,WebExceptionStatus.ConnectFailure,WebExceptionStatus.Timeout,WebExceptionStatus.TrustFailure,WebExceptionStatus.SecureChannelFailure}){string result=NetworkFailure(new WebException("password=private-password",item));if(string.IsNullOrWhiteSpace(result)||result.Contains("private-password"))throw new Exception("Unsafe diagnostic");}
 }
 static Dictionary<string,object> Dict(object o){return o as Dictionary<string,object>??new Dictionary<string,object>();}
 static string Get(Dictionary<string,object>d,string key,string fallback=""){object v;return d.TryGetValue(key,out v)&&v!=null?Convert.ToString(v):fallback;}
 static IEnumerable<object> Rows(object o){return (o as object[])??new object[0];}
 void Busy(bool value,string text){busy=value;login.Enabled=mode.Enabled=play.Enabled=guide.Enabled=back.Enabled=m3u.Enabled=!value;status.Text=text;if(loginStatus!=null)loginStatus.Text=text;if(loadingStatus!=null)loadingStatus.Text=text;server.Enabled=user.Enabled=pass.Enabled=!value;}
 async Task Connect(){if(busy)return;Uri uri;if(!Uri.TryCreate(server.Text.Trim(),UriKind.Absolute,out uri)||(uri.Scheme!="https"&&uri.Scheme!="http")||!string.IsNullOrEmpty(uri.UserInfo)||!string.IsNullOrEmpty(uri.Query)){MessageBox.Show("Informe somente o endereço do servidor, incluindo http:// ou https://.");return;}
  host=server.Text.Trim().TrimEnd('/');if(host.EndsWith("/player_api.php",StringComparison.OrdinalIgnoreCase))host=host.Substring(0,host.Length-15);username=user.Text.Trim();password=pass.Text;Busy(true,"Verificando conta…");
  try{ValidateLogin(await Fetch(Api("")));SavePreferences();SaveSession();Busy(false,"Conectado.");catalogs.Clear();await RefreshCatalogs();}

  catch(Exception ex){host=null;Busy(false,"Falha ao conectar. Confira a mensagem abaixo.");ShowLogin();ShowError(ex);}
 }
 void ShowError(Exception ex){MessageBox.Show(ex is WebException?NetworkFailure((WebException)ex):ex.Message,"XCIPTV-PC",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
 async Task<CatalogData> ReadCatalog(string type){
  var categories=Rows(await Fetch(Api("get_"+type+"_categories"))).Select(Dict).Select(d=>new Entry(){Name=Get(d,"category_name"),Id=Get(d,"category_id")}).ToList();
  var entries=Rows(await Fetch(Api(type=="series"?"get_series":"get_"+type+"_streams"))).Select(Dict).Select(d=>new Entry(){Name=Get(d,"name"),Id=Get(d,type=="series"?"series_id":"stream_id"),Extension=Get(d,"container_extension",type=="live"?"ts":"mp4"),Category=Get(d,"category_id"),Icon=Get(d,"stream_icon",Get(d,"cover",Get(d,"movie_image")))}).ToList();
  return new CatalogData(){Entries=entries,Categories=categories};
 }
 async Task RefreshCatalogs(){if(busy)return;if(host==null){ShowLogin();return;}HidePages();loadingScreen.Visible=true;loadingScreen.BringToFront();CenterScreens();AttachHome(loadingScreen);Busy(true,"Atualizando conteúdos de mídia…");string[] types={"live","vod","series"};for(int i=0;i<3;i++)SetLoading(i,"Esperando…");SetLoading(3,"Ao escolher canal");int failures=0;
  for(int i=0;i<types.Length;i++){SetLoading(i,"Carregando…");loadingStatus.Text="Atualizando "+(i==0?"TV ao vivo":i==1?"filmes":"séries")+"…";try{catalogs[types[i]]=await ReadCatalog(types[i]);SetLoading(i,"SUCESSO!");}catch{SetLoading(i,"Falhou — tentar na seção");failures++;}}
  Busy(false,failures==0?"Conteúdos atualizados.":"Algumas listas falharam; abra a seção para tentar novamente.");ShowHome();
 }
 async Task LoadList(){if(busy||host==null)return;kind=mode.SelectedIndex==1?"vod":mode.SelectedIndex==2?"series":"live";episodes=false;if(categorySide!=null)categorySide.Enabled=true;Busy(true,"Carregando catálogo…");
  try{CatalogData data;if(!catalogs.TryGetValue(kind,out data)){data=await ReadCatalog(kind);catalogs[kind]=data;}entries=data.Entries;category.Items.Clear();category.Items.Add(new Entry(){Name="Todas as categorias",Id=""});foreach(var cat in data.Categories)category.Items.Add(new Entry(){Name=cat.Name,Id=cat.Id});category.SelectedIndex=0;SyncSidebar();Filter();Busy(false,entries.Count+" itens. Pesquise pelo nome ou escolha uma categoria.");}
  catch(Exception ex){Busy(false,"Não foi possível carregar o catálogo.");ShowError(ex);}
 }
 void Filter(){var cat=category.SelectedItem as Entry;items.BeginUpdate();items.Items.Clear();foreach(var e in entries){if(e.Name.IndexOf(search.Text,StringComparison.CurrentCultureIgnoreCase)<0)continue;if(!episodes&&cat!=null&&cat.Id!=""&&e.Category!=cat.Id)continue;if(mode.SelectedIndex==3&&!favorites.Contains(Key(e)))continue;items.Items.Add(e);}items.EndUpdate();items.ScrollToStart();}
 string Key(Entry e){return (e.Url??host)+"|"+kind+"|"+e.Id;}
 void ToggleFavorite(){var e=items.SelectedItem as Entry;if(e==null)return;string key=Key(e);if(!favorites.Add(key))favorites.Remove(key);File.WriteAllLines(Path.Combine(dataDir,"favorites.txt"),favorites);status.Text=favorites.Contains(key)?"Adicionado aos favoritos.":"Removido dos favoritos.";if(mode.SelectedIndex==3)Filter();}
 async void Play(){if(busy)return;var e=items.SelectedItem as Entry;if(e==null)return;if(guideMode){await Guide();return;}if(kind=="vod"||kind=="series"){await ShowDetails(e);return;}await StartMedia(e);}
 async Task LoadEpisodes(Entry series){Busy(true,"Carregando episódios…");try{object raw=series.Details==null?null:series.Details.Episodes;if(raw==null){var result=Dict(await Fetch(Api("get_series_info&series_id="+Esc(series.Id))));result.TryGetValue("episodes",out raw);}if(raw==null)throw new Exception("O servidor não retornou episódios.");var list=new List<Entry>();foreach(var season in Dict(raw)){foreach(var value in Rows(season.Value)){var d=Dict(value);list.Add(new Entry(){Name="T"+season.Key+" · "+Get(d,"episode_num")+" · "+Get(d,"title"),Id=Get(d,"id"),Extension=Get(d,"container_extension","mp4"),Icon=series.Icon,Details=series.Details});}}entries=list;episodes=true;categorySide.Enabled=false;search.Clear();Filter();Busy(false,list.Count+" episódios de "+series.Name);}catch(Exception ex){Busy(false,"Falha ao carregar episódios.");ShowError(ex);}}
 static string Decode(string s){try{return Encoding.UTF8.GetString(Convert.FromBase64String(s));}catch{return s;}}
 async Task Guide(){var e=items.SelectedItem as Entry;if(e==null||e.Url!=null||host==null||kind!="live"||busy)return;Busy(true,"Carregando programação…");try{var result=Dict(await Fetch(Api("get_short_epg&stream_id="+Esc(e.Id)+"&limit=12")));object raw;var lines=new List<string>();if(result.TryGetValue("epg_listings",out raw))foreach(var row in Rows(raw)){var d=Dict(row);lines.Add(Get(d,"start")+" — "+Decode(Get(d,"title"))+Environment.NewLine+Decode(Get(d,"description")));}MessageBox.Show(lines.Count==0?"Sem programação disponível.":string.Join(Environment.NewLine+Environment.NewLine,lines),e.Name);Busy(false,"Programação consultada.");}catch(Exception ex){Busy(false,"Falha ao consultar programação.");ShowError(ex);}}
 async Task ImportM3u(){
  if(busy)return;using(var dialog=new OpenFileDialog(){Title="Abrir lista M3U local",Filter="Listas M3U|*.m3u;*.m3u8|Todos os arquivos|*.*"}){
   if(dialog.ShowDialog()!=DialogResult.OK)return;Busy(true,"Importando lista M3U…");
   try{string text=await Task.Run(()=>File.ReadAllText(dialog.FileName));if(!text.TrimStart('\uFEFF',' ','\r','\n').StartsWith("#EXTM3U"))throw new Exception("O arquivo não contém um cabeçalho M3U válido.");
    var result=new List<Entry>();string title="",group="";foreach(var raw in text.Split('\n')){string line=raw.Trim();if(line.StartsWith("#EXTINF:")){int comma=line.LastIndexOf(',');title=comma>=0?line.Substring(comma+1):"Canal";var match=System.Text.RegularExpressions.Regex.Match(line,"group-title=\"([^\"]*)\"");group=match.Success?match.Groups[1].Value:"";}else if(line.Length>0&&!line.StartsWith("#")){Uri uri;if(Uri.TryCreate(line,UriKind.Absolute,out uri)&&(uri.Scheme=="http"||uri.Scheme=="https")){result.Add(new Entry(){Name=title.Length>0?title:"Canal "+(result.Count+1),Id=result.Count.ToString(),Url=line,Category=group});}title="";group="";}}
    entries=result;episodes=false;kind="live";category.Items.Clear();category.Items.Add(new Entry(){Name="Todas as categorias",Id=""});foreach(var g in entries.Select(x=>x.Category).Where(x=>x!="").Distinct())category.Items.Add(new Entry(){Name=g,Id=g});category.SelectedIndex=0;search.Clear();Filter();Busy(false,result.Count+" links HTTP/HTTPS importados. Diretivas VLC e autenticação especial da lista não são aplicadas.");
   }catch(Exception ex){Busy(false,"Falha ao importar lista.");ShowError(ex);}
  }
 }
 [STAThread]public static void Main(string[] args){if(args.Contains("--input-test")){Application.EnableVisualStyles();InputPlaybackTest(FindVlc(null));return;}if(args.Contains("--player-test")){string executable=FindVlc(null);if(executable==null)throw new Exception("VLC installation missing for media test");EmbeddedVlc.LocalPlaybackTest(executable);return;}if(args.Contains("--self-test")){SelfTest();return;}ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;Application.EnableVisualStyles();if(args.Contains("--smoke-test")){Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);AppDomain.CurrentDomain.UnhandledException+=(s,e)=>File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-error.txt"),e.ExceptionObject.ToString());}var window=new XCIPTVPC();if(args.Contains("--smoke-test"))window.Shown+=(sender,eventArgs)=>window.BeginInvoke(new Action(()=>{try{window.CheckLayout();window.Close();}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-error.txt"),ex.ToString());Environment.Exit(1);}}));Application.Run(window);}
}








