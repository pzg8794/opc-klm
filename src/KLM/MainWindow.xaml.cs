#region Library
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using Microsoft.SqlServer.Server;

using Leap;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using Kinect;
using LeapMotion;
using System.Threading.Tasks;
using System.Windows.Controls;
using KinectBackgroundR;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization.Formatters.Binary;
using KStreams;
using System.Collections;
using System.Data;
using System.ComponentModel;
#endregion 
namespace KinectLeap
{
    public partial class MainWindow
    {
        #region Class Members
        public Mode _mode = Mode.Default;
        public bool _displayBody = false;

        public MainWindow1 kinectd;
        public MainWindow2 leapMot;
        public String frames;
        public String path = "pack://application:,,,/Resources/icon.jpg";
        public List<string> data = new List<string>();
        public Object rSend = null;
        public static int modelC = -1;
        public Boolean pause = false, resume = false, quit = false;

        public static int stime = 6;
        public String dir = "";
        public String[] select = null;
        public Boolean pos = false;
        public String position = "Text";
        public string selection = "";
        public static int dNum = -1;

        public String[] kDataSets = { "Body_Parts", "Feelings", "Directions", "Weather" };
        public String[] lmDataSets = { "Alphabet", "Time", "Numbers" };
        public bool process = false;
        public TabItem selected;
        private string tpath = "";
        private string projectPath = "";
        private string dataPath = "";
        private string signsPath = "";
        private string sSectionPath = "";
        private string dSectionPath = "";
        private bool wrongDir;
        public int index = 0;
        static bool testb = false;

        //Create a Delegate that matches the Signature of the ProgressBar's SetValue method
        private delegate void UpdateProgressBarDelegate(System.Windows.DependencyProperty dp, Object value);

        #endregion
        public MainWindow()
        {
            getProjectPath();
            kinectd = new MainWindow1(this);
            leapMot = new MainWindow2(this);

            kinectd.DoWork();
            leapMot.DoWork();
            //saveAllFrames();
            #region comment

            #endregion
        }
        #region Datasets Paths
        private void getProjectPath()
        {

            projectPath = Directory.GetCurrentDirectory();
            string[] parts = projectPath.Split('\\');
            projectPath = "";
            for (int x = 0; x < (parts.Length - 3); x++)
            {
                projectPath += parts[x] + "\\";
               // Console.WriteLine(projectPath + " "+i++);

            }

            dataPath = projectPath + "\\Data\\";
            signsPath = projectPath + "\\Signs\\";
        }
        #endregion 
        #region Data Update
        /// <summary>
        /// Default CTOR
        /// </summary>
        private void start_Click(object sender, RoutedEventArgs e)//DISPLAYING FOR TRAINING
        {
            if (bpos.IsChecked.Value)
            {
                bpos.IsChecked = false;
                Position.Text = " ";
                Position1.Text = "";
            }

            if (modelC == -1)
            {
                Position.Text = "<== PLEASE, SELECT A MODEL!";
                Position1.Text = "";
                start.IsChecked = false;
            }
            else if ((modelC != -1) && selection == null)
            {
                Position.Text = "";
                Position1.Text = "SELECT A TRAINING SECTION!";
                start.IsChecked = false;
            }
            else
            {
                dataDisplay(false);
                saveAllFrames();
            }

        }
        public void UpdateKT()
        {
            if (tab1.IsSelected)
            {
                HappyResult.Text = kinectd.dataK[0]; EngagedResult.Text = kinectd.dataK[1];
                GlassesResult.Text = kinectd.dataK[2]; LeftEyeResult.Text = kinectd.dataK[3];
                RightEyeResult.Text = kinectd.dataK[4]; MouthOpenResult.Text = kinectd.dataK[5];
                MouthMovedResult.Text = kinectd.dataK[6]; LookingAwayResult.Text = kinectd.dataK[7];
            }

            if (tab2.IsSelected)
            {
                HappyResult1.Text = kinectd.dataK[0]; EngagedResult1.Text = kinectd.dataK[1];
                GlassesResult1.Text = kinectd.dataK[2]; LeftEyeResult1.Text = kinectd.dataK[3];
                RightEyeResult1.Text = kinectd.dataK[4]; MouthOpenResult1.Text = kinectd.dataK[5];
                MouthMovedResult1.Text = kinectd.dataK[6]; LookingAwayResult1.Text = kinectd.dataK[7];
            }
        }

