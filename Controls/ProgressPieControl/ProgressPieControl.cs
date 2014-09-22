using System;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.CoreAnimation;
using System.Drawing;
using MonoMac.CoreGraphics;
using Util.ResourceManagement;
using McViewModel;
using McViewModel.SpecifiedTypes;

namespace McGui
{
    internal sealed partial class ProgressPieControl : UView
    {
        #region Fields

        CAShapeLayer _progressLayer;
        CAShapeLayer _backgroundLayer;
        ProgressPieControlLayer _pieTimerLayer;
        CATextLayer _textLayer;
        readonly int _isShowsText = ProgressPieIntegers.IsShowsText.AsResourceInterger();
        double _state;
        const double DRAWED_STATE = 0;
        CGPath _lastCurrentProgress;
        bool _isAnimationFinished;
        bool _drawWasCalled;

        #endregion

        #region Properties

        public double State
        {
            get{ return DRAWED_STATE; }
            set
            { 
                _state = value;
                if ((_pieTimerLayer.State < _state && _progressLayer.AnimationKeys == null))
                {
                    _pieTimerLayer.State = _state;
                    ProgressAnimationDrawing();

                    var backgroundProgressLayer = CreateLayer();
                    if (_state == 100)
                    {
                        _pieTimerLayer.LastCurrentProgress = Convert.ToSingle((Math.PI / 180) * (ProgressPieIntegers.StartPoint.AsInt() - (int)0) * ProgressPieFloats.CurProgress.AsFloat());
                        backgroundProgressLayer.Path = _pieTimerLayer.ProgressDrawing(100);
                    }
                    if (_lastCurrentProgress != null)
                    {
                        backgroundProgressLayer.Path = _lastCurrentProgress;
                    }
                    _lastCurrentProgress = _progressLayer.Path;
                    _progressLayer.AddSublayer(backgroundProgressLayer);

                    _drawWasCalled |= _state == 100; 
                }
            }
        }

        public bool IsAnimationFinished
        {
            get{ return _isAnimationFinished; } 
        }

        #endregion

        #region Constructors

        public ProgressPieControl(IntPtr handle) : base(handle)
        {
        }

        [Export("initWithCoder:")]
        public ProgressPieControl(NSCoder coder) : base(coder)
        {
        }

        //        public ProgressPieControl()
        //        {
        //        }

        #endregion

        #region Overrided members

        public override void AwakeFromNib()
        {
            _pieTimerLayer = new ProgressPieControlLayer();
            Layer = SetupLayers();
            _progressLayer.Bind("path", _pieTimerLayer, "currentPath", null);
            _textLayer.Bind("string", _pieTimerLayer, "currentString", null);
            State = 0;
            WantsLayer = true;
        }

        #endregion

        #region Setup Layers

        CAShapeLayer CreateLayer()
        {
            var layer = new CAShapeLayer();
            layer.StrokeColor = ProgressPieColors.ProgressLayerFillColor.AsResourceCgColor();
            layer.LineWidth = ProgressPieFloats.Width.AsResourceFloat();
            layer.FillColor = ProgressPieColors.Clear.AsResourceCgColor();

            return  layer;
        }

        void ProgressAnimationDrawing()
        {
            var progressAnimation = CABasicAnimation.FromKeyPath("strokeEnd");
            progressAnimation.From = new NSObject(NSNumber.FromFloat(0).Handle);
            progressAnimation.To = new NSObject(NSNumber.FromFloat(1).Handle);

            progressAnimation.Duration = ProgressPieFloats.AnimationDuration.AsResourceFloat(); 

            progressAnimation.AnimationStopped += delegate
            {
                if (!_isAnimationFinished)
                { 
                    if (_state == 100 && _drawWasCalled)
                    {
                        State = _state;
                        _isAnimationFinished = true;
                    }
                }
            };
            progressAnimation.RemovedOnCompletion = true;
            _progressLayer.AddAnimation(progressAnimation, "progressAnimation"); 
        }

        CALayer SetupLayers()
        {
            _progressLayer = SetupProgressLayer();
            if (_isShowsText == 1)
                _progressLayer.AddSublayer(SetupTextLayer());
            _backgroundLayer = SetupBackgroundLayer();
            _backgroundLayer.AddSublayer(_progressLayer);
            return _backgroundLayer;
        }

        CAShapeLayer SetupBackgroundLayer()
        {
            _backgroundLayer = new CAShapeLayer
            {
                FillColor = ProgressPieColors.Clear.AsResourceCgColor(),
                StrokeColor = ProgressPieColors.BackgroundLayerFillColor.AsResourceCgColor(),
                LineWidth = ProgressPieFloats.Width.AsResourceFloat(),
                Path = _pieTimerLayer.ProgressDrawing(100, true)
            };
            return _backgroundLayer;
        }

