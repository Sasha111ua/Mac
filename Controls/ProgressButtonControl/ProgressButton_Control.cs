using System;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Drawing;
using MonoMac.CoreAnimation;
using MonoMac.CoreGraphics;
using McViewModel;
using Util.ResourceManagement;
using Util.Linq;
using McViewModel.SpecifiedTypes;

namespace McGui
{
    public enum ProgressButtonState
    {
        Active,
        Scaning,
        Disabled,
        ScanComplete,
        Cleaned
    }

    internal sealed partial class ProgressButton_Control : NSView
    {
        #region Fields

        MainWindow _mainwindow;
        CAShapeLayer _colorAnimationRing;
        ProgressButtonLayer _progressButtonLayer;
        CATextLayer _textLayer;
        CAShapeLayer _progressLayer;
        ProgressButtonState _state;
        //        NSImage _image;
        CABasicAnimation _mainAnimation;
        CGColor _animationColor;
        RectangleF _currentView;
        readonly int _drawProgress = ProgressButtonIntegers.DrawProgress.AsResourceInterger();
        CGImage _buttonNormal;
        CGImage _buttonPushed;
        //        float _val;
        RectangleF _rectangle;

        #endregion

        #region Events

        internal event Action<NSEvent> ButtonClicked;
        internal event Action StateUpdated;

        #endregion

        #region Properties

        ProgressButtonState State
        {
            get{ return _state; }
            set
            {
                _progressButtonLayer.State = value;
                _state = value;
                StateChanged(value, null, null);
            }
        }

        //        NSImage Image
        //        {
        //            get{ return _image; }
        //            set
        //            {
        //                if (value != null)
        //                {
        //                    _progressButtonLayer.CurImage = value.CGImage;
        //                    _image = value;
        //                }
        //            }
        //        }

        //        internal float Val
        //        {
        //            get{ return _val; }
        //            set
        //            {
        //                _progressButtonLayer.CurrentProgress = value;
        //                _val = value;
        //            }
        //        }

        internal RectangleF Rectangle
        {
            get{ return _rectangle; }
            set
            {
                _rectangle = value;
                _progressButtonLayer.Rectangle = value;
                _progressButtonLayer.GlobalCenter = new PointF(_rectangle.Width / 2 + ProgressButtonFloats.GlobalCenterMargins.AsFloat(), _rectangle.Height / 2 + ProgressButtonFloats.GlobalCenterMargins.AsFloat());
            }
        }

        #endregion

        #region Constructors

        public ProgressButton_Control(IntPtr handle) : base(handle)
        {
        }

        //        public ProgressButton_Control() : base()
        //        {
        //        }

        [Export("initWithCoder:")]
        public ProgressButton_Control(NSCoder coder) : base(coder)
        {
        }

        #endregion

        #region UpdateMethods

        internal void StateChanged(ProgressButtonState state, string customtext, CGColor animationColor, bool reDrawOptimized = true)
        {
            if (StateUpdated != null)
                StateUpdated();

            if (State != state)
            {
                _colorAnimationRing.RemoveAllAnimations();

            }
            _progressButtonLayer.CurText = customtext;
            _progressButtonLayer.State = state;
            if (state == ProgressButtonState.Cleaned)
                _colorAnimationRing.AddAnimation(DrawAnimationWithColor(ProgressButtonColors.CleanedStateProgress.AsResourceCgColor()), PARAMs.Animation);
            else if (animationColor != null && state != ProgressButtonState.Disabled && (state != State || !reDrawOptimized))
                _colorAnimationRing.AddAnimation(DrawAnimationWithColor(animationColor), PARAMs.Animation);
            if (state == ProgressButtonState.Active && _animationColor != animationColor)
            {
                _colorAnimationRing.RemoveAllAnimations();
                _colorAnimationRing.AddAnimation(DrawAnimationWithColor(animationColor), PARAMs.Animation);
            }
            _state = state;
            _animationColor = animationColor;

        }

        #endregion

        #region Methods