        public void UpdateLM()
        {
            if (tab1.IsSelected)
            {
                HandsResult.Text = leapMot.dataLM[0]; FingersResult.Text = leapMot.dataLM[1];
                FrameIDResult.Text = leapMot.dataLM[2]; LeftHandResult.Text = leapMot.dataLM[3];
                RightHandResult.Text = leapMot.dataLM[4]; PalmPositionResult.Text = leapMot.dataLM[5];
                ArmDirectionResult.Text = leapMot.dataLM[6]; TimeStampResult.Text = leapMot.dataLM[7];
            }

            if (tab3.IsSelected)
            {
                HandsResult1.Text = leapMot.dataLM[0]; FingersResult1.Text = leapMot.dataLM[1];
                FrameIDResult1.Text = leapMot.dataLM[2]; LeftHandResult1.Text = leapMot.dataLM[3];
                RightHandResult1.Text = leapMot.dataLM[4]; PalmPositionResult1.Text = leapMot.dataLM[5];
                ArmDirectionResult1.Text = leapMot.dataLM[6]; TimeStampResult1.Text = leapMot.dataLM[7];
            }

            if (pos)
                this.Position1.Text = leapMot.dataLM[8];
        }

        internal void updateKT2(String frames, string bodyS)
        {
            if (frames.Length != 0)
            {
                this.frames = frames;
                //Console.WriteLine(this.frames);
            }
            bupdate.Text = bodyS;
        }

        //SELECTING DATABASE ===========================================================================
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((modelC != -1) && selection == null)
            {
                Position.Text = "";
                Position1.Text = "SELECT A TRAINING SECTION";
                start.IsChecked = false;
            }
            var list = new Dictionary<string, String[]>();

            dir = signsPath;

            var comboBox = sender as ComboBox;// ... Get the ComboBox.
            selection = comboBox.SelectedItem as string;// ... Set SelectedItem as Window Title.
            if (selection != null && !selection.Equals("Select A Model") && !selection.Equals("SELECT A SECTION"))
            {
                Position.Text = "";
                Position1.Text = "";
                list[selection] = Directory.GetFiles(signsPath + selection, "*.png")
                         .Select(path => System.IO.Path.GetFileNameWithoutExtension(path))
                         .ToArray();

                if (selection.Equals("Time"))
                {
                    list[selection] = Directory.GetFiles(signsPath + selection, "*.png").OrderBy(d => new FileInfo(d).
                        LastWriteTime).Select(path => System.IO.Path.GetFileNameWithoutExtension(path)).ToArray();

                }
                wrongDir = false;

                dir = "pack://application:,,,/Signs/" + selection + "/";
                sSectionPath = signsPath + "\\" + selection + "\\";
                dSectionPath = dataPath + "\\" + selection + "\\";
                select = list[selection];
            }
            else
            {
                dir = "pack://application:,,,/Resources/";
                String[] temp = { "icon" };
                select = temp;
                wrongDir = true;
            }
        }
        //SELECTING DATABASE ===============================================================================

        public Boolean updateScreen()
        {
            if ((modelC != -1) && (selection.Equals("Select A Model") || selection.Equals("SELECT A SECTION") || selection == null))
            {
                Position.Text = "";
                Position1.Text = "SELECT A TRAINING SECTION";
                start.IsChecked = false;
                return false;
            }
            else if ((modelC == -1))
            {
                Position.Text = "<== PLEASE, SELECT A MODEL";
                Position1.Text = "";
                start.IsChecked = false;
                return false;
            }
            else
            {
                Position.Text = "";
                Position1.Text = "";
                return true;
            }
        }
        private void Position_Click(object sender, RoutedEventArgs e)
        {
            pos = !pos;
            _mode = Mode.Position;

            if (!pos)
            {
                updateScreen();
            }
            else
            {
                this.Position.Text = "KINECT POSITION STATUS";
                this.Position1.Text = "LEAP-MOTION POSITION STATUS";
                //Console.WriteLine("Text");
            }
        }

