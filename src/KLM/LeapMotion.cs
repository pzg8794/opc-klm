#region Library
using System.Windows;
using System.ComponentModel;
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
using System.Windows.Shapes;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
#endregion 
namespace LeapMotion
{
    public partial class MainWindow2 : Window, ILeapEventDelegate
    {
        #region members
        MainWindow update;
        public Controller controller = new Controller();
        System.Windows.Controls.Image[] images = new System.Windows.Controls.Image[5];

        Device device;

        private LeapEventListener listener;
        private Boolean isClosing = false;
        static string handsS = "NO HANDS";
        private delegate void NoArgDelegate();
        private delegate void OneArgDelegate(String[] arg);
        string[] data = new String[9];
        static string file = "";
        public BitmapImage bi = new BitmapImage();
        public Frame frame;
        delegate void LeapEventDelegate(string EventName);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        #endregion
        #region constructors
        /// <summary>
        /// Default CTOR
        /// </summary>
        public MainWindow2(MainWindow thread)
        {
            update = thread;
            device = controller.Devices[0];
        }  
        // Volatile is used as hint to the compiler that this data 
        // member will be accessed by multiple threads. 
        public void DoWork()
        {
           //update.InitializeComponent();
            this.controller = new Controller();
            this.listener = new LeapEventListener(this);
            controller.AddListener(listener);
        }    
        #endregion 
        #region Leap Motion code
        public string[] dataLM    // the Name property
        {
            get{return data;}
        }
        public void LeapEventNotification(string EventName)
        {
            if (this.CheckAccess())
            {
                switch (EventName)
                {
                    case "onInit":
                        Debug.WriteLine("Init");
                        break;
                    case "onConnect":
                        this.connectHandler();
                        break;
                    case "onFrame":
                        if (!this.isClosing)
                            this.newFrameHandler(this.controller.Frame());
                        break;
                    case "onImages":
                        if (!this.isClosing)
                            this.newImage(this.controller);
                        break;
                    case "onDisconnect":
                        this.onDisconnect();
                        break;
                }
            }
            else
            {   //this.onDisconnect();
                Dispatcher.Invoke(new LeapEventDelegate(LeapEventNotification), new object[] { EventName });
            }
        }
        void onDisconnect()
        {
            update.UpdateLM();
        }
        void connectHandler()
        {
            this.controller.SetPolicy(Controller.PolicyFlag.POLICY_IMAGES);
            this.controller.SetPolicy(Controller.PolicyFlag.POLICY_BACKGROUND_FRAMES);
            this.controller.SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD); 
            this.controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);
            controller.EnableGesture(Gesture.GestureType.TYPE_CIRCLE);
            controller.EnableGesture(Gesture.GestureType.TYPE_KEY_TAP);
            controller.EnableGesture(Gesture.GestureType.TYPE_SCREEN_TAP);
            controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);

            this.controller.Config.SetFloat("Gesture.Swipe.MinLength", 100.0f);
        }
        void newImage(Leap.Controller frame)
        {
            //Console.WriteLine("New Image");
            // System.Drawing.Image myImage;
            Leap.Image image = frame.Images[0];

            float cameraOffset = 20; //x-axis offset in millimeters
            FingerList fingers = frame.Frame().Fingers;
            foreach (Finger finger in fingers)
            {
                Leap.Vector tip = finger.TipPosition;
                float hSlope = -(tip.x + cameraOffset * (2 * image.Id - 1)) / tip.y;
                float vSlope = tip.z / tip.y;

                Leap.Vector pixel = image.Warp(new Leap.Vector(hSlope, vSlope, 0));
                //Draw tip at pixel
            }

            Bitmap bitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            //set palette
            ColorPalette grayscale = bitmap.Palette;
            for (int i = 0; i < 256; i++)
            {
                grayscale.Entries[i] = System.Drawing.Color.FromArgb((int)255, i, i, i);
            }
            bitmap.Palette = grayscale;

            System.Drawing.Rectangle lockArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(lockArea, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            byte[] rawImageData = image.Data;
            System.Runtime.InteropServices.Marshal.Copy(rawImageData, 0, bitmapData.Scan0, image.Width * image.Height);
            bitmap.UnlockBits(bitmapData);

            IntPtr hBitmap = bitmap.GetHbitmap();
            System.Windows.Media.ImageSource WpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap
                (hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            
            DeleteObject(hBitmap);

            if (update.tab3.IsSelected) 
            {
                update.img.Source = WpfBitmap;
                update.img.Width = 500;
                update.img.Height = 600;
                update.img.Stretch = System.Windows.Media.Stretch.Fill;
            }

            if (update.tab4.IsSelected)
            {
                 BackgroundWorker worker = new BackgroundWorker();
                 worker.DoWork += delegate(object s, DoWorkEventArgs args)
                 {
                     updateFrame(frame.Frame());
                 };

                 // RunWorkerCompleted will fire on the UI thread when the background process is complete
                 worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
                 {
                     update.setLMF(file, handsS);
                 };

                 worker.RunWorkerAsync();

                update.LMCamera1.Source = WpfBitmap;
                update.LMCamera1.Width = 400;
                update.LMCamera1.Height = 500;
                update.LMCamera1.Stretch = System.Windows.Media.Stretch.Fill;
            }

            if(update.tab0.IsSelected)
            {
               // BackgroundWorker worker = new BackgroundWorker();
               // worker.DoWork += delegate(object s, DoWorkEventArgs args)
               // {
                   // ASL_TO_TEXT(frame.Frame());
                //};
                // RunWorkerCompleted will fire on the UI thread when the background process is complete
               // worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
               // {
               // };

                // worker.RunWorkerAsync();
                update.aslclm.Source = WpfBitmap;
                update.aslclm.Width = 250;
                update.aslclm.Height = 250;
                update.aslclm.Stretch = System.Windows.Media.Stretch.Fill;


            }
            
        }
        void ASL_TO_TEXT(Leap.Frame frame)
        {
            string hand = "";

            update.aslupdate.Text = "TRANSLATOR ON";
            update.aslupdate.Background = System.Windows.Media.Brushes.Green;

            foreach (Hand leapHand in frame.Hands)
            {
                 //Given a valid hand object
               /* if (Math.Abs(leapHand.PalmNormal.x) > 0.7)
                {
                    if (leapHand.PalmNormal.x > 0 && leapHand.PalmNormal.Magnitude > 0)
                    { // Right hand
                        //Thumbs down
                        Console.WriteLine("Thumb Down");
                    }
                    else
                        Console.WriteLine("Thumb Up");

                    if (leapHand.PalmNormal.Magnitude <= 0 && leapHand.PalmNormal.Magnitude < 0)
                    { //Left hand
                        //Thumbs down
                        Console.WriteLine("Thumb Down");
                    }
                    else
                        Console.WriteLine("Thumb Up");
                }*/

                if (leapHand.IsLeft)
                    hand = "Left_Hand";
                else
                    hand = "Right_Hand";

                bool[] extended = new bool[5];
                for (int fs = 0; fs < leapHand.Fingers.Count; fs++)
                {
                    Finger leapFinger = leapHand.Fingers[fs];

                    switch(leapHand.Fingers[fs].Type().ToString())
                    {
                        case "TYPE_THUMB":
                            //Console.WriteLine(leapHand.Fingers[fs].Type() + " " + fs);
                            //Console.WriteLine(leapFinger.IsExtended);
                            extended[0] = leapFinger.IsExtended;

                            break;
                        case "TYPE_INDEX":
                            //Console.WriteLine(leapHand.Fingers[fs].Type() + " " + fs);
                            //Console.WriteLine(leapFinger.IsExtended);
                            //Console.WriteLine(leapFinger.Direction);
                            extended[1] = leapFinger.IsExtended;
                            break;
                        case "TYPE_MIDDLE":
                            //Console.WriteLine(leapHand.Fingers[fs].Type() + " " + fs);
                            //Console.WriteLine(leapFinger.IsExtended);
                            //Console.WriteLine(leapFinger.Direction);
                            extended[2] = leapFinger.IsExtended;
                            break;
                        case "TYPE_RING":
                           // Console.WriteLine(leapHand.Fingers[fs].Type() + " " + fs);
                            //Console.WriteLine(leapFinger.IsExtended);
                            extended[3] = leapFinger.IsExtended;
                            break;
                        case "TYPE_PINKY":
                            //Console.WriteLine(leapHand.Fingers[fs].Type() + " " + fs);
                            //Console.WriteLine(leapFinger.IsExtended);
                            extended[4] = leapFinger.IsExtended;
                            break;
                    }
                }

                if (extended[0] && !extended[1] && !extended[2] && !extended[3] && !extended[4])
                {
                    // Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    //{
                   update.trans.Text = "A";
                   // }));
               }

                if (!extended[0] && extended[1] && extended[2] && extended[3] && extended[4])
                {
                    update.trans.Text = "B";
                }


                if (!extended[0] && !extended[1] && !extended[2] && !extended[3] && !extended[4])
                {
                    update.trans.Text = "C";
                }

                if (!extended[0] && extended[1] && !extended[2] && !extended[3] && !extended[4])
                {
                    update.trans.Text = "D";
                }

                if (!extended[0] && !extended[1] && extended[2] && extended[3] && extended[4])
                {
                    update.trans.Text = "F";
                }

                if (!extended[0] && extended[1] && extended[2] && !extended[3] && !extended[4])
                {
                    update.trans.Text = "U";
                    //update.trans.Text = "V";
                    //update.trans.Text = "H";
                }

                if (!extended[0] && !extended[1] && !extended[2] && !extended[3] && extended[4])
                {
                    update.trans.Text = "I";
                }

                if (extended[0] && extended[1] && !extended[2] && !extended[3] && !extended[4])
                {
                    update.trans.Text = "L";
                }

                if (!extended[0] && extended[1] && extended[2] && extended[3] && !extended[4])
                {
                    update.trans.Text = "W";
                }

                if (extended[0] && !extended[1] && !extended[2] && !extended[3] && extended[4])
                {
                    update.trans.Text = "Y";
                }

            }
        }
        void updateFrame(Leap.Frame frame)
        {
            file = "";
            string hand = "";
            handsS = "";

            if (frame.Hands.Count == 0)
                handsS = "HANDS NOT TRACKED";
            else
                handsS = "TRACKED";

            foreach (Hand leapHand in frame.Hands)
            {
                if (leapHand.IsLeft)
                    hand = "Left_Hand";
                else
                    hand = "Right_Hand";

                Leap.Vector handXBasis = leapHand.PalmNormal.Cross(leapHand.Direction).Normalized;
                Leap.Vector handYBasis = -leapHand.PalmNormal;
                Leap.Vector handZBasis = -leapHand.Direction;
                Leap.Vector handOrigin = leapHand.PalmPosition;
                Leap.Vector velocity = leapHand.PalmVelocity;

                string format = "{0,-22} , {1,-12} , {2, 12} , {3,12}";
                Leap.Matrix handTransform = new Leap.Matrix(handXBasis, handYBasis, handZBasis, handOrigin);
                handTransform = handTransform.RigidInverse();
                file += String.Format(format, hand + "MatrixXB", handTransform.xBasis.x, handTransform.xBasis.y, handTransform.xBasis.y + "\n");
                file += String.Format(format, hand + "MatrixYB", handTransform.yBasis.x, handTransform.yBasis.y, handTransform.yBasis.z + "\n");
                file += String.Format(format, hand + "MatrixZB", handTransform.zBasis.x, handTransform.zBasis.y, handTransform.zBasis.z + "\n");
                file += String.Format(format, hand + "MatrixO", handTransform.origin.x, handTransform.origin.y, handTransform.origin.z + "\n");
                file += String.Format(format, hand + "_D", leapHand.Direction.x, leapHand.Direction.y, leapHand.Direction.z + "\n");
                file += String.Format(format, hand + "_N", leapHand.Direction.Normalized.x, leapHand.Direction.Normalized.y, leapHand.Direction.Normalized.y + "\n");
                file += String.Format(format, "Others_Hand_Features", leapHand.Direction.Pitch, leapHand.Direction.Roll, leapHand.Direction.Yaw + "\n");
                file += String.Format(format, hand + "_M", "0", "0", leapHand.Direction.Magnitude + "\n");

                file += String.Format(format, hand + "Palm_P", leapHand.PalmPosition.x, leapHand.PalmPosition.y, leapHand.PalmPosition.z + "\n");
                file += String.Format(format, hand + "Palm_N", leapHand.PalmPosition.Normalized.x, leapHand.PalmPosition.Normalized.y, leapHand.PalmPosition.Normalized.z + "\n");
                file += String.Format(format, "Others_Palm_Features", leapHand.PalmPosition.Pitch, leapHand.PalmPosition.Roll, leapHand.PalmPosition.Yaw + "\n");
                file += String.Format(format, hand + "Palm_M", "0", "0", leapHand.PalmPosition.Magnitude + "\n");

                file += String.Format(format, hand + "Wrist_P", leapHand.WristPosition.x, leapHand.WristPosition.y, leapHand.WristPosition.z + "\n");
                file += String.Format(format, hand + "Wrist_N", leapHand.WristPosition.Normalized.x, leapHand.WristPosition.Normalized.y, leapHand.WristPosition.Normalized.z + "\n");
                file += String.Format(format, "Others_Wrist_Features", leapHand.WristPosition.Pitch, leapHand.WristPosition.Roll, leapHand.WristPosition.Yaw + "\n");
                file += String.Format(format, hand + "Wrist_M", "0", "0", leapHand.WristPosition.Magnitude + "\n");

                file += String.Format(format, hand + "Arm_D", leapHand.Arm.Direction.x, leapHand.Arm.Direction.y, leapHand.Arm.Direction.z + "\n");
                file += String.Format(format, hand + "Arm_N", leapHand.Arm.Direction.Normalized.x, leapHand.Arm.Direction.Normalized.y, leapHand.Arm.Direction.Normalized.z + "\n");
                file += String.Format(format, "Others_Arm_Features", leapHand.Arm.Direction.Pitch, leapHand.Arm.Direction.Roll, leapHand.Arm.Direction.Yaw + "\n");
                file += String.Format(format, hand + "Arm_M", "0", "0", leapHand.Arm.Direction.Magnitude + "\n");

                file += String.Format(format, hand + "Elbow_", leapHand.Arm.ElbowPosition.x, leapHand.Arm.ElbowPosition.y, leapHand.Arm.ElbowPosition.z + "\n");
                file += String.Format(format, hand + "Elbow_N", leapHand.Arm.ElbowPosition.Normalized.x, leapHand.Arm.ElbowPosition.Normalized.y, leapHand.Arm.ElbowPosition.Normalized.z + "\n");
                file += String.Format(format, "Others_Elbow_Features", leapHand.Arm.ElbowPosition.Pitch, leapHand.Arm.ElbowPosition.Roll, leapHand.Arm.ElbowPosition.Yaw + "\n");
                file += String.Format(format, hand + "Elbow_M", "0", "0", leapHand.Arm.ElbowPosition.Magnitude + "\n");

                //Console.WriteLine("Matrix " + handTransform);
                for (int fs = 0; fs < leapHand.Fingers.Count; fs++)
                {
                    Finger leapFinger = leapHand.Fingers[fs];
                    Leap.Vector transformedPosition = handTransform.TransformPoint(leapFinger.TipPosition);
                    Leap.Vector transformedDirection = handTransform.TransformDirection(leapFinger.Direction);
                    Leap.Vector transformedDirectionVelocity = handTransform.TransformDirection(leapFinger.TipVelocity);

                    file += String.Format(format, leapFinger.Type(), leapFinger.Direction.x, leapFinger.Direction.y, leapFinger.Direction.z + "\n");
                    file += String.Format(format, leapFinger.Type() + "TipP", leapFinger.TipPosition.x, leapFinger.TipPosition.y, leapFinger.TipPosition.y + "\n");
                    file += String.Format(format, leapFinger.Type() + "TipV", leapFinger.TipVelocity.x, leapFinger.TipVelocity.y, leapFinger.TipVelocity.z + "\n");
                    file += String.Format(format, leapFinger.Type() + "TipS", leapFinger.StabilizedTipPosition.x, leapFinger.StabilizedTipPosition.y, leapFinger.StabilizedTipPosition.z + "\n");
                    file += String.Format(format, leapFinger.Type() + "D_N", leapFinger.Direction.Normalized.x, leapFinger.Direction.Normalized.y, leapFinger.Direction.Normalized.z + "\n");
                    file += String.Format(format, "Others_Finger_Featuers", leapFinger.Direction.Pitch, leapFinger.Direction.Roll, leapFinger.Direction.Yaw + "\n");
                }
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                if (file.Length != 0)
                {
                    handsS = "Hands Tracked";
                    if (update.bupdate.Text.Equals("Body Tracked"))
                        update.bhupdate.Text = "KLM";
                    else
                        update.bhupdate.Text = "LM";
                }
                else
                {
                    handsS = "Hands Not Tracked";
                    if (update.bupdate.Text.Equals("Body Tracked"))
                        update.bhupdate.Text = "KT";
                    else
                        update.bhupdate.Text = "NO MODE";
                }
            }));
           // Console.WriteLine(file.Length);
        }
        void newFrameHandler(Leap.Frame frame)
        {
           // Console.WriteLine("New Frame");
            this.frame = frame;
            updateFrame(frame);
            update.setLMF(file, handsS);

            data[7] = frame.Timestamp.ToString();
            data[2] = frame.Id.ToString();
            data[0] = frame.Hands.Count.ToString();

            foreach (Hand leapHand in frame.Hands)
            {
                InteractionBox interactionBox = frame.InteractionBox;
                // Leap.Vector point = new Leap.Vector(100, 75, -125);
                Leap.Vector normalizedCoordinates = interactionBox.NormalizePoint(leapHand.PalmPosition); //pending
                //Console.WriteLine(normalizedCoordinates);


                    // Get the Arm bone
                Arm arm = leapHand.Arm;

                foreach (Finger finger in leapHand.Fingers) // Get fingers
                {
                    Bone bone;// Get finger bones
                    foreach (Bone.BoneType boneType in (Bone.BoneType[])Enum.GetValues(typeof(Bone.BoneType)))
                    {
                        bone = finger.Bone(boneType);

                        if (frame.Hands.Count.Equals(2))
                        {
                            data[3] = "Yes"; data[4] = "Yes";
                            data[1] = frame.Fingers.Count.ToString();
                            data[5] = "R " + frame.Hands.Rightmost.PalmPosition.ToString();
                            data[6] = "R " + frame.Hands.Rightmost.Arm.Direction.ToString();
                        }

                        if (frame.Hands.Count.Equals(1))
                        {
                            if (frame.Hands.Frontmost.IsRight)
                            {
                                data[4] = "Yes"; data[3] = "No";
                                data[1] = frame.Hands.Rightmost.Fingers.Count.ToString();
                                data[5] = "R " + frame.Hands.Rightmost.PalmPosition.ToString();
                                data[6] = "R " + frame.Hands.Rightmost.Arm.Direction.ToString();
                            }
                            else
                            {
                                data[4] = "No"; data[3] = "Yes";
                                data[1] = frame.Hands.Leftmost.Fingers.Count.ToString();
                                data[5] = "L " + frame.Hands.Leftmost.PalmPosition.ToString();
                                data[6] = "L " + frame.Hands.Leftmost.Arm.Direction.ToString();
                            }

                            //Console.WriteLine("Bad Position");
                        }

                        //Console.WriteLine(normalizedCoordinates);
                        if (update.pos)
                        {
                            if (normalizedCoordinates.z > 0)
                                data[8] = "Your " + " Hand is Too Close";

                            else if ((normalizedCoordinates.x < 1 && normalizedCoordinates.y < 1) && (normalizedCoordinates.x > 0.7 
                                || normalizedCoordinates.y > 0.7) && normalizedCoordinates.z == 0)
                            {
                                data[8] = "Your " + "  hand is Too Far";
                            }
                            else if ((normalizedCoordinates.x == 1))
                            {
                                data[8] = "Move Your Hand " + " More To The Left";
                            }
                            else if (normalizedCoordinates.x == 0)
                            {
                                data[8] = "Move Your Hand " + " More To The Right";
                            }
                            else if (normalizedCoordinates.x == normalizedCoordinates.y)
                            {
                                data[8] = "Center Of Hands Focus";
                            }
                            else
                            {
                                data[8] = "Your Hand " + " is Centered";
                            }
                        }
                        else
                        {
                            data[8] = "";
                        }
                    }
                }
                if (update.bpos.IsChecked.Value || update.start.IsChecked.Value || update.tab1.IsSelected || update.tab3.IsSelected)
                    update.UpdateLM();
            }

        }

        public string getFile()
        {
            return file;
        }
        Leap.Vector differentialNormalizer(Leap.Vector leapPoint, InteractionBox iBox, bool isLeft, bool clamp)
        {
            Leap.Vector normalized = iBox.NormalizePoint(leapPoint, false);
            float offset = isLeft ? 0.25f : -0.25f;
            normalized.x += offset;

            //clamp after offsetting
            normalized.x = (clamp && normalized.x < 0) ? 0 : normalized.x;
            normalized.x = (clamp && normalized.x > 1) ? 1 : normalized.x;
            normalized.y = (clamp && normalized.y < 0) ? 0 : normalized.y;
            normalized.y = (clamp && normalized.y > 1) ? 1 : normalized.y;

            return normalized;
        }
        internal void test(Frame frame)
        {

            //Console.WriteLine(frame.InteractionBox.Depth);
            // Get the hand's normal vector and direction
            Leap.Vector normal = frame.Hands.Frontmost.PalmNormal;
            Leap.Vector direction = frame.Hands.Frontmost.Direction;
            Leap.Vector origen = Leap.Vector.Zero;

            float distance = origen.DistanceTo(frame.Hands.Frontmost.PalmPosition); // distance = 10
            //Console.WriteLine(distance); //270 meters is a good range.

            InteractionBox interactionBox = frame.InteractionBox;
            // Leap.Vector point = new Leap.Vector(100, 75, -125);
            Leap.Vector normalizedCoordinates = interactionBox.NormalizePoint(frame.Hands.Frontmost.PalmPosition); //pending
            //Console.WriteLine(normalizedCoordinates);
        }
    }
    public interface ILeapEventDelegate
    { 
        void LeapEventNotification(string EventName);
    }
    public class LeapEventListener : Listener
    {
        ILeapEventDelegate eventDelegate;
        public LeapEventListener(ILeapEventDelegate delegateObject)
        {       this.eventDelegate = delegateObject;
        }
        public override void OnInit(Controller controller)
        {   this.eventDelegate.LeapEventNotification("onInit");
        }
        public override void OnConnect(Controller controller)
        {
            controller.SetPolicy(Controller.PolicyFlag.POLICY_IMAGES);
            controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);
            this.eventDelegate.LeapEventNotification("onConnect");
        }
        public override void OnFrame(Controller controller)
        {
            this.eventDelegate.LeapEventNotification("onFrame");           
        }

        public override void  OnImages (Controller controller)
        {
            this.eventDelegate.LeapEventNotification("onImages");
        }
        public override void OnExit(Controller controller)
        {   this.eventDelegate.LeapEventNotification("onExit");
        }
        public override void OnDisconnect(Controller controller)
        {   this.eventDelegate.LeapEventNotification("onDisconnect");
        } 
        #endregion
    }
}