        CABasicAnimation DrawAnimationWithColor(CGColor animationColor)
        {
            _mainAnimation = CABasicAnimation.FromKeyPath("fillColor");
            _mainAnimation.From = new NSObject(animationColor.Handle);
            _mainAnimation.To = _progressButtonLayer.State == ProgressButtonState.Cleaned ? 
                new NSObject(animationColor.Handle) : 
                new NSObject(new CGColor(animationColor, ProgressButtonFloats.AnimationSecondColorAlfa.AsResourceFloat()).Handle);
            _mainAnimation.Duration = ProgressButtonFloats.AnimationDuration.AsResourceFloat();
            _mainAnimation.AutoReverses = true;
            _mainAnimation.BeginTime = 0;
            _mainAnimation.RepeatCount = 10;
            return _mainAnimation;
        }

        #endregion

        #region MouseClicks

        public override void MouseDown(NSEvent theEvent)
        {
            if (theEvent == null)
                return;
             
            if (_progressButtonLayer.State != ProgressButtonState.Disabled)
            {
                base.MouseDown(theEvent);
                _currentView = Superview.Frame;
                var curRect = new RectangleF(_currentView.X + ProgressButtonFloats.CurRectMargins.AsFloat(), _currentView.Y + ProgressButtonFloats.CurRectMargins.AsFloat(), _currentView.Width - ProgressButtonFloats.CurRectWidthHeight.AsFloat(), _currentView.Height - ProgressButtonFloats.CurRectWidthHeight.AsFloat());
                if (IsMouseInRect(new PointF(theEvent.LocationInWindow.X, theEvent.LocationInWindow.Y), curRect))
                {
                    CATransaction.AnimationDuration = ProgressButtonFloats.DefaultAnimationDuration.AsFloat();
                    _colorAnimationRing.Contents = _buttonPushed;
                    _progressButtonLayer.StateChanged();
                } 
            }
        }

        public override void MouseUp(NSEvent theEvent)
        {
            if (theEvent == null)
                return;

            CATransaction.AnimationDuration = ProgressButtonFloats.DefaultAnimationDuration.AsFloat();
            _colorAnimationRing.Contents = _buttonNormal;
            if (_progressButtonLayer.State != ProgressButtonState.Disabled)
            {
                base.MouseUp(theEvent);

                _currentView = Superview.Frame;
                var curRect = new RectangleF(_currentView.X + ProgressButtonFloats.CurRectMargins.AsFloat(), _currentView.Y + ProgressButtonFloats.CurRectMargins.AsFloat(), _currentView.Width - ProgressButtonFloats.CurRectWidthHeight.AsFloat(), _currentView.Height - ProgressButtonFloats.CurRectWidthHeight.AsFloat());
                if (IsMouseInRect(new PointF(theEvent.LocationInWindow.X, theEvent.LocationInWindow.Y), curRect))
                { 
                    if (ButtonClicked != null)
                    {                     
                        ButtonClicked(theEvent);
                    }
                }
            }
        }

        #endregion

        #region Overrided Methods

        public override void AwakeFromNib()
        {
            _state = ProgressButtonState.Disabled;
            _buttonNormal = ProgressButtonImages.BackgroundImage.AsResourceNsImage().CGImage;
            _buttonPushed = ProgressButtonImages.BackgroundImagePushed.AsResourceNsImage().CGImage;
            _progressButtonLayer = new ProgressButtonLayer(_drawProgress);    
            Rectangle = ProgressButtonRects.ButtonRect.AsRect(); 
            Layer = SetupLayers();
//            StateUpdated += () =>
//            {
//            };
            WantsLayer = true;

            var windows = NSApplication.SharedApplication.Windows;
            if (windows != null)
            {
                var mainwindow = windows.FirstOrDefaultAs<MainWindow>();
                if (mainwindow != null)
                {
                    _mainwindow = mainwindow;
                    _mainwindow.DidMiniaturize += delegate
                    {
                        _colorAnimationRing.RemoveAllAnimations();
                    };
                    _mainwindow.DidDeminiaturize += delegate
                    { 
                        if (_progressButtonLayer != null)
                            StateChanged(_state, _progressButtonLayer.CurText, _animationColor, false);
                    };
                }
            }
            //Event for Enter Key
            _mainwindow.ReturnClicked += obj =>
            {
                if (_progressButtonLayer.State != ProgressButtonState.Disabled)
                {
                    _progressButtonLayer.StateChanged();
                    ButtonClicked(obj);
                }
            };

        }

