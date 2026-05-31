#region Library
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using Leap;
using KinectLeap;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using KinectBackgroundR;
using KStreams;
using K4W.Expressions;
using Coding4Fun.Kinect.Wpf;
using System.ComponentModel;
using LightBuzz.Vitruvius;
#endregion 
namespace Kinect
{        
    public partial class MainWindow1 : Window
    {
        #region Class Members
        MainWindow update; 
        public MultiSourceFrameReader _reader;
        static string file = "";
        static BackgroundWorker _bw;
        static int time = 0;

        KinectSensor _sensor;
        //IEnumerable<Body> _bodies;
        GestureController _gestureController;

        // 1) Create a background removal tool.
        BackgroundRemoval _backgroundRemoval;

        /// <summary>
        /// Instance of Kinect sensor
        /// </summary>
        private KinectSensor _kinect;

        /// <summary>
        /// Body reader
        /// </summary>
        public BodyFrameReader _bodyReader;

        /// <summary>
        /// Collection of all tracked bodies
        /// </summary>
        public Body[] _bodies;

        /// <summary>
        /// Requested face features
        /// </summary>
        private const FaceFrameFeatures _faceFrameFeatures = FaceFrameFeatures.BoundingBoxInInfraredSpace
                                                            | FaceFrameFeatures.PointsInInfraredSpace
                                                            | FaceFrameFeatures.MouthMoved
                                                            | FaceFrameFeatures.MouthOpen
                                                            | FaceFrameFeatures.LeftEyeClosed
                                                            | FaceFrameFeatures.RightEyeClosed
                                                            | FaceFrameFeatures.LookingAway
                                                            | FaceFrameFeatures.Happy
                                                            | FaceFrameFeatures.FaceEngagement
                                                            | FaceFrameFeatures.Glasses;

        /// <summary>
        /// Face Source
        /// </summary>
        private FaceFrameSource _faceSource;

        /// <summary>
        /// Face Reader
        /// </summary>
        private FaceFrameReader _faceReader;
        public string[] kdata = new string[8];

        public delegate void NextPrimeDelegate();


        /// <summary>
        /// Size fo the RGB pixel in bitmap
        /// </summary>
        private readonly int _bytePerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        public string position;
        public string bodystate = "NO STATE";
        //private KinectSkeltonTracker.MainViewModel mainViewModel;

        #endregion
        #region Constructors
        /// <summary>
        /// Default CTOR
        /// </summary>
        public MainWindow1(MainWindow thread)
        {
            update = thread;
        }

        public void DoWork()
        {
            update.InitializeComponent();
            // Initialize Kinect
            InitializeKinect();
        }

        public string[] dataK    // the Name property
        {
            get {return kdata;}
        }
        #endregion constructors

        #region CAMERA
        public void InitializeCamera()
        {
            if (_kinect == null) return;

            //Console.WriteLine("INITIALIZE");
            // 2) Initialize the background removal tool.
            _backgroundRemoval = new BackgroundRemoval(_kinect.CoordinateMapper);
           // _gestureController = new GestureController();
            //Calling Background removal tool and centering body
            _reader     = _kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);
            _bodyReader = _kinect.BodyFrameSource.OpenReader();// Body Reader
            _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            //_gestureController.GestureRecognized += GestureController_GestureRecognized;
            //}
        }
        public string getFile(){
            return file;
        }
        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
              //Console.WriteLine(update.TabIndex);
            var reference = e.FrameReference.AcquireFrame();
            time = Convert.ToInt32(update.time.Text);
  
            file  = "";
            if(update.tab4.IsSelected)
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += delegate(object s, DoWorkEventArgs args)
                {
                    using (var frame = reference.BodyFrameReference.AcquireFrame())
                    {
                        if (frame != null)
                        {
                            _bodies = new Body[frame.BodyFrameSource.BodyCount];
                            frame.GetAndRefreshBodyData(_bodies);

                            foreach (var body in _bodies)
                            {
                                if (body != null)
                                {
                                    if (body.IsTracked)
                                    {
                                        int i = 0;
                                        var temp = body.Joints.ToArray();//13,14,15,17,18,19,
                                        foreach (var jt in temp)
                                        {
                                            if (i != 13 && i != 14 && i != 15 && i != 17 && i != 18 && i != 19 && i!=12 && i!=0)
                                            {
                                                file += String.Format("{0,-22} , {1,-12} , {2, 12} , {3,12}", jt.Value.JointType, 
                                                    jt.Value.Position.X, jt.Value.Position.Y, jt.Value.Position.Z + "\n");
                                            }
                                            i++;
                                        }
                                    }
                                }
                            }

                            //bodystate = args.Result.ToString();
                        }
                    }
                };