        private void ProgressBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!PAUSE.IsChecked.Value)
                this.time.Text = e.NewValue + "";
        }

        private void waterMarkTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int stime2 = 0;
            this.Position1.Text = "";
            this.Position.Text = "";
            this.text.Text = "ASL TO TEXT";

            bool canConvert = int.TryParse(waterMarkTextBox.Text, out stime2);
            if (canConvert)
            {
                if (stime2 > stime)
                {
                    this.text.Text = "THE TIME PER SIGN IS NOW " + waterMarkTextBox.Text;
                    stime = stime2;
                    time.Text = waterMarkTextBox.Text;
                }
                else
                {
                    if (10 == stime2)
                    {
                        this.Position1.Text = "THE DEFAULT TIME PER SIGN IS 10 \nNO CHANGES MADE ";
                        stime = stime2;
                        time.Text = waterMarkTextBox.Text;
                    }
                    else
                        this.Position1.Text = "THE MINIMUM TIME PER SIGN IS 10 \nYOU ENTERED " + waterMarkTextBox.Text;
                }
            }
            else
            {
                if (!waterMarkTextBox.Text.Equals("CHANGE TIME TO SIGN") && !waterMarkTextBox.Text.Equals(""))
                {
                    this.Position1.Text = "INVALID ENTRY, TO CHANGE DEFAULT TIME ENTER A NUMBER GREATER THAN 10";
                }
            }

            if (stime2 >= 10)
                updateScreen();
        }

        public void matching()
        {
            var length = select.Count();

            for (int i = 0; i < length; i++)
            {
                //creSetDir(i);
                this.Process(i);
            }
        }

        private void Process()
        {
            //Configure the ProgressBar
            ProgressBar1.Minimum = 0;
            ProgressBar1.Maximum = stime;
            ProgressBar1.Value = 0;

            double value = stime;//Stores the value of the ProgressBar
            if (resting)
                value = 5;

            //Create a new instance of our ProgressBar Delegate that points
            //  to the ProgressBar's SetValue method.
            UpdateProgressBarDelegate updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar1.SetValue);

            do //Tight Loop:  Loop until the ProgressBar.Value reaches the max
            {
                for (int x = 0; x < 100000000; x++) { }
                Dispatcher.Invoke(updatePbDelegate,
                System.Windows.Threading.DispatcherPriority.Background,
                new object[] { ProgressBar.ValueProperty, value-- });

                    saveImages(index, 0, value);
                    //Console.WriteLine("I AM SAVING!");

                index++;
            }
            while (ProgressBar1.Value != ProgressBar1.Minimum);
            index = 0;
        }
        public void dataDisplay(bool testb)
        {
            Position.Text = "";
            Position1.Text = "";
            Itrain.Visibility = Visibility.Visible;

            if (wrongDir) //No Model and, or, dataset was chosen
            {
                dir = "pack://application:,,,/Resources/";
                String[] temp = { "icon" };
                select = temp;

                this.text.Text = select[0];
                path = dir + select[0] + ".png";
                var uri = new Uri(path);
                var bitmap = new BitmapImage(uri);
                Itrain.Source = bitmap;
            }
            else
            {
                var length = select.Count();

                path = dir + select[0] + ".png";
                var uri1 = new Uri(path);
                var bitmap1 = new BitmapImage(uri1);
                Itrain2.Source = bitmap1;

                resting = true;
                Itrain.Visibility = Visibility.Hidden;
                Itrain2.Visibility = Visibility.Visible;
                text.Text = "";
                Position.Text = "First Sign Is";
                Position1.Text = select[0];
                Process(0);
                resting = false;

                for (int i = 0; i < length; i++)
                {
                    //-------------------------------------------------
                    //pause, reset and quit functions
                    if (PAUSE.IsChecked.Value)
                    {
                        i--;
                        if (i < 0)
                            i = 0;
                    }

                    if (RESET.IsChecked.Value)
                    {
                        reset();
                        RESET.IsChecked = false;
                        break;
                    }

                    if (QUIT.IsChecked.Value)
                    {
                        index = 0;
                        break;
                    }
                    //----------------------------------------------

                    if ((i + 1) < length)
                    {
                        var index = i + 1;

                        path = dir + select[index] + ".png";
                        var uri2 = new Uri(path);
                        var bitmap2 = new BitmapImage(uri2);
                        Itrain2.Source = bitmap2;
                    }

                    //updating screen
                    Position.Text = "";
                    Position1.Text = "";
                    Itrain.Visibility = Visibility.Visible;
                    Itrain2.Visibility = Visibility.Visible;

                    this.text.Text = select[i];
                    path = dir + select[i] + ".png";
                    var uri = new Uri(path);
                    var bitmap = new BitmapImage(uri);
                    Itrain.Source = bitmap;

                    //Creating or setting directories
                    if (!PAUSE.IsChecked.Value)
                        creSetDir(i);

                    this.Process(i);

                    if (((i + 1) < length) && !PAUSE.IsChecked.Value && !RESET.IsChecked.Value && !QUIT.IsChecked.Value)
                    {
                        resting = true;
                        Itrain.Visibility = Visibility.Hidden;
                        text.Text = "";
                        Position.Text = "CHANGE NOW TO NEXT SIGN";
                        Position1.Text = "NEXT SIGN IS " + select[i + 1];
                        Process(i);
                        resting = false;
                    }
                }
                Console.WriteLine("Empty Files");
                Console.WriteLine(KTef + " " + LMef);
                //saveAllFrames();
            }

            //cleaning
            Itrain.Visibility = Visibility.Hidden;
            if (!test.IsChecked.Value)
                text.Text = "";
            else
                text.Text = "TIMING TEST IS ON";

            //updating screen
            updateScreen();

            //quit function 
            if (QUIT.IsChecked.Value)
            {
                index = 0;
                QUIT.IsChecked = false;
                QUIT.Content = "QUIT";
                text.Text = "ASL-TO-TEXT";
            }
        }

        static bool resting = false;
        private void reset()
        {
            test.IsChecked = false;
            start.IsChecked = false;
            QUIT.IsChecked = false;
            PAUSE.IsChecked = false;
            testb = !testb;
            kinect.IsChecked = false;
            LM.IsChecked = false;
            KLM.IsChecked = false;
        }
        private void Process(int i)
        {
            //Configure the ProgressBar
            ProgressBar1.Minimum = 0;
            ProgressBar1.Maximum = stime;
            ProgressBar1.Value = 0;

            double value = stime;//Stores the value of the ProgressBar
            if (resting)
                value = 5;

            //Create a new instance of our ProgressBar Delegate that points
            //  to the ProgressBar's SetValue method.
            UpdateProgressBarDelegate updatePbDelegate = new UpdateProgressBarDelegate(ProgressBar1.SetValue);

            do //Tight Loop:  Loop until the ProgressBar.Value reaches the max
            {
                for (int x = 0; x < 100000000; x++) { }
                Dispatcher.Invoke(updatePbDelegate,
                System.Windows.Threading.DispatcherPriority.Background,
                new object[] { ProgressBar.ValueProperty, value-- });

                if (!resting && !test.IsChecked.Value && !QUIT.IsChecked.Value && !PAUSE.IsChecked.Value && !RESET.IsChecked.Value)
                {
                    saveImages(index, i, value);
                    //Console.WriteLine("I AM SAVING!");
                }
                index++;
            }
            while (ProgressBar1.Value != ProgressBar1.Minimum);
            index = 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Position.Text = "";
            Position1.Text = "";
            testb = !testb;
            //Itrain.Visibility = Visibility.Visible;
            if (updateScreen())
            {
                if (testb)
                {
                    text.Text = "TIMING TEST IS ON";
                    dataDisplay(true);
                }
                else
                {
                    text.Text = "TIMING TEST IS OFF";
                    test.IsChecked = false;
                }
            }
            else
            {
                Position.Text = "TRY AGAIN, MAKE SURE";
                Position1.Text = "YOU SELECT A MODEL AND A TRAINING SECTION";
                text.Text = "TIMING TEST IS OFF";

                test.IsChecked = false;
                testb = false;
                return;
            }

        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (PAUSE.IsChecked.Value)
            {
                if (!start.IsChecked.Value && !test.IsChecked.Value)
                {
                    PAUSE.IsChecked = false;
                    return;
                }
                PAUSE.Content = "RESUME";
            }
            else
            {
                PAUSE.Content = "PAUSE";
            }


            if (RESET.IsChecked.Value)
            {

                if (!start.IsChecked.Value && !test.IsChecked.Value)
                {
                    RESET.IsChecked = false;
                    return;
                }

                Itrain.Visibility = Visibility.Hidden;
                Itrain2.Visibility = Visibility.Hidden;
                time.Text = "6";
                stime = 6;
                text.Text = "ASL-TO-TEXT";
                Position.Text = "";
                Position1.Text = "";

                waterMarkTextBox.Text = "";
                sectionList.SelectedIndex = 0;
                selection = null;

                RESET.Content = "RESET";
                QUIT.Content = "QUIT";
                PAUSE.Content = "PAUSE";
                modelC = -1;

            }

            if (QUIT.IsChecked.Value)
            {
                if (!start.IsChecked.Value && !test.IsChecked.Value)
                {
                    QUIT.IsChecked = false;
                    return;
                }

                Itrain2.Visibility = Visibility.Hidden;
                testb = false;
                start.IsChecked = false;
                test.IsChecked = false;
                time.Text = stime.ToString();
                PAUSE.Content = "PAUSE";
                PAUSE.IsChecked = false;
                QUIT.Content = "CANCEL";
            }
            else
            {
                QUIT.Content = "QUIT";
            }
        }
        private void Time_TextChanged(object sender, TextChangedEventArgs args)
        {
            args.Source = "";
        }
        private void Color_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Color;
        }

        private void Depth_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Depth;
        }

        private void Infrared_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Infrared;
        }

        private void Default_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Default;
        }
        private void Body_Click(object sender, RoutedEventArgs e)
        {
            _displayBody = !_displayBody;
        }

        public Mode getMode
        {
            get { return _mode; }
        }

        public Boolean getBodyC
        {
            get { return _displayBody; }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }
        #endregion
        #region Data Loading
        //LOADING DATABASE ==================================================================
        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (data.Count == 0)             // ... A List.
            {
                data.Add("Select A Model");
                updateScreen();
            }
            rSend = sender;
            loadList(rSend, data);
        }    //For the Models
        private void loadList(object sender, List<string> data)
        {
            var comboBox = sender as ComboBox;// ... Get the ComboBox reference.
            comboBox.ItemsSource = data;      // ... Assign the ItemsSource to the List.
            comboBox.SelectedIndex = 0;       // ... Make the first item selected
        }         //For the Datasets
        private void kinect_Checked(object sender, RoutedEventArgs e)
        {
            modelC = 0;
            data = new List<string>();

            data.Add("SELECT A SECTION");

            for (int i = 0; i < kDataSets.Length; i++)
                data.Add(kDataSets[i]);

            loadList(rSend, data);
        }   //For Kinect Model
        private void LM_Checked(object sender, RoutedEventArgs e)
        {
            modelC = 1;
            data = new List<string>();
            data.Add("SELECT A SECTION");

            for (int i = 0; i < lmDataSets.Length; i++)
                data.Add(lmDataSets[i]);

            loadList(rSend, data);
        }  //For Leap Motion Model
        private void KLM_Checked(object sender, RoutedEventArgs e)
        {
            modelC = 2;
            data = new List<string>();
            data.Add("SELECT A SECTION");

            for (int i = 0; i < kDataSets.Length; i++)
                data.Add(kDataSets[i]);

            for (int i = 0; i < lmDataSets.Length; i++)
                data.Add(lmDataSets[i]);

            loadList(rSend, data);
        }   //For Kinect+LM model
        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            // ... Create a new BitmapImage.
            BitmapImage b = new BitmapImage();
            b.BeginInit();
            b.UriSource = new Uri("pack://application:,,,/Resources/icon.png");
            b.EndInit();

            // ... Get Image reference from sender.
            var image = sender as System.Windows.Controls.Image;
            // ... Assign Source.
            img.Source = b;
        }//Load Images to Screen
        #endregion
        #region Saving Frames and Images

        private void creSetDir(int i)
        {
            if (!test.IsChecked.Value)
            {
                tpath = dSectionPath + "\\" + select[i];
                var tpd1 = System.IO.Directory.CreateDirectory(tpath);
                var dc = System.IO.Directory.GetDirectories(tpath).Length + 1;
                tpath = dSectionPath + "\\" + select[i] + "\\" + (select[i] + dc);
                var tpd2 = System.IO.Directory.CreateDirectory(tpath);
            }
        }
        public void saveframe(string file, string filename)
        {
            try
            {
                //Pass the filepath and filename to the StreamWriter Constructor
                StreamWriter sw = new StreamWriter(tpath + "\\" + filename);
                string[] lines = file.Split("\n".ToCharArray());

                foreach (string line in lines)
                {
                    sw.WriteLine(line);
                }

                //Close the file
                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
            }
        }
        private void saveImages(int index, int i, double value)
        {
            //Saving Images
            if (value < stime)
            {
                //Leap Motion Image
                SaveToPng(LMCamera1, "image" + (index) + ".png");

                if (leapMot != null && fileLM.Length != 0)
                {
                    saveframe(fileLM, select[i] + index + "LM.txt");
                    LMFiles.AddLast(fileLM);
                }
                else
                {
                    LMEF.Text = ++LMef + "";
                    Console.WriteLine(select[i] + index + "LM.txt");
                    //saveframe(fileLM, "LMEF" + index + "LM.txt");
                }
                //Kinect Depth and Infrared
                SaveToPng(CameraImage2, "imageD" + (index) + ".png");

                if (this.kinectd != null && kinectd.getFile().Length != 0)
                {
                    saveframe(kinectd.getFile(), select[i] + (index) + "K.txt");
                    KTFiles.AddLast(kinectd.getFile());
                }
                else
                {
                    KTEF.Text = ++KTef + "";
                    Console.WriteLine(select[i] + (index) + "K.txt");
                    //saveframe(kinectd.getFile(), "KEF" + (index) + ".txt");
                }
            }
        }

        private void saveAllFrames()
        {
            String[] dirs = System.IO.Directory.GetDirectories(dSectionPath);  //Directories in the selected section directory of signs       
            string[] vectors = new string[19];
            foreach (string f in dirs) //Sign Directory
            {//Console.WriteLine(f);
                String[] ls = System.IO.Directory.GetDirectories(f); //Sign latest sub-directtory
                //foreach (string fl in ls)
                //{//Console.WriteLine(fl);

                //var tp = System.IO.Directory.GetDirectories(fl);
                var df = ls[ls.Length - 1];
                kfeaturesV = new Dictionary<string, string>();
                lmfeaturesV = new Dictionary<string, string>();
                var files = System.IO.Directory.GetFiles(df);

                //Console.WriteLine(df);
                foreach (String fi in files) // files in the sub-directory
                {
                    if (fi.Contains("K.txt"))//Console.WriteLine(fi);
                        kfeaturesV = featureMetrix(kfeaturesV, fi);

                    if (fi.Contains("LM.txt"))
                    {
                        //Console.WriteLine(fi);
                        lmfeaturesV = featureMetrix(lmfeaturesV, fi);
                    }
                }
                saveFeatures(kfeaturesV, df);
                saveFeatures(lmfeaturesV, df);
                //}
            }
        }
        private Dictionary<string, string> featureMetrix(Dictionary<string, string> featuresV, string fi)
        {
            //Console.WriteLine(fi);
            var lines = File.ReadLines(fi);
            foreach (string line in lines.ToArray())
            {
                if (line != "")
                {
                    var nam = line.Split(",".ToCharArray())[0];
                    if (!featuresV.ContainsKey(nam))
                        featuresV[nam] = line + "\n";
                    else
                        featuresV[line.Split(",".ToCharArray())[0]] += line + "\n";
                    //Console.WriteLine(line);
                }
            }
            return featuresV;
        }
        private void saveFeatures(Dictionary<string, string> featuresV, string df)
        {
            StreamWriter sw = null;
            try
            {
                //Pass the filepath and filename to the StreamWriter Constructor
                var dir = System.IO.Directory.CreateDirectory(df + "\\Metrices\\");
                foreach (var vect in featuresV)
                {
                    sw = new StreamWriter(df + "\\Metrices\\" + vect.Key + ".txt");
                    foreach (string line in vect.Value.Split("\n".ToCharArray()))
                    {
                        //Console.WriteLine(line);
                        sw.WriteLine(line);
                    }
                    //Close the file
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
            }
        }

        static int LMef = 0;
        static int KTef = 0;
        static string fileLM = "";

        // MAYBE - WRITTING THE FILES ONCE INSTEAD OF MULTIPLE TIMES WOULD MAKE THE SYSTEM FASTER
        public LinkedList<String> LMFiles = new LinkedList<String>();
        public LinkedList<String> KTFiles = new LinkedList<String>();
        Dictionary<string, string> kfeaturesV;
        Dictionary<string, string> lmfeaturesV;


        public void setLMF(string file, string handsS)
        {
            fileLM = file;
            hupdate.Text = handsS;
        }

        public void SaveToBmp(FrameworkElement visual, string fileName)
        {
            var encoder = new BmpBitmapEncoder();
            SaveUsingEncoder(visual, fileName, encoder);
        }

        public void SaveToPng(FrameworkElement visual, string fileName)
        {
            var encoder = new PngBitmapEncoder();
            SaveUsingEncoder(visual, fileName, encoder);
        }

        public void SaveUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            string dir = Environment.CurrentDirectory;

            if (this.tpath.Length != 0)
                Directory.SetCurrentDirectory(@"" + tpath);

            RenderTargetBitmap bitmap = new RenderTargetBitmap(300, 400, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            //var cb = new CroppedBitmap(bitmap, new Int32Rect(24, 178, 1508, 845));

            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
                stream.Dispose();
            }
            Directory.SetCurrentDirectory(dir);
            bitmap.Clear();
        }
        #endregion
        #region Test
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void K_Tab(object sender, RoutedEventArgs e)
        {
        }

        private void KLM_Tab(object sender, RoutedEventArgs e)
        {
        }
        private void ASL_Tab(object sender, RoutedEventArgs e)
        {
        }

        private void LM_Tab(object sender, RoutedEventArgs e)
        {
        }

        private void Train_Tab(object sender, RoutedEventArgs e)
        {
        }

        private void testKLM()
        {
            tpath = dataPath;
            String[] dirs = System.IO.Directory.GetDirectories(tpath);

            foreach (string fi in dirs)
            {
                Console.WriteLine(fi);
                //testLM(fi, false);
                testKT(fi, false);
            }
        }

        private void testLM(String dirs, bool del)
        {
            LinkedList<String> tp = new LinkedList<String>();
            LinkedList<String> tf = new LinkedList<String>();

            //Console.WriteLine(dirs[0]);
            //System.IO.Directory.CreateDirectory(tpath);
            String[] list = System.IO.Directory.GetDirectories(dirs);
            int v = -1;
            foreach (string f in list)
            {
                //Console.WriteLine(f);
                v++;
                String[] ls = System.IO.Directory.GetDirectories(f);
                int i = -1;
                foreach (string fl in ls)
                {
                    //Console.WriteLine(fl);
                    i++;

                    var files = System.IO.Directory.GetFiles(fl);
                    foreach (String fi in files)
                    {
                        if (fi.Contains("LM"))
                        {
                            // Console.WriteLine(fi);
                            Controller controller = new Controller();
                            Leap.Frame rframe = controller.Frame();
                            byte[] frameData = System.IO.File.ReadAllBytes(fi);
                            rframe.Deserialize(frameData);
                            //Console.WriteLine(fi);
                            //leapMot.test(rframe);

                            Leap.HandList hands = rframe.Hands;
                            //Console.WriteLine("h " + hands.Count);

                            if (hands.Count != 0)
                            {
                                tp.AddLast(fi);
                            }
                            else
                            {
                                tf.AddLast(fi);

                                if (del)
                                {
                                    Console.WriteLine("Deleting " + fi);
                                    File.Delete(fi);
                                }

                            }

                            for (int h = 0; h < rframe.Hands.Count; h++)
                            {
                                Hand leapHand = rframe.Hands[h];

                                Leap.Vector handXBasis = leapHand.PalmNormal.Cross(leapHand.Direction).Normalized;
                                Leap.Vector handYBasis = -leapHand.PalmNormal;
                                Leap.Vector handZBasis = -leapHand.Direction;
                                Leap.Vector handOrigin = leapHand.PalmPosition;
                                Leap.Vector velocity = leapHand.PalmVelocity;

                                var a = leapHand.PalmPosition.Magnitude;
                                var b = leapHand.PalmPosition.MagnitudeSquared;
                                var c = leapHand.PalmPosition.Normalized;
                                var d = leapHand.PalmPosition.Pitch;
                                var e = leapHand.PalmPosition.Roll;
                                var g = leapHand.PalmPosition.Yaw; ;

                                var a1 = leapHand.WristPosition.Magnitude;
                                var b1 = leapHand.WristPosition.MagnitudeSquared;
                                var c1 = leapHand.WristPosition.Normalized;
                                var d1 = leapHand.WristPosition.Pitch;
                                var e1 = leapHand.WristPosition.Roll;
                                var f1 = leapHand.WristPosition.Yaw; ;

                                var a2 = leapHand.Direction.Magnitude;
                                var b2 = leapHand.Direction.MagnitudeSquared;
                                var c2 = leapHand.Direction.Normalized;
                                var d2 = leapHand.Direction.Pitch;
                                var e2 = leapHand.Direction.Roll;
                                var f2 = leapHand.Direction.Yaw; ;

                                var a3 = leapHand.Arm.Direction.Magnitude;
                                var b3 = leapHand.Arm.Direction.MagnitudeSquared;
                                var c3 = leapHand.Arm.Direction.Normalized;
                                var d3 = leapHand.Arm.Direction.Pitch;
                                var e3 = leapHand.Arm.Direction.Roll;
                                var f3 = leapHand.Arm.Direction.Yaw; ;

                                var a4 = leapHand.Arm.ElbowPosition.Magnitude;
                                var b4 = leapHand.Arm.ElbowPosition.MagnitudeSquared;
                                var c4 = leapHand.Arm.ElbowPosition.Normalized;
                                var d4 = leapHand.Arm.ElbowPosition.Pitch;
                                var e4 = leapHand.Arm.ElbowPosition.Roll;
                                var f4 = leapHand.Arm.ElbowPosition.Yaw; ;
                                //Console.WriteLine("x " + handXBasis);
                                //Console.WriteLine("y " + handYBasis);
                                //Console.WriteLine("z " + handZBasis);
                                //Console.WriteLine("o " + handOrigin);

                                Leap.Matrix handTransform = new Leap.Matrix(handXBasis, handYBasis, handZBasis, handOrigin);
                                handTransform = handTransform.RigidInverse();
                                //Console.WriteLine("Matrix " + handTransform);

                                for (int fs = 0; fs < leapHand.Fingers.Count; fs++)
                                {
                                    Finger leapFinger = leapHand.Fingers[fs];
                                    Leap.Vector transformedPosition = handTransform.TransformPoint(leapFinger.TipPosition);
                                    Leap.Vector transformedDirection = handTransform.TransformDirection(leapFinger.Direction);
                                    Leap.Vector transformedDirectionVelocity = handTransform.TransformDirection(leapFinger.TipVelocity);
                                    var a5 = leapFinger.Direction.Magnitude;
                                    var b5 = leapFinger.Direction.MagnitudeSquared;
                                    var c5 = leapFinger.Direction.Normalized;
                                    var d5 = leapFinger.Direction.Pitch;
                                    var e5 = leapFinger.Direction.Roll;
                                    var f5 = leapFinger.Direction.Yaw;
                                    // Do something with the transformed fingers

                                    //Console.WriteLine(leapFinger.TipPosition);
                                    //Console.WriteLine(leapFinger.TipVelocity);
                                    //Console.WriteLine(leapFinger.StabilizedTipPosition);
                                    //Console.WriteLine(leapFinger.Id);
                                    //Console.WriteLine(leapFinger.Direction);
                                    //Console.WriteLine(leapFinger.Type());
                                }
                                //tl.AddLast(test);
                            }

                            Hand leftmost = hands.Leftmost;
                            Hand rightmost = hands.Rightmost;
                            Hand frontmost = hands.Frontmost;

                            Leap.Vector lhposition = leftmost.PalmPosition;
                            Leap.Vector lhvelocity = leftmost.PalmVelocity;
                            Leap.Vector lhdirection = leftmost.Direction;

                            Leap.Vector rhposition = rightmost.PalmPosition;
                            Leap.Vector rhvelocity = rightmost.PalmVelocity;
                            Leap.Vector rhdirection = rightmost.Direction;

                            // hand is a Hand object
                            PointableList lhpointables = leftmost.Pointables;
                            FingerList fingers = leftmost.Fingers;
                        }
                        else
                            continue;

                    }
                }
            }

            foreach (String fi in tf)
            {
                Console.WriteLine(fi);
            }
            Console.WriteLine(tp.Count + " Files Good");
            Console.WriteLine(tf.Count + " Files Wrong");
        }

        private void testKT(String dirs, bool del)
        {
            LinkedList<String> tp = new LinkedList<String>();
            LinkedList<String> tf = new LinkedList<String>();

            //Console.WriteLine(dirs[0]);
            //System.IO.Directory.CreateDirectory(tpath);
            String[] list = System.IO.Directory.GetDirectories(dirs);
            int v = -1;
            foreach (string f in list)
            {
                //Console.WriteLine(f);
                v++;
                String[] ls = System.IO.Directory.GetDirectories(f);
                int i = -1;
                foreach (string fl in ls)
                {
                    //Console.WriteLine(fl);
                    i++;

                    var files = System.IO.Directory.GetFiles(fl);
                    foreach (String fi in files)
                    {
                        if (fi.Contains("K.txt"))
                        {
                            // Console.WriteLine(fi);
                            var lineCount = File.ReadLines(fi).Count();
                            //Console.WriteLine("h " + hands.Count);

                            var lines = File.ReadLines(fi);
                            int counter = 0;
                            foreach (string line in lines.ToArray())
                            {
                                if (line == "")
                                    counter++;
                                //Console.WriteLine(line);
                            }

                            if ((lineCount < 124 && counter == 4) || (lineCount < 116 && counter == 3) || (lineCount < 108 && counter == 2))
                            {
                                tf.AddLast(fi);

                                if (del)
                                {
                                    Console.WriteLine("Deleting " + fi);
                                    File.Delete(fi);
                                }

                            }
                            else
                                tp.AddLast(fi);

                        }
                        else
                            continue;
                    }
                }
            }

            foreach (String fi in tf)
            {
                Console.WriteLine(fi);
            }
            Console.WriteLine(tp.Count + " Files Good");
            Console.WriteLine(tf.Count + " Files Wrong");
        }

        private void dRTraining_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveTraining(object sender, RoutedEventArgs e)
        {
            if(!updateScreen())
            {
                Position.Text = "NEED TO SELECT A MODEL AND A SECTION";
                Position1.Text = "TO DELETE CURRENT TRAINED DATA";
                return;
            }

            String[] list = System.IO.Directory.GetDirectories(dSectionPath);
            foreach (string fl in list)
            {
                String[] ls = System.IO.Directory.GetDirectories(fl);
                Directory.Delete(ls[ls.Length-1], true);
            }
        }

      #endregion 
        # region KLM Gestures


        # endregion 
    }
    #region Camera Modes
    public enum Mode
    {
        Color,
        Depth,
        Infrared,
        Default,
        Position
    }
    #endregion 
}