        #endregion

        #region SetupLayers

        CALayer SetupLayers()
        { 
            _colorAnimationRing = SetupBackgroundLayer();
            _colorAnimationRing.AddSublayer(SetupProgressLayer());
            _colorAnimationRing.AddSublayer(SetupTextLayer());
            _colorAnimationRing.Contents = ProgressButtonImages.BackgroundImage.AsResourceNsImage().CGImage;
            return _colorAnimationRing;
        }

        CAShapeLayer SetupBackgroundLayer()
        {
            var path = new CGPath();
            var angle = (Math.PI / 180) * (ProgressButtonIntegers.StartPosition.AsInt() - ProgressButtonIntegers.Size.AsInt()) * ProgressButtonFloats.CurrentProgress.AsFloat();
            var center = new PointF(Rectangle.Height * 2 - ProgressButtonFloats.CenterMargin.AsFloat(), Rectangle.Height * 2);
            int radius = (int)Rectangle.Height;
            path.MoveToPoint(new PointF(center.X, ProgressButtonFloats.CenterSize.AsFloat() + center.Y));
            path.AddArc(center.X, center.Y, radius, Convert.ToSingle(Math.PI / 2), Convert.ToSingle(angle), true);
            path.AddArc(center.X, center.Y, radius - ProgressButtonFloats.CenterRadius.AsFloat(), Convert.ToSingle(angle), Convert.ToSingle(Math.PI / 2), false);
            // var _backgroundImage = ProgressButtonImages.BackgroundImage.AsResourceNsImage();

            _colorAnimationRing = new CAShapeLayer
            {
                FillColor = ProgressButtonColors.ProgressColorDisableState.AsResourceCgColor(),
                Path = path
            };
         
            return _colorAnimationRing;
        }

        CAShapeLayer SetupProgressLayer()
        {
            _progressLayer = new CAShapeLayer
            {
                ShadowOpacity = ProgressButtonFloats.ProgressLayerShadowOpacity.AsResourceFloat(),
                FillColor = ProgressButtonColors.FillProgressColor.AsResourceCgColor()
            };
            _progressLayer.Bind("path", _progressButtonLayer, "curPath", null);
            var checkMarkLayer = new CALayer();
            checkMarkLayer.Frame = ProgressButtonRects.CheckMarkFrame.AsRect();
            checkMarkLayer.Bind("contents", _progressButtonLayer, "curImage", null);
            _progressLayer.AddSublayer(checkMarkLayer);
            return _progressLayer;
        }

        CATextLayer SetupTextLayer()
        {
            _textLayer = new CATextLayer
            {
                AlignmentMode = CATextLayer.AlignmentCenter,
                ShadowOpacity = ProgressButtonFloats.TextLayerShadowOpacity.AsResourceFloat(),
            };
            _textLayer.Bind("foregroundColor", _progressButtonLayer, "curForegroundColor", null);
            _textLayer.Bind("Frame", _progressButtonLayer, "curFrame", null);
            _textLayer.Bind("string", _progressButtonLayer, "curText", null);
            _textLayer.Bind("FontSize", _progressButtonLayer, "curFontSize", null);

            return _textLayer;
        }

        #endregion
    }

    internal sealed class ProgressButtonLayer : CALayer
    {
        #region Fields

        PointF _globalCenter;
        ProgressButtonState _state;
        float _currentProgress;
        readonly CGColor _buttonColor;
        CGPath _curPath = new CGPath();
        CGPath _currentBlackPath = new CGPath();
        string _currentTextOnButton = String.Empty;
        CGColor _currentColorOnProgress = ProgressButtonColors.ButtonColor.AsCgColor();
        CGColor _currentButtonColor = ProgressButtonColors.CurrentButtonColor.AsCgColor();
        float _currentFontSize = 5;
        RectangleF _currentFrame = ProgressButtonRects.CurrentFrame.AsRect();
        CGImage _currentImage = new CGImage(new IntPtr());
        CGColor _currentForegroundColor;
        int _drawProgress = 1;