                // RunWorkerCompleted will fire on the UI thread when the background process is complete
                worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
                {
                    //update.bupdate.Text = (string)args.Result;
                    if(file.Length!=0)
                    {
                        bodystate = "Body Tracked";
                        if (update.hupdate.Text.Equals("Hands Tracked"))
                            update.bhupdate.Text = "KLM";
                        else
                            update.bhupdate.Text = "KT";
                    }
                    else
                    {
                        bodystate = "Body Not Tracked";
                        if (update.hupdate.Text.Equals("Hands Tracked"))
                            update.bhupdate.Text = "LM";
                        else
                            update.bhupdate.Text = "NO MODE";
                    }
                        
       
                };

                worker.RunWorkerAsync();
            }

            if (!update.tab3.IsSelected && !update.tab4.IsSelected)
            {
                //Color
                using (var frame = reference.ColorFrameReference.AcquireFrame())
                {
                    if (frame != null)
                    {
                        if (update.getMode == Mode.Color)
                        {
                            update.CameraImage1.Source = frame.ToBitmap();
                        }
                        if (update.tab1.IsSelected)
                            update.CameraImage.Source = frame.ToBitmap();
                        if (update.tab0.IsSelected)
                            update.CameraImage0.Source = frame.ToBitmap();
                    }
                }
            }

            if (update.tab2.IsSelected || update.tab4.IsSelected || update.tab0.IsSelected)
            {
                // Depth
                using (var frame = reference.DepthFrameReference.AcquireFrame())
                {
                    if (frame != null)
                    {
                        if (update.getMode == Mode.Depth)
                        {
                            update.CameraImage1.Source = frame.ToBitmap();
                        }
                        if (update.tab4.IsSelected)
                        {
                            if (update.index % 2 == 0)
                                update.CameraImage2.Source = frame.ToBitmap();
                        }
                        if (update.tab0.IsSelected)
                        {
                                update.aslck.Source = frame.ToBitmap();
                        }
                    }
                }
            }



            if (update.tab2.IsSelected || update.tab4.IsSelected)
            {
                // Infrared
                using (var frame = reference.InfraredFrameReference.AcquireFrame())
                {
                    if (frame != null)
                    {
                        if (update.getMode == Mode.Infrared)
                        {
                            update.CameraImage1.Source = frame.Bitmap();
                        }

                        if (update.tab4.IsSelected)
                        {
                            if (update.index % 2 == 1)
                                update.CameraImage2.Source = frame.Bitmap();

                            update.CameraImage3.Source = frame.Bitmap();
                        }

                    }
                }
            }

            if (update.tab2.IsSelected || update.tab0.IsSelected || update.tab1.IsSelected)
            {
                // Body
                using (var frame = reference.BodyFrameReference.AcquireFrame())
                {
                    if (frame != null)
                    {
                        update.canvas.Children.Clear();
                        update.canvas0.Children.Clear();
                        _bodies = new Body[frame.BodyFrameSource.BodyCount];
                        frame.GetAndRefreshBodyData(_bodies);

                        foreach (var body in _bodies)
                        {
                            if (body != null)
                            {
                                frame.GetAndRefreshBodyData(_bodies);// Refresh bodies

                                if (body.IsTracked)
                                {
                                    // Draw skeleton.
                                    if (update.getBodyC || update.tab0.IsSelected)
                                    {
                                        if(!update.tab0.IsSelected)
                                            update.canvas.DrawSkeleton(body);
                                        
                                        update.canvas0.DrawSkeleton(body);
                                    }

                                    // Update body gestures.
                                    //_gestureController.Update(body);

                                    // //left hand in front of left Shoulder
                                   ASL_TO_TEXT(body);
                                    
                                    // Display user height.
                                    //var tblHeights = string.Format("\nUser {0}: {1}cm", body.TrackingId, Math.Round(body.Height(), 2));

                                    if (_faceSource == null)
                                    {
                                        // Create new sources with body TrackingId
                                        _faceSource = new FaceFrameSource(_kinect, body.TrackingId, _faceFrameFeatures);
                                        _faceReader = _faceSource.OpenReader();// Create new reader
                                        _faceReader.FrameArrived += OnFaceFrameArrived;// Wire events
                                        _faceSource.TrackingIdLost += OnTrackingIdLost;
                                    }
                                    
                                }
                            }
                        }
                    }
                }
                //Console.WriteLine(frames);
            }

            if ((update.getMode == Mode.Default && update.tab2.IsSelected) || update.bpos.IsChecked.Value)
            {
                //DepthFrame depth = reference.DepthFrameReference.AcquireFrame();

                using (var colorFrame = reference.ColorFrameReference.AcquireFrame())
                using (var depthFrame = reference.DepthFrameReference.AcquireFrame())
                using (var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame())
                {
                    if (colorFrame != null && depthFrame != null && bodyIndexFrame != null)
                    {
                        // 3) Update the image source.
                        update.CameraImage1.Source = _backgroundRemoval.GreenScreen(colorFrame, depthFrame, bodyIndexFrame);

                        if (update.pos)
                            update.Position.Text = _backgroundRemoval.position;
                        //Console.WriteLine("Background Removal Method " + this.position);
                    }
                }
            }
            //Console.WriteLine(file.Length);
            update.updateKT2(file, bodystate);
        }
        #endregion CAMERA

        #region Kinect Code
        /// <summary>
        /// Initialize Kinect
        /// </summary>
        private void InitializeKinect()
        {
            _kinect = KinectSensor.GetDefault();// Get Kinect sensor
            if (_kinect == null) return;

            InitializeCamera();// Initialize Camera          
            _kinect.Open();// Start receiving
        }
        /// <summary>
        /// Process the face frame
        /// </summary>
        private void OnFaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            FaceFrameReference faceRef = e.FrameReference;// Retrieve the face reference
            if (faceRef == null) return;
   
            using (FaceFrame faceFrame = faceRef.AcquireFrame())// Acquire the face frame
            {
                if (faceFrame == null) return;
                // Retrieve the face frame result
                FaceFrameResult frameResult = faceFrame.FaceFrameResult;
                
                if (frameResult != null) {     
                    kdata[0] = frameResult.FaceProperties[FaceProperty.Happy].ToString();
                    kdata[1] = frameResult.FaceProperties[FaceProperty.Engaged].ToString();                    
                    kdata[2] = frameResult.FaceProperties[FaceProperty.WearingGlasses].ToString();                    
                    kdata[3] = frameResult.FaceProperties[FaceProperty.LeftEyeClosed].ToString();                  
                    kdata[4] = frameResult.FaceProperties[FaceProperty.RightEyeClosed].ToString();                  
                    kdata[5] = frameResult.FaceProperties[FaceProperty.MouthOpen].ToString();                   
                    kdata[6] = frameResult.FaceProperties[FaceProperty.MouthMoved].ToString();            
                    kdata[7] = frameResult.FaceProperties[FaceProperty.LookingAway].ToString();
                    update.UpdateKT();
                }        
            }
        }
        /// <summary>
        /// Handle when the tracked body is gone
        /// </summary>
        private void OnTrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
             update.UpdateKT();// Reset values for next body
            _faceReader = null; _faceSource = null;
        }
        #endregion
        #region Data Segmentation
        private void ASL_TO_TEXT(Body body)
        {

            var righthand = body.Joints[JointType.HandRight];
            var lefthand  = body.Joints[JointType.HandLeft];
            var rightwrist = body.Joints[JointType.WristRight];
            var leftwrist  = body.Joints[JointType.WristLeft];
            var leftShoulder  = body.Joints[JointType.ShoulderLeft];
            var rightShoulder = body.Joints[JointType.ShoulderRight];
            var leftelbow = body.Joints[JointType.ElbowLeft];
            var rightelbow = body.Joints[JointType.ElbowRight];
            var neck = body.Joints[JointType.Neck];
            var spineshoulder = body.Joints[JointType.SpineShoulder];
            var head = body.Joints[JointType.Head];
            var midspine = body.Joints[JointType.SpineMid];
            var rightThumb = body.Joints[JointType.ThumbRight];
            var leftThumb = body.Joints[JointType.ThumbLeft];
            var lefttip = body.Joints[JointType.HandTipLeft];
            var righttip = body.Joints[JointType.HandTipRight];
            //Console.WriteLine("RIGHT HAND " + body.Joints[JointType.HandRight].Position.X);
            //Console.WriteLine("RIGHT HAND " + body.Joints[JointType.HandRight].Position.ToVector3());
           // Console.WriteLine("LEFT HAND " + body.Joints[JointType.HandLeft].Position.ToVector3());
           // Console.WriteLine("SPINES HAND " + body.Joints[JointType.SpineShoulder].Position.ToVector3());


            //body.Joints[JointType.WristRight].AngleBetween(body.Joints[JointType.WristLeft], body.Joints[JointType.SpineShoulder]);
            //Console.WriteLine("LEFT " + body.Joints[JointType.WristLeft].Position.Y );
           // Console.WriteLine("SPINE " + body.Joints[JointType.SpineMid].Position.Y);
            //Console.WriteLine("RIGHT" + body.Joints[JointType.WristRight].Position.Y );
            //Console.WriteLine("NECK" + body.Joints[JointType.Neck].Position.Y);
           // Console.WriteLine("LE" + body.Joints[JointType.ElbowLeft].Position.Y);
            //Console.WriteLine("RE" + body.Joints[JointType.ElbowRight].Position.Y);
            //Console.WriteLine("LW" + body.Joints[JointType.WristRight].Position.Y);
           // Console.WriteLine("RW" + body.Joints[JointType.WristRight].Position.Y);


            //Console.WriteLine("LEFT SHOULDER "+body.Joints[JointType.ShoulderLeft].AngleBetween(body.Joints[JointType.HandRight], body.Joints[JointType.WristLeft]));
            //Console.WriteLine("RIGHT SHOULDER "+body.Joints[JointType.ShoulderRight].AngleBetween(body.Joints[JointType.HandLeft], body.Joints[JointType.WristRight]));

            //var ltp = body.Joints[JointType.ShoulderLeft].AngleBetween(body.Joints[JointType.HandRight], body.Joints[JointType.WristLeft]);
            //var rtp = body.Joints[JointType.ShoulderRight].AngleBetween(body.Joints[JointType.HandLeft], body.Joints[JointType.WristRight]);

            //Console.WriteLine(body.Joints[JointType.WristRight].AngleBetween(body.Joints[JointType.WristLeft], body.Joints[JointType.SpineShoulder]));
            //var temp = rightwrist.AngleBetween(leftwrist, spineshoulder);
            if (lefthand.Position.Y < neck.Position.Y
                && righthand.Position.Y < neck.Position.Y 

                && leftwrist.Position.Y > rightelbow.Position.Y
                && rightwrist.Position.Y > leftelbow.Position.Y 

                && leftwrist.Position.Y > midspine.Position.Y
                && rightwrist.Position.Y > midspine.Position.Y

                && leftwrist.Position.X > spineshoulder.Position.X
                && rightwrist.Position.X < spineshoulder.Position.X

                && rightelbow.Position.X > midspine.Position.X
                && leftelbow.Position.X < midspine.Position.X

                && righthand.Position.X < lefthand.Position.X

               // && rightwrist.Position.X - leftwrist.Position.X < 0

                )
                update.trans.Text = "LOVE";
           // Console.WriteLine("HEAD " + body.Joints[JointType.Head].Position.ToVector3());

            if (righthand.Position.Y > rightelbow.Position.Y &&
                rightThumb.Position.Y > righthand.Position.Y &&
                rightwrist.Position.Y < neck.Position.Y && 
                rightwrist.Position.Y > midspine.Position.Y &&
                righthand.Position.X > midspine.Position.X &&
                righttip.Position.X < rightShoulder.Position.X &&
                righttip.Position.Y > rightThumb.Position.Y && 
                rightThumb.Position.Y > rightShoulder.Position.Y &&
               // rightwrist.Position.Y < rightShoulder.Position.Y &&

               righthand.Position.X > lefthand.Position.X

                && leftThumb.Position.Y > leftShoulder.Position.Y
                && leftThumb.Position.Y > lefthand.Position.Y
                && lefthand.Position.Y > leftelbow.Position.Y 
                && rightwrist.Position.Y < neck.Position.Y
                && leftwrist.Position.Y > midspine.Position.Y 
                && lefthand.Position.X < midspine.Position.X
                && lefttip.Position.X > leftShoulder.Position.X
                && lefttip.Position.Y > leftThumb.Position.Y
                && leftThumb.Position.Y > leftShoulder.Position.Y
                 //&& leftwrist.Position.Y < leftShoulder.Position.Y
                )
                update.trans.Text = "HAPPY";
            //Console.WriteLine(head.AngleBetween(righthand, rightwrist));

            //var htp = head.AngleBetween(righthand, rightwrist);
            if (righthand.Position.Y > neck.Position.Y
                && rightwrist.Position.Y < head.Position.Y
                &&  righthand.Position.Y > rightThumb.Position.Y
                && righthand.Position.X < rightShoulder.Position.X
                //&& righthand.Position.X == head.Position.X
                && righthand.Position.Y > midspine.Position.Y
                && righthand.Position.X > leftShoulder.Position.X
                && rightelbow.Position.X < rightShoulder.Position.X
                && righthand.Position.X - lefthand.Position.X > 0)
            {
                update.trans.Text = "Engry";
            }

            if (rightThumb.Position.Y > rightShoulder.Position.Y
                && leftThumb.Position.Y > leftShoulder.Position.Y
                && righthand.Position.X < rightShoulder.Position.X
                && lefthand.Position.X > leftShoulder.Position.X
                //&& righttip.Position.Y < rightShoulder.Position.Y
                && rightThumb.Position.Y > righthand.Position.Y
                && rightThumb.Position.Y > righttip.Position.Y
                && leftThumb.Position.Y > lefthand.Position.Y
                && leftThumb.Position.Y > lefttip.Position.Y

                && righthand.Position.X > lefthand.Position.X

                && rightelbow.Position.Y < rightShoulder.Position.Y
                 && leftelbow.Position.Y < leftShoulder.Position.Y
               // && lefttip.Position.Y < leftShoulder.Position.Y
                //&& body.HandLeftState == HandState.Open
               // && body.HandRightState == HandState.Open
                //&& righthand.Position.X == head.Position.X
                 //&& righthand.Position.Y > midspine.Position.Y
               //  && lefthand.Position.Y > midspine.Position.Y
                //&& righthand.Position.X < spineshoulder.Position.X
                // && righthand.Position.X > rightShoulder.Position.X

                //&& lefthand.Position.X < spineshoulder.Position.X
                // && lefthand.Position.X < leftShoulder.Position.X
                )
            {
                update.trans.Text = "Exited";
            }

            /*if (righthand.Position.Y > lefthand.Position.Y
                    && lefthand.Position.Y > leftelbow.Position.Y
                //&& lefthand.Position.X == righthand.Position.X
                && righthand.Position.Y > rightelbow.Position.Y
                  && lefthand.Position.Y < leftShoulder.Position.Y
                  && righthand.Position.Y < rightShoulder.Position.Y
                    && rightelbow.Position.Y < rightShoulder.Position.Y
                    && leftelbow.Position.Y < leftShoulder.Position.Y)
            {
                update.trans.Text = "Annoy";
            }*/

             if (rightThumb.Position.Y < head.Position.Y
                 && rightThumb.Position.Y < righthand.Position.Y
                 && righthand.Position.Y > neck.Position.Y
                 && righttip.Position.Y > neck.Position.Y
                 && rightThumb.Position.Y < righttip.Position.Y
                //&& righthand.Position.X < head.Position.X
                 && rightelbow.Position.Y < rightShoulder.Position.Y
                 && lefthand.Position.Y < leftelbow.Position.Y)
             {
                 update.trans.Text = "Bored";
             }

             if (righttip.Position.Y > neck.Position.Y
                && rightThumb.Position.Y < neck.Position.Y
                && righthand.Position.Y < head.Position.Y
                 //&& righthand.Position.X < head.Position.X
                && rightelbow.Position.Y < rightShoulder.Position.Y
                && rightelbow.Position.X < rightShoulder.Position.X
                && lefthand.Position.Y < leftelbow.Position.Y)
             {
                 update.trans.Text = "Disappointed";

             }


             if (righthand.Position.Y < rightShoulder.Position.Y
              && righthand.Position.X < rightShoulder.Position.X
              && righthand.Position.Y > midspine.Position.Y
               && righthand.Position.X > leftShoulder.Position.X
              // && rightelbow.Position.Y < rightShoulder.Position.Y
               && righthand.Position.Y > rightelbow.Position.Y
               && lefthand.Position.Y < leftelbow.Position.Y
                  && rightShoulder.Position.X < rightelbow.Position.X
                  && rightThumb.Position.Y < righthand.Position.Y
                  && rightThumb.Position.Y < righttip.Position.Y)
             {
                 update.trans.Text = "Disgusted";

             }

            /*
             if (righttip.Position.Y > neck.Position.Y
                 && rightThumb.Position.Y > rightShoulder.Position.Y
                 && rightwrist.Position.Y < rightShoulder.Position.Y
                 && righthand.Position.Y > midspine.Position.Y
                 && righthand.Position.X < rightShoulder.Position.X
                 && righthand.Position.X > midspine.Position.X
                 // && rightelbow.Position.Y < rightShoulder.Position.Y
                 && lefttip.Position.Y > neck.Position.Y
                 && leftThumb.Position.Y > leftShoulder.Position.Y
                 && leftwrist.Position.Y < leftShoulder.Position.Y
                 && lefthand.Position.Y > midspine.Position.Y
                 && lefthand.Position.X > leftShoulder.Position.X
                 && lefthand.Position.X < midspine.Position.X)
             {
                 update.trans.Text = "Embarrased";

             }*/

             if (righthand.Position.Y < leftShoulder.Position.Y
                && righttip.Position.Y < leftShoulder.Position.Y
                && rightThumb.Position.Y < leftShoulder.Position.Y
                && rightThumb.Position.Y > righttip.Position.Y
                && righthand.Position.X < midspine.Position.X
                && righthand.Position.X < rightelbow.Position.X
                && rightelbow.Position.X > midspine.Position.X
                 // && rightelbow.Position.Y < rightShoulder.Position.Y
                && righthand.Position.Y > rightelbow.Position.Y
                && lefthand.Position.Y < leftelbow.Position.Y)
             {
                 update.trans.Text = "Guilt";

             }

             // Right and Left Hand in front of Shoulders
             if (//lefthand.Position.Z < leftelbow.Position.Z 
                 //&& righthand.Position.Z < rightelbow.Position.Z

                // && 
                 righthand.Position.X < rightelbow.Position.X
                 && lefthand.Position.X > leftelbow.Position.X

                 && righttip.Position.Y > righthand.Position.Y
                 && lefttip.Position.Y  > lefthand.Position.Y
                 //&& righthand.Position.Y < head.Position.Y
                // && lefthand.Position.Y < head.Position.Y
                 //&& righthand.Position.Y < neck.Position.Y
                // && righthand.Position.X > leftShoulder.Position.X
                // && lefthand.Position.X < rightShoulder.Position.X

                 && rightelbow.Position.Y < midspine.Position.Y
                 && leftelbow.Position.Y < midspine.Position.Y
                // && 
                 && righthand.Position.X - lefthand.Position.X < 0
                 && rightThumb.Position.X - leftThumb.Position.X < 0
                 && righttip.Position.X - lefttip.Position.X < 0
                 )
             {
                 update.trans.Text = "Hope";
             }

             if (
                 //righthand.Position.Y < rightShoulder.Position.Y
               // && righttip.Position.Y < rightShoulder.Position.Y
                //&& rightThumb.Position.Y < rightShoulder.Position.Y
               // && rightThumb.Position.Y > righttip.Position.Y
                //&& righthand.Position.X < rightShoulder.Position.X
               // && rightShoulder.Position.X < rightelbow.Position.X
                
                 // && rightelbow.Position.Y < rightShoulder.Position.Y
              //  && lefttip.Position.Y < leftShoulder.Position.Y
               // && leftThumb.Position.Y < leftShoulder.Position.Y
               // && leftThumb.Position.Y > lefttip.Position.Y
               // && lefthand.Position.X < leftShoulder.Position.X
               // && leftShoulder.Position.X < leftelbow.Position.X

                 righthand.Position.Y < neck.Position.Y &&
                 lefthand.Position.Y < neck.Position.Y &&

                 righttip.Position.Y < neck.Position.Y &&
                 righthand.Position.Y > midspine.Position.Y &&
                 lefttip.Position.Y < neck.Position.Y && 

                // righthand.Position.X > midspine.Position.X
                // && lefthand.Position.X < midspine.Position.X &&
                 righthand.Position.X > lefthand.Position.X

                 && leftThumb.Position.Y > lefttip.Position.Y
                 && rightThumb.Position.Y > righttip.Position.Y

                 && righttip.Position.X - lefttip.Position.X < 0
                 // && righthand.Position.X - lefthand.Position.X > 0
                // && rightThumb.Position.X - leftThumb.Position.X > 0
                 )
             {
                 update.trans.Text = "Hurt";

             }

             if (righthand.Position.Y > neck.Position.Y
                 && righttip.Position.Y < rightThumb.Position.Y
                 && righthand.Position.X > leftShoulder.Position.X
                 && rightelbow.Position.X > rightShoulder.Position.X
                 && lefthand.Position.Y < neck.Position.Y
              )
             {
                 update.trans.Text = "Mad";

             }

             if (righthand.Position.Y > head.Position.Y
                 || lefthand.Position.Y > head.Position.Y 
                 && righthand.Position.Y < rightThumb.Position.Y
                 && lefthand.Position.Y < leftThumb.Position.Y
                  && rightelbow.Position.Y > rightShoulder.Position.Y
                 && leftelbow.Position.Y > leftShoulder.Position.Y
                  //&& righthand.Position.X < rightShoulder.Position.X
                 // && righthand.Position.X > head.Position.X
                 // && lefthand.Position.X > leftShoulder.Position.X
                  //&& lefthand.Position.X < head.Position.X
                 // && rightelbow.Position.Y < rightShoulder.Position.Y
               )
             {
                 update.trans.Text = "Sad";

             }

             if (//body.HandRightState == HandState.Closed && body.HandLeftState == HandState.Closed
                 //&& 
                 rightwrist.Position.Y > neck.Position.Y
                    && leftwrist.Position.Y > neck.Position.Y

                  && righthand.Position.X < rightelbow.Position.X
                  && lefthand.Position.X > leftelbow.Position.X

                 && righthand.Position.Y > rightThumb.Position.Y
                 && lefthand.Position.Y > leftThumb.Position.Y

                  && leftelbow.Position.X < leftShoulder.Position.X
                  && rightelbow.Position.X > rightShoulder.Position.X

                  && rightelbow.Position.Y <rightShoulder.Position.Y
                  && leftelbow.Position.Y < leftShoulder.Position.Y
                  
                )
             {
                 update.trans.Text = "Safe";

             }

             if (righthand.Position.Y < rightShoulder.Position.Y
                && righthand.Position.X < rightShoulder.Position.X
                && righthand.Position.Y > midspine.Position.Y
                && righthand.Position.X > leftShoulder.Position.X
                 // && rightelbow.Position.Y < rightShoulder.Position.Y
                && righthand.Position.Y > rightelbow.Position.Y
                && lefthand.Position.Y < leftelbow.Position.Y
                && righthand.Position.X < midspine.Position.X
                && rightShoulder.Position.X < rightelbow.Position.X
                 && rightThumb.Position.Y < righttip.Position.Y
                )
             {
                 update.trans.Text = "Sorry";

             }

           /*  if (//body.HandRightState == HandState.Closed && body.HandLeftState == HandState.Closed
                 //&& 
                righthand.Position.Y < rightShoulder.Position.Y
                && lefthand.Position.Y < leftShoulder.Position.Y

                //&& righttip.Position.X < leftShoulder.Position.X
               // && lefttip.Position.X > rightShoulder.Position.X

                 && righthand.Position.X < midspine.Position.X
                 && lefthand.Position.X > midspine.Position.X

                //&& righthand.Position.Y < rightThumb.Position.Y
               // && lefthand.Position.Y < leftThumb.Position.Y

                 && leftelbow.Position.X < leftShoulder.Position.X
                 && rightelbow.Position.X > rightShoulder.Position.X

               )
             {
                 update.trans.Text = "Scared";

             }*/

             if (righthand.Position.Y > neck.Position.Y
                 && rightThumb.Position.Y > righthand.Position.Y
                 && rightThumb.Position.Y > righttip.Position.Y
                  && rightThumb.Position.Y > rightwrist.Position.Y
                 //&& righthand.Position.X < head.Position.X
                 && rightelbow.Position.X > rightShoulder.Position.X
                 && lefthand.Position.Y < leftelbow.Position.Y)
             {
                 update.trans.Text = "Shy";

             }

             if (righthand.Position.Y > neck.Position.Y 
                 && lefthand.Position.Y > neck.Position.Y

                 && rightelbow.Position.Y < rightShoulder.Position.Y
                 && leftelbow.Position.Y < leftShoulder.Position.Y

                 && righthand.Position.X < rightShoulder.Position.X
                 && lefthand.Position.X > leftShoulder.Position.X

                 //&& leftelbow.Position.X < leftShoulder.Position.X

                // && rightelbow.Position.Y > rightShoulder.Position.Y
                // && leftelbow.Position.Y > leftShoulder.Position.Y
                 )
             {
                 update.trans.Text = "Shame";

             }

             if (//lefthand.Position.Z < leftelbow.Position.Z 
                 //&& righthand.Position.Z < rightelbow.Position.Z

             // && 
             righthand.Position.X   < rightShoulder.Position.X
             && rightelbow.Position.X  > rightShoulder.Position.X

             && lefthand.Position.X > leftShoulder.Position.X
             && leftelbow.Position.X < leftShoulder.Position.X
             //&& righttip.Position.X < righthand.Position.X
             //&& lefttip.Position.X  < lefthand.Position.X
             && righthand.Position.Y < neck.Position.Y
             && lefthand.Position.Y  < neck.Position.Y

             && righthand.Position.X > lefthand.Position.X

             && righthand.Position.X > leftShoulder.Position.X
             && lefthand.Position.X < rightShoulder.Position.X
                 //&& lefthand.Position.Y < neck.Position.Y
                 // && 
             && righthand.Position.X - lefthand.Position.X < 0
             )
             {
                 update.trans.Text = "Stressed";
             }

             if (righthand.Position.Y < head.Position.Y
                 && lefthand.Position.Y < head.Position.Y

                 && righttip.Position.Y > rightThumb.Position.Y
                 && lefttip.Position.Y > leftThumb.Position.Y

                 && righttip.Position.Y > righthand.Position.Y
                 && lefttip.Position.Y > lefthand.Position.Y

                 && righthand.Position.Y < midspine.Position.Y
                 && lefthand.Position.Y < midspine.Position.Y

                 && righthand.Position.X > lefthand.Position.X
                 //&& rightelbow.Position.X < rightShoulder.Position.X
                 //&& leftelbow.Position.X > leftShoulder.Position.X

                 //&& rightThumb.Position.X - leftThumb.Position.X < 0
                 //&& righthand.Position.X < rightShoulder.Position.X
                 // && righthand.Position.X > head.Position.X
                 // && lefthand.Position.X > leftShoulder.Position.X
                 //&& lefthand.Position.X < head.Position.X
                 // && rightelbow.Position.Y < rightShoulder.Position.Y
                )
             {
                 update.trans.Text = "Surprised";

             }

             if (righttip.Position.Y > neck.Position.Y
                && lefttip.Position.Y > neck.Position.Y

                && rightThumb.Position.Y < righttip.Position.Y
                && leftThumb.Position.Y < lefttip.Position.Y

                && rightelbow.Position.Y < rightShoulder.Position.Y
                && leftelbow.Position.Y < leftShoulder.Position.Y

                && righthand.Position.X < rightShoulder.Position.X
                && lefthand.Position.X > leftShoulder.Position.X

                && righttip.Position.X < righthand.Position.X
                && lefttip.Position.X > lefthand.Position.X

                && righttip.Position.X < rightThumb.Position.X
                && lefttip.Position.X > leftThumb.Position.X

                && righthand.Position.X > lefthand.Position.X

           //&& leftelbow.Position.X < leftShoulder.Position.X

          // && rightelbow.Position.Y > rightShoulder.Position.Y
                 // && leftelbow.Position.Y > leftShoulder.Position.Y
                )
             {
                 update.trans.Text = "Worry";

             }

            if(rightelbow.Position.Y > rightShoulder.Position.Y)
            {
                update.aslupdate.Text = "TRANSLATOR ON";
                update.aslupdate.Background = Brushes.Green;
            }

            if (leftelbow.Position.Y > leftShoulder.Position.Y)
            {
                update.aslupdate.Text = "TRANSLATOR OFF";
                update.aslupdate.Background = Brushes.Red;
            }

            //Console.WriteLine(body.HandRightState + " " + body.HandLeftState);
            if(body.HandRightState == HandState.Open && body.HandLeftState == HandState.Open)
            {
                update.trans.Text = "HANDS OPEN";
            }
            else if(body.HandRightState == HandState.Closed && body.HandLeftState == HandState.Closed)
            {
                update.trans.Text = "HANDS CLOSE";
            }

            //Console.WriteLine(body.HandRightState + " " + body.HandLeftState);
            if (body.HandRightState == HandState.Open && body.HandLeftState == HandState.Closed)
            {
                update.trans.Text = "RIGHT HAND OPEN";
            }
            else if (body.HandRightState == HandState.Closed && body.HandLeftState == HandState.Open)
            {
                update.trans.Text = "LEFT HAND OPEN";
            }
        }

        private float ScaleVector(int length, float position)
        {
            float value = (((((float)length) / 1f) / 2f) * position) + (length / 2);
            if (value > length)
            {
                return (float)length;
            }
            if (value < 0f)
            {
                return 0f;
            }
            return value;
        }

        bool statusOn = false;
        void GestureController_GestureRecognized(object sender, GestureEventArgs e)
        {
            // Do something according to the type of the gesture.
            //Console.WriteLine("Looking for a gesture");
            Console.WriteLine(e.GestureType);
            switch (e.GestureType)
            {
                case GestureType.JoinedHands:    
                    update.aslupdate.Text = "TRANSLATOR ON";
                    update.aslupdate.Background = Brushes.Green;
                    break;
                case GestureType.Menu:
                    break;
                case GestureType.Sorry:
                    update.trans.Text = "OPEN HANDS";
                    break;
                case GestureType.SwipeDown:
                    break;
                case GestureType.SwipeLeft:
                    break;
                case GestureType.SwipeRight:
               
                    break;
                case GestureType.SwipeUp:
                    break;
                case GestureType.WaveLeft:
                    update.aslupdate.Text = "TRANSLATOR OFF";
                    update.aslupdate.Background = Brushes.Red;
                    break;
                case GestureType.WaveRight:
                    statusOn = !statusOn;

                    update.aslupdate.Text = "TRANSLATOR ON";
                    update.aslupdate.Background = Brushes.Green;
                    break;
                case GestureType.ZoomIn:
                    break;
                case GestureType.ZoomOut:
                    break;
                default:
                    break;
            }
        }
    }
        #endregion 
}