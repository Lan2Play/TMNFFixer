using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using Microsoft.Win32;
using System.IO.Compression;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using System.Reflection;
using System.Security.Cryptography;

namespace tmnffixer
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
       static void Main()
        {
            string MSGboxcaption = "TNMFFixer";
            string message = "";
            string TMNFPath = "";
            bool pathaccepted = false;
            bool pathfirstrun = true;
            var wc = new WebClient();

            //get installation Path
            Console.WriteLine("get Trackmania installed Path");
            TMNFPath = SearchTMNFKey().Replace(@"\TmForever.exe", "");

            while (!pathaccepted)
            {
                if (TMNFPath.Length == 0)
                {
                    if (pathfirstrun)
                    {
                        message = "Es konnte kein Pfad von TMNF Automatisch gefunden werden, bitte manuell auswählen!";
                        MessageBox.Show(message, MSGboxcaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        pathfirstrun = false;
                    }
                    FolderBrowserDialog b = new FolderBrowserDialog();
                    if (b.ShowDialog() == DialogResult.OK)
                    {
                        TMNFPath = b.SelectedPath;
                    }
                    if (b.ShowDialog() == DialogResult.Cancel)
                    {
                        Environment.Exit(0);
                    }


                }
                else
                {
                    message = "Folgender Installationspfad für Trackmania wurde gewählt : " + TMNFPath + " ist das korrekt? Achtung: während des Fixes wird nichts angezeigt, bitte warten bis die Erfolgsbestätigung erscheint!";
                    var result = MessageBox.Show(message, MSGboxcaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        pathaccepted = true;
                    }
                    if (result == DialogResult.No)
                    {
                        TMNFPath = "";
                    }
                    if (result == DialogResult.Cancel)
                    {
                        Environment.Exit(0);
                    }

                }
            }

            Console.WriteLine("Path " + TMNFPath);

            //read tmnf version and check for update
            string nadeoini = File.ReadAllText(TMNFPath + @"\Nadeo.ini");

            Console.WriteLine("Check if newest version");
            if (!Regex.IsMatch(nadeoini, "Version=2.11.26"))
            {
                Console.WriteLine("not newest version, updating!");
                //download TM Update 2010-03-15
                string updatepath = Path.GetTempPath() + "TmUnitedForever_Update_2010-03-15_Setup.exe";
                wc.DownloadFile("http://files2.trackmaniaforever.com/TmUnitedForever_Update_2010-03-15_Setup.exe", updatepath);
                string extractfolder = Path.GetTempPath() + "TMNFextract";
                Directory.CreateDirectory(extractfolder);

                //download uniextract
                string uniexctractzip = Path.GetTempPath() + "UniExtractRC3.zip";
                string uniextractfolder = Path.GetTempPath() + "uniexctract";
                string uniextractpath = uniextractfolder + @"\UniExtract";
                string uniextractexe = uniextractpath + @"\UniExtract.exe";
                wc.DownloadFile(" https://github.com/Bioruebe/UniExtract2/releases/download/v2.0.0-rc.3/UniExtractRC3.zip", uniexctractzip);
                if(Directory.Exists(uniextractfolder))
                {
                    Directory.Delete(uniextractfolder, true);
                }
                Directory.CreateDirectory(uniextractfolder);
                ZipFile.ExtractToDirectory(uniexctractzip, uniextractfolder);

                //configure uniextract
                Guid g = Guid.NewGuid();
                string uniexini = File.ReadAllText(uniextractpath + @"\UniExtract.ini");
                uniexini = uniexini.Replace("silentmode=0", "silentmode=1");
                uniexini = uniexini.Replace("ID=", "ID=" + g.ToString());
                File.WriteAllText(uniextractpath + @"\UniExtract.ini", uniexini);

                //extract tmnf update
                var process = new Process();
                process.StartInfo.FileName = uniextractexe;
                process.StartInfo.Arguments = updatepath + " " + extractfolder + " /silent";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.OutputDataReceived += (sender, data) =>
                {
                    Console.WriteLine(data.Data);
                };
                process.StartInfo.RedirectStandardError = true;
                process.ErrorDataReceived += (sender, data) =>
                {
                    Console.WriteLine(data.Data);
                };
                process.Start();
                process.WaitForExit();

                //update TMNF
                File.Copy(extractfolder + @"\TmForever.exe", TMNFPath + @"\TmForever.exe", true);
                File.Copy(extractfolder + @"\TmForeverLauncher.exe", TMNFPath + @"\TmForeverLauncher.exe", true);
                FileSystem.CopyDirectory(extractfolder + @"\GameData", TMNFPath + @"\GameData", true);

                nadeoini = nadeoini.Replace("Distro=MOLUX", "Distro=MILIN");
                nadeoini = Regex.Replace(nadeoini, "Version=.*", "Version=2.11.26");
                File.WriteAllText(TMNFPath + @"\Nadeo.ini", nadeoini);


                //delete stuff
                Directory.Delete(extractfolder, true);
                Directory.Delete(uniextractfolder, true);
                File.Delete(uniexctractzip);
                File.Delete(updatepath);

            }
            else
            {
                Console.WriteLine("newest version, nothing to do!");
            }

            //Add l2ptmnf URL Handler if not existent

            RegistryKey urlhandlerkey = Registry.ClassesRoot.OpenSubKey("l2ptmnf", false);
            if (urlhandlerkey == null)
            {
                Console.WriteLine("l2ptmnf URL Handler not existent, adding!");
                urlhandlerkey = Registry.ClassesRoot.CreateSubKey("l2ptmnf", true);
                urlhandlerkey.SetValue("", "URL:l2ptmnf protocol");
                urlhandlerkey.SetValue("URL Protocol", "");
                urlhandlerkey.Close();
                urlhandlerkey = Registry.ClassesRoot.OpenSubKey("l2ptmnf", true).CreateSubKey("DefaultIcon", true);

                urlhandlerkey.SetValue("", TMNFPath + @"\Gbx.ico");
                urlhandlerkey.Close();
                urlhandlerkey = Registry.ClassesRoot.OpenSubKey("l2ptmnf", true).CreateSubKey("Shell", true);
                urlhandlerkey.Close();
                urlhandlerkey = Registry.ClassesRoot.OpenSubKey("l2ptmnf", true).OpenSubKey("Shell", true).CreateSubKey("Open", true);
                urlhandlerkey.Close();
                urlhandlerkey = Registry.ClassesRoot.OpenSubKey("l2ptmnf", true).OpenSubKey("Shell", true).OpenSubKey("Open", true).CreateSubKey("Command", true);
                urlhandlerkey.SetValue("", "\"" + TMNFPath + "\\urllauncher.exe\" \"%1\" \"" + TMNFPath + "\\TmForever.exe\" \"/useexedir /singleinst /join=\" true");
                urlhandlerkey.Close();
            }
            else
            {
                Console.WriteLine("l2ptmnf URL Handler existent, nothing to do!");
            }

            //copy urllauncher to tm directory

            using (Assembly.GetExecutingAssembly().GetManifestResourceStream("urllauncher"))
            {
                if (!File.Exists(TMNFPath + "\\urllauncher.exe"))
                {
                    Console.WriteLine("urllauncher not existent, copy it!");
                    using (var file = new FileStream(TMNFPath + "\\urllauncher.exe", FileMode.Create, FileAccess.Write))
                    {
                        Assembly.GetExecutingAssembly().GetManifestResourceStream("urllauncher").CopyTo(file);
                    }
                }
                else
                {
                    using (MD5 md5 = MD5.Create())
                    {
                        byte[] resourcehash = md5.ComputeHash(Assembly.GetExecutingAssembly().GetManifestResourceStream("urllauncher"));
                        Console.WriteLine(Convert.ToBase64String(resourcehash));
                        using (var stream = File.OpenRead(TMNFPath + "\\urllauncher.exe"))
                        {
                            byte[] filehash = md5.ComputeHash(stream);
                            Console.WriteLine(Convert.ToBase64String(filehash));
                            stream.Close();
                            if (Convert.ToBase64String(filehash) != Convert.ToBase64String(resourcehash))
                            {
                                File.Delete(TMNFPath + "\\urllauncher.exe");
                                using (var file = new FileStream(TMNFPath + "\\urllauncher.exe", FileMode.Create, FileAccess.Write))
                                {
                                    Console.WriteLine("urllauncher existent but MD5 missmatch, copy it!");
                                    Assembly.GetExecutingAssembly().GetManifestResourceStream("urllauncher").CopyTo(file);
                                }
                            }
                            else
                            {
                                Console.WriteLine("urllauncher existent and the same, nothing to do!");
                            }
                        }


                    }


                }



            }

                    message = "Der Lan2play TMNF Fix wurde erfolgreich durchgeführt!";
                    MessageBox.Show(message, MSGboxcaption, MessageBoxButtons.OK, MessageBoxIcon.Question);


        }

        static public string SearchTMNFKey()
        {
            RegistryKey TMNFKey = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore\Children");
            var programs = TMNFKey.GetSubKeyNames();

            foreach (var program in programs)
            {
                RegistryKey subkey = TMNFKey.OpenSubKey(program);
                string strtemp = subkey.GetValue("MatchedExeFullPath", string.Empty).ToString();
                if (strtemp.Contains("TmForever.exe"))
                {
                    return strtemp;
                }
            }

            return string.Empty;
        }
    }
}