        #endregion

        #region Ctors

        public ProgressButtonLayer(int drawProgress) : base()
        {
            _drawProgress = drawProgress;
            StateChanged();
            _state = ProgressButtonState.Active;
            _buttonColor = ProgressButtonColors.ButtonColor.AsCgColor();
        }

        public ProgressButtonLayer(IntPtr handle) : base(handle)
        {
            throw new CannotInvokeConstructorException(handle, "ProgressButtonLayer");
        }

        [Export("initWithCoder:")]
        public ProgressButtonLayer(NSCoder coder) : base(coder)
        {
            throw new CannotInvokeConstructorException(coder, "ProgressButtonLayer");
        }

        #endregion

        #region Properties

        internal PointF GlobalCenter
        {
            get{ return _globalCenter; }
            set{ _globalCenter = value; }
        }

        internal ProgressButtonState State
        {
            get { return _state; }
            set
            {
                _state = value;
                StateChanged();
            }
        }

        internal RectangleF Rectangle
        {
            get;
            set;
        }

        internal float CurrentProgress
        {
            get{ return _currentProgress; }
            set
            {
                _currentProgress = value;
                StateChanged();
            }
        }

        CGPath ProgressDrawing(float currentProgress)
        {
            return currentProgress > 0 ? CircleDrawing(currentProgress) : new CGPath();
        }

        CGPath ProgressBlackDrawing()
        {
            return CircleDrawing(200f);
        }

        CGPath CircleDrawing(float currentProgress)
        {
            var path = new CGPath();
            var angle = (Math.PI / 180) * (ProgressButtonIntegers.StartPosition.AsInt() - (int)currentProgress) * ProgressButtonFloats.CurrentProgress.AsFloat();
            var center = new  PointF(Rectangle.Height * 2 - ProgressButtonFloats.CenterPointX.AsFloat(), Rectangle.Height * 2);
            int radius = ProgressButtonIntegers.Radius.AsInt();
            path.MoveToPoint(new PointF(center.X, radius + center.Y));
            path.AddArc(center.X, center.Y, radius, Convert.ToSingle(Math.PI / 2), Convert.ToSingle(angle), true);
            path.AddArc(center.X, center.Y, radius - ProgressButtonFloats.CenterRadius.AsFloat(), Convert.ToSingle(angle), Convert.ToSingle(Math.PI / 2), false);
            return path;
        }

        #endregion

        #region Update Methods

        internal void StateChanged()
        {
            SetTextFrameAndSizeStandart(ProgressButtonFonts.TextOnButton.AsResourceNsFont());
            switch (_state)
            {
                case ProgressButtonState.Active:
                    CurImage = new CGImage(new IntPtr());
                    ActiveStateDrawing();
                    break;
                case ProgressButtonState.Scaning:
                    if (_drawProgress != 0)
                    {
                        ScaningStateDrawing();
                    }
                    break;
                case ProgressButtonState.Disabled:
                    CurImage = new CGImage(new IntPtr());
                    DisableStateDrawing();
                    break;
                case ProgressButtonState.ScanComplete:
                    CurImage = new CGImage(new IntPtr());
                    ScanCompleteStateDrawing();
                    break;
                case ProgressButtonState.Cleaned:
                    CleanedStateDrawing();
                    break;
                default:
                    CurPath = new CGPath();
                    CurBlackPath = new CGPath();
                    break;
            }
            CurColor = _buttonColor;
        }

        #endregion

        #region Drawing

        void ActiveStateDrawing()
        {
            CurPath = new CGPath();
            CurBlackPath = new CGPath();
        }

        void ScaningStateDrawing()
        {
            CurPath = ProgressDrawing(_currentProgress);
            CurBlackPath = ProgressBlackDrawing();
        }

