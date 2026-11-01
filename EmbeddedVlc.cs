using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

// Bindings for the stable LibVLC 3 API supplied by the user's VLC installation.
public sealed class EmbeddedVlc : IDisposable {
 IntPtr instance,player;static string loadedDirectory;
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr LoadLibraryEx(string path,IntPtr file,uint flags);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr libvlc_get_version();
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr libvlc_new(int argc,IntPtr argv);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern void libvlc_release(IntPtr i);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr libvlc_media_player_new(IntPtr i);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern void libvlc_media_player_release(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr libvlc_media_new_location(IntPtr i,IntPtr location);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern void libvlc_media_release(IntPtr m);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern void libvlc_media_player_set_media(IntPtr p,IntPtr m);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern void libvlc_media_player_set_hwnd(IntPtr p,IntPtr hwnd);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern void libvlc_video_set_mouse_input(IntPtr p,uint on);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern void libvlc_video_set_key_input(IntPtr p,uint on);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern int libvlc_media_player_play(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern void libvlc_media_player_set_pause(IntPtr p,int pause);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern void libvlc_media_player_stop(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern int libvlc_media_player_get_state(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern int libvlc_media_player_is_seekable(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern long libvlc_media_player_get_time(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern long libvlc_media_player_get_length(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern float libvlc_media_player_get_position(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern void libvlc_media_player_set_position(IntPtr p,float position);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern int libvlc_audio_set_volume(IntPtr p,int volume);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern uint libvlc_media_player_has_vout(IntPtr p);
 [DllImport("libvlc.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr libvlc_media_player_get_hwnd(IntPtr p);
 static IntPtr Utf8(string value){byte[] bytes=Encoding.UTF8.GetBytes(value+"\0");var ptr=Marshal.AllocHGlobal(bytes.Length);Marshal.Copy(bytes,0,ptr,bytes.Length);return ptr;}
 public EmbeddedVlc(string executable,bool test){
  string directory=Path.GetFullPath(Path.GetDirectoryName(executable));
  if(loadedDirectory!=null&&!string.Equals(loadedDirectory,directory,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Feche e reabra o XCIPTV-PC para usar outra instalação do VLC.");
  if(!File.Exists(Path.Combine(directory,"libvlc.dll"))||!Directory.Exists(Path.Combine(directory,"plugins")))throw new InvalidOperationException("A instalação selecionada não contém o motor completo do VLC. Selecione uma instalação completa do VLC 3.");
  if(loadedDirectory==null){
   if(LoadLibraryEx(Path.Combine(directory,"libvlccore.dll"),IntPtr.Zero,8)==IntPtr.Zero||LoadLibraryEx(Path.Combine(directory,"libvlc.dll"),IntPtr.Zero,8)==IntPtr.Zero)
    throw new InvalidOperationException("Não foi possível carregar o VLC interno. O VLC deve ter a mesma arquitetura do programa: "+(Environment.Is64BitProcess?"64 bits":"32 bits")+". Código do Windows: "+Marshal.GetLastWin32Error()+".");
   loadedDirectory=directory;
  }
  string version=Marshal.PtrToStringAnsi(libvlc_get_version());if(version==null||!version.StartsWith("3."))throw new InvalidOperationException("O player interno desta versão requer VLC 3.x. A versão instalada não usa essa API.");
  Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH",Path.Combine(directory,"plugins"));
  string[] options=test?new[]{"--quiet","--no-video-title-show","--aout=dummy","--vout=wingdi"}:new[]{"--quiet","--no-video-title-show","--network-caching=1500"};
  var pointers=new IntPtr[options.Length];var argv=Marshal.AllocHGlobal(IntPtr.Size*options.Length);
  try{for(int i=0;i<options.Length;i++){pointers[i]=Utf8(options[i]);Marshal.WriteIntPtr(argv,i*IntPtr.Size,pointers[i]);}instance=libvlc_new(options.Length,argv);}
  finally{foreach(var ptr in pointers)if(ptr!=IntPtr.Zero)Marshal.FreeHGlobal(ptr);Marshal.FreeHGlobal(argv);}
  if(instance==IntPtr.Zero)throw new InvalidOperationException("O motor do VLC não conseguiu iniciar.");
  player=libvlc_media_player_new(instance);if(player==IntPtr.Zero){libvlc_release(instance);instance=IntPtr.Zero;throw new InvalidOperationException("Não foi possível criar o player interno.");}
 }
 public void SetWindow(IntPtr hwnd){if(player!=IntPtr.Zero){libvlc_media_player_set_hwnd(player,hwnd);libvlc_video_set_mouse_input(player,0);libvlc_video_set_key_input(player,0);}}
 public void Play(string url){if(player==IntPtr.Zero)throw new ObjectDisposedException("EmbeddedVlc");var ptr=Utf8(url);IntPtr media;try{media=libvlc_media_new_location(instance,ptr);}finally{Marshal.FreeHGlobal(ptr);}if(media==IntPtr.Zero)throw new InvalidOperationException("O VLC não conseguiu abrir a mídia.");try{libvlc_media_player_set_media(player,media);}finally{libvlc_media_release(media);}if(libvlc_media_player_play(player)!=0)throw new InvalidOperationException("Não foi possível iniciar a reprodução interna.");}
 public int State {get{return player==IntPtr.Zero?0:libvlc_media_player_get_state(player);}}
 public bool Seekable {get{return player!=IntPtr.Zero&&libvlc_media_player_is_seekable(player)!=0;}}
 public long Time {get{return player==IntPtr.Zero?0:libvlc_media_player_get_time(player);}}
 public long Length {get{return player==IntPtr.Zero?0:libvlc_media_player_get_length(player);}}
 public float Position {get{return player==IntPtr.Zero?0:libvlc_media_player_get_position(player);}set{if(Seekable)libvlc_media_player_set_position(player,Math.Max(0,Math.Min(1,value)));}}
 public void Pause(bool paused){if(player!=IntPtr.Zero)libvlc_media_player_set_pause(player,paused?1:0);}
 public void Stop(){if(player!=IntPtr.Zero)libvlc_media_player_stop(player);}
 public void Volume(int value){if(player!=IntPtr.Zero)libvlc_audio_set_volume(player,Math.Max(0,Math.Min(100,value)));}
 public void Dispose(){if(player!=IntPtr.Zero){libvlc_media_player_stop(player);libvlc_media_player_release(player);player=IntPtr.Zero;}if(instance!=IntPtr.Zero){libvlc_release(instance);instance=IntPtr.Zero;}}
 static void Four(BinaryWriter w,string value){w.Write(Encoding.ASCII.GetBytes(value));}
 static long List(BinaryWriter w,string type){Four(w,"LIST");long size=w.BaseStream.Position;w.Write(0);Four(w,type);return size;}
 static void Finish(BinaryWriter w,long size){long end=w.BaseStream.Position;w.BaseStream.Position=size;w.Write((int)(end-size-4));w.BaseStream.Position=end;}
 static void WriteVideo(string path){using(var w=new BinaryWriter(File.Create(path))){const int width=160,height=90,frames=40,frameSize=width*height*3;
  Four(w,"RIFF");long riff=w.BaseStream.Position;w.Write(0);Four(w,"AVI ");long hdrl=List(w,"hdrl");Four(w,"avih");w.Write(56);foreach(int value in new[]{100000,frameSize*10,0,0,frames,0,1,frameSize,width,height,0,0,0,0})w.Write(value);
  long strl=List(w,"strl");Four(w,"strh");w.Write(56);Four(w,"vids");Four(w,"DIB ");w.Write(0);w.Write((short)0);w.Write((short)0);foreach(int value in new[]{0,1,10,0,frames,frameSize,-1,0})w.Write(value);w.Write((short)0);w.Write((short)0);w.Write((short)width);w.Write((short)height);
  Four(w,"strf");w.Write(40);w.Write(40);w.Write(width);w.Write(height);w.Write((short)1);w.Write((short)24);foreach(int value in new[]{0,frameSize,0,0,0,0})w.Write(value);Finish(w,strl);Finish(w,hdrl);
  long movi=List(w,"movi");for(int f=0;f<frames;f++){Four(w,"00db");w.Write(frameSize);for(int y=0;y<height;y++)for(int x=0;x<width;x++){w.Write((byte)(x+f*4));w.Write((byte)(y*2));w.Write((byte)(f*5));}}Finish(w,movi);Finish(w,riff);
 }}
 public static void LocalPlaybackTest(string executable){
  string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")+".avi");
  try{WriteVideo(path);using(var window=new Form(){Text="Local internal player verification",Size=new Size(480,320)})using(var surface=new Panel(){Dock=DockStyle.Fill,BackColor=Color.Black}){
   window.Controls.Add(surface);window.Show();Application.DoEvents();using(var engine=new EmbeddedVlc(executable,true)){
    engine.SetWindow(surface.Handle);if(libvlc_media_player_get_hwnd(engine.player)!=surface.Handle)throw new Exception("Video HWND binding failed");engine.Play(new Uri(path).AbsoluteUri);
    bool playing=false;for(int i=0;i<90;i++){Application.DoEvents();System.Threading.Thread.Sleep(100);if(engine.State==7)throw new Exception("Local LibVLC video playback failed");if(engine.State==3&&engine.Time>100&&libvlc_media_player_has_vout(engine.player)>0){playing=true;break;}}
    if(!playing)throw new Exception("Internal video output did not start");engine.Volume(35);engine.Pause(true);for(int i=0;i<5;i++){Application.DoEvents();System.Threading.Thread.Sleep(50);}if(engine.State!=4)throw new Exception("Pause failed");
    engine.Position=0.5f;engine.Pause(false);engine.Stop();
   }window.Close();
  }}finally{if(File.Exists(path))File.Delete(path);}
 }
}
