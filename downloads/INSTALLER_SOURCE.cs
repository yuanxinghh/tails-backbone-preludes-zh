using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;

public static class PatchCore {
    public const string PayloadName="zzzz_BackboneStories_ChineseFull_P.pak";
    public const string PayloadHash="c0bf9c5011ca02fad501ef93e76450d01df89466c07b98646cba54ecc8b5af8d";
    public static readonly string[] OldNames={"z_BackboneStories_Repair_P.pak","z_BackboneStories_Voices_P.pak","zz_BackboneStories_InteractionFix_P.pak","zzz_BackboneStories_ConfirmText_P.pak",PayloadName};
    public const string EnglishHash="8fb5c6cada21ad767d85fd4b86259424f6e955bf8b8e2f2e379241afb9aa54af";
    public const string ChineseHash="7daa0ac940e56b4c7b853b5b4942d97c6309944f7f64eba0e11bf99ac940e675";
    static string Combine(string root,string rel){return Path.Combine(root,rel.Replace('/',Path.DirectorySeparatorChar));}
    public static string Paks(string root){return Combine(root,"BackboneStories/Content/Paks");}
    public static string Hash(string p){using(var f=File.OpenRead(p))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
    public static string HashText(string value){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
    public static string NormalizeRoot(string input){
        var root=Path.GetFullPath(input.Trim().Trim('"')).TrimEnd(Path.DirectorySeparatorChar);
        if(File.Exists(Combine(root,"BackboneStories-Windows.pak")))root=Path.GetFullPath(Path.Combine(root,"../../.."));
        else if(Directory.Exists(Combine(root,"Content/Paks")))root=Directory.GetParent(root).FullName;
        if(!File.Exists(Combine(Paks(root),"BackboneStories-Windows.pak")))throw new Exception("未找到游戏主文件。请选择含 BackboneStories 文件夹的游戏安装目录。");
        return root;
    }
    public static void ValidateGame(string root,bool fixture){
        if(fixture)return;
        if(!File.Exists(Combine(root,"BackboneStories/Binaries/Win64/BackboneStories-Win64-Shipping.exe")))throw new Exception("目录不完整：未找到游戏程序。");
        foreach(var p in Process.GetProcesses())try{using(p){var name=p.ProcessName;if(name.StartsWith("BackboneStories",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("请保存进度并完全退出游戏，再安装或卸载汉化。");}}catch(InvalidOperationException){throw;}catch{}
        var hash=Hash(Combine(Paks(root),"BackboneStories-Windows.pak"));
        if(hash!=EnglishHash&&hash!=ChineseHash)throw new Exception("此游戏版本未通过兼容性验证，已停止安装。请将游戏版本和主 Pak 的 SHA-256 发给 dr.Dorr："+hash);
    }
    public static void VerifyBundle(){using(var src=Assembly.GetExecutingAssembly().GetManifestResourceStream("ChinesePatch"))using(var h=SHA256.Create()){
        if(src==null)throw new Exception("安装程序缺少补丁资源。");
        var hash=BitConverter.ToString(h.ComputeHash(src)).Replace("-","").ToLowerInvariant();if(hash!=PayloadHash)throw new Exception("安装程序资源校验失败，请重新下载。");
    }}
    public static void Install(string input,Action<string> status,bool fixture){
        var root=NormalizeRoot(input);ValidateGame(root,fixture);VerifyBundle();
        var dir=Paks(root);var backup=Combine(root,".drdorr-zh-backup");var state=Path.Combine(backup,"state.txt");var target=Path.Combine(dir,PayloadName);
        if(Directory.Exists(backup)){
            if(File.Exists(state)&&File.Exists(target)&&Hash(target)==PayloadHash&&File.ReadAllLines(state).FirstOrDefault()==PayloadHash){status("完整汉化已安装，无需重复安装。");return;}
            throw new Exception("发现已有安装备份。请先使用本程序卸载，或检查 .drdorr-zh-backup，避免覆盖原备份。");
        }
        var temp=Path.Combine(dir,"drdorr-"+Guid.NewGuid().ToString("N")+".tmp");
        var originals=OldNames.Where(n=>File.Exists(Path.Combine(dir,n))).ToArray();
        bool changed=false;
        try{
            status("正在写入并校验完整汉化资源……");
            using(var src=Assembly.GetExecutingAssembly().GetManifestResourceStream("ChinesePatch"))using(var dst=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){src.CopyTo(dst);dst.Flush(true);}
            if(Hash(temp)!=PayloadHash)throw new Exception("写入后的补丁校验失败。");
            Directory.CreateDirectory(backup);
            foreach(var n in originals)File.Copy(Path.Combine(dir,n),Path.Combine(backup,n),false);
            File.WriteAllLines(state,new[]{PayloadHash}.Concat(originals).ToArray(),new UTF8Encoding(false));
            changed=true;
            foreach(var n in originals)File.Delete(Path.Combine(dir,n));
            File.Move(temp,target);
            if(Hash(target)!=PayloadHash)throw new Exception("安装结果校验失败。");
            status("安装完成。直接启动游戏即可，无需解包 Pak。可再次运行本程序卸载并恢复旧补丁。");
        }catch{
            if(changed){
                if(File.Exists(target))File.Delete(target);
                foreach(var n in originals)File.Copy(Path.Combine(backup,n),Path.Combine(dir,n),true);
                File.Delete(state);
                foreach(var n in originals)File.Delete(Path.Combine(backup,n));
                Directory.Delete(backup);
            }else if(Directory.Exists(backup)&&!File.Exists(state)){
                foreach(var n in originals){var p=Path.Combine(backup,n);if(File.Exists(p))File.Delete(p);}
                if(!Directory.EnumerateFileSystemEntries(backup).Any())Directory.Delete(backup);
            }
            throw;
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public static void Uninstall(string input,Action<string> status,bool fixture){
        var root=NormalizeRoot(input);ValidateGame(root,fixture);var dir=Paks(root);var backup=Combine(root,".drdorr-zh-backup");var state=Path.Combine(backup,"state.txt");
        if(!File.Exists(state))throw new Exception("此目录没有本安装程序的安装记录。");
        var lines=File.ReadAllLines(state);if(lines.Length==0||lines[0]!=PayloadHash||lines.Skip(1).Any(n=>!OldNames.Contains(n))||lines.Skip(1).Distinct().Count()!=lines.Length-1)throw new Exception("安装记录不完整，已停止卸载。");
        var originals=lines.Skip(1).ToArray();var target=Path.Combine(dir,PayloadName);
        if(File.Exists(target)&&Hash(target)!=PayloadHash)throw new Exception("完整补丁已被其他文件替换。请先检查该文件，避免卸载时删除其他补丁。");
        foreach(var n in originals){if(!File.Exists(Path.Combine(backup,n)))throw new Exception("备份缺失："+n);if(n!=PayloadName&&File.Exists(Path.Combine(dir,n)))throw new Exception("检测到安装后新增的同名旧补丁，请先移出："+n);}
        status("正在恢复安装前的补丁……");
        var restoreTemp=Path.Combine(backup,"restore-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(restoreTemp);
        foreach(var n in originals)File.Copy(Path.Combine(backup,n),Path.Combine(restoreTemp,n));
        if(File.Exists(target))File.Delete(target);
        foreach(var n in originals)File.Move(Path.Combine(restoreTemp,n),Path.Combine(dir,n));
        Directory.Delete(restoreTemp);
        File.Delete(state);foreach(var n in originals)File.Delete(Path.Combine(backup,n));Directory.Delete(backup);
        status("卸载完成，已恢复安装前的补丁。游戏主文件与存档未修改。");
    }
}

public class InstallerForm:Form {
    TextBox folder=new TextBox();Label status=new Label();Button install=new Button(),uninstall=new Button(),browse=new Button();ProgressBar progress=new ProgressBar();
    bool busy;
    protected override void OnFormClosing(FormClosingEventArgs e){if(busy)e.Cancel=true;base.OnFormClosing(e);}
    public InstallerForm(){
        Text="浣熊硬探2 · 完整汉化安装程序 | dr.Dorr";ClientSize=new Size(680,430);MinimumSize=new Size(696,469);MaximumSize=MinimumSize;StartPosition=FormStartPosition.CenterScreen;Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(244,246,250);
        var title=new Label{Text="浣熊硬探2 · 完整汉化补丁",Font=new Font(Font.FontFamily,21,FontStyle.Bold),Location=new Point(28,24),Size=new Size(620,45)};Controls.Add(title);
        var intro=new Label{Text="适用：已核验的 Steam Windows 版本 · 2026.11.27\n已包含基础汉化与后续修复。选择游戏目录即可安装，无需打开或解包 .pak。",Location=new Point(30,83),Size=new Size(620,55)};Controls.Add(intro);
        var caption=new Label{Text="游戏安装目录",Location=new Point(30,152),AutoSize=true};Controls.Add(caption);
        folder.SetBounds(30,181,510,30);folder.Text=FindGame();Controls.Add(folder);
        browse.Text="选择目录";browse.SetBounds(552,178,100,34);browse.Click+=(s,e)=>{using(var dialog=new FolderBrowserDialog{Description="选择浣熊硬探2的安装目录",SelectedPath=folder.Text})if(dialog.ShowDialog(this)==DialogResult.OK)folder.Text=dialog.SelectedPath;};Controls.Add(browse);
        install.Text="安装完整汉化";install.SetBounds(30,234,180,43);install.BackColor=Color.FromArgb(248,120,53);install.FlatStyle=FlatStyle.Flat;install.Click+=async(s,e)=>await Run(false);Controls.Add(install);
        uninstall.Text="卸载并恢复";uninstall.SetBounds(224,234,160,43);uninstall.Click+=async(s,e)=>await Run(true);Controls.Add(uninstall);
        progress.SetBounds(30,293,620,8);Controls.Add(progress);
        status.Text="安装前请保存并退出游戏。程序会备份同名旧补丁，不修改游戏主 Pak 和存档。";status.SetBounds(30,315,620,58);Controls.Add(status);
        var note=new Label{Text="非官方汉化交流 · 反馈：drchenjunpeng@hotmail.com\n已做资源与安装校验；全流程实机验证仍在跟进。",ForeColor=Color.FromArgb(90,100,118),Location=new Point(30,378),Size=new Size(620,45),Font=new Font(Font.FontFamily,9)};Controls.Add(note);
    }
    string FindGame(){
        var libraries=new System.Collections.Generic.List<string>();
        foreach(var hive in new[]{Registry.CurrentUser,Registry.LocalMachine})foreach(var key in new[]{@"Software\Valve\Steam",@"Software\WOW6432Node\Valve\Steam"})using(var k=hive.OpenSubKey(key)){var p=k==null?null:(k.GetValue("SteamPath")??k.GetValue("InstallPath"))as string;if(!String.IsNullOrEmpty(p))libraries.Add(p);}
        foreach(var lib in libraries.ToArray()){var v=Path.Combine(lib,"steamapps/libraryfolders.vdf");if(File.Exists(v))foreach(System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(v),"\"path\"\\s+\"([^\"]+)\""))libraries.Add(m.Groups[1].Value.Replace("\\\\","\\"));}
        foreach(var lib in libraries.Distinct())foreach(var name in new[]{"Tails The Backbone Preludes","Tails Noir Preludes","Tails The Backbone Preludes_debug"}){var p=Path.Combine(lib,"steamapps/common/"+name);if(File.Exists(Path.Combine(PatchCore.Paks(p),"BackboneStories-Windows.pak")))return p;}
        return "";
    }
    async Task Run(bool remove){
        var p=folder.Text;if(String.IsNullOrWhiteSpace(p)){MessageBox.Show(this,"请先选择游戏安装目录。");return;}
        busy=true;install.Enabled=uninstall.Enabled=browse.Enabled=folder.Enabled=false;progress.Style=ProgressBarStyle.Marquee;
        Action<string> report=t=>BeginInvoke(new Action(()=>status.Text=t));
        try{await Task.Run(()=>{var key=PatchCore.HashText(Path.GetFullPath(p).ToLowerInvariant());using(var mutex=new System.Threading.Mutex(false,"Local\\DrDorrTailsChinese-"+key)){if(!mutex.WaitOne(0))throw new Exception("此目录已有安装操作正在执行，请稍后再试。");try{if(remove)PatchCore.Uninstall(p,report,false);else PatchCore.Install(p,report,false);}finally{mutex.ReleaseMutex();}}});}
        catch(Exception ex){status.Text=ex.Message;MessageBox.Show(this,ex.Message,"未完成",MessageBoxButtons.OK,MessageBoxIcon.Information);}
        finally{busy=false;install.Enabled=uninstall.Enabled=browse.Enabled=folder.Enabled=true;progress.Style=ProgressBarStyle.Blocks;}
    }
    [STAThread]public static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new InstallerForm());}
}
