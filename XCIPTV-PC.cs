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

public class Entry {
 public string Name, Id, Extension, Url, Category;
 public override string ToString(){return Name;}
}
public class XCIPTVPC : Form {
 TextBox server=new TextBox(),user=new TextBox(),pass=new TextBox(),search=new TextBox();
 ComboBox mode=new ComboBox(),category=new ComboBox(); ListBox items=new ListBox();
 Button login=new Button(),play=new Button(),back=new Button(),star=new Button(),guide=new Button(),m3u=new Button();
 Label status=new Label(); List<Entry> entries=new List<Entry>();
 string host,username,password,kind="live",vlc; bool busy=false,episodes=false;
 JavaScriptSerializer json=new JavaScriptSerializer(){MaxJsonLength=int.MaxValue};
 HashSet<string> favorites=new HashSet<string>();
 string dataDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EtherPlayer");
 public XCIPTVPC(){
  Text="XCIPTV-PC — Windows • prévia 0.1.3"; Width=1100;Height=740;MinimumSize=new Size(850,600);
  BackColor=Color.FromArgb(22,26,35);ForeColor=Color.White;Font=new Font("Segoe UI",10);
  var root=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(16)};
  root.RowStyles.Add(new RowStyle(SizeType.Absolute,85));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Absolute,32));Controls.Add(root);
  var auth=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=4,RowCount=2};
  foreach(var text in new[]{"Servidor (http:// ou https://)","Usuário","Senha","Conexão"})auth.Controls.Add(new Label(){Text=text,AutoSize=true});
  server.Dock=user.Dock=pass.Dock=DockStyle.Fill;pass.UseSystemPasswordChar=true;login.Text="Conectar";login.Dock=DockStyle.Fill;
  auth.Controls.Add(server);auth.Controls.Add(user);auth.Controls.Add(pass);auth.Controls.Add(login);for(int i=0;i<4;i++)auth.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,i==0?40:20));root.Controls.Add(auth);
  var filters=new TableLayoutPanel(){Dock=DockStyle.Fill,ColumnCount=3};foreach(float width in new[]{22f,35f,43f})filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,width));
  mode.Items.AddRange(new object[]{"TV ao vivo","Filmes","Séries","Favoritos (lista atual)"});mode.SelectedIndex=0;mode.DropDownStyle=category.DropDownStyle=ComboBoxStyle.DropDownList;mode.Dock=category.Dock=search.Dock=DockStyle.Fill;
  filters.Controls.Add(mode);filters.Controls.Add(category);filters.Controls.Add(search);root.Controls.Add(filters);
  items.Dock=DockStyle.Fill;items.BackColor=Color.FromArgb(30,36,48);items.ForeColor=Color.White;items.BorderStyle=BorderStyle.None;items.HorizontalScrollbar=true;root.Controls.Add(items);
  var actions=new FlowLayoutPanel(){Dock=DockStyle.Fill};play.Text="▶ Reproduzir";star.Text="☆ Favorito";guide.Text="Programação";back.Text="Voltar às séries";back.Width=155;play.Width=140;guide.Width=140;m3u.Text="Abrir M3U";m3u.Width=120;foreach(Button button in new[]{play,star,guide,back,m3u}){button.AutoSize=true;button.MinimumSize=new Size(110,32);}actions.Controls.AddRange(new Control[]{play,star,guide,back,m3u});root.Controls.Add(actions);
  status.Dock=DockStyle.Fill;root.Controls.Add(status);status.Text="Informe os dados do seu provedor. A senha não será salva.";
  login.Click+=async(s,e)=>await Connect();mode.SelectedIndexChanged+=async(s,e)=>{if(mode.SelectedIndex==3){Filter();return;}if(host!=null)await LoadList();};
  search.TextChanged+=(s,e)=>Filter();category.SelectedIndexChanged+=(s,e)=>Filter();play.Click+=(s,e)=>Play();items.DoubleClick+=(s,e)=>Play();
  back.Click+=async(s,e)=>await LoadList();star.Click+=(s,e)=>ToggleFavorite();guide.Click+=async(s,e)=>await Guide();
  m3u.Click+=async(s,e)=>await ImportM3u();
  Directory.CreateDirectory(dataDir);var fav=Path.Combine(dataDir,"favorites.txt");if(File.Exists(fav))favorites=new HashSet<string>(File.ReadAllLines(fav));
  vlc=new[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"VideoLAN","VLC","vlc.exe"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"VideoLAN","VLC","vlc.exe")}.FirstOrDefault(File.Exists);
 }
 string Esc(string value){return Uri.EscapeDataString(value??"");}
 string Api(string action){return host+"/player_api.php?username="+Esc(username)+"&password="+Esc(password)+(action==""?"":"&action="+action);}
 async Task<object> Fetch(string url){
  return await Task.Run(()=>{
   var req=(HttpWebRequest)WebRequest.Create(url);req.Timeout=45000;req.ReadWriteTimeout=45000;req.UserAgent="XCIPTV-PC/0.1.3";
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
 void Busy(bool value,string text){busy=value;login.Enabled=mode.Enabled=play.Enabled=guide.Enabled=back.Enabled=m3u.Enabled=!value;status.Text=text;}
 async Task Connect(){if(busy)return;Uri uri;if(!Uri.TryCreate(server.Text.Trim(),UriKind.Absolute,out uri)||(uri.Scheme!="https"&&uri.Scheme!="http")||!string.IsNullOrEmpty(uri.UserInfo)||!string.IsNullOrEmpty(uri.Query)){MessageBox.Show("Informe somente o endereço do servidor, incluindo http:// ou https://.");return;}
  host=server.Text.Trim().TrimEnd('/');if(host.EndsWith("/player_api.php",StringComparison.OrdinalIgnoreCase))host=host.Substring(0,host.Length-15);username=user.Text.Trim();password=pass.Text;Busy(true,"Verificando conta…");
  try{ValidateLogin(await Fetch(Api("")));Busy(false,"Conectado.");await LoadList();}
  catch(Exception ex){host=null;Busy(false,"Falha ao conectar.");ShowError(ex);}
 }
 void ShowError(Exception ex){MessageBox.Show(ex is WebException?NetworkFailure((WebException)ex):ex.Message,"XCIPTV-PC",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
 async Task LoadList(){if(busy||host==null)return;Busy(true,"Carregando catálogo…");episodes=false;kind=mode.SelectedIndex==1?"vod":mode.SelectedIndex==2?"series":"live";
  try{var cats=Rows(await Fetch(Api("get_"+kind+"_categories"))).Select(Dict).ToList();var rows=Rows(await Fetch(Api(kind=="series"?"get_series":"get_"+kind+"_streams")));entries=rows.Select(x=>{var d=Dict(x);return new Entry(){Name=Get(d,"name"),Id=Get(d,kind=="series"?"series_id":"stream_id"),Extension=Get(d,"container_extension",kind=="live"?"ts":"mp4"),Category=Get(d,"category_id")};}).ToList();
   category.Items.Clear();category.Items.Add(new Entry(){Name="Todas as categorias",Id=""});foreach(var cat in cats)category.Items.Add(new Entry(){Name=Get(cat,"category_name"),Id=Get(cat,"category_id")});category.SelectedIndex=0;Filter();Busy(false,entries.Count+" itens. Digite no campo à direita para buscar.");}
  catch(Exception ex){Busy(false,"Não foi possível carregar o catálogo.");ShowError(ex);}
 }
 void Filter(){var cat=category.SelectedItem as Entry;items.BeginUpdate();items.Items.Clear();foreach(var e in entries){if(e.Name.IndexOf(search.Text,StringComparison.CurrentCultureIgnoreCase)<0)continue;if(!episodes&&cat!=null&&cat.Id!=""&&e.Category!=cat.Id)continue;if(mode.SelectedIndex==3&&!favorites.Contains(Key(e)))continue;items.Items.Add(e);}items.EndUpdate();}
 string Key(Entry e){return (e.Url??host)+"|"+kind+"|"+e.Id;}
 void ToggleFavorite(){var e=items.SelectedItem as Entry;if(e==null)return;string key=Key(e);if(!favorites.Add(key))favorites.Remove(key);File.WriteAllLines(Path.Combine(dataDir,"favorites.txt"),favorites);status.Text=favorites.Contains(key)?"Adicionado aos favoritos.":"Removido dos favoritos.";if(mode.SelectedIndex==3)Filter();}
 async void Play(){if(busy)return;var e=items.SelectedItem as Entry;if(e==null)return;if(kind=="series"&&!episodes){await LoadEpisodes(e);return;}
  if(vlc==null){using(var dialog=new OpenFileDialog(){Title="Selecione vlc.exe",Filter="VLC|vlc.exe"}){if(dialog.ShowDialog()!=DialogResult.OK)return;vlc=dialog.FileName;}}
  var url=e.Url??host+"/"+(kind=="vod"?"movie":kind)+"/"+Esc(username)+"/"+Esc(password)+"/"+Esc(e.Id)+"."+Esc(e.Extension);
  try{Process.Start(new ProcessStartInfo(vlc,"--no-one-instance --play-and-exit \""+url.Replace("\"","%22")+"\""){UseShellExecute=false});status.Text="Reprodução aberta no VLC: "+e.Name;}catch{MessageBox.Show("Não foi possível iniciar o VLC.");}
 }
 async Task LoadEpisodes(Entry series){Busy(true,"Carregando episódios…");try{var result=Dict(await Fetch(Api("get_series_info&series_id="+Esc(series.Id))));object raw;if(!result.TryGetValue("episodes",out raw))throw new Exception("O servidor não retornou episódios.");var list=new List<Entry>();foreach(var season in Dict(raw)){foreach(var value in Rows(season.Value)){var d=Dict(value);list.Add(new Entry(){Name="T"+season.Key+" · "+Get(d,"episode_num")+" · "+Get(d,"title"),Id=Get(d,"id"),Extension=Get(d,"container_extension","mp4")});}}entries=list;episodes=true;search.Clear();Filter();Busy(false,list.Count+" episódios de "+series.Name);}catch(Exception ex){Busy(false,"Falha ao carregar episódios.");ShowError(ex);}}
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
 [STAThread]public static void Main(string[] args){if(args.Contains("--self-test")){SelfTest();return;}ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;Application.EnableVisualStyles();var window=new XCIPTVPC();if(args.Contains("--smoke-test"))window.Shown+=(sender,eventArgs)=>window.BeginInvoke(new Action(()=>window.Close()));Application.Run(window);}
}