        CAShapeLayer SetupProgressLayer()
        {
            _progressLayer = new CAShapeLayer
            {
                FillColor = ProgressPieColors.Clear.AsResourceCgColor(),
                StrokeColor = ProgressPieColors.ProgressLayerFillColor.AsResourceCgColor(),
                LineWidth = ProgressPieFloats.Width.AsResourceFloat()
            };
            return _progressLayer;

        }

        CATextLayer SetupTextLayer()
        {
            _textLayer = new CATextLayer
            {
                ForegroundColor = ProgressPieColors.TextLayerForegroundColor.AsResourceCgColor(),
                AlignmentMode = CATextLayer.AlignmentCenter,
            };

            _textLayer.SetFont(ProgressPieFonts.TextFont.AsResourceNsFont());

            var attrs = new NSMutableDictionary
            {
                { 
                    NSAttributedString.FontAttributeName,
                    NSFont.FromFontName(ProgressPieFonts.TextFont.AsResourceNsFont().FontName, ProgressPieFonts.TextFont.AsResourceNsFont().PointSize)
                },
            };

            var applicationName = new NSAttributedString("100%", attrs);
            var textSize = applicationName.Size;
            _textLayer.Frame = new RectangleF(ProgressPieFloats.Radius.AsResourceFloat() - textSize.Width / 2 + ProgressPieFloats.TextLayerMargin.AsFloat(), ProgressPieFloats.Radius.AsResourceFloat() - textSize.Height / 2, textSize.Width, textSize.Height);
            return _textLayer;
        }

        #endregion
    }

    internal sealed class ProgressPieControlLayer : CALayer
    {
        #region Fields

        CGPath _currentPath = new CGPath();
        string _curValue = String.Empty;
        double _state;
        readonly float _radius = ProgressPieFloats.Radius.AsResourceFloat();
        float _lastCurrentProgress = Convert.ToSingle((Math.PI / 180) * (ProgressPieIntegers.StartPoint.AsInt() - 0) * ProgressPieFloats.CurProgress.AsFloat());

        #endregion

        #region Constructors

        public ProgressPieControlLayer() : base()
        {
            State = 0; 
        }

        public ProgressPieControlLayer(IntPtr handler) : base(handler)
        {
            throw new CannotInvokeConstructorException(handler, "ProgressPieControlLayer");
        }

        [Export("initWithCoder:")]
        public ProgressPieControlLayer(NSCoder coder) : base(coder)
        {
            throw new CannotInvokeConstructorException(coder, "ProgressPieControlLayer");
        }

        #endregion

        #region Properties

        public float LastCurrentProgress
        {
            get{ return _lastCurrentProgress; }
            set{ _lastCurrentProgress = value; }
        }

        public double State
        {
            get{ return _state; }
            set
            {
                _state = value;
                CurrentPath = ProgressDrawing((float)_state);
            }
        }

        #endregion

        #region Drawing

        public CGPath ProgressDrawing(float currentProgress, bool isBackground = false)
        {
            if (!isBackground)
            {
                CurrentString = currentProgress <= 100 ? string.Format("{0:N0} %", currentProgress) : "100";
            }
            if (currentProgress > 99.1)
                currentProgress = 100;
            var p = new CGPath();
            var angle = (Math.PI / 180) * (ProgressPieIntegers.StartPoint.AsInt() - (int)currentProgress) * ProgressPieFloats.CurProgress.AsFloat();
            var center = new PointF(_radius + ProgressPieFloats.RadiusSize.AsFloat(), _radius + ProgressPieFloats.RadiusSize.AsFloat());
            int radius = (int)_radius;
            var delta = currentProgress > 0 ? 0.1f : 0;
            p.AddArc(center.X, center.Y, radius - ProgressPieFloats.Width.AsResourceFloat() / 4, _lastCurrentProgress, Convert.ToSingle(angle), true);
            _lastCurrentProgress = Convert.ToSingle((Math.PI / 180) * (ProgressPieIntegers.StartPoint.AsInt() - (int)currentProgress + delta) * ProgressPieFloats.CurProgress.AsFloat());
            return p;

        }

        #endregion

        #region Bind properties

        [Export("currentPath")]
        public CGPath CurrentPath
        {
            get
            {
                return _currentPath; 
            }

            set
            {
                WillChangeValue("currentPath");
                _currentPath = value;
                DidChangeValue("currentPath");
            }
        }

        [Export("currentString")]
        public string CurrentString
        {
            get
            {
                return _curValue; 
            }

            set
            {
                WillChangeValue("currentString");
                _curValue = value;
                DidChangeValue("currentString");
            }
        }

        #endregion
    }
}

