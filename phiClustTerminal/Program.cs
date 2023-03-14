using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Linq.Expressions;
using System.Timers;
using phiClustCore;
using phiClustCore.Interface;
using phiClustCore.Distance;
using phiClustCore.Profiles;



namespace phiClustTerminal
{
    class Program
    {
        static JobManager manager = new JobManager();
       // static Timer t = new Timer();
        private static void UpdateProgress(object sender, EventArgs e)
        {
           
            Dictionary<string, double> res = manager.ProgressUpdate();

            if (res == null)
            {
//                TimeInterval.Stop();
                Console.Write("\r                                                             ");
                return;
            }
            Console.Write("\r                                                                              ");
            foreach (var item in res)
                Console.Write("\rProgress " + item.Key + " " + (item.Value*100).ToString("0.00")+"%");

        }

        public static void ErrorMessage(string message)
        {
            Console.WriteLine(message);
        }
        static void Main(string[] args)
        {
            bool errors = false;
            bool times = false;
            bool binary = false;
            bool progress = false;
            bool automaticProfiles=false;
            bool graphics = false;
            int resWidth = 0;
            int resHeight = 0;
            string graphFileName = "";
            string configFileName = "";
            string binaryFile = "";
            string testFile = "";
            Options opt = new Options();
            ClusterVis clusterOut = new ClusterVis();
            try
            {
                InternalProfilesManager.InitProfiles();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Some of the profiles are not available: ", ex.Message);
            }
            //Console.WriteLine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            /*foreach(var item in InternalProfilesManager.internalList)
            {
                Console.WriteLine("profile=" + item);
            }*/
            if (args.Length == 0)
            {
                Console.WriteLine("Following argument is required:");
                Console.WriteLine("-f configuration_file");
                
                Console.WriteLine("Following options may be specified");
                Console.WriteLine("-e \n\t show all errors");
                Console.WriteLine("-m \n\t set the input mode\n\tRNA or PROTEIN [default PROTEIN]");
                Console.WriteLine("-n \n\t number of cores to be used");
                Console.WriteLine("-t \n\tshow time information");
                Console.WriteLine("-a \n\tgenerate automatic profiles (can be used only when aligned profile is set in configuration file)");
                Console.WriteLine("-b fileName \n\tSave results to binary file ");
                Console.WriteLine("-l fileName profileTest\n\tTest profileTest file on object saved in fileName");
                Console.WriteLine("-sg fileName resWidthxresHeigth \n\tSave results to png file (if possible)");
                Console.WriteLine("-p \n\tShow progres bar");
                return;
            }
            Settings set = new Settings();
            set.Load();
            if (set.profilesDir == null || set.profilesDir.Length == 0)
            {
                set.profilesDir = "generatedProfiles";
            //    set.Save();
            }
            //set.mode = INPUTMODE.PROTEIN;
            set.mode = INPUTMODE.USER_DEFINED;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-f":
                        if(i+1>=args.Length)
                        {
                            Console.WriteLine("After -f option you have to provide configuration file");
                                return;
                        }
                        if (!File.Exists(args[i + 1]))
                        {
                            Console.WriteLine("File " + args[i + 1] + " does not exist");
                            return;
                        }
                        configFileName = args[i + 1];
                        i++;
                        break;
                    case "-b":
                        binary = true;
                        binaryFile = args[++i];                        
                        break;
                    case "-n":
                        if (args.Length > i)
                        {                                        
                            int num;
                            try
                            {
                                num = Convert.ToInt32(args[++i]);                                
                                set.numberOfCores = num;
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine("Wrong definition of number of cores: " + ex.Message);
                                return;
                            }
                        }
                        else
                            Console.WriteLine("Number of cores has been not provided");
                        break;
                    case "-m":
                        if (args.Length > i)
                        {
                            set.Load();
                            i++;
                            if (args[i] == "USER")
                                set.mode = INPUTMODE.USER_DEFINED;
                            else
                            {
                                Console.WriteLine("Incorrect mode:" + args[i]);
                                return;
                            }

                        }
                        else
                            Console.WriteLine("No mode specified");

                        break;
                    case "-e":
                        errors = true;
                        break;
                    case "-t":
                        times = true;
                        break;
                    case "-a":
                        automaticProfiles=true;
                        break;
                    case "-p":
                        progress = true;
                        break;
                    case "-sg":
                        graphics = true;                       
                        if(i+2>args.Length)
                        {
                            Console.WriteLine("Incorrect graphics format should be: fileName resWidthxresHeight, e.g. test.png 1000x500");
                            return;
                        }
                        graphFileName = args[++i];
                        string tmp = args[++i];
                        string[] x = tmp.Split('x');
                        if (x.Length == 2)
                        {
                            try
                            {
                                resWidth = Convert.ToInt32(x[0]);
                                resHeight = Convert.ToInt32(x[1]);
                            }
                            catch
                            {
                                Console.WriteLine("Incorrect resolution format, should be resWidthxresHeight, e.g. 1000x500");
                                return;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Incorrect resolution formt, should be resWidthxresHeight, e.g. 1000x500");
                            return;
                        }
                        break;
                    case "-l":
                        binaryFile = args[++i];
                        testFile = args[++i];
                        break;
                    default:
                        if(args[i].Contains("-"))
                            Console.WriteLine("Unknown option " + args[i]);
                        break;

                }
            }
            set.Save();
            if (configFileName.Length == 0)
            {
                Console.WriteLine("Configurarion file has been not provided!");
                return;
            }
            Dictionary<string, ClusterOutput> clOut = new Dictionary<string, ClusterOutput>();
            try
            {
                if (testFile.Length > 0)
                {
                    ISerialize test = GeneralFunctionality.LoadBinary(binaryFile);
                    ClusterOutput res = null;
                    res = test.outCl;
                    res.clusterType = test.ToString();
                    clOut.Add("test", res);
                    
                }
                else
                {
                    Random r = new Random();
                    Console.WriteLine("Configuration file " + configFileName);
                    opt.ReadOptionFile(configFileName);
                    opt.omics.processName = "Batch_" + r.Next(1000);
                    if (automaticProfiles)
                        opt.GenerateAutomaticProfiles(null);
                    if (binary)
                        opt.binaryFile = binaryFile;
                    manager.opt = opt;
                    manager.message = ErrorMessage;
                    if (progress)
                    {
                        TimeIntervalTerminal.InitTimer(UpdateProgress);
                        TimeIntervalTerminal.Start();
                    }
                    //manager.RunJob(opt.omics.processName,data);
                    manager.WaitAllNotFinished();
                    UpdateProgress(null, null);

                    if (progress)
                        TimeIntervalTerminal.Stop();
                    Console.Write("\r                                                                     ");
                    clOut = manager.clOutput;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception : " + ex.Message);
            }
            if (clOut.Count > 0)
            {
                foreach (var item in clOut.Keys)
                {
                    clusterOut.output = clOut[item];
                    string clustName = clOut[item].clusterType;
                    if(clustName!=null && clustName.Contains(":"))
                    {
                        clustName=clustName.Replace(':', '-');
                    }
                    clusterOut.output.SaveTxt(clustName + "_" + opt.outputFile);
                    if (graphics)
                    {
                        clusterOut.output.SaveGraph(graphFileName,resWidth,resHeight);                       
                    }
                }
            }
            if (times)
            {
                foreach (var item in clOut)
                    Console.WriteLine(item.Value.dirName + " " + item.Value.measure + " " + item.Value.time);
            }
            if (errors)
            {
                foreach (var item in ErrorBase.GetErrors())
                    Console.WriteLine(item);
            }
            Console.WriteLine();
        }
    }
}