        void DisableStateDrawing()
        {
            CurPath = ProgressDrawing(0);
            CurBlackPath = ProgressBlackDrawing();
            CurForegroundColor = ProgressButtonColors.TextColorInDisableState.AsResourceCgColor();
        }

        void ScanCompleteStateDrawing()
        {
            CurBlackPath = new CGPath();
            CurPath = new CGPath();
        }

        void CleanedStateDrawing()
        {
            CurText = String.Empty;
            CurPath = new CGPath();
            CurImage = ProgressButtonImages.ImageOnCleanedState.AsResourceNsImage().CGImage;
        }

        void SetTextFrameAndSizeStandart(NSFont font)
        {
            CurForegroundColor = ProgressButtonColors.TextColorNormal.AsResourceCgColor();
            var attrs = new NSMutableDictionary
            {
                { 
                    NSAttributedString.FontAttributeName,
                    NSFont.FromFontName(font.FontName, font.PointSize)
                },
            };

            var applicationName = new NSAttributedString(CurText, attrs);
            var textSize = applicationName.Size;
            CurFontSize = font.PointSize;
            CATransaction.AnimationDuration = ProgressButtonFloats.DefaultAnimationDuration.AsFloat();
            CurFrame = new RectangleF(ProgressButtonFloats.TextFrameX.AsFloat() + (ProgressButtonFloats.TextFrameMargins.AsFloat() - textSize.Width) / 2, ProgressButtonFloats.TextFrameY.AsFloat() + (ProgressButtonFloats.TextFrameMargins.AsFloat() - textSize.Height) / 2, textSize.Width, textSize.Height);
        }

        #endregion

        #region Bind properties

        [Export("curPath")]
        internal CGPath CurPath
        {
            get
            {
                return _curPath; 
            }

            set
            {
                WillChangeValue("curPath");
                _curPath = value;
                DidChangeValue("curPath");
            }
        }

        [Export("curblackPath")]
        internal CGPath CurBlackPath
        {
            get
            {
                return _currentBlackPath; 
            }

            set
            {
                WillChangeValue("curblackPath");
                _currentBlackPath = value;
                DidChangeValue("curblackPath");
            }
        }

        [Export("curText")]
        internal string CurText
        {
            get
            {
                return _currentTextOnButton; 
            }

            set
            {
                WillChangeValue("curText");
                _currentTextOnButton = value;
                DidChangeValue("curText");
            }
        }

        [Export("curColor")]
        internal CGColor CurColor
        {
            get
            {
                return _currentColorOnProgress; 
            }

            set
            {
                WillChangeValue("curColor");
                _currentColorOnProgress = value;
                DidChangeValue("curColor");
            }
        }

        [Export("curFillColor")]
        internal CGColor CurFillColor
        {
            get
            {
                return _currentButtonColor;
            }
        
            set
            {
                WillChangeValue("curFillColor");
                _currentButtonColor = value;
                DidChangeValue("curFillColor");
            }
        }

        [Export("curFontSize")]
        internal float CurFontSize
        {
            get
            {
                return _currentFontSize; 
            }

            set
            {
                WillChangeValue("curFontSize");
                _currentFontSize = value;
                DidChangeValue("curFontSize");
            }
        }

        [Export("curFrame")]
        internal RectangleF CurFrame
        {
            get
            {
                return _currentFrame; 
            }

            set
            {
                WillChangeValue("curFrame");
                _currentFrame = value;
                DidChangeValue("curFrame");
            }
        }

        [Export("curImage")]
        internal CGImage CurImage
        {
            get
            {
                return _currentImage; 
            }

            set
            {
                WillChangeValue("curImage");
                _currentImage = value;
                DidChangeValue("curImage");
            }
        }

        [Export("curForegroundColor")]
        internal CGColor CurForegroundColor
        {
            get
            {
                return _currentForegroundColor; 
            }

            set
            {
                WillChangeValue("curForegroundColor");
                _currentForegroundColor = value;
                DidChangeValue("curForegroundColor");
            }
        }

        #endregion
    }
